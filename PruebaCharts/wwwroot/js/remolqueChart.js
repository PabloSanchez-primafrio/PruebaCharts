let chart = null;
let baseSvgText = null;
let lastBoxCount = 0;
let currentMapName = 'RemolqueDynamic';

// Opciones por defecto
const defaults = {
    padding: 0,        // sin margen interno (las cargas ocupan todo el remolque)
    textColor: '#1b1b1b',
    showLabels: true,  // pintar texto (id/código) dentro de cada caja
    labelKey: 'label', // propiedad a usar como texto dentro de cada caja
    valueKey: 'value'  // propiedad a usar para el tamaño y color
};

// Pequeño helper para serializar DOM -> string
function serializeXml(doc) {
    return new XMLSerializer().serializeToString(doc);
}

// Construye un SVG nuevo insertando N cajas horizontales con ancho proporcional a su value
function buildSvgWithCargoBoxes(items, options) {
    const cfg = { ...defaults, ...options };
    const parser = new DOMParser();
    const svgDoc = parser.parseFromString(baseSvgText, 'image/svg+xml');

    // Tomamos el rectángulo principal del remolque
    const box = svgDoc.getElementById('RemolqueBox');
    if (!box) return baseSvgText;

    // Usar las dimensiones completas del RemolqueBox
    const x = parseFloat(box.getAttribute('x'));
    const y = parseFloat(box.getAttribute('y'));
    const w = parseFloat(box.getAttribute('width'));
    const h = parseFloat(box.getAttribute('height'));

    const N = items.length;
    if (N === 0) return baseSvgText;

    // Ordenar items de mayor a menor según su value
    const sortedItems = [...items].sort((a, b) => {
        const valueA = Number(a[cfg.valueKey] ?? 0);
        const valueB = Number(b[cfg.valueKey] ?? 0);
        return valueB - valueA; // Mayor a menor
    });

    // Calcular la suma total de values para distribuir proporcionalmente
    const totalValue = sortedItems.reduce((sum, item) =>
        sum + Math.max(0, Number(item[cfg.valueKey] ?? 0)), 0);

    // Si la suma total es 0, distribuir equitativamente
    const useEqualWidth = totalValue === 0;

    // Capa contenedora de cargas
    const cargoLayer = svgDoc.createElementNS('http://www.w3.org/2000/svg', 'g');
    cargoLayer.setAttribute('id', 'CargoLayer');

    let currentX = x;

    for (let i = 0; i < N; i++) {
        const itemValue = Math.max(0, Number(sortedItems[i][cfg.valueKey] ?? 0));

        // Calcular ancho proporcional al value
        let cellW;
        if (i === N - 1) {
            // La última carga ocupa todo el espacio restante (evita errores de redondeo)
            cellW = x + w - currentX;
        } else {
            cellW = useEqualWidth
                ? w / N
                : (itemValue / totalValue) * w;
        }

        const cellH = h;

        // Usar el label real del item como nombre
        const name = (sortedItems[i][cfg.labelKey] ?? `Carga${i + 1}`).toString();

        // Grupo por caja con id/name para ECharts
        const g = svgDoc.createElementNS('http://www.w3.org/2000/svg', 'g');
        g.setAttribute('id', name);
        g.setAttribute('name', name);

        // Rectángulo de la caja
        const rect = svgDoc.createElementNS('http://www.w3.org/2000/svg', 'rect');
        rect.setAttribute('x', String(currentX));
        rect.setAttribute('y', String(y));
        rect.setAttribute('width', String(cellW));
        rect.setAttribute('height', String(cellH));
        rect.setAttribute('rx', '0'); // sin bordes redondeados para mejor ajuste
        rect.setAttribute('fill', '#ffffff');          // color base (lo sobreescribe visualMap)
        rect.setAttribute('stroke', '#666');
        rect.setAttribute('stroke-width', '1');

        g.appendChild(rect);

        // Texto central (label)
        if (cfg.showLabels && cellW > 20) { // solo mostrar si hay espacio suficiente
            const label = (sortedItems[i][cfg.labelKey] ?? name).toString();
            const text = svgDoc.createElementNS('http://www.w3.org/2000/svg', 'text');
            text.setAttribute('x', String(currentX + cellW / 2));
            text.setAttribute('y', String(y + cellH / 2 + 5));
            text.setAttribute('text-anchor', 'middle');
            text.setAttribute('font-size', Math.min(14, cellW / 3).toString());
            text.setAttribute('fill', cfg.textColor);
            text.textContent = label;
            g.appendChild(text);
        }

        cargoLayer.appendChild(g);

        // Avanzar la posición X para la siguiente caja
        currentX += cellW;
    }

    // Insertamos el cargoLayer después del grupo Remolque (para que quede “encima”)
    const remolqueGroup = svgDoc.getElementById('Remolque');
    if (remolqueGroup && remolqueGroup.parentNode) {
        remolqueGroup.parentNode.insertBefore(cargoLayer, remolqueGroup.nextSibling);
    } else {
        // fallback: lo metemos al final
        svgDoc.documentElement.appendChild(cargoLayer);
    }

    return serializeXml(svgDoc);
}

// Inicialización del gráfico (sin cajas aún)
export async function initRemolque(divId, options = {}) {
    const el = document.getElementById(divId);
    if (!el || typeof echarts === 'undefined') return;

    // Cargamos el SVG base (sin cajas)
    const res = await fetch('/assets/remolque.svg');
    baseSvgText = await res.text();

    // De inicio, registramos el mapa con el SVG base
    echarts.registerMap(currentMapName, { svg: baseSvgText });

    chart = echarts.init(el);

    const option = {
        tooltip: {
            trigger: 'item',
            formatter: (p) =>
                p.value != null ? `<b>${p.value}</b>` : ''
        },
        visualMap: {
            left: 'center',
            bottom: 10,
            min: 0,
            max: 100,
            calculable: true,
            orient: 'horizontal',
            inRange: {
                // Gradiente Primafrio (amarillo → azul)
                color: ['#FFE000', '#FFC300', '#06038D']
            }
        },
        series: [{
            type: 'map',
            map: currentMapName,
            roam: false,
            selectedMode: false,
            label: { show: false },
            emphasis: { label: { show: false } },
            select: { label: { show: false } },
            blur: { label: { show: false } },
            data: [] // lo rellenaremos en update
        }]
    };

    chart.setOption(option);
    window.addEventListener('resize', () => chart.resize());
}

// Actualización dinámica de cargas (crea/borra/redistribuye cajas según items.length)
export function updateRemolque(items, options = {}) {
    if (!chart || !baseSvgText) return;

    const N = items?.length ?? 0;

    // 1) Si ha cambiado el número de cajas, reconstruimos el SVG y re‑registramos el mapa
    if (N !== lastBoxCount) {
        const svgWithBoxes = buildSvgWithCargoBoxes(items, options);
        currentMapName = 'RemolqueDynamic_' + N; // nuevo nombre para forzar refresco
        echarts.registerMap(currentMapName, { svg: svgWithBoxes });

        // 2) Reasignamos el mapa y vaciamos data (se vuelve a cargar abajo)
        chart.setOption({
            series: [{
                type: 'map',
                map: currentMapName,
                roam: false,
                selectedMode: true,
                label: { show: false },
                emphasis: { label: { show: false } },
                select: { label: { show: false } },
                blur: { label: { show: false } },
                data: []
            }]
        }, { replaceMerge: ['series'] });
        lastBoxCount = N;
    }

    // 3) Montamos los datos para ECharts (Remolque + Carga1..N)
    const data = [];
    // Si quieres un KPI global del remolque, añade este registro:
    // data.push({ name: 'Remolque', value: calcularKpiGlobal(items) });

    // Ordenar items de mayor a menor (igual que en buildSvgWithCargoBoxes)
    const valueKey = options.valueKey ?? 'value';
    const labelKey = options.labelKey ?? 'label';
    const sortedItems = [...items].sort((a, b) => {
        const valueA = Number(a[valueKey] ?? 0);
        const valueB = Number(b[valueKey] ?? 0);
        return valueB - valueA; // Mayor a menor
    });

    for (let i = 0; i < N; i++) {
        const name = (sortedItems[i][labelKey] ?? `Carga${i + 1}`).toString();
        data.push({
            name,
            value: Number(sortedItems[i][valueKey] ?? 0)
        });
    }

    // 4) Aplicamos los datos (colores por visualMap)
    chart.setOption({
        series: [{ data }]
    });
}
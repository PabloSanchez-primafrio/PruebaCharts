let chart = null;
let baseSvgText = null;
let lastBoxCount = 0;
let currentMapName = 'RemolqueDynamic';

// Opciones por defecto
const defaults = {
    padding: 10,       // margen interno dentro de la caja del remolque
    maxCols: null,     // si lo dejas en null, se calcula con sqrt(n)
    textColor: '#1b1b1b',
    showLabels: true,  // pintar texto (id/código) dentro de cada caja
    labelKey: 'label', // propiedad a usar como texto dentro de cada caja
    valueKey: 'value'  // propiedad a usar para el color (0..100)
};

// Pequeño helper para serializar DOM -> string
function serializeXml(doc) {
    return new XMLSerializer().serializeToString(doc);
}

// Construye un SVG nuevo (a partir del base) insertando N cajas: Carga1..CargaN
function buildSvgWithCargoBoxes(items, options) {
    const cfg = { ...defaults, ...options };
    const parser = new DOMParser();
    const svgDoc = parser.parseFromString(baseSvgText, 'image/svg+xml');

    // Tomamos el rectángulo principal del remolque
    const box = svgDoc.getElementById('RemolqueBox');
    if (!box) return baseSvgText;

    const x = parseFloat(box.getAttribute('x'));
    const y = parseFloat(box.getAttribute('y'));
    const w = parseFloat(box.getAttribute('width'));
    const h = parseFloat(box.getAttribute('height'));

    const pad = cfg.padding;
    const innerX = x + pad;
    const innerY = y + pad;
    const innerW = w - 2 * pad;
    const innerH = h - 2 * pad;

    const N = items.length;

    // Cálculo de rejilla (cols/rows) automático
    let cols = cfg.maxCols ?? Math.ceil(Math.sqrt(N));
    cols = Math.min(cols, N === 0 ? 1 : N);
    const rows = Math.ceil(N / cols);

    const cellW = innerW / cols;
    const cellH = innerH / rows;

    // Capa contenedora de cargas (opcional, por organización)
    const cargoLayer = svgDoc.createElementNS('http://www.w3.org/2000/svg', 'g');
    cargoLayer.setAttribute('id', 'CargoLayer');

    for (let i = 0; i < N; i++) {
        const col = i % cols;
        const row = Math.floor(i / cols);

        const cx = innerX + col * cellW;
        const cy = innerY + row * cellH;

        // Grupo por caja con id/name para ECharts
        const g = svgDoc.createElementNS('http://www.w3.org/2000/svg', 'g');
        const name = `Carga${i + 1}`;
        g.setAttribute('id', name);
        g.setAttribute('name', name);

        // Rectángulo de la caja
        const rect = svgDoc.createElementNS('http://www.w3.org/2000/svg', 'rect');
        rect.setAttribute('x', String(cx + 2));
        rect.setAttribute('y', String(cy + 2));
        rect.setAttribute('width', String(cellW - 4));
        rect.setAttribute('height', String(cellH - 4));
        rect.setAttribute('rx', '6');
        rect.setAttribute('fill', '#ffffff');          // color base (lo sobreescribe visualMap)
        rect.setAttribute('stroke', '#666');
        rect.setAttribute('stroke-width', '1');

        g.appendChild(rect);

        // Texto central (label)
        if (cfg.showLabels) {
            const label = (items[i][cfg.labelKey] ?? name).toString();
            const text = svgDoc.createElementNS('http://www.w3.org/2000/svg', 'text');
            text.setAttribute('x', String(cx + cellW / 2));
            text.setAttribute('y', String(cy + cellH / 2 + 5)); // +5 para centrar ópticamente
            text.setAttribute('text-anchor', 'middle');
            text.setAttribute('font-size', Math.max(12, Math.min(cellW, cellH) / 5).toString());
            text.setAttribute('fill', cfg.textColor);
            text.textContent = label;
            g.appendChild(text);
        }

        cargoLayer.appendChild(g);
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
                p.value!=null ? `<b>${p.value}</b>` : ''
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
        }, { replaceMerge: ['series']});
        lastBoxCount = N;
    }

    // 3) Montamos los datos para ECharts (Remolque + Carga1..N)
    const data = [];
    // Si quieres un KPI global del remolque, añade este registro:
    // data.push({ name: 'Remolque', value: calcularKpiGlobal(items) });

    for (let i = 0; i < N; i++) {
        const name = `Carga${i + 1}`;
        data.push({
            name,
            value: Number(items[i][options.valueKey ?? 'value'] ?? 0)
        });
    }

    // 4) Aplicamos los datos (colores por visualMap)
    chart.setOption({
        series: [{ data }]
    });
}
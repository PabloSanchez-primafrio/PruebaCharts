let chart = null;
let baseSvgText = null;
let lastBoxCount = 0;
let currentMapName = 'RemolqueDynamic';

const defaults = {
    padding: 0,
    textColor: '#1b1b1b',
    showLabels: true,
    labelKey: 'label',
    valueKey: 'value'
};

function serializeXml(doc) {
    return new XMLSerializer().serializeToString(doc);
}

function buildSvgWithCargoBoxes(items, options) {
    const cfg = { ...defaults, ...options };
    const parser = new DOMParser();
    const svgDoc = parser.parseFromString(baseSvgText, 'image/svg+xml');

    const box = svgDoc.getElementById('RemolqueBox');
    if (!box) return baseSvgText;

    const x = parseFloat(box.getAttribute('x'));
    const y = parseFloat(box.getAttribute('y'));
    const w = parseFloat(box.getAttribute('width'));
    const h = parseFloat(box.getAttribute('height'));

    const N = items.length;
    if (N === 0) return baseSvgText;

    const sortedItems = [...items].sort((a, b) => {
        const valueA = Number(a[cfg.valueKey] ?? 0);
        const valueB = Number(b[cfg.valueKey] ?? 0);
        return valueB - valueA;
    });

    const totalValue = sortedItems.reduce((sum, item) =>
        sum + Math.max(0, Number(item[cfg.valueKey] ?? 0)), 0);

    const useEqualWidth = totalValue === 0;

    const cargoLayer = svgDoc.createElementNS('http://www.w3.org/2000/svg', 'g');
    cargoLayer.setAttribute('id', 'CargoLayer');

    let currentX = x;

    for (let i = 0; i < N; i++) {
        const itemValue = Math.max(0, Number(sortedItems[i][cfg.valueKey] ?? 0));

        let cellW;
        if (i === N - 1) {
            cellW = x + w - currentX;
        } else {
            cellW = useEqualWidth
                ? w / N
                : (itemValue / totalValue) * w;
        }

        const cellH = h;

        const name = (sortedItems[i][cfg.labelKey] ?? `Carga${i + 1}`).toString();

        const g = svgDoc.createElementNS('http://www.w3.org/2000/svg', 'g');
        g.setAttribute('id', name);
        g.setAttribute('name', name);

        const rect = svgDoc.createElementNS('http://www.w3.org/2000/svg', 'rect');
        rect.setAttribute('x', String(currentX));
        rect.setAttribute('y', String(y));
        rect.setAttribute('width', String(cellW));
        rect.setAttribute('height', String(cellH));
        rect.setAttribute('rx', '0');
        rect.setAttribute('fill', '#ffffff');
        rect.setAttribute('stroke', '#666');
        rect.setAttribute('stroke-width', '1');

        g.appendChild(rect);

        if (cfg.showLabels && cellW > 20) {
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

        currentX += cellW;
    }

    const remolqueGroup = svgDoc.getElementById('Remolque');
    if (remolqueGroup && remolqueGroup.parentNode) {
        remolqueGroup.parentNode.insertBefore(cargoLayer, remolqueGroup.nextSibling);
    } else {
        svgDoc.documentElement.appendChild(cargoLayer);
    }

    return serializeXml(svgDoc);
}

export async function initRemolque(divId, options = {}) {
    const el = document.getElementById(divId);
    if (!el || typeof echarts === 'undefined') return;

    const res = await fetch('/assets/remolque.svg');
    baseSvgText = await res.text();

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
            data: []
        }]
    };

    chart.setOption(option);
    window.addEventListener('resize', () => chart.resize());
}

export function updateRemolque(items, options = {}) {
    if (!chart || !baseSvgText) return;

    const N = items?.length ?? 0;

    if (N !== lastBoxCount) {
        const svgWithBoxes = buildSvgWithCargoBoxes(items, options);
        currentMapName = 'RemolqueDynamic_' + N;
        echarts.registerMap(currentMapName, { svg: svgWithBoxes });

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

    const data = [];

    const valueKey = options.valueKey ?? 'value';
    const labelKey = options.labelKey ?? 'label';
    const sortedItems = [...items].sort((a, b) => {
        const valueA = Number(a[valueKey] ?? 0);
        const valueB = Number(b[valueKey] ?? 0);
        return valueB - valueA;
    });

    for (let i = 0; i < N; i++) {
        const name = (sortedItems[i][labelKey] ?? `Carga${i + 1}`).toString();
        data.push({
            name,
            value: Number(sortedItems[i][valueKey] ?? 0)
        });
    }

    chart.setOption({
        series: [{ data }]
    });
}
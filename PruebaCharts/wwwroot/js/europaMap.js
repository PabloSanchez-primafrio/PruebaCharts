let europaChart = null;

export async function initEuropaMap(divId, paisesCarga, paisesDescarga) {
    const el = document.getElementById(divId);
    if (!el || typeof echarts === 'undefined') return;

    if (europaChart) {
        europaChart.dispose();
        europaChart = null;
    }

    const res = await fetch('https://raw.githubusercontent.com/leakyMirror/map-of-europe/master/GeoJSON/europe.geojson');
    const geoJson = await res.json();

    echarts.registerMap('Europa', geoJson);

    europaChart = echarts.init(el);

    const coordenadas = {
        'Spain': [-3.7, 40.4],
        'France': [2.3, 46.2],
        'Germany': [10.4, 51.1],
        'Italy': [12.5, 41.9],
        'Portugal': [-8.2, 39.4],
        'Netherlands': [5.3, 52.1],
        'Belgium': [4.5, 50.5],
        'Poland': [19.1, 52.2],
        'Switzerland': [8.2, 46.8],
        'Austria': [14.5, 47.5],
        'Czech Republic': [15.5, 49.8],
        'Hungary': [19.5, 47.2],
        'Romania': [25.0, 45.9],
        'Sweden': [18.6, 60.1],
        'Norway': [10.7, 59.9],
        'Denmark': [10.0, 56.3],
        'Finland': [25.7, 61.9],
        'Greece': [21.8, 39.1],
        'United Kingdom': [-1.5, 52.5],
        'Ireland': [-8.2, 53.4],
        'Slovakia': [19.7, 48.7],
        'Croatia': [15.2, 45.1],
        'Serbia': [21.0, 44.0],
        'Bulgaria': [25.5, 42.7],
        'Lithuania': [23.9, 55.2],
        'Latvia': [24.6, 56.9],
        'Estonia': [25.0, 58.6],
        'Slovenia': [14.8, 46.1],
        'Luxembourg': [6.1, 49.8],
        'Turkey': [35.2, 38.9],
        'Ukraine': [31.2, 48.4],
        'Belarus': [27.9, 53.7],
        'Russia': [37.6, 55.8],
    };

    const nombreAIngles = {
        'ESPAÑA': 'Spain',
        'FRANCIA': 'France',
        'ALEMANIA': 'Germany',
        'ITALIA': 'Italy',
        'PORTUGAL': 'Portugal',
        'PAÍSES BAJOS': 'Netherlands',
        'BELGICA': 'Belgium',
        'POLONIA': 'Poland',
        'SUIZA': 'Switzerland',
        'AUSTRIA': 'Austria',
        'REPUBLICA CHECA': 'Czech Republic',
        'HUNGRIA': 'Hungary',
        'RUMANIA': 'Romania',
        'SUECIA': 'Sweden',
        'NORUEGA': 'Norway',
        'DINAMARCA': 'Denmark',
        'FINLANDIA': 'Finland',
        'GRECIA': 'Greece',
        'REINO UNIDO': 'United Kingdom',
        'IRLANDA': 'Ireland',
        'ESLOVAQUIA': 'Slovakia',
        'CROACIA': 'Croatia',
        'SERBIA': 'Serbia',
        'BULGARIA': 'Bulgaria',
        'LITUANIA': 'Lithuania',
        'LETONIA': 'Latvia',
        'ESTONIA': 'Estonia',
        'ESLOVENIA': 'Slovenia',
        'LUXEMBURGO': 'Luxembourg',
        'TURQUIA': 'Turkey',
        'UCRANIA': 'Ukraine',
        'BIELORRUSIA': 'Belarus',
        'RUSIA': 'Russia',
    };

    const cargaEN = paisesCarga.map(p => nombreAIngles[p] ?? p);
    const descargaEN = paisesDescarga.map(p => nombreAIngles[p] ?? p);

    const regions = [
        ...cargaEN.map(p => ({ name: p, itemStyle: { areaColor: '#ffcccc' } })),
        ...descargaEN.map(p => ({ name: p, itemStyle: {areaColor: 'cce0ff' } })),
    ];

    const lines = [];
    for (const c of cargaEN) {
        for (const d of descargaEN) {
            if (c !== d && coordenadas[c] && coordenadas[d]) {
                lines.push({
                    coords: [coordenadas[c], coordenadas[d]],
                    lineStyle: { color: '06038D', width: 2, type: 'dashed' }
                });
            }
        }
    }

    const points = [];

    for (const p of paisesCarga) {
        const en = nombreAIngles[p] ?? p;
        if (coordenadas[en]) points.push({
            name: `🔴 Carga: ${p}`,
            value: [...coordenadas[en], 1],
            symbolSize: 12,
            itemStyle: { color: '#e03434' }
        });
    }

    for (const p of paisesDescarga) {
        const en = nombreAIngles[p] ?? p;
        if (coordenadas[en]) points.push({
            name: `🔵 Descarga: ${p}`,
            value: [...coordenadas[en], 1],
            symbolSize: 12,
            itemStyle: { color: '#06038D' }
        });
    }

    const option = {
        tooltip: {
            trigger: 'item',
            formatter: (p) => p.seriesType === 'effectScatter' ? p.name : p.name
        },
        geo: {
            map: 'Europa',
            roam: false,
            center: [13, 52],
            zoom: 1.1,
            itemStyle: {
                areaColor: '#f0f4ff',
                borderColor: '#aaa',
                borderWidth: 0.5
            },
            emphasis: {
                itemStyle: { areaColor: '#d0d8f0' },
                label: { show: false }
            },
            regions: regions
        },
        series: [
            {
                type: 'lines',
                coordinateSystem: 'geo',
                data: lines,
                effect: {
                    show: true,
                    speed: 4,
                    symbol: 'arrow',
                    symbolSize: 8,
                    color: '#06038D'
                },
                lineStyle: { color: '#06038D', width: 2, opacity: 0.8, curveness: 0.2 }
            },
            {
                type: 'effectScatter',
                coordinateSystem: 'geo',
                data: points,
                rippleEffect: { brushType: 'stroke' },
                label: { show: false },
                zlevel: 2
            }
        ]
    };

    europaChart.setOption(option);
    window.addEventListener('resize', () => europaChart?.resize());
}

export function disposeEuropaMap() {
    if (europaChart) {
        europaChart.dispose();
        europaChart = null;
    }
}
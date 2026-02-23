let europaChart = null;

/**
 * @param {string} divId
 * @param {Array<{nombre: string, pais: string, lat: number|null, lng: number|null}>} lugaresCarga
 * @param {Array<{nombre: string, pais: string, lat: number|null, lng: number|null}>} lugaresDescarga
 */
export async function initEuropaMap(divId, lugaresCarga, lugaresDescarga) {
    const el = document.getElementById(divId);
    if (!el || typeof echarts === 'undefined') return;

    if (europaChart) {
        europaChart.dispose();
        europaChart = null;
    }

    const res = await fetch('https://raw.githubusercontent.com/datasets/geo-countries/master/data/countries.geojson');
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
        'Morocco': [-7.1, 31.8]
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
        'MARRUECOS': 'Morocco'
    };

    const cargaEN = [...new Set(lugaresCarga.map(l => nombreAIngles[l.pais] ?? l.pais))];
    const descargaEN = [...new Set(lugaresDescarga.map(l => nombreAIngles[l.pais] ?? l.pais))];

    const regions = [
        ...cargaEN.map(p => ({ name: p, itemStyle: { areaColor: '#FFFADD' } })),
        ...descargaEN.map(p => ({ name: p, itemStyle: { areaColor: '#D0D8F0' } })),
    ];

    function getCoord(lugar, offset = [0, 0]) {
        if (lugar.lat != null && lugar.lng != null) {
            return [lugar.lng, lugar.lat];
        }
        const en = nombreAIngles[lugar.pais] ?? lugar.pais;
        const coord = coordenadas[en] ?? null;
        if (!coord) return null
        return [coord[0] + offset[0], coord[1] + offset[1]];
    }

    const lines = [];

    for (const c of lugaresCarga) {
        for (const d of lugaresDescarga) {
            const mismoPais = c.pais === d.pais;
            const coordC = getCoord(c, mismoPais ? [-0.8, 0.5] : [0, 0]);
            const coordD = getCoord(d, mismoPais ? [0.8, -0.5] : [0, 0]);
            if (coordC && coordD) {
                lines.push({
                    coords: [coordC, coordD],
                    lineStyle: { color: '#06038D', width: 2, type: 'dashed' }
                });
            }
        }
    }

    const points = [];

    for (const l of lugaresCarga) {
        const mismoPais = lugaresDescarga.some(d => d.pais === l.pais);
        const coord = getCoord(l, mismoPais ? [-0.8, 0.5] : [0, 0]);
        if (coord) points.push({
            name: `🔴 Lugar de Carga: ${l.nombre} (${l.pais})`,
            value: [...coord, 1],
            symbolSize: 12,
            itemStyle: { color: '#FFD700' }
        });
    }

    for (const l of lugaresDescarga) {
        const mismoPais = lugaresCarga.some(c => c.pais === l.pais);
        const coord = getCoord(l, mismoPais ? [0.8, -0.5] : [0, 0]);
        if (coord) points.push({
            name: `🔵 Lugar de Descarga: ${l.nombre} (${l.pais})`,
            value: [...coord, 1],
            symbolSize: 12,
            itemStyle: { color: '#06038D' }
        });
    }

    const todasCoords = [...lugaresCarga, ...lugaresDescarga]
        .map(l => getCoord(l))
        .filter(c => c !== null);

    let center = [13, 52];
    let zoom = 1.1;

    if (todasCoords.length > 0) {
        const lngs = todasCoords.map(c => c[0]);
        const lats = todasCoords.map(c => c[1]);

        const minLng = Math.min(...lngs);
        const maxLng = Math.max(...lngs);
        const minLat = Math.min(...lats);
        const maxLat = Math.max(...lats);

        center = [(minLng + maxLng) / 2, (minLat + maxLat) / 2];

        const diffLng = maxLng - minLng;
        const diffLat = maxLat - minLat;
        const diff = Math.max(diffLng, diffLat);

        if (diff < 2) zoom = 40;
        else if (diff < 10) zoom = 30;
        else if (diff < 15) zoom = 10;
        else if (diff < 30) zoom = 5;
        else zoom = 1.2;
    }

    const option = {
        tooltip: {
            trigger: 'item',
            formatter: (p) => p.seriesType === 'effectScatter' ? p.name : p.name
        },
        geo: {
            map: 'Europa',
            roam: false,
            center: center,
            zoom: zoom,
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
                    speed: 2,
                    symbol: 'arrow',
                    symbolSize: 10,
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
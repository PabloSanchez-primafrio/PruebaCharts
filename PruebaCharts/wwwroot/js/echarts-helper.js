window.renderEChart = function (elementId, options) {
    const chartDom = document.getElementById(elementId);
    if (!chartDom) {
        console.error('Elemento no encontrado:', elementId);
        return;
    }

    const existingInstance = echarts.getInstanceByDom(chartDom);
    if (existingInstance) {
        existingInstance.dispose();
    }

    const myChart = echarts.init(chartDom);

    if (options.tooltip?.formatter && typeof options.tooltip.formatter === "string") {
        const raw = options.tooltip.formatter.trim();

        try {
            options.tooltip.formatter = eval("(" + raw + ")");
        } catch (e) {
            console.error("No se pudo compilar tooltip.formatter:", e, raw);
        }
    }

    myChart.setOption(options);

    if (chartDom._resizeHandler) {
        window.removeEventListener('resize', chartDom._resizeHandler);
    }

    chartDom._resizeHandler = () => myChart.resize();
    window.addEventListener('resize', chartDom._resizeHandler);
};
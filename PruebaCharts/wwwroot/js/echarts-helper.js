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

    // Esta es la funcion para tooltip para que aparezca correctamente en FacturacionCliente
    // Pruebala mañana
    if (options.tooltip?.formatter && typeof options.tooltip.formatter === "string") {
        options.tooltip.formatter = new Function("return " + options.tooltip.formatter)();
    }

    myChart.setOption(options);

    if (chartDom._resizeHandler) {
        window.removeEventListener('resize', chartDom._resizeHandler);
    }

    chartDom._resizeHandler = () => myChart.resize();
    window.addEventListener('resize', chartDom._resizeHandler);
};
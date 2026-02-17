window.renderEChart = function (elementId, options) {
    const chartDom = document.getElementById(elementId);
    if (!chartDom) {
        console.error('Elemento no encontrado:', elementId);
        return;
    }

    // Destruir instancia previa si existe
    const existingInstance = echarts.getInstanceByDom(chartDom);
    if (existingInstance) {
        existingInstance.dispose();
    }

    // Crear nueva instancia
    const myChart = echarts.init(chartDom);
    myChart.setOption(options);

    // Hacer responsive
    window.addEventListener('resize', function () {
        myChart.resize();
    });
};
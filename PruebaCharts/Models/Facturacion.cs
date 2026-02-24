namespace PruebaCharts.Models
{
    public class Facturacion
    {
        public string DocumentoVentas { get; set; }
        public string ClaseDocumentoVentas { get; set; }
        public string SociedadFac { get; set; }
        public DateTime FechaPedido { get; set; }
        public string CodigoTrayecto { get; set; }
        public string Solicitante { get; set; }
        public string Nombre1 { get; set; }
        public float Vtotal { get; set; }
        public string Dpto_original { get; set; }
        public DateTime FDescarga { get; set; }
    }
}

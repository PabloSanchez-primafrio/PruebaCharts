namespace PruebaCharts.Models
{
    public class PaletsTotales
    {
        public int Cliente { get; set; }
        public string Matricula { get; set; }
        public string NombreCliente { get; set; }
        public string PaisCarga { get; set; }
        public string NombreLugarCarga { get; set; }
        public float LatitudLugarCarga { get; set; }
        public float LongitudLugarCarga { get; set; }
        public string PaisDescarga { get; set; }
        public string NombreLugarDescarga { get; set; }
        public float LatitudLugarDescarga { get; set; }
        public float LongitudLugarDescarga { get; set; }
        public DateTime FechaTrabajo { get; set; }
        public string? TipoPalets { get; set; }
        public int Trayecto { get; set; }
        public string? Mercancia { get; set; }
        public int NumeroPalets { get; set; }
    }
}

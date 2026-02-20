namespace PruebaCharts.Models
{
    public class PaletsTotales
    {
        public int Cliente { get; set; }
        public string NombreCliente { get; set; }
        public string PaisCarga { get; set; }
        public string PaisDescarga { get; set; }
        public DateTime FechaTrabajo { get; set; }
        public string? TipoPalets { get; set; }
        public int Trayecto { get; set; }
        public string? Mercancia { get; set; }
        public int NumeroPalets { get; set; }
    }
}

using System.Collections.ObjectModel;

namespace PruebaCharts.Models;

/// <summary>
/// Representa un nodo en el árbol de menú de informes.
/// Nodo CON hijos = carpeta. Nodo SIN hijos = informe final.
/// </summary>
public class MenuNode
{
    public string Nombre { get; set; } = string.Empty;
    public string RutaCompleta { get; set; } = string.Empty;
    public ObservableCollection<MenuNode> Children { get; set; } = new();
    public bool HasChildren => Children.Count > 0;
    public bool IsExpanded { get; set; } = false;

    /// <summary>
    /// Icono del nodo: carpeta o documento
    /// </summary>
    public string Icono => HasChildren ? "📁" : "📄";

    /// <summary>
    /// Si este nodo es un informe final (hoja), contiene la consulta asociada.
    /// </summary>
    public ConsultaInfo? Consulta { get; set; }

    /// <summary>
    /// Es un informe ejecutable (hoja sin hijos).
    /// </summary>
    public bool EsInforme => Consulta != null;

    /// <summary>
    /// Crea un árbol de menú a partir de las consultas.
    /// Cada ruta se divide por ';' donde los segmentos intermedios son carpetas
    /// y el último segmento es el nombre del informe (hoja).
    /// </summary>
    public static ObservableCollection<MenuNode> BuildTree(IEnumerable<ConsultaInfo> consultas)
    {
        var root = new ObservableCollection<MenuNode>();

        foreach (var consulta in consultas.OrderBy(c => c.Ruta))
        {
            if (string.IsNullOrWhiteSpace(consulta.Ruta)) continue;

            var partes = consulta.Ruta.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var currentLevel = root;
            var rutaAcumulada = "";

            for (int i = 0; i < partes.Length; i++)
            {
                var nombreParte = partes[i].Trim();
                rutaAcumulada = string.IsNullOrEmpty(rutaAcumulada)
                    ? nombreParte
                    : $"{rutaAcumulada};{nombreParte}";

                var existente = currentLevel.FirstOrDefault(n => n.Nombre == nombreParte);

                if (existente == null)
                {
                    existente = new MenuNode
                    {
                        Nombre = nombreParte,
                        RutaCompleta = rutaAcumulada
                    };

                    // Si es el último segmento, es el informe (hoja)
                    if (i == partes.Length - 1)
                    {
                        existente.Consulta = consulta;
                    }

                    currentLevel.Add(existente);
                }
                else
                {
                    // Si ya existe y estamos en el último segmento,
                    // asignar la consulta a este nodo (puede ser carpeta + informe)
                    if (i == partes.Length - 1)
                    {
                        existente.Consulta = consulta;
                    }
                }

                currentLevel = existente.Children;
            }
        }

        return root;
    }
}

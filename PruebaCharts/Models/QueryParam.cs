using System.ComponentModel;

namespace PruebaCharts.Models;

/// <summary>
/// Representa un parámetro de consulta con su nombre y valor.
/// Usa INotifyPropertyChanged para binding bidireccional en DataGrid.
/// </summary>
public class QueryParam : INotifyPropertyChanged
{
    private string _nombre = string.Empty;
    private string _valor = string.Empty;

    public string Nombre
    {
        get => _nombre;
        set
        {
            _nombre = value;
            OnPropertyChanged(nameof(Nombre));
        }
    }

    public string Valor
    {
        get => _valor;
        set
        {
            _valor = value;
            OnPropertyChanged(nameof(Valor));
        }
    }

    /// <summary>
    /// Nombre del parámetro con @ para usar en SQL.
    /// </summary>
    public string NombreParametro => Nombre.StartsWith("@") ? Nombre : $"@{Nombre}";

    /// <summary>
    /// Indica si tiene consulta para cargar valores desde BD.
    /// Se establece al cruzar con ParametrosValor.
    /// </summary>
    public bool TieneValoresCargables { get; set; }

    /// <summary>
    /// Indica si permite múltiples valores separados por coma.
    /// </summary>
    public bool Multiple { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Configuración de carga de valores para un parámetro específico.
/// Corresponde a una entrada en ParametrosValor/ConsultasValor/ClavesValores/MultiplesValores.
/// Replica exactamente la lógica de LoadValuesParam en QueryEx.
/// </summary>
public class LoadValuesConfig
{
    /// <summary>
    /// Nombre del parámetro (ej: "@Clientes") tal como aparece en ParametrosValor.
    /// </summary>
    public string Parametro { get; set; } = string.Empty;

    /// <summary>
    /// Consulta SQL final ya construida con Clave/Valor AS aliases.
    /// </summary>
    public string Consulta { get; set; } = string.Empty;

    /// <summary>
    /// Clave-valor original (ej: "CodCli-NomCli").
    /// </summary>
    public string ClaveValor { get; set; } = string.Empty;

    /// <summary>
    /// Permite selección múltiple.
    /// </summary>
    public bool Multiple { get; set; }
}

/// <summary>
/// Representa un par clave-valor para la selección de valores.
/// </summary>
public class KeyValue : INotifyPropertyChanged
{
    private bool _seleccionado;

    public bool Seleccionado
    {
        get => _seleccionado;
        set
        {
            _seleccionado = value;
            OnPropertyChanged(nameof(Seleccionado));
        }
    }

    public string Clave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

namespace PruebaCharts.Models;

/// <summary>
/// Representa una consulta almacenada en QUERYEX_CONSULTAS.
/// Esta tabla es la configuración maestra de todos los informes.
/// </summary>
public class ConsultaInfo
{
    public int IdQuery { get; set; }
    public string Ruta { get; set; } = string.Empty;

    /// <summary>
    /// Parámetros del formulario separados por ';'
    /// Ejemplo: "Fecha Desde;Fecha Hasta;Cliente"
    /// </summary>
    public string? Parametros { get; set; }

    /// <summary>
    /// Nombre del informe que se muestra al usuario.
    /// </summary>
    public string Descripcion { get; set; } = string.Empty;

    public string? Permiso { get; set; }

    /// <summary>
    /// Consulta SQL a ejecutar con los parámetros.
    /// </summary>
    public string Consulta { get; set; } = string.Empty;

    public bool Automatica { get; set; }
    public string? TiempoEnvio { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public string? Destinatarios { get; set; }
    public string? AsuntoEmail { get; set; }
    public string? PermisoCliente { get; set; }
    public string? PermisoGrupos { get; set; }
    public string? PermisoOU { get; set; }
    public bool Edicion { get; set; }

    /// <summary>
    /// Nombres de columnas personalizados para el Excel separados por ';'.
    /// </summary>
    public string? Columnas { get; set; }

    /// <summary>
    /// Valores por defecto de los parámetros separados por ';'.
    /// Ejemplo: "@Desde;@Hasta" donde se pueden usar funciones como GETDATE()
    /// </summary>
    public string? ParametrosValor { get; set; }

    /// <summary>
    /// Consultas SQL para poblar ComboBox de parámetros separados por ';'.
    /// Ejemplo: "SELECT Id, Nombre FROM Clientes;SELECT Id, Nombre FROM Proveedores"
    /// </summary>
    public string? ConsultasValor { get; set; }

    /// <summary>
    /// Claves y valores para ComboBox estáticos.
    /// Formato: "param1:valor1,valor2,valor3;param2:valorA,valorB"
    /// </summary>
    public string? ClavesValores { get; set; }

    /// <summary>
    /// Permite selección múltiple en parámetros.
    /// </summary>
    public string? MultiplesValores { get; set; }

    /// <summary>
    /// Columnas por las que agrupar en Excel separadas por ';'.
    /// </summary>
    public string? AgrupFilasExcel { get; set; }

    /// <summary>
    /// Columnas a sumar en Excel separadas por ';'.
    /// </summary>
    public string? SumaColumnasExcel { get; set; }

    /// <summary>
    /// Obtiene los parámetros como lista separados por ';'.
    /// </summary>
    public List<ParametroInfo> GetParametros()
    {
        if (string.IsNullOrWhiteSpace(Parametros))
            return new List<ParametroInfo>();

        // Usar Split sin RemoveEmptyEntries para mantener posiciones sincronizadas
        // con ConsultasValor, ClavesValores, etc.
        var parametrosList = Parametros.Split(';');
        var valoresDefecto = GetValoresDefecto();
        var consultasCombo = GetConsultasValor();
        var clavesCombo = GetClavesValores();

        return parametrosList
            .Select((p, index) =>
            {
                var nombre = p.Trim();
                var nombreParam = $"@{nombre.Replace(" ", "")}";

                return new ParametroInfo
                {
                    Nombre = nombre,
                    NombreParametro = nombreParam,
                    Orden = index,
                    ValorDefecto = valoresDefecto.ElementAtOrDefault(index),
                    ConsultaValores = consultasCombo.ElementAtOrDefault(index),
                    ValoresEstaticos = clavesCombo.GetValueOrDefault(nombre)
                };
            })
            .Where(p => !string.IsNullOrWhiteSpace(p.Nombre))
            .ToList();
    }

    /// <summary>
    /// Obtiene los valores por defecto de los parámetros.
    /// </summary>
    public List<string?> GetValoresDefecto()
    {
        if (string.IsNullOrWhiteSpace(ParametrosValor))
            return new List<string?>();

        return ParametrosValor
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(v => string.IsNullOrWhiteSpace(v) ? null : v.Trim())
            .ToList();
    }

    /// <summary>
    /// Obtiene las consultas SQL para poblar ComboBox.
    /// </summary>
    public List<string?> GetConsultasValor()
    {
        if (string.IsNullOrWhiteSpace(ConsultasValor))
            return new List<string?>();

        return ConsultasValor
            .Split(';', StringSplitOptions.None)
            .Select(v => string.IsNullOrWhiteSpace(v) ? null : v.Trim())
            .ToList();
    }

    /// <summary>
    /// Obtiene los valores estáticos para ComboBox.
    /// Formato: "param1:valor1,valor2,valor3;param2:valorA,valorB"
    /// </summary>
    public Dictionary<string, List<string>> GetClavesValores()
    {
        var result = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(ClavesValores))
            return result;

        var pares = ClavesValores.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var par in pares)
        {
            var partes = par.Split(':', 2);
            if (partes.Length == 2)
            {
                var clave = partes[0].Trim();
                var valores = partes[1].Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => v.Trim())
                    .ToList();
                result[clave] = valores;
            }
        }

        return result;
    }

    /// <summary>
    /// Obtiene las columnas a agrupar en Excel.
    /// </summary>
    public List<string> GetColumnasAgrupar()
    {
        if (string.IsNullOrWhiteSpace(AgrupFilasExcel))
            return new List<string>();

        return AgrupFilasExcel
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim())
            .ToList();
    }

    /// <summary>
    /// Obtiene las columnas a sumar en Excel.
    /// </summary>
    public List<string> GetColumnasSumar()
    {
        if (string.IsNullOrWhiteSpace(SumaColumnasExcel))
            return new List<string>();

        return SumaColumnasExcel
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim())
            .ToList();
    }

    /// <summary>
    /// Obtiene los nombres personalizados de columnas para Excel.
    /// </summary>
    public List<string> GetNombresColumnas()
    {
        if (string.IsNullOrWhiteSpace(Columnas))
            return new List<string>();

        return Columnas
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim())
            .ToList();
    }
}

/// <summary>
/// Representa un parámetro de consulta con toda su configuración.
/// </summary>
public class ParametroInfo
{
    public string Nombre { get; set; } = string.Empty;
    public string NombreParametro { get; set; } = string.Empty;
    public int Orden { get; set; }
    public object? Valor { get; set; }
    public TipoParametro Tipo { get; set; } = TipoParametro.Texto;

    /// <summary>
    /// Valor por defecto del parámetro.
    /// </summary>
    public string? ValorDefecto { get; set; }

    /// <summary>
    /// Consulta SQL para poblar un ComboBox con valores dinámicos.
    /// </summary>
    public string? ConsultaValores { get; set; }

    /// <summary>
    /// Lista de valores estáticos para un ComboBox.
    /// </summary>
    public List<string>? ValoresEstaticos { get; set; }

    /// <summary>
    /// Indica si este parámetro debe mostrarse como ComboBox.
    /// </summary>
    public bool EsComboBox => !string.IsNullOrWhiteSpace(ConsultaValores) ||
                              (ValoresEstaticos != null && ValoresEstaticos.Count > 0);

    /// <summary>
    /// Detecta el tipo de parámetro basado en el nombre.
    /// </summary>
    public static TipoParametro DetectarTipo(string nombre)
    {
        var nombreLower = nombre.ToLower();

        if (nombreLower.Contains("fecha") || nombreLower.Contains("date"))
            return TipoParametro.Fecha;

        if (nombreLower.Contains("desde") || nombreLower.Contains("hasta") ||
            nombreLower.Contains("inicio") || nombreLower.Contains("fin"))
            return TipoParametro.Fecha;

        if (nombreLower.Contains("año") || nombreLower.Contains("mes") ||
            nombreLower.Contains("cantidad") || nombreLower.Contains("numero"))
            return TipoParametro.Numero;

        if (nombreLower.Contains("activo") || nombreLower.Contains("habilitado") ||
            nombreLower.Contains("sino"))
            return TipoParametro.Booleano;

        return TipoParametro.Texto;
    }
}

/// <summary>
/// Tipos de parámetros soportados para el formulario.
/// </summary>
public enum TipoParametro
{
    Texto,
    Numero,
    Fecha,
    Booleano,
    ComboBox
}

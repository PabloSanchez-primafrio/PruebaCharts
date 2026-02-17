using PruebaCharts.Data;
using PruebaCharts.Models;
using System.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PruebaCharts.Services;

public class ConsultaService
{
    private readonly ActiveDirectoryService _adService;

    public ConsultaService(ActiveDirectoryService adService)
    {
        _adService = adService;
    }

    /// <summary>
    /// Obtiene las consultas para una ruta, filtrando por permisos del usuario.
    /// Misma logica que AdvanceReport: trae todas por ruta y filtra en codigo.
    /// </summary>
    public async Task<IEnumerable<ConsultaInfo>> GetConsultasPorRutaAsync(string ruta, CancellationToken token = default)
    {
        const string sql = @"
            SELECT
                IdQuery, Ruta, Parametros, Descripcion, Permiso, Consulta,
                Automatica, TiempoEnvio, FechaEnvio, Destinatarios, AsuntoEmail,
                PermisoCliente, PermisoGrupos, PermisoOU, Edicion, Columnas,
                ParametrosValor, ConsultasValor, ClavesValores, MultiplesValores,
                AgrupFilasExcel, SumaColumnasExcel
            FROM QUERYEX_CONSULTAS
            WHERE (Ruta = @Ruta OR Ruta LIKE @RutaLike)
            ORDER BY Ruta, Descripcion";

        var consultas = await GenericRepository.GetAllAsync<ConsultaInfo>(
            sql,
            new { Ruta = ruta, RutaLike = ruta + "%" },
            token);

        return FiltrarPorPermisos(consultas);
    }

    public async Task<ConsultaInfo?> GetConsultaPorIdAsync(int id, CancellationToken token = default)
    {
        const string sql = @"
            SELECT
                IdQuery, Ruta, Parametros, Descripcion, Permiso, Consulta,
                Automatica, TiempoEnvio, FechaEnvio, Destinatarios, AsuntoEmail,
                PermisoCliente, PermisoGrupos, PermisoOU, Edicion, Columnas,
                ParametrosValor, ConsultasValor, ClavesValores, MultiplesValores,
                AgrupFilasExcel, SumaColumnasExcel
            FROM QUERYEX_CONSULTAS
            WHERE IdQuery = @Id";

        return await GenericRepository.GetAsync<ConsultaInfo>(sql, new { Id = id }, token);
    }

    public async Task<List<NumViajes>> GetNumViajes(int id)
    {
        const string sql = @"
            SELECT Cliente, FechaTrabajo, Nombre, Count(*) NumeroViajes
            FROM GRUPAJES_CABECERA JOIN CLIENTES ON Cliente=CodigoCli
            WHERE Cliente=@Id
            GROUP BY Cliente, Nombre, FechaTrabajo";

        return (List<NumViajes>) await GenericRepository.GetAllAsync<NumViajes>(sql, new { Id = id });
    }

    public async Task<DataTable> EjecutarConsultaAsync(
        ConsultaInfo consulta,
        Dictionary<string, object?> parametros,
        CancellationToken token = default)
    {
        var expandoObj = new System.Dynamic.ExpandoObject() as IDictionary<string, object?>;

        foreach (var kvp in parametros)
        {
            var nombreParam = kvp.Key.TrimStart('@');
            expandoObj[nombreParam] = kvp.Value;
        }

        return await GenericRepository.GetDataTableAsync(consulta.Consulta, expandoObj, token);
    }

    public async Task<List<KeyValuePair<string, string>>> GetValoresComboAsync(
        string consultaSql, CancellationToken token = default)
    {
        var result = new List<KeyValuePair<string, string>>();

        var dt = await GenericRepository.GetDataTableAsync(consultaSql, token: token);

        foreach (DataRow row in dt.Rows)
        {
            if (dt.Columns.Count >= 2)
                result.Add(new(row[0]?.ToString() ?? "", row[1]?.ToString() ?? ""));
            else if (dt.Columns.Count == 1)
                result.Add(new(row[0]?.ToString() ?? "", row[0]?.ToString() ?? ""));
        }

        return result;
    }

    /// <summary>
    /// Filtra consultas segun permisos del usuario (Permiso, PermisoGrupos, PermisoOU).
    /// Igual que AdvanceReport.
    /// </summary>
    private IEnumerable<ConsultaInfo> FiltrarPorPermisos(IEnumerable<ConsultaInfo> consultas)
    {
        return consultas.Where(c => _adService.TieneAcceso(c.Permiso, c.PermisoGrupos, c.PermisoOU));
    }
}
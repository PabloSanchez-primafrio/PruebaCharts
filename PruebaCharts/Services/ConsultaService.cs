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
            SELECT G.Cliente Cliente, L.Nombre NombreCliente, YEAR(C.FechaTrabajo) Anyo, MONTH(C.FechaTrabajo) Mes, COUNT(DISTINCT NTrayecto1) NumeroViajes
            FROM GRUPAJES_CABECERA C INNER JOIN GRUPAJES G ON C.FechaTrabajo=G.FechaTrabajo AND C.Departamento = G.Departamento AND C.Agrupacion=G.IdAgrupacionOptimizador AND G.Anulado=0
            JOIN Clientes L ON G.Cliente=L.Codigocli
            WHERE G.Cliente=@Id AND C.Anulada=0
            GROUP BY G.Cliente, L.Nombre, YEAR(C.FechaTrabajo), MONTH(C.FechaTrabajo)
            ORDER BY Anyo, Mes";

        return (List<NumViajes>) await GenericRepository.GetAllAsync<NumViajes>(sql, new { Id = id });
    }

    public async Task<List<PaletsTotales>> GetCarga(int id)
    {
        const string sql = @"
            SELECT G.Cliente Cliente, L.Nombre NombreCliente, P.Pais PaisCarga, PD.Pais PaisDescarga, G.FechaTrabajo FechaTrabajo, G.TipoPalets TipoPalets, C.NTrayecto1 Trayecto, G.Mercancia Mercancia, G.Palets NumeroPalets
            FROM GRUPAJES_CABECERA C JOIN GRUPAJES G
                                        ON C.FechaTrabajo = G.FechaTrabajo
                                        AND C.Departamento = G.Departamento
                                        AND C.Agrupacion = G.IdAgrupacionOptimizador
                                        AND G.Anulado = 0
                                        JOIN Clientes L ON G.Cliente = L.Codigocli
                                        JOIN Lugar U ON G.LugarCarga = U.Codigo
                                        JOIN Pais P ON U.Pais = P.Codigo_pais
                                        JOIN Lugar UD ON G.LugarDescarga = UD.Codigo
                                        JOIN Pais PD ON UD.Pais = PD.Codigo_pais
            WHERE G.Cliente=@Id AND C.Anulada = 0 AND G.Palets > 0 AND G.Palets <=
                                                                            CASE
                                                                                WHEN G.TipoPalets IN ('H', 'E', 'N', 'NE') THEN 33
                                                                                ELSE 26
                                                                            END
            ORDER BY G.FechaTrabajo, C.NTrayecto1";

        return (List<PaletsTotales>)await GenericRepository.GetAllAsync<PaletsTotales>(sql, new { Id = id });
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
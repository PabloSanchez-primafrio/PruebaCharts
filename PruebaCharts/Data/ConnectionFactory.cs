using Microsoft.Data.SqlClient;

namespace PruebaCharts.Data;

public static class ConnectionFactory
{
#if DEBUG
    private static readonly string _bdatosConnectionString =
        "Data Source=SRVDES;Initial Catalog=bdatosSQL_dev;Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=True;Application Name=ControlerModular";
#else
    private static readonly string _bdatosConnectionString =
        "Data Source=SERVERNT;Initial Catalog=bdatosSQL;Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=True;Application Name=ControlerModular";
#endif

    public enum Database
    {
        BDATOS
    }

    public static SqlConnection GetConnection(Database database = Database.BDATOS)
    {
        return database switch
        {
            Database.BDATOS => new SqlConnection(_bdatosConnectionString),
            _ => throw new ArgumentException($"Base de datos no configurada: {database}")
        };
    }

    public static string GetConnectionString(Database database = Database.BDATOS)
    {
        return database switch
        {
            Database.BDATOS => _bdatosConnectionString,
            _ => throw new ArgumentException($"Base de datos no configurada: {database}")
        };
    }
}

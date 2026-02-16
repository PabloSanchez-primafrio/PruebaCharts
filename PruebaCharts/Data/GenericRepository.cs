using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using static PruebaCharts.Data.ConnectionFactory;

namespace PruebaCharts.Data;

public static class GenericRepository
{
    private const int DefaultTimeout = 30;
    private const int StoredProcedureTimeout = 120;

    #region Query Methods

    public static async Task<IEnumerable<T>> GetAllAsync<T>(
        string sql,
        object? param = null,
        CancellationToken token = default,
        Database database = Database.BDATOS)
    {
        await using var db = GetConnection(database);
        await db.OpenAsync(token);
        return await db.QueryAsync<T>(new CommandDefinition(sql, param, commandTimeout: DefaultTimeout, cancellationToken: token));
    }

    public static async Task<T?> GetAsync<T>(
        string sql,
        object? param = null,
        CancellationToken token = default,
        Database database = Database.BDATOS)
    {
        await using var db = GetConnection(database);
        await db.OpenAsync(token);
        return await db.QueryFirstOrDefaultAsync<T>(new CommandDefinition(sql, param, commandTimeout: DefaultTimeout, cancellationToken: token));
    }

    public static async Task<int> ExecuteAsync(
        string sql,
        object? param = null,
        CancellationToken token = default,
        IDbTransaction? transaction = null,
        Database database = Database.BDATOS)
    {
        if (transaction != null)
        {
            return await ((SqlConnection)transaction.Connection!).ExecuteAsync(
                new CommandDefinition(sql, param, transaction, commandTimeout: DefaultTimeout, cancellationToken: token));
        }

        await using var db = GetConnection(database);
        await db.OpenAsync(token);
        return await db.ExecuteAsync(new CommandDefinition(sql, param, commandTimeout: DefaultTimeout, cancellationToken: token));
    }

    public static async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        object? param = null,
        CancellationToken token = default,
        Database database = Database.BDATOS)
    {
        await using var db = GetConnection(database);
        await db.OpenAsync(token);
        return await db.ExecuteScalarAsync<T>(new CommandDefinition(sql, param, commandTimeout: DefaultTimeout, cancellationToken: token));
    }

    #endregion

    #region DataTable Methods

    public static async Task<DataTable> GetDataTableAsync(
        string sql,
        object? param = null,
        CancellationToken token = default,
        Database database = Database.BDATOS)
    {
        await using var db = GetConnection(database);
        await db.OpenAsync(token);

        var dataTable = new DataTable();

        await using var reader = await db.ExecuteReaderAsync(
            new CommandDefinition(sql, param, commandTimeout: StoredProcedureTimeout, cancellationToken: token));

        var columnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool hayDuplicados = false;

        for (int i = 0; i < reader.FieldCount; i++)
        {
            var name = reader.GetName(i);
            if (!columnNames.Add(name))
            {
                hayDuplicados = true;
                break;
            }
        }

        if (!hayDuplicados)
        {
            dataTable.Load(reader);
        }
        else
        {
            var columnCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var baseName = reader.GetName(i);
                var columnName = baseName;

                if (columnCount.ContainsKey(baseName))
                {
                    columnCount[baseName]++;
                    columnName = $"{baseName}{columnCount[baseName]}";
                }
                else
                {
                    columnCount[baseName] = 1;
                }

                var fieldType = reader.GetFieldType(i);
                dataTable.Columns.Add(columnName, Nullable.GetUnderlyingType(fieldType) ?? fieldType);
            }

            var values = new object[reader.FieldCount];
            while (await reader.ReadAsync(token))
            {
                reader.GetValues(values);
                var row = dataTable.NewRow();
                for (int i = 0; i < values.Length; i++)
                {
                    row[i] = values[i] ?? DBNull.Value;
                }
                dataTable.Rows.Add(row);
            }
        }

        return dataTable;
    }

    #endregion

    #region Stored Procedures

    public static async Task<IEnumerable<T>> ExecuteStoredProcedureAsync<T>(
        string storedProcedure,
        object? param = null,
        CancellationToken token = default,
        Database database = Database.BDATOS)
    {
        await using var db = GetConnection(database);
        await db.OpenAsync(token);
        return await db.QueryAsync<T>(new CommandDefinition(
            storedProcedure,
            param,
            commandType: CommandType.StoredProcedure,
            commandTimeout: StoredProcedureTimeout,
            cancellationToken: token));
    }

    #endregion
}

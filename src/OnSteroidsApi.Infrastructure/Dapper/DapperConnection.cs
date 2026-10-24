using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OnSteroidsApi.Application.Abstractions.Interfaces.IDapper;
using OnSteroidsApi.Domain.Models.AppSettingsModels;
using System.Data;

namespace OnSteroidsApi.Infrastructure.Dapper;

public class DapperConnection(
    IOptions<AppSettings> appSettings,
    ILogger<DapperConnection> logger
) : IDapperConnection
{
    private readonly AppSettings _appSettings = appSettings.Value;
    private readonly ILogger<DapperConnection> _logger = logger;

    private string ConnectionString => _appSettings.SqlSettings?.ConnectionString
        ?? throw new InvalidOperationException("SQL connection string is not configured.");

    private Dictionary<string, object> ExtractParameters(DynamicParameters? parameters)
    {
        var dict = new Dictionary<string, object>();
        if (parameters != null)
        {
            foreach (var name in parameters.ParameterNames)
            {
                dict[name] = parameters.Get<object>(name);
            }
        }
        return dict;
    }

    public async Task<int> Execute(DynamicParameters parameters, string procedureName, CommandType commandType = CommandType.StoredProcedure)
    {
        var pDict = ExtractParameters(parameters);
        if (procedureName != "spApiLogs_Insert")
            _logger.LogInformation("Executing DB {ProcedureName} with params: {Params}", procedureName, Newtonsoft.Json.JsonConvert.SerializeObject(pDict));
        
        using var connection = new SqlConnection(ConnectionString);
        try
        {
            await connection.OpenAsync();
            var rows = await connection.ExecuteAsync(procedureName, parameters, commandType: commandType);
            
            if (procedureName != "spApiLogs_Insert")
                _logger.LogInformation("Executed DB {ProcedureName}, affected rows: {Rows}", procedureName, rows);
                
            return rows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute procedure {ProcedureName} with params {Params}", procedureName, Newtonsoft.Json.JsonConvert.SerializeObject(pDict));
            throw;
        }
    }

    public async Task<T> Query<T>(DynamicParameters parameters, string procedureName, CommandType commandType = CommandType.StoredProcedure)
    {
        var pDict = ExtractParameters(parameters);
        _logger.LogInformation("Executing DB Query {ProcedureName} with params: {Params}", procedureName, Newtonsoft.Json.JsonConvert.SerializeObject(pDict));
        using var connection = new SqlConnection(ConnectionString);
        try
        {
            await connection.OpenAsync();
            var result = await connection.QueryFirstOrDefaultAsync<T>(procedureName, parameters, commandType: commandType);
            _logger.LogInformation("Executed DB Query {ProcedureName}, returned data: {Data}", procedureName, Newtonsoft.Json.JsonConvert.SerializeObject(result));
            return result!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute query procedure {ProcedureName} with params {Params}", procedureName, Newtonsoft.Json.JsonConvert.SerializeObject(pDict));
            throw;
        }
    }

    public async Task<IEnumerable<T>> QueryAll<T>(DynamicParameters? parameters, string procedureName, CommandType commandType = CommandType.StoredProcedure)
    {
        var pDict = ExtractParameters(parameters);
        _logger.LogInformation("Executing DB QueryAll {ProcedureName} with params: {Params}", procedureName, Newtonsoft.Json.JsonConvert.SerializeObject(pDict));
        using var connection = new SqlConnection(ConnectionString);
        try
        {
            await connection.OpenAsync();
            var result = await connection.QueryAsync<T>(procedureName, parameters, commandType: commandType);
            _logger.LogInformation("Executed DB QueryAll {ProcedureName}, returned count: {Count}, data: {Data}", procedureName, result?.Count() ?? 0, Newtonsoft.Json.JsonConvert.SerializeObject(result));
            return result ?? Enumerable.Empty<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute query all procedure {ProcedureName} with params {Params}", procedureName, Newtonsoft.Json.JsonConvert.SerializeObject(pDict));
            throw;
        }
    }
}

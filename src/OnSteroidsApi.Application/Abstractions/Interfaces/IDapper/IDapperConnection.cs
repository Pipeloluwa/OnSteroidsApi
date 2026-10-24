using Dapper;
using System.Data;

namespace OnSteroidsApi.Application.Abstractions.Interfaces.IDapper;

public interface IDapperConnection
{
    Task<int> Execute(DynamicParameters parameters, string procedureName, CommandType commandType = CommandType.StoredProcedure);
    Task<T> Query<T>(DynamicParameters parameters, string procedureName, CommandType commandType = CommandType.StoredProcedure);
    Task<IEnumerable<T>> QueryAll<T>(DynamicParameters? parameters, string procedureName, CommandType commandType = CommandType.StoredProcedure);
}

using QuantLab.Api.Data;
using QuantLab.Api.Dtos;
using System;
using Dapper;
using Npgsql;
namespace QuantLab.Api.Data;

public class AssetRepository : IAssetRepository
{
    private readonly string _connectionString;

    public AssetRepository(string connectionString) => _connectionString = connectionString;
    public async Task<IEnumerable<AssetDto>> GetAllAsync()
    {
        const string sql = "SELECT id, symbol, asset_name AS \"Name\", exchange, currency FROM assets";

        using var connection = new NpgsqlConnection(_connectionString);

        return await connection.QueryAsync<AssetDto>(sql);
    }

    public async Task<AssetDto?> GetByIdAsync(long id)
    {
        const string sql = "SELECT id, symbol, asset_name AS \"Name\", exchange, currency FROM assets WHERE Id = @id";

        using var connection = new NpgsqlConnection(_connectionString);

        return await connection.QuerySingleOrDefaultAsync<AssetDto>(sql, new{Id = id});
    }
}
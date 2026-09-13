using Dapper;
using Npgsql;
using QuantLab.Api.Data;

namespace QuantLab.Api.Tests;

public class AssetRepositoryTests
{
    private static string GenerateTestSymbol() => $"T{Guid.NewGuid():N}"[..10];

    private static async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = new NpgsqlConnection(DatabaseConfig.GetConnectionString());
        await connection.OpenAsync();
        return connection;
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsAsset_WhenItExists()
    {
        await using var connection = await OpenConnectionAsync();
        var symbol = GenerateTestSymbol();
        var insertedId = await connection.ExecuteScalarAsync<long>(
            "INSERT INTO assets (symbol, asset_name, exchange, currency) VALUES (@symbol, @name, @exchange, @currency) RETURNING id",
            new { symbol, name = "Test Asset", exchange = "TEST", currency = "USD" });

        try
        {
            var repository = new AssetRepository(DatabaseConfig.GetConnectionString());
            var result = await repository.GetByIdAsync(insertedId);

            Assert.NotNull(result);
            Assert.Equal(symbol, result!.Symbol);
            Assert.Equal("Test Asset", result.Name);
            Assert.Equal("TEST", result.Exchange);
            Assert.Equal("USD", result.Currency);
        }
        finally
        {
            await connection.ExecuteAsync("DELETE FROM assets WHERE id = @id", new { id = insertedId });
        }
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var repository = new AssetRepository(DatabaseConfig.GetConnectionString());
        var result = await repository.GetByIdAsync(-1); // no real asset will ever have a negative id

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_IncludesSeededAsset()
    {
        await using var connection = await OpenConnectionAsync();
        var symbol = GenerateTestSymbol();
        var insertedId = await connection.ExecuteScalarAsync<long>(
            "INSERT INTO assets (symbol, asset_name, exchange, currency) VALUES (@symbol, @name, @exchange, @currency) RETURNING id",
            new { symbol, name = "Test Asset", exchange = "TEST", currency = "USD" });

        try
        {
            var repository = new AssetRepository(DatabaseConfig.GetConnectionString());
            var all = await repository.GetAllAsync();

            Assert.Contains(all, a => a.Id == insertedId && a.Symbol == symbol);
        }
        finally
        {
            await connection.ExecuteAsync("DELETE FROM assets WHERE id = @id", new { id = insertedId });
        }
    }
}

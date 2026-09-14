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

    private static async Task<long> SeedAssetAsync(NpgsqlConnection connection)
    {
        var symbol = GenerateTestSymbol();
        return await connection.ExecuteScalarAsync<long>(
            "INSERT INTO assets (symbol, asset_name, exchange, currency) VALUES (@symbol, @name, @exchange, @currency) RETURNING id",
            new { symbol, name = "Test Asset", exchange = "TEST", currency = "USD" });
    }

    private static async Task SeedPriceBarAsync(NpgsqlConnection connection, long assetId, DateTime timestamp)
    {
        await connection.ExecuteAsync(
            "INSERT INTO price_bars (asset_id, timestamp, open, high, low, close, volume) " +
            "VALUES (@assetId, @timestamp, 100, 105, 99, 103, 1000)",
            new { assetId, timestamp });
    }

    [Fact]
    public async Task GetPricesAsync_ReturnsBarsOrderedByTimestamp_NotInsertionOrder()
    {
        await using var connection = await OpenConnectionAsync();
        var assetId = await SeedAssetAsync(connection);

        // deliberately inserted out of chronological order to prove the query sorts them
        await SeedPriceBarAsync(connection, assetId, new DateTime(2024, 1, 3));
        await SeedPriceBarAsync(connection, assetId, new DateTime(2024, 1, 1));
        await SeedPriceBarAsync(connection, assetId, new DateTime(2024, 1, 2));

        try
        {
            var repository = new AssetRepository(DatabaseConfig.GetConnectionString());
            var result = (await repository.GetPricesAsync(assetId, from: null, to: null, limit: null)).ToList();

            Assert.Equal(3, result.Count);
            Assert.True(result[0].Timestamp < result[1].Timestamp);
            Assert.True(result[1].Timestamp < result[2].Timestamp);
        }
        finally
        {
            await connection.ExecuteAsync("DELETE FROM price_bars WHERE asset_id = @assetId", new { assetId });
            await connection.ExecuteAsync("DELETE FROM assets WHERE id = @assetId", new { assetId });
        }
    }

    [Fact]
    public async Task GetPricesAsync_RespectsFromAndToFilters()
    {
        await using var connection = await OpenConnectionAsync();
        var assetId = await SeedAssetAsync(connection);

        await SeedPriceBarAsync(connection, assetId, new DateTime(2024, 1, 1));
        await SeedPriceBarAsync(connection, assetId, new DateTime(2024, 1, 2));
        await SeedPriceBarAsync(connection, assetId, new DateTime(2024, 1, 3));

        try
        {
            var repository = new AssetRepository(DatabaseConfig.GetConnectionString());
            var result = (await repository.GetPricesAsync(
                assetId, from: new DateTime(2024, 1, 2), to: new DateTime(2024, 1, 2), limit: null)).ToList();

            Assert.Single(result);
            Assert.Equal(new DateTime(2024, 1, 2), result[0].Timestamp.Date);
        }
        finally
        {
            await connection.ExecuteAsync("DELETE FROM price_bars WHERE asset_id = @assetId", new { assetId });
            await connection.ExecuteAsync("DELETE FROM assets WHERE id = @assetId", new { assetId });
        }
    }

    [Fact]
    public async Task GetPricesAsync_RespectsLimit()
    {
        await using var connection = await OpenConnectionAsync();
        var assetId = await SeedAssetAsync(connection);

        await SeedPriceBarAsync(connection, assetId, new DateTime(2024, 1, 1));
        await SeedPriceBarAsync(connection, assetId, new DateTime(2024, 1, 2));
        await SeedPriceBarAsync(connection, assetId, new DateTime(2024, 1, 3));

        try
        {
            var repository = new AssetRepository(DatabaseConfig.GetConnectionString());
            var result = (await repository.GetPricesAsync(assetId, from: null, to: null, limit: 1)).ToList();

            Assert.Single(result);
        }
        finally
        {
            await connection.ExecuteAsync("DELETE FROM price_bars WHERE asset_id = @assetId", new { assetId });
            await connection.ExecuteAsync("DELETE FROM assets WHERE id = @assetId", new { assetId });
        }
    }

    [Fact]
    public async Task GetPricesAsync_ReturnsEmpty_WhenAssetHasNoPrices()
    {
        await using var connection = await OpenConnectionAsync();
        var assetId = await SeedAssetAsync(connection);

        try
        {
            var repository = new AssetRepository(DatabaseConfig.GetConnectionString());
            var result = await repository.GetPricesAsync(assetId, from: null, to: null, limit: null);

            Assert.Empty(result);
        }
        finally
        {
            await connection.ExecuteAsync("DELETE FROM assets WHERE id = @assetId", new { assetId });
        }
    }
}

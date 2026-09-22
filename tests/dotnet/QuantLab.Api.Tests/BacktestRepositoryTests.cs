using Dapper;
using Npgsql;
using QuantLab.Api.Data;
using QuantLab.Api.Dtos;

namespace QuantLab.Api.Tests;

public class BacktestRepositoryTests
{
    private static string GenerateTestSymbol() => $"T{Guid.NewGuid():N}"[..10];

    private static async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = new NpgsqlConnection(DatabaseConfig.GetConnectionString());
        await connection.OpenAsync();
        return connection;
    }

    private static async Task<long> SeedAssetAsync(NpgsqlConnection connection)
    {
        var symbol = GenerateTestSymbol();
        return await connection.ExecuteScalarAsync<long>(
            "INSERT INTO assets (symbol, asset_name, exchange, currency) VALUES (@symbol, @name, @exchange, @currency) RETURNING id",
            new { symbol, name = "Test Asset", exchange = "TEST", currency = "USD" });
    }

    private static async Task<long> SeedBacktestAsync(NpgsqlConnection connection, long assetId,
        string strategyName = "test_strategy", DateTime? startDate = null, DateTime? endDate = null,
        decimal initialCapital = 1000m, decimal finalEquity = 1200m, decimal totalReturn = 0.2m)
    {
        return await connection.ExecuteScalarAsync<long>(
            "INSERT INTO backtests (asset_id, strategy_name, start_date, end_date, initial_capital, final_equity, total_return) " +
            "VALUES (@assetId, @strategyName, @startDate, @endDate, @initialCapital, @finalEquity, @totalReturn) RETURNING id",
            new
            {
                assetId,
                strategyName,
                startDate = startDate ?? new DateTime(2024, 1, 1),
                endDate = endDate ?? new DateTime(2024, 1, 10),
                initialCapital,
                finalEquity,
                totalReturn,
            });
    }

    private static async Task SeedTradeAsync(NpgsqlConnection connection, long backtestId, DateTime timestamp,
        string side = "BUY")
    {
        await connection.ExecuteAsync(
            "INSERT INTO trades (backtest_id, timestamp, side, quantity, execution_price, commission, slippage) " +
            "VALUES (@backtestId, @timestamp, @side, 10, 100, 1, 0)",
            new { backtestId, timestamp, side });
    }

    private static async Task SeedEquityPointAsync(NpgsqlConnection connection, long backtestId, DateTime timestamp,
        decimal equity = 1000m)
    {
        await connection.ExecuteAsync(
            "INSERT INTO equity_curves (backtest_id, timestamp, equity) VALUES (@backtestId, @timestamp, @equity)",
            new { backtestId, timestamp, equity });
    }

    [Fact]
    public async Task GetSummaryByIdAsync_ReturnsBacktest_WhenItExists()
    {
        await using var connection = await OpenConnectionAsync();
        var assetId = await SeedAssetAsync(connection);
        var backtestId = await SeedBacktestAsync(connection, assetId, strategyName: "ma_crossover",
            initialCapital: 1000m, finalEquity: 1150m, totalReturn: 0.15m);

        try
        {
            var repository = new BacktestRepository(DatabaseConfig.GetConnectionString());
            var result = await repository.GetSummaryByIdAsync(backtestId);

            Assert.NotNull(result);
            Assert.Equal(assetId, result!.AssetId);
            Assert.Equal("ma_crossover", result.StrategyName);
            Assert.Equal(1000m, result.InitialCapital);
            Assert.Equal(1150m, result.FinalEquity);
            Assert.Equal(0.15m, result.TotalReturn);
        }
        finally
        {
            await connection.ExecuteAsync("DELETE FROM backtests WHERE id = @id", new { id = backtestId });
            await connection.ExecuteAsync("DELETE FROM assets WHERE id = @assetId", new { assetId });
        }
    }

    [Fact]
    public async Task GetSummaryByIdAsync_ReturnsNull_WhenNotFound()
    {
        var repository = new BacktestRepository(DatabaseConfig.GetConnectionString());
        var result = await repository.GetSummaryByIdAsync(-1); // no real backtest will ever have a negative id

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_IncludesSeededBacktest()
    {
        await using var connection = await OpenConnectionAsync();
        var assetId = await SeedAssetAsync(connection);
        var backtestId = await SeedBacktestAsync(connection, assetId, strategyName: "ma_crossover");

        try
        {
            var repository = new BacktestRepository(DatabaseConfig.GetConnectionString());
            var all = await repository.GetAllAsync();

            Assert.Contains(all, b => b.Id == backtestId && b.StrategyName == "ma_crossover");
        }
        finally
        {
            await connection.ExecuteAsync("DELETE FROM backtests WHERE id = @id", new { id = backtestId });
            await connection.ExecuteAsync("DELETE FROM assets WHERE id = @assetId", new { assetId });
        }
    }

    [Fact]
    public async Task GetTradesAsync_ReturnsTradesOrderedByTimestamp_NotInsertionOrder()
    {
        await using var connection = await OpenConnectionAsync();
        var assetId = await SeedAssetAsync(connection);
        var backtestId = await SeedBacktestAsync(connection, assetId);

        // deliberately inserted out of chronological order to prove the query sorts them
        await SeedTradeAsync(connection, backtestId, new DateTime(2024, 1, 3), side: "SELL");
        await SeedTradeAsync(connection, backtestId, new DateTime(2024, 1, 1), side: "BUY");

        try
        {
            var repository = new BacktestRepository(DatabaseConfig.GetConnectionString());
            var result = (await repository.GetTradesAsync(backtestId)).ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal("BUY", result[0].Side);
            Assert.Equal("SELL", result[1].Side);
            Assert.True(result[0].Timestamp < result[1].Timestamp);
        }
        finally
        {
            await connection.ExecuteAsync("DELETE FROM backtests WHERE id = @id", new { id = backtestId });
            await connection.ExecuteAsync("DELETE FROM assets WHERE id = @assetId", new { assetId });
        }
    }

    [Fact]
    public async Task GetTradesAsync_ReturnsEmpty_WhenBacktestHasNoTrades()
    {
        await using var connection = await OpenConnectionAsync();
        var assetId = await SeedAssetAsync(connection);
        var backtestId = await SeedBacktestAsync(connection, assetId);

        try
        {
            var repository = new BacktestRepository(DatabaseConfig.GetConnectionString());
            var result = await repository.GetTradesAsync(backtestId);

            Assert.Empty(result);
        }
        finally
        {
            await connection.ExecuteAsync("DELETE FROM backtests WHERE id = @id", new { id = backtestId });
            await connection.ExecuteAsync("DELETE FROM assets WHERE id = @assetId", new { assetId });
        }
    }

    [Fact]
    public async Task GetEquityCurveAsync_ReturnsPointsOrderedByTimestamp_NotInsertionOrder()
    {
        await using var connection = await OpenConnectionAsync();
        var assetId = await SeedAssetAsync(connection);
        var backtestId = await SeedBacktestAsync(connection, assetId);

        // deliberately inserted out of chronological order to prove the query sorts them
        await SeedEquityPointAsync(connection, backtestId, new DateTime(2024, 1, 3), equity: 1300m);
        await SeedEquityPointAsync(connection, backtestId, new DateTime(2024, 1, 1), equity: 1000m);
        await SeedEquityPointAsync(connection, backtestId, new DateTime(2024, 1, 2), equity: 1150m);

        try
        {
            var repository = new BacktestRepository(DatabaseConfig.GetConnectionString());
            var result = (await repository.GetEquityCurveAsync(backtestId)).ToList();

            Assert.Equal(3, result.Count);
            Assert.True(result[0].Timestamp < result[1].Timestamp);
            Assert.True(result[1].Timestamp < result[2].Timestamp);
            Assert.Equal(1000m, result[0].Equity);
            Assert.Equal(1300m, result[2].Equity);
        }
        finally
        {
            await connection.ExecuteAsync("DELETE FROM backtests WHERE id = @id", new { id = backtestId });
            await connection.ExecuteAsync("DELETE FROM assets WHERE id = @assetId", new { assetId });
        }
    }

    [Fact]
    public async Task GetEquityCurveAsync_ReturnsEmpty_WhenBacktestHasNoEquityCurve()
    {
        await using var connection = await OpenConnectionAsync();
        var assetId = await SeedAssetAsync(connection);
        var backtestId = await SeedBacktestAsync(connection, assetId);

        try
        {
            var repository = new BacktestRepository(DatabaseConfig.GetConnectionString());
            var result = await repository.GetEquityCurveAsync(backtestId);

            Assert.Empty(result);
        }
        finally
        {
            await connection.ExecuteAsync("DELETE FROM backtests WHERE id = @id", new { id = backtestId });
            await connection.ExecuteAsync("DELETE FROM assets WHERE id = @assetId", new { assetId });
        }
    }
}

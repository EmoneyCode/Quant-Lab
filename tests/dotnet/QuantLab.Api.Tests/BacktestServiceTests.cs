// Contract for backend/Services/BacktestService.cs and backend/Dtos/BacktestDetailDto.cs
// (neither exists yet):
//
//   public class BacktestService
//   {
//       public BacktestService(IBacktestRepository backtestRepository) { ... }
//       public Task<IEnumerable<BacktestSummaryDto>> GetBacktestsAsync();
//       public Task<BacktestDetailDto?> GetBacktestAsync(long id);
//   }
//
// GetBacktestAsync composes three repository calls: GetSummaryByIdAsync(id) first --
// if that comes back null, return null immediately (the controller turns that into a
// 404) without calling GetTradesAsync/GetEquityCurveAsync at all. Otherwise, fetch
// GetTradesAsync(id) and GetEquityCurveAsync(id) and fold everything into one
// BacktestDetailDto:
//
//   public record BacktestDetailDto(long Id, long AssetId, string StrategyName,
//       DateTime StartDate, DateTime EndDate, decimal InitialCapital, decimal FinalEquity,
//       decimal TotalReturn, IEnumerable<TradeDto> Trades, IEnumerable<EquityPointDto> EquityCurve);
//
// (Property order doesn't matter here since this DTO is never itself materialized by
// Dapper -- it's assembled by hand in the service from pieces Dapper already mapped.)

using QuantLab.Api.Data;
using QuantLab.Api.Dtos;
using QuantLab.Api.Services;

namespace QuantLab.Api.Tests;

public class FakeBacktestRepository : IBacktestRepository
{
    private readonly List<BacktestSummaryDto> _summaries = new();
    private readonly List<(long BacktestId, TradeDto Trade)> _trades = new();
    private readonly List<(long BacktestId, EquityPointDto Point)> _equityCurve = new();

    public void Seed(BacktestSummaryDto summary) => _summaries.Add(summary);
    public void SeedTrade(long backtestId, TradeDto trade) => _trades.Add((backtestId, trade));
    public void SeedEquityPoint(long backtestId, EquityPointDto point) => _equityCurve.Add((backtestId, point));

    public Task<IEnumerable<BacktestSummaryDto>> GetAllAsync() =>
        Task.FromResult(_summaries.AsEnumerable());

    public Task<BacktestSummaryDto?> GetSummaryByIdAsync(long id) =>
        Task.FromResult(_summaries.FirstOrDefault(b => b.Id == id));

    public Task<IEnumerable<TradeDto>> GetTradesAsync(long backtestId) =>
        Task.FromResult(_trades.Where(t => t.BacktestId == backtestId).Select(t => t.Trade));

    public Task<IEnumerable<EquityPointDto>> GetEquityCurveAsync(long backtestId) =>
        Task.FromResult(_equityCurve.Where(e => e.BacktestId == backtestId).Select(e => e.Point));
}

public class BacktestServiceTests
{
    private static BacktestSummaryDto MakeSummary(long id, long assetId = 1, string strategyName = "ma_crossover") =>
        new(id, assetId, strategyName, 1000m, 1200m, 0.2m, new DateTime(2024, 1, 1), new DateTime(2024, 1, 10));

    [Fact]
    public async Task GetBacktestsAsync_ReturnsAllBacktestsFromRepository()
    {
        var repository = new FakeBacktestRepository();
        repository.Seed(MakeSummary(1, strategyName: "ma_crossover"));
        repository.Seed(MakeSummary(2, strategyName: "mean_reversion"));
        var service = new BacktestService(repository);

        var result = (await service.GetBacktestsAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, b => b.StrategyName == "ma_crossover");
        Assert.Contains(result, b => b.StrategyName == "mean_reversion");
    }

    [Fact]
    public async Task GetBacktestAsync_ReturnsNull_WhenNotFound()
    {
        var repository = new FakeBacktestRepository();
        var service = new BacktestService(repository);

        var result = await service.GetBacktestAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBacktestAsync_ReturnsConfigurationAndMetrics_WhenFound()
    {
        var repository = new FakeBacktestRepository();
        repository.Seed(MakeSummary(1, assetId: 7, strategyName: "ma_crossover"));
        var service = new BacktestService(repository);

        var result = await service.GetBacktestAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result!.Id);
        Assert.Equal(7, result.AssetId);
        Assert.Equal("ma_crossover", result.StrategyName);
        Assert.Equal(1000m, result.InitialCapital);
        Assert.Equal(1200m, result.FinalEquity);
        Assert.Equal(0.2m, result.TotalReturn);
    }

    [Fact]
    public async Task GetBacktestAsync_IncludesTradesAndEquityCurve_WhenFound()
    {
        var repository = new FakeBacktestRepository();
        repository.Seed(MakeSummary(1));
        repository.SeedTrade(1, new TradeDto(new DateTime(2024, 1, 2), "BUY", 10m, 100m, 1m, 0m));
        repository.SeedTrade(1, new TradeDto(new DateTime(2024, 1, 5), "SELL", 10m, 110m, 1m, 0m));
        repository.SeedEquityPoint(1, new EquityPointDto(new DateTime(2024, 1, 1), 1000m));
        repository.SeedEquityPoint(1, new EquityPointDto(new DateTime(2024, 1, 2), 1050m));
        var service = new BacktestService(repository);

        var result = await service.GetBacktestAsync(1);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Trades.Count());
        Assert.Contains(result.Trades, t => t.Side == "BUY");
        Assert.Contains(result.Trades, t => t.Side == "SELL");
        Assert.Equal(2, result.EquityCurve.Count());
    }

    [Fact]
    public async Task GetBacktestAsync_DoesNotLeakTradesOrEquityCurveFromOtherBacktests()
    {
        var repository = new FakeBacktestRepository();
        repository.Seed(MakeSummary(1));
        repository.Seed(MakeSummary(2));
        repository.SeedTrade(1, new TradeDto(new DateTime(2024, 1, 2), "BUY", 10m, 100m, 1m, 0m));
        repository.SeedTrade(2, new TradeDto(new DateTime(2024, 1, 3), "SELL", 5m, 50m, 1m, 0m));
        repository.SeedEquityPoint(1, new EquityPointDto(new DateTime(2024, 1, 1), 1000m));
        repository.SeedEquityPoint(2, new EquityPointDto(new DateTime(2024, 1, 1), 500m));
        var service = new BacktestService(repository);

        var result = await service.GetBacktestAsync(1);

        Assert.NotNull(result);
        Assert.Single(result!.Trades);
        Assert.Equal("BUY", result.Trades.Single().Side);
        Assert.Single(result.EquityCurve);
        Assert.Equal(1000m, result.EquityCurve.Single().Equity);
    }
}

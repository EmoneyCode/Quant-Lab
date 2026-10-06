// Concurrency contract for BacktestService.GetBacktestAsync (no new types needed):
//
//   1. The summary lookup still runs first and alone; it decides whether the backtest exists.
//   2. If it comes back null, trades and equity curve are never requested.
//   3. Otherwise trades and equity curve are requested at the same time, not one after the other,
//      since neither depends on the other.

using QuantLab.Api.Data;
using QuantLab.Api.Dtos;
using QuantLab.Api.Services;

namespace QuantLab.Api.Tests;

// Both child fetches wait for each other to be in flight at the same moment. If the service
// runs them one after the other, the first waits alone until the timeout and Overlapped stays false.
public class OverlapDetectingBacktestRepository : IBacktestRepository
{
    private static readonly TimeSpan RendezvousTimeout = TimeSpan.FromMilliseconds(500);

    private readonly FakeBacktestRepository _inner = new();
    private readonly TaskCompletionSource _bothInFlight = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _inFlight;
    private int _tradesCalls;
    private int _equityCalls;

    public bool Overlapped => _bothInFlight.Task.IsCompleted;
    public int TradesCalls => Volatile.Read(ref _tradesCalls);
    public int EquityCalls => Volatile.Read(ref _equityCalls);

    public void Seed(BacktestSummaryDto summary) => _inner.Seed(summary);
    public void SeedTrade(long backtestId, TradeDto trade) => _inner.SeedTrade(backtestId, trade);
    public void SeedEquityPoint(long backtestId, EquityPointDto point) => _inner.SeedEquityPoint(backtestId, point);

    private async Task WaitForOtherCallAsync()
    {
        if (Interlocked.Increment(ref _inFlight) >= 2)
        {
            _bothInFlight.TrySetResult();
        }

        await Task.WhenAny(_bothInFlight.Task, Task.Delay(RendezvousTimeout));
        Interlocked.Decrement(ref _inFlight);
    }

    public Task<IEnumerable<BacktestSummaryDto>> GetAllAsync() => _inner.GetAllAsync();

    public Task<BacktestSummaryDto?> GetSummaryByIdAsync(long id) => _inner.GetSummaryByIdAsync(id);

    public async Task<IEnumerable<TradeDto>> GetTradesAsync(long backtestId)
    {
        Interlocked.Increment(ref _tradesCalls);
        await WaitForOtherCallAsync();
        return await _inner.GetTradesAsync(backtestId);
    }

    public async Task<IEnumerable<EquityPointDto>> GetEquityCurveAsync(long backtestId)
    {
        Interlocked.Increment(ref _equityCalls);
        await WaitForOtherCallAsync();
        return await _inner.GetEquityCurveAsync(backtestId);
    }
}

public class BacktestServiceConcurrencyTests
{
    private static BacktestSummaryDto MakeSummary(long id) =>
        new(id, 1, "ma_crossover", 1000m, 1200m, 0.2m, new DateTime(2024, 1, 1), new DateTime(2024, 1, 10));

    [Fact]
    public async Task GetBacktestAsync_FetchesTradesAndEquityCurveConcurrently()
    {
        var repository = new OverlapDetectingBacktestRepository();
        repository.Seed(MakeSummary(1));
        repository.SeedTrade(1, new TradeDto(new DateTime(2024, 1, 2), "BUY", 10m, 100m, 1m, 0m));
        repository.SeedEquityPoint(1, new EquityPointDto(new DateTime(2024, 1, 1), 1000m));
        var service = new BacktestService(repository);

        var result = await service.GetBacktestAsync(1);

        Assert.True(repository.Overlapped, "trades and equity curve were fetched one after the other");
        Assert.NotNull(result);
        Assert.Single(result!.Trades);
        Assert.Single(result.EquityCurve);
    }

    [Fact]
    public async Task GetBacktestAsync_DoesNotFetchTradesOrEquityCurve_WhenBacktestNotFound()
    {
        var repository = new OverlapDetectingBacktestRepository();
        var service = new BacktestService(repository);

        var result = await service.GetBacktestAsync(999);

        Assert.Null(result);
        Assert.Equal(0, repository.TradesCalls);
        Assert.Equal(0, repository.EquityCalls);
    }
}

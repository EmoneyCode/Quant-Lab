// Contract for backend/Data/CachedBacktestRepository.cs (doesn't exist yet):
//
//   public class CachedBacktestRepository : IBacktestRepository
//   {
//       public CachedBacktestRepository(IBacktestRepository inner) { ... }
//   }
//
// A decorator: same interface as the real repository, so BacktestService never changes.
//
//   Cached per id:  GetSummaryByIdAsync, GetTradesAsync, GetEquityCurveAsync
//   Pass-through:   GetAllAsync (the list grows whenever Python writes a new backtest)
//
// Rules the tests below pin down:
//   1. Concurrent callers asking for the same uncached id share ONE inner call.
//   2. A null result (backtest not found) is never cached, since Python may write it later.
//   3. A failed load is never cached, so the next call retries instead of replaying the failure.
//   4. Different ids are cached independently.

using QuantLab.Api.Data;
using QuantLab.Api.Dtos;

namespace QuantLab.Api.Tests;

public class InstrumentedBacktestRepository : IBacktestRepository
{
    private readonly FakeBacktestRepository _inner = new();
    private int _summaryCalls;
    private int _tradesCalls;
    private int _equityCalls;
    private int _allCalls;
    private int _failuresRemaining;
    private Task _gate = Task.CompletedTask;

    public int SummaryCalls => Volatile.Read(ref _summaryCalls);
    public int TradesCalls => Volatile.Read(ref _tradesCalls);
    public int EquityCalls => Volatile.Read(ref _equityCalls);
    public int AllCalls => Volatile.Read(ref _allCalls);

    public void Seed(BacktestSummaryDto summary) => _inner.Seed(summary);
    public void SeedTrade(long backtestId, TradeDto trade) => _inner.SeedTrade(backtestId, trade);
    public void SeedEquityPoint(long backtestId, EquityPointDto point) => _inner.SeedEquityPoint(backtestId, point);

    // every call waits on this task before returning, so a test can hold the first load in flight
    public void BlockUntil(Task gate) => _gate = gate;

    // the next N calls (to any method) throw instead of returning data
    public void FailNext(int times) => _failuresRemaining = times;

    private void ThrowIfFailing()
    {
        if (Interlocked.Decrement(ref _failuresRemaining) >= 0)
        {
            throw new InvalidOperationException("simulated DB failure");
        }
    }

    public async Task<IEnumerable<BacktestSummaryDto>> GetAllAsync()
    {
        Interlocked.Increment(ref _allCalls);
        await _gate;
        ThrowIfFailing();
        return await _inner.GetAllAsync();
    }

    public async Task<BacktestSummaryDto?> GetSummaryByIdAsync(long id)
    {
        Interlocked.Increment(ref _summaryCalls);
        await _gate;
        ThrowIfFailing();
        return await _inner.GetSummaryByIdAsync(id);
    }

    public async Task<IEnumerable<TradeDto>> GetTradesAsync(long backtestId)
    {
        Interlocked.Increment(ref _tradesCalls);
        await _gate;
        ThrowIfFailing();
        return await _inner.GetTradesAsync(backtestId);
    }

    public async Task<IEnumerable<EquityPointDto>> GetEquityCurveAsync(long backtestId)
    {
        Interlocked.Increment(ref _equityCalls);
        await _gate;
        ThrowIfFailing();
        return await _inner.GetEquityCurveAsync(backtestId);
    }
}

public class CachedBacktestRepositoryTests
{
    private static BacktestSummaryDto MakeSummary(long id, string strategyName = "ma_crossover") =>
        new(id, 1, strategyName, 1000m, 1200m, 0.2m, new DateTime(2024, 1, 1), new DateTime(2024, 1, 10));

    [Fact]
    public async Task GetSummaryByIdAsync_SecondCallForSameId_DoesNotHitInnerRepository()
    {
        var inner = new InstrumentedBacktestRepository();
        inner.Seed(MakeSummary(1));
        var cache = new CachedBacktestRepository(inner);

        var first = await cache.GetSummaryByIdAsync(1);
        var second = await cache.GetSummaryByIdAsync(1);

        Assert.NotNull(first);
        Assert.Equal(first, second);
        Assert.Equal(1, inner.SummaryCalls);
    }

    [Fact]
    public async Task GetTradesAsync_SecondCallForSameId_DoesNotHitInnerRepository()
    {
        var inner = new InstrumentedBacktestRepository();
        inner.Seed(MakeSummary(1));
        inner.SeedTrade(1, new TradeDto(new DateTime(2024, 1, 2), "BUY", 10m, 100m, 1m, 0m));
        var cache = new CachedBacktestRepository(inner);

        var first = (await cache.GetTradesAsync(1)).ToList();
        var second = (await cache.GetTradesAsync(1)).ToList();

        Assert.Single(first);
        Assert.Equal(first, second);
        Assert.Equal(1, inner.TradesCalls);
    }

    [Fact]
    public async Task GetEquityCurveAsync_SecondCallForSameId_DoesNotHitInnerRepository()
    {
        var inner = new InstrumentedBacktestRepository();
        inner.Seed(MakeSummary(1));
        inner.SeedEquityPoint(1, new EquityPointDto(new DateTime(2024, 1, 1), 1000m));
        var cache = new CachedBacktestRepository(inner);

        var first = (await cache.GetEquityCurveAsync(1)).ToList();
        var second = (await cache.GetEquityCurveAsync(1)).ToList();

        Assert.Single(first);
        Assert.Equal(first, second);
        Assert.Equal(1, inner.EquityCalls);
    }

    [Fact]
    public async Task GetAllAsync_IsNotCached()
    {
        // new backtests keep arriving from the Python writer, so a cached list would go stale
        var inner = new InstrumentedBacktestRepository();
        inner.Seed(MakeSummary(1));
        var cache = new CachedBacktestRepository(inner);

        await cache.GetAllAsync();
        await cache.GetAllAsync();

        Assert.Equal(2, inner.AllCalls);
    }

    [Fact]
    public async Task DifferentIds_AreCachedIndependently()
    {
        var inner = new InstrumentedBacktestRepository();
        inner.Seed(MakeSummary(1, strategyName: "ma_crossover"));
        inner.Seed(MakeSummary(2, strategyName: "mean_reversion"));
        var cache = new CachedBacktestRepository(inner);

        var one = await cache.GetSummaryByIdAsync(1);
        var two = await cache.GetSummaryByIdAsync(2);
        var oneAgain = await cache.GetSummaryByIdAsync(1);
        var twoAgain = await cache.GetSummaryByIdAsync(2);

        Assert.Equal("ma_crossover", one!.StrategyName);
        Assert.Equal("mean_reversion", two!.StrategyName);
        Assert.Equal("ma_crossover", oneAgain!.StrategyName);
        Assert.Equal("mean_reversion", twoAgain!.StrategyName);
        Assert.Equal(2, inner.SummaryCalls);
    }

    [Fact]
    public async Task ConcurrentCallersForSameId_ShareOneInnerCall()
    {
        var inner = new InstrumentedBacktestRepository();
        inner.Seed(MakeSummary(1));
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        inner.BlockUntil(release.Task);
        var cache = new CachedBacktestRepository(inner);

        var callers = Enumerable.Range(0, 50)
            .Select(_ => Task.Run(() => cache.GetSummaryByIdAsync(1)))
            .ToArray();

        // hold the first load in flight so all 50 callers reach the cache before it completes
        await Task.Delay(200);
        release.SetResult();
        var results = await Task.WhenAll(callers);

        Assert.Equal(1, inner.SummaryCalls);
        Assert.All(results, r => Assert.Equal(1, r!.Id));
    }

    [Fact]
    public async Task NotFound_IsNotCached_SoALaterWriteBecomesVisible()
    {
        var inner = new InstrumentedBacktestRepository();
        var cache = new CachedBacktestRepository(inner);

        Assert.Null(await cache.GetSummaryByIdAsync(7));

        inner.Seed(MakeSummary(7)); // Python writes the backtest after the first miss
        var result = await cache.GetSummaryByIdAsync(7);

        Assert.NotNull(result);
        Assert.Equal(2, inner.SummaryCalls);
    }

    [Fact]
    public async Task FailedLoad_IsNotCached_SoTheNextCallRetries()
    {
        var inner = new InstrumentedBacktestRepository();
        inner.Seed(MakeSummary(1));
        inner.FailNext(1);
        var cache = new CachedBacktestRepository(inner);

        await Assert.ThrowsAsync<InvalidOperationException>(() => cache.GetSummaryByIdAsync(1));
        var result = await cache.GetSummaryByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(2, inner.SummaryCalls);
    }
}

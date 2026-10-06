using QuantLab.Api.Dtos;
using System.Collections.Concurrent;

namespace QuantLab.Api.Data;

public class CachedBacktestRepository : IBacktestRepository
{
    private readonly IBacktestRepository _backtestRepository; 
    private readonly ConcurrentDictionary<long, Lazy<Task<BacktestSummaryDto?>>> _cacheSummary = new ConcurrentDictionary<long, Lazy<Task<BacktestSummaryDto?>>>();
    private readonly ConcurrentDictionary<long, Lazy<Task<IEnumerable<EquityPointDto>>>> _cacheEC = new ConcurrentDictionary<long, Lazy<Task<IEnumerable<EquityPointDto>>>>();
    private readonly ConcurrentDictionary<long, Lazy<Task<IEnumerable<TradeDto>>>> _cacheTrades = new ConcurrentDictionary<long, Lazy<Task<IEnumerable<TradeDto>>>>();

    public CachedBacktestRepository(IBacktestRepository backtestRepository)
    {
        _backtestRepository = backtestRepository;
    }
    public Task<IEnumerable<BacktestSummaryDto>> GetAllAsync()
    {
        return _backtestRepository.GetAllAsync();
    }

    public Task<IEnumerable<EquityPointDto>> GetEquityCurveAsync(long backtestId)
    {
        var lazyEntry = _cacheEC.GetOrAdd(backtestId, backtestId =>
        {
            return new Lazy<Task<IEnumerable<EquityPointDto>>>(async ()=>
            {
                try
                {
                    return await _backtestRepository.GetEquityCurveAsync(backtestId);
                }
                catch(Exception)
                {
                    _cacheEC.TryRemove(backtestId, out _);
                    throw;
                }
            });
        });
        return lazyEntry.Value;
    }

    public async Task<BacktestSummaryDto?> GetSummaryByIdAsync(long backtestId)
    {
        var lazyEntry = _cacheSummary.GetOrAdd(backtestId, backtestId =>
        {
            return new Lazy<Task<BacktestSummaryDto?>>(async ()=>
            {
                try
                {
                    BacktestSummaryDto? summary = await _backtestRepository.GetSummaryByIdAsync(backtestId);
                    if (summary == null)
                    {
                        _cacheSummary.TryRemove(backtestId, out _);
                        return null;
                    }

                    return summary;
                }
                catch (Exception)
                {
                    _cacheSummary.TryRemove(backtestId, out _);
                    throw;
                }
            });
        });

        return await lazyEntry.Value;
    }

    public async Task<IEnumerable<TradeDto>> GetTradesAsync(long backtestId)
    {
        var lazyEntry = _cacheTrades.GetOrAdd(backtestId, backtestId =>
        {
            return new Lazy<Task<IEnumerable<TradeDto>>>(async ()=>
            {
                try
                {
                    return await _backtestRepository.GetTradesAsync(backtestId);
                }
                catch(Exception)
                {
                    _cacheTrades.TryRemove(backtestId, out _);
                    throw;
                }
            });
        });
        return await lazyEntry.Value;
    }
}
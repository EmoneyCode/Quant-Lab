using System;
using QuantLab.Api.Data;
using QuantLab.Api.Dtos;

namespace QuantLab.Api.Services;

public class BacktestService
{
    private readonly IBacktestRepository _backtestRepository;

    public BacktestService(IBacktestRepository backtestRepository)
    {
        _backtestRepository = backtestRepository;
    }

    public async Task<IEnumerable<BacktestSummaryDto>> GetBacktestsAsync()
    {
        return await _backtestRepository.GetAllAsync();
    }
    public async Task<BacktestDetailDto?> GetBacktestAsync(long id)
    {
        BacktestSummaryDto? summary = await _backtestRepository.GetSummaryByIdAsync(id);
        if (summary == null)
        {
            return null;
        }
        Task<IEnumerable<TradeDto>> tradesTask = _backtestRepository.GetTradesAsync(id);
        Task<IEnumerable<EquityPointDto>> equityTask = _backtestRepository.GetEquityCurveAsync(id);

        await Task.WhenAll(tradesTask,equityTask);
        IEnumerable<TradeDto> trades = await tradesTask;
        IEnumerable<EquityPointDto> equity = await equityTask;
        return new BacktestDetailDto(summary.Id, summary.AssetId, summary.StrategyName, summary.StartDate, summary.EndDate, summary.InitialCapital, summary.FinalEquity, summary.TotalReturn, trades, equity);
    }

}
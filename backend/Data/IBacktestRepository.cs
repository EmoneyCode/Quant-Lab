using QuantLab.Api.Dtos;

namespace QuantLab.Api.Data;
public interface IBacktestRepository
{
    public Task<BacktestSummaryDto?> GetSummaryByIdAsync(long backtestId);

    public Task<IEnumerable<BacktestSummaryDto>> GetAllAsync();

    public Task<IEnumerable<TradeDto>> GetTradesAsync(long backtestId);

    public Task<IEnumerable<EquityPointDto>> GetEquityCurveAsync(long backtestId);
}
namespace QuantLab.Api.Dtos;
public record BacktestDetailDto(long Id, long AssetId, string StrategyName,
    DateTime StartDate, DateTime EndDate, decimal InitialCapital, decimal FinalEquity,
    decimal TotalReturn, IEnumerable<TradeDto> Trades, IEnumerable<EquityPointDto> EquityCurve);
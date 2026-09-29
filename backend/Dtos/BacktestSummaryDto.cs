namespace QuantLab.Api.Dtos;

public record BacktestSummaryDto(long Id, long AssetId, string StrategyName, decimal InitialCapital, decimal FinalEquity, decimal TotalReturn, DateTime StartDate, DateTime EndDate);
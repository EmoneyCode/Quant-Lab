namespace QuantLab.Api.Dtos;

public record TradeDto(DateTime Timestamp, string Side, decimal Quantity, decimal ExecutionPrice, Decimal Commission, Decimal Slippage);
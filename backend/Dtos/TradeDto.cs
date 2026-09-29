namespace QuantLab.Api.Dtos;

public record TradeDto(DateTime Timestamp, string Side, decimal Quantity, decimal Execution_price, Decimal Commission, Decimal Slippage);
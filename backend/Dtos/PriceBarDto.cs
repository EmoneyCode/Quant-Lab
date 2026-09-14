namespace QuantLab.Api.Data;

public record PriceBarDto(DateTime Timestamp, decimal Open, decimal High, decimal Low, decimal Close, decimal Volume);
# QuantLab — Testing Strategy

## Python

Test data validation for missing timestamps, duplicates, negative prices, invalid OHLC relationships, negative volume, and missing columns.

Test indicators with deterministic datasets.

Example:

```text
prices = [1, 2, 3, 4, 5]
window = 3
expected SMA = 4
```

Test strategies for BUY, SELL, HOLD, and insufficient history.

Test the backtester for initial cash, buys, sells, commission, slippage, final equity, and trade recording.

## .NET

Test services, validation, not-found behavior, and API response contracts.

## Integration

Eventually test:

```text
API → Service → PostgreSQL
```

## Principle

A strategy producing impressive returns is not enough. Results should be reproducible, testable, explainable, and based on documented assumptions.

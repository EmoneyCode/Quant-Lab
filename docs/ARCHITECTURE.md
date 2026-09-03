# QuantLab — Architecture

```text
Historical Market Data
        |
        v
Python Data Pipeline
        |
   Validation
        |
        v
   PostgreSQL
      /   \\
     /     \\
    v       v
Quant/     ASP.NET
Backtest     API
 Engine       |
    |         v
    |      React UI
    v
Results/Risk
```

## Responsibilities

### Python

- ingestion
- cleaning
- validation
- indicators
- strategy logic
- backtesting
- statistics

### ASP.NET Core

- HTTP endpoints
- DTOs
- API validation
- application services
- persistence
- Swagger/OpenAPI

### PostgreSQL

- assets
- price bars
- strategies
- backtests
- trades
- performance results

### React

Eventually visualize prices, signals, equity curves, drawdowns, metrics, and trades.

## Rule

Keep quantitative calculations independent from HTTP code.

Prefer:

```text
Controller → Application Service → Quant/Backtest Engine → Data Access → PostgreSQL
```

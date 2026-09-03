# QuantLab — Quantitative Research & Backtesting Platform

A portfolio-grade platform for researching quantitative strategies, running reproducible backtests, and analyzing portfolio/risk performance.

## Starter Goal

Build a small end-to-end system:

1. Load historical OHLCV market data.
2. Validate and normalize it.
3. Store it in PostgreSQL.
4. Calculate quantitative indicators.
5. Run one simple strategy.
6. Backtest it with realistic assumptions.
7. Expose results through ASP.NET Core.
8. Test and document the system.

## Stack

- Python — data processing, indicators, research, backtesting
- C# / ASP.NET Core — REST API
- PostgreSQL — persistence
- React — dashboard, later
- Docker Compose — local infrastructure
- pytest — Python tests
- xUnit — .NET tests

## Repository Layout

```text
quantlab/
├── backend/
├── quant/
├── frontend/
├── tests/
├── data/
├── docker/
├── docs/
├── .env.example
├── .gitignore
└── README.md
```

## v0.1 Definition of Done

- [ ] Repository created
- [ ] Python environment works
- [ ] ASP.NET Core API starts
- [ ] PostgreSQL starts with Docker
- [ ] Database connection works
- [ ] Asset and PriceBar schema exists
- [ ] Sample market data imports
- [ ] Data validation works
- [ ] SMA works
- [ ] Moving-average crossover works
- [ ] Backtester tracks cash, positions, trades, and equity
- [ ] API exposes assets and prices
- [ ] Unit tests pass

## Do Not Build Yet

Avoid AI/ML prediction, live trading, broker integration, HFT, complex optimization, C++ rewrites, and a large frontend until the core pipeline is correct.

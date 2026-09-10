# QuantLab — LinkedIn Content & Concurrent Status Tracker

> Inject this file into future QuantLab conversations. Keep it synchronized with actual project progress. Never describe unfinished work as completed.

## Project
**QuantLab — Quantitative Research & Backtesting Platform**

Goal: build a portfolio-grade system combining software engineering, backend/API development, databases, data engineering, statistics, quantitative finance, testing, and performance engineering.

### Positioning
> **Software engineer who can build quantitative systems.**

Avoid presenting it as an AI trading bot or claiming profitability. Emphasize quantitative research, financial time-series data, backtesting, statistical analysis, execution modeling, risk, reproducibility, and engineering quality.

## Stack
- Python — data processing, indicators, research, backtesting
- C# / ASP.NET Core — REST API/backend
- PostgreSQL — market/backtest persistence
- Docker — infrastructure
- React — dashboard later
- pytest — Python tests
- xUnit — .NET tests
- C++ — optional later for performance

## Current Architecture
```text
Historical Market Data
        |
        v
Python Data Pipeline
        |
Validation / Normalization
        |
        v
PostgreSQL
        |
   +----+----+
   |         |
   v         v
Indicators  ASP.NET Core API
   |
   v
Strategy Engine
   |
   v
Backtesting Engine
   |
   v
Performance / Risk
```

## Roadmap Status

### Phase 0 — Setup
- [x] Git repository
- [x] Project structure
- [x] Python environment
- [x] .NET setup
- [x] Docker/PostgreSQL
- [x] Environment configuration
- [ ] CI/CD

### Phase 1 — Market Data
- [x] Asset model
- [x] PriceBar model
- [x] Database schema
- [x] Historical data ingestion
- [x] Data validation
- [x] Duplicate detection
- [x] Timestamp handling
- [x] Sample dataset

### Phase 2 — Quantitative Indicators — COMPLETE
- [x] SMA — implemented + tested (matches documented example, missing-until-window-fills, no-look-ahead)
- [x] EMA — implemented + tested (verified against hand-computed values; caught and fixed a real `com`/`span` positional-argument bug via verification, not just a passing run)
- [x] Returns — implemented + tested against hand-computed values
- [x] Rolling volatility — implemented + tested (std of returns, kept semantically distinct from price-level std)
- [x] Standard deviation — extracted as a shared `rolling_std` helper used by both volatility and z-score, so neither function's name misrepresents what it computes
- [x] Correlation — implemented + tested (perfectly inverse and perfectly matching series both verified)
- [x] Z-score — implemented + tested against a hand-computed linear-sequence example

All of Phase 2 backed by 12 indicator tests; 37 tests passing project-wide.

Potential later indicators:
- [ ] WMA
- [ ] RSI
- [ ] MACD
- [ ] ROC
- [ ] ATR
- [ ] Covariance

### Phase 3 — Strategy Engine
- [ ] Strategy abstraction
- [ ] BUY/SELL/HOLD signals
- [ ] Moving-average crossover
- [ ] Mean reversion
- [ ] Momentum
- [ ] Pairs trading

### Phase 4 — Backtesting
- [ ] Initial capital
- [ ] Cash accounting
- [ ] Positions
- [ ] Orders
- [ ] Simulated execution
- [ ] Commission
- [ ] Slippage
- [ ] Equity curve
- [ ] Trade history
- [ ] Look-ahead-bias protection

### Phase 5 — Risk
- [ ] Total return
- [ ] Annualized return
- [ ] Volatility
- [ ] Maximum drawdown
- [ ] Sharpe
- [ ] Sortino
- [ ] VaR
- [ ] CVaR

### Phase 6 — API
- [ ] Assets endpoints
- [ ] Price endpoints
- [ ] Strategy endpoints
- [ ] Backtest endpoints
- [ ] Results endpoints
- [ ] Swagger/OpenAPI

### Phase 7 — Dashboard
- [ ] Portfolio/equity chart
- [ ] Drawdown chart
- [ ] Candlestick chart
- [ ] Indicators
- [ ] Signals
- [ ] Performance summary
- [ ] Trade table

### Phase 8 — Advanced
- [ ] Multi-asset portfolio
- [ ] Pairs trading
- [ ] Portfolio optimization
- [ ] Paper trading
- [ ] Market microstructure
- [ ] Order-book simulation
- [ ] C++ backtester
- [ ] Python vs C++ benchmark
- [ ] Profiling/performance optimization

# LinkedIn Strategy

Do not repeatedly post generic updates like "I'm building a quant project." Turn meaningful milestones into technical demonstrations.

## Content pillars
1. **Engineering:** PostgreSQL, APIs, validation, testing, Docker, performance, architecture.
2. **Quant:** SMA, EMA, volatility, correlation, z-score, mean reversion, Sharpe, drawdown.
3. **Backtesting:** look-ahead bias, costs, slippage, execution timing, overfitting, reproducibility.
4. **Learning:** bugs, unexpected results, design decisions, lessons learned.

## Posting frequency
Target approximately **2–3 meaningful posts per week** during active development. Not every commit deserves a post.

# LinkedIn Post Queue

| Priority | Post | Trigger | Status |
|---|---|---|---|
| 1 | Market Data Pipeline | Phase 1 complete | READY |
| 2 | SMA from Scratch | SMA complete + tested | READY |
| 3 | SMA vs EMA | EMA complete + tested | READY |
| 4 | Returns + Volatility | Both complete | READY |
| 5 | Correlation + Z-score | Both complete | READY |
| 6 | Strategy Engine | First strategy works | WAITING |
| 7 | Backtesting Engine | First backtest works | WAITING |
| 8 | Look-Ahead Bias | Execution model implemented | WAITING |
| 9 | Transaction Costs / Slippage | Costs implemented | WAITING |
| 10 | Risk Metrics | Risk engine begins | WAITING |
| 11 | Full Architecture | Major components integrated | WAITING |
| 12 | Dashboard | Dashboard works | WAITING |
| 13 | Performance Engineering | Real benchmark exists | WAITING |
| 14 | C++ Benchmark | C++ implementation complete | FUTURE |

# Post Specifications

## Post 1 — Market Data Pipeline
**Status: READY**

Angle: "How I built the market-data layer for QuantLab."

Show:
- OHLCV data
- PostgreSQL schema
- validation
- duplicate detection
- timestamps
- ingestion flow

Strong hook:
> Before writing a trading strategy, I wanted to make sure I could trust the data feeding it.

## Post 2 — SMA from Scratch
**Status: READY** — SMA implementation and tests complete.

Cover:
- rolling window
- formula
- insufficient history
- implementation
- deterministic tests

Formula:
```text
SMA(t) = mean(Close[t-N+1 : t])
```

## Post 3 — SMA vs EMA
**Status: READY** — EMA complete and tested.

Explain equal weighting vs greater weight on recent observations. Do not claim EMA is inherently better.

Worth including honestly: the implementation bug caught during verification, where `.ewm(span, adjust=False)` silently passed `span` into pandas' *first* positional parameter (`com`, a different smoothing definition) instead — it never raised an error, just quietly computed the wrong curve. Caught only by checking output against a hand-computed value instead of trusting that it ran without error. This is a stronger, more credible engineering story than a clean success — it demonstrates verification discipline.

## Post 4 — Returns + Volatility
**Trigger:** returns and rolling volatility complete.

Discuss returns, rolling windows, standard deviation, annualization, and why volatility is not a complete definition of risk.

## Post 5 — Correlation + Z-score
**Trigger:** both complete.

Use this to transition toward statistical strategies and mean reversion. Explain limitations of correlation.

## Post 6 — Strategy Engine
**Trigger:** first strategy works.

Show:
```text
Indicator -> Signal -> BUY / SELL / HOLD
```
Explain actual strategy rules and assumptions.

## Post 7 — Backtesting Engine
**Trigger:** first complete backtest.

Show initial capital, positions, cash, trades, portfolio value, and equity curve.

## Post 8 — Look-Ahead Bias
**Trigger:** execution model exists.

Show:
```text
Incorrect:
Signal at T -> execute using information from T

More realistic:
Signal at T -> execute at T+1
```
Explain why this can materially distort results.

## Post 9 — Transaction Costs / Slippage
**Trigger:** costs implemented.

Compare strategy results before and after realistic trading costs. Use actual measurements only.

## Post 10 — Risk Metrics
**Trigger:** risk engine begins.

Cover return, volatility, max drawdown, Sharpe, and Sortino. Emphasize why one metric is insufficient.

## Post 11 — Full Architecture
**Trigger:** major components integrated.

Show:
```text
Data -> Indicators -> Strategy -> Backtester -> Risk -> API -> Dashboard
```
Discuss boundaries and design decisions.

## Post 12 — Dashboard
**Trigger:** dashboard functional.

Show equity curve, price chart, signals, performance metrics, and trades.

## Post 13 — Performance Engineering
**Trigger:** measurable optimization.

Use real before/after measurements such as runtime, memory, query performance, or throughput. Never invent numbers.

## Post 14 — C++ Benchmark
**Trigger:** C++ implementation complete.

Compare Python vs C++ on a defined dataset and methodology. Discuss tradeoffs, not just speed.

# What to Capture While Coding

```text
docs/linkedin/
├── screenshots/
├── charts/
├── diagrams/
├── benchmarks/
└── post-notes/
```

Capture:
- architecture diagrams
- database diagrams
- validation errors
- sample datasets
- indicator output
- charts
- tests
- debugging discoveries
- backtest equity curves
- trade logs
- transaction-cost comparisons
- benchmark results
- before/after improvements

# Rules for Every Future Update

1. Read **Current Status** first.
2. Treat confirmed completed items as authoritative.
3. Never mark unfinished work complete.
4. Identify the next meaningful LinkedIn milestone.
5. Tell me what evidence to capture.
6. Draft a post only when the milestone is sufficiently complete.
7. Keep the story aligned with actual code and results.
8. Never invent benchmarks, datasets, returns, or performance improvements.
9. Prefer technical posts that demonstrate engineering judgment.
10. Update the queue whenever a milestone changes.

# Concurrent Status

## Current Phase
`Phase 3 — Strategy Engine` (Phase 2 complete)

## Completed
- [x] SMA — implemented, verified against the documented example, passing pytest tests
- [x] EMA — implemented, verified against hand-computed values, passing pytest tests (includes a real caught-and-fixed bug)
- [x] Returns, rolling volatility, rolling std, rolling correlation, rolling z-score — all implemented, verified against hand-computed values, passing pytest tests
- [x] Phase 2 entirely complete: 12 indicator tests, 37 tests passing project-wide

## In Progress
- [ ] Nothing yet started on Phase 3

## Next
- [ ] Strategy abstraction (interface/base class for a strategy)
- [ ] BUY/SELL/HOLD signal generation
- [ ] Moving-average crossover strategy (short/long SMA cross)
- [ ] Capture evidence for Posts 2–5 (see below), draft and post them

## LinkedIn Opportunity
**Post:** #2 "SMA from Scratch", #3 "SMA vs EMA", #4 "Returns + Volatility", and #5 "Correlation + Z-score" — all READY now.

**Why it matters:** The entire indicator layer is done and tested. The EMA bug (a wrong-but-plausible silent miscalculation caught only through hand-verification, not by anything erroring out) is a genuinely strong engineering-judgment story — more credible than a clean success. Posts 4/5 demonstrate the same verification discipline applied consistently, not as a one-off.

**Capture:**
- Screenshot of `pytest tests/python/test_indicators.py -v` showing all 12 tests passing, and `pytest tests/python/ -v` showing all 37 project-wide
- The SMA/EMA formulas alongside the actual code (`quant/indicators.py`)
- A short before/after snippet of the EMA bug: `ewm(span, adjust=False)` (wrong) vs `ewm(span=span, adjust=False)` (correct), with the differing output values
- For Post 5: the `rolling_std` extraction — worth mentioning that `rolling_volatility` and `rolling_zscore` share one honestly-named primitive instead of duplicating `.rolling(window).std()` or reusing a mismatched name
- Optional: a small matplotlib chart of SMA and EMA plotted over the sample price series, to make the "equal weight vs recent-weighted" distinction visually obvious

**Status:** Draft / Ready / Posted → currently **Ready to draft**, none posted yet.

## Next Post
Once the first strategy (moving-average crossover) is implemented and tested, Post #6 becomes ready.

# Current Next Action

Build **Phase 3 — Strategy Engine**: a strategy abstraction, BUY/SELL/HOLD signal generation, and the moving-average crossover strategy from `docs/QUANT_ENGINE.md` (short_window=20, long_window=50 by default; short MA crosses above long MA → BUY, crosses below → SELL, otherwise HOLD).

When each piece is complete:
1. Verify against a hand-computed or clearly reasoned example before trusting the output (not just "it ran").
2. Add deterministic pytest tests.
3. Update this file's status.
4. Mark the relevant post READY.
5. Draft the LinkedIn post.
6. Move to the next piece of Phase 3.

# Future ChatGPT Prompt

After injecting this file, I can say:

> "Update my QuantLab status based on what I just completed, tell me whether it is LinkedIn-worthy, and tell me exactly what I should capture for the post."

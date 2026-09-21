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
- [ ] Strategy abstraction — deliberately deferred until a second strategy exists to compare against (avoids guessing at a shared interface prematurely)
- [x] BUY/SELL/HOLD signals — crossover-event detection (not continuous-state), verified against hand-traced examples
- [x] Moving-average crossover — implemented, tested (BUY/SELL fire once at the actual crossing bar, insufficient-history and exact-equality edge cases both verified)
- [ ] Mean reversion
- [ ] Momentum
- [ ] Pairs trading

### Phase 4 — Backtesting — COMPLETE
- [x] Initial capital
- [x] Cash accounting
- [x] Positions
- [x] Orders (all-in/all-out sizing, documented assumption)
- [x] Simulated execution — signal at bar T, executes at bar T+1's open
- [x] Commission — per-share rate + minimum (IBKR-style), reserved before position sizing so it can't drive cash negative
- [x] Slippage — buy price × (1 + slippage), sell price × (1 - slippage)
- [x] Equity curve — verified not to leak a signaled-but-not-yet-executed trade into the signal bar's own mark-to-market
- [x] Trade history — timestamp/side/quantity/execution_price/commission/slippage, verified against hand-traced examples
- [x] Look-ahead-bias protection

v1 backtester complete and tested (12 backtester tests, 55 passing project-wide). Several real bugs caught during development, all via hand-verification rather than anything erroring out: a bitwise `~positive` vs `not positive` typo in the strategy layer that silently fired BUY on every bar instead of once; an equity-curve timing leak where a signaled-but-not-yet-executed trade's effect showed up one bar too early; a regression back to executing at bar close instead of bar open when commission/slippage were added; a commission parameter that looked configurable but was silently ignored (hardcoded values used instead); and commission being able to drive cash negative because position sizing didn't reserve for it up front.

### Phase 5 — Risk
- [ ] Total return
- [ ] Annualized return
- [ ] Volatility
- [ ] Maximum drawdown
- [ ] Sharpe
- [ ] Sortino
- [ ] VaR
- [ ] CVaR

### Phase 6 — API — IN PROGRESS
- [x] Assets endpoints — GET /api/assets, GET /api/assets/{id} (404 when missing), full stack verified live against real Postgres data (not just tests in isolation)
- [x] Price endpoints — GET /api/assets/{id}/prices with from/to/limit filtering, 404 correctly distinguished from "asset exists but has no prices" (200 + empty list), verified live against real data (7 real price bars, correct ascending order, correct limit behavior)
- [ ] Strategy endpoints
- [ ] Backtest endpoints (blocked on a real design decision: no `backtests`/`trades` schema exists yet, and nothing persists a Python-computed backtest result to Postgres)
- [ ] Results endpoints
- [x] Swagger/OpenAPI — auto-generated, picks up new routes automatically

Built test-first (TDD): wrote failing xUnit tests for each layer (DTOs → IAssetRepository/AssetRepository via Dapper → AssetService → AssetsController) before any implementation existed, then implemented against them. 17 passing C# tests. Real repository-layer tests hit the live Postgres container directly, seeding and cleaning up their own test data rather than mocking the database.

Several real, non-obvious bugs caught building the Prices endpoint, all via hand-verification or live testing rather than the unit tests alone:
- A Dapper column-mapping gotcha (`asset_name` doesn't auto-map to a `Name` DTO property; needs an explicit SQL alias, or it silently leaves the property null instead of erroring)
- Postgres couldn't infer the type of a nullable filter parameter (`could not determine data type of parameter`) unless every textual occurrence was explicitly cast, not just one
- A DB-column-vs-DTO type mismatch (`numeric` column vs `int` property) that only a real Postgres round-trip could catch — the in-memory fake repository could never have caught it
- The subtlest one: the route template used `{id}` but the action parameter was named `assetId` — ASP.NET Core binds route values by name, so the mismatch silently left the parameter at its default value instead of erroring. Every request "worked" in the sense of returning *a* response, and the not-found test even passed, but only because every asset ID coincidentally produced the same wrong answer. Only caught by testing against a real asset that actually had data.

Chose Dapper over EF Core deliberately: consistent with keeping the schema itself ORM-agnostic (raw SQL migrations, since Python also writes to the same tables), and more aligned with the explicit-control-over-SQL expectations common at quant/trading shops versus a typical enterprise line-of-business app.

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
| 1 | Market Data Pipeline | Phase 1 complete | POSTED |
| 2 | SMA from Scratch | SMA complete + tested | POSTED |
| 3 | SMA vs EMA | EMA complete + tested | CONSOLIDATED — see note |
| 4 | Returns + Volatility | Both complete | CONSOLIDATED — see note |
| 5 | Correlation + Z-score | Both complete | CONSOLIDATED — see note |
| 6 | Strategy Engine | First strategy works | READY |
| 7 | Backtesting Engine | First backtest works | READY |
| 8 | Look-Ahead Bias | Execution model implemented | READY |
| 9 | Transaction Costs / Slippage | Costs implemented | READY |
| 10 | Risk Metrics | Risk engine begins | WAITING |
| 11 | Full Architecture | Major components integrated | WAITING |
| 12 | Dashboard | Dashboard works | WAITING |
| 13 | Performance Engineering | Real benchmark exists | WAITING |
| 14 | C++ Benchmark | C++ implementation complete | FUTURE |
| 15 | Backend/API (Dapper + TDD) | Assets endpoints working end-to-end | POSTED |
| 16 | The bug that passed its own test (route param naming) | Prices endpoint working end-to-end | POSTED |

# Post Specifications

## Post 1 — Market Data Pipeline
**Status: POSTED**

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
**Status: POSTED** — SMA implementation and tests complete.

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
`Phase 3 — Strategy Engine` (Phase 2 complete; moving-average crossover done)

## Completed
- [x] SMA — implemented, verified against the documented example, passing pytest tests
- [x] EMA — implemented, verified against hand-computed values, passing pytest tests (includes a real caught-and-fixed bug)
- [x] Returns, rolling volatility, rolling std, rolling correlation, rolling z-score — all implemented, verified against hand-computed values, passing pytest tests
- [x] Phase 2 entirely complete: 12 indicator tests, 37 tests passing project-wide
- [x] Moving-average crossover strategy — implemented, tested (6 tests): fires BUY/SELL exactly once at the actual crossing bar rather than continuously, correctly holds through insufficient history and exact-equality edge cases, preserves the original price index, no look-ahead
- [x] 43 tests passing project-wide

## In Progress
- [ ] Nothing yet started beyond the first strategy

## Next
- [ ] Decide whether to add mean reversion / momentum next, or move straight to Phase 4 (backtesting) — README's v0.1 Definition of Done only requires the crossover strategy, so this is optional scope
- [ ] Capture evidence for Post #6 (see below), draft and post it
- [ ] Post the consolidated Phase 2 wrap post (was #3/#4/#5, see note below) — drafted, not yet posted

## LinkedIn Opportunity
**Post:** #6 "Strategy Engine" is now READY — first strategy works and is tested.

**Note on #3/#4/#5:** these were consolidated into a single "Phase 2 wrap" post (EMA bug story, verification discipline across returns/correlation/z-score, and the `rolling_std` naming fix) instead of three separate posts, to avoid over-posting the same general topic back to back. That combined post is drafted and ready but not yet posted — post it before #6 so the chronology (indicators → strategy) reads naturally.

**Why #6 matters:** it's the first time indicators turn into an actual decision (BUY/SELL/HOLD), and the "detect the crossing bar, not the ongoing state" bug is a strong, concrete story — a `~positive` vs `not positive` typo that silently fired BUY on every bar instead of once, caught only by hand-tracing the expected output first.

**Capture:**
- Screenshot of `pytest tests/python/test_indicators.py -v` (12 passing) and `pytest tests/python/ -v` (43 passing project-wide)
- The `~positive` (wrong) vs `not positive` (correct) line, plus the before/after signal output showing repeated `BUY` vs a single correct one
- The small price-series table (price / short_sma / long_sma / diff / signal) that made the crossing bar visually obvious
- The SMA/EMA formulas alongside the actual code (`quant/indicators.py`), still usable if posting the consolidated Phase 2 wrap first

**Status:** Posts #1, #2, #15, and #16 **posted**. Phase 2 wrap (former #3/#4/#5), #6 (Strategy Engine), #7 (Backtesting Engine), #8 (Look-Ahead Bias), and #9 (Transaction Costs/Slippage) all **ready to draft/post**, not yet posted — space them roughly 2-3/week rather than batching.

## Next Post
Once Phase 4 (backtesting) produces a first working backtest, Post #7 becomes ready.

# Current Next Action

Decide next: add mean reversion/momentum to Phase 3 (optional, not required for v0.1), or move to **Phase 4 — Backtesting** (cash accounting, positions, simulated execution, commission/slippage, equity curve, look-ahead protection) per `docs/QUANT_ENGINE.md` and `docs/ROADMAP.md`. The `README.md` v0.1 Definition of Done only requires the crossover strategy, so backtesting is the more direct path to a complete end-to-end system.

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

# QuantLab — Session Handoff

Read this first in a new session, before touching code. It captures decisions and working style that aren't obvious from the code alone. For phase-by-phase completion status and LinkedIn post tracking, see `docs/linkedin/QuantLab_LinkedIn_Status_Tracker.md` — that file is the living source of truth for "what's done," kept in sync throughout. This doc is about *why* things are the way they are, and how this project gets worked on.

## Immediate next step

Build the **Python write path** for backtest persistence: a function/script that takes a `Backtester.run()` result (already exists, `quant/backtester.py`) and writes it into the `backtests`, `trades`, and `equity_curves` tables via `psycopg` — mirroring exactly how `quant/importer.py` already writes into `assets`/`price_bars`. This is the last missing piece before the C# API can serve real backtest data.

The schema for this is done and verified: `db/migrations/create_backtests.sql`, `create_trades.sql`, `create_equity_curves.sql` all exist, all have correct constraints, and `ON DELETE CASCADE` on the two child tables was proven live (deleted a backtest, confirmed its trades/equity_curve rows vanished automatically). All three tables are currently empty in the real DB — no data has been written by anything yet.

**Uncommitted at handoff time** — commit these before continuing: a rename (`create_equity_curve.sql` → `create_equity_curves.sql`, matching the pluralized convention every other table uses) and fixes to `create_trades.sql` (added `ON DELETE CASCADE`, renamed two constraints that were shadowing their own column names).

## Architectural decisions worth knowing the "why" of

1. **Python owns all quant computation; C# never does quant math.** The API only reads from Postgres and shapes DTOs. This is `docs/ARCHITECTURE.md`'s rule, applied consistently everywhere.
2. **Schema is raw SQL migrations (`db/migrations/*.sql`), not owned by any ORM.** Because Python writes directly to the same tables the C# API reads from, no single language's migration tooling should own the schema — it has to be the neutral, common layer both sides can read.
3. **C# uses Dapper, not EF Core**, for the same reason plus one more: explicit SQL control is a better fit for a quant/trading-shop positioning than an ORM that generates queries for you. This was a deliberate, defensible choice — a good interview answer, not just a preference.
4. **Backtest persistence: Python writes directly to `backtests`/`trades`/`equity_curves`, the same way it writes `assets`/`price_bars`.** The API stays purely read-only for backtests too (`GET /api/backtests`, `GET /api/backtests/{id}`) — no `POST /api/backtests` that triggers a live computation from HTTP. This was a deliberate reinterpretation of `docs/API.md`'s original spec, decided because C#-triggers-Python would introduce cross-process orchestration that doesn't exist anywhere else in the project.
5. **`equity_curves` is its own normalized table** (one row per bar per backtest), not a JSON blob column — consistent with how `price_bars` already works, same reasoning, same query tools.
6. **Backtester v1's documented assumptions** (all deliberate, not oversights): all-in/all-out position sizing (no partial sizing yet); a signal at bar T executes at bar T+1's open (look-ahead-bias protection); commission modeled as per-share rate + minimum fee (IBKR-style — more realistic than a flat percentage); slippage as a % applied against execution price.
7. **`ON DELETE CASCADE`** on `trades.backtest_id` / `equity_curves.backtest_id` (children are meaningless without their backtest) but explicitly **not** on `price_bars.asset_id` (deleting an asset should never silently wipe its price history). This asymmetry is deliberate — be able to explain it, don't "fix" it into consistency.

## How this project gets worked on (collaboration notes)

- The user drives C#/SQL more independently (their strong suit); Python/pandas got more scaffolding early since it was newer territory for them. Match the level of hand-holding to which side of the stack is in play.
- **API work follows TDD**: write failing tests first (defining the contract — DTOs, repository interface, service, controller), then implement against them. Continue this pattern for any new endpoints (Strategy, Backtest, Results).
- **Never trust "it works" — always re-verify independently**, by actually running tests/queries/the live server, not by reading code and reasoning about it. This caught real bugs repeatedly this session: a route/parameter name mismatch that made a `NotFound` test pass for completely the wrong reason (every request coincidentally hit the same wrong answer), a Dapper column-mapping silent-null bug, a Postgres parameter-type-inference failure, a DB-column-vs-DTO type mismatch. None of these were visible from reading the code alone.
- The user does not want heavy Socratic pre-implementation interrogation for territory they're already comfortable in (C#/SQL) — give a clear recommendation and move forward; reserve deeper back-and-forth for genuine open design forks (there have been several: Dapper vs EF Core, raw SQL vs EF migrations, equity curve as table vs blob, cascade vs restrict).
- **LinkedIn**: `docs/linkedin/QuantLab_LinkedIn_Status_Tracker.md` is the living tracker — keep it updated whenever a milestone completes or a post goes out. Positioning is "software engineer who can build quantitative systems"; never claim trading profitability; ground posts in real caught bugs, not manufactured drama; no em dashes in drafted post text (explicit preference); plain, slightly informal human tone, not polished "AI voice."
- A resume update (adding a QuantLab entry, removing an older project, fitting to one page) was done separately as a `.docx` file outside this repo — not part of the codebase, but part of the broader context for why this project exists (career fair prep, breaking into quant).

## Current state snapshot (verified at handoff time)

- **55 Python tests passing** (`pytest tests/python/`) — validation (25), indicators (12), strategy (6), backtester (12)
- **17 C# tests passing** (`dotnet test tests/dotnet/QuantLab.Api.Tests/`)
- **Schema**: `assets`, `price_bars`, `backtests`, `trades`, `equity_curves` — all live, all constraints verified, cascade delete proven
- **`quant/`**: `validation.py`, `importer.py`, `indicators.py`, `strategy.py`, `backtester.py`
- **`backend/`**: `Dtos/AssetDto.cs`, `Dtos/PriceBarDto.cs`, `Data/IAssetRepository.cs`, `Data/AssetRepository.cs`, `Data/DatabaseConfig.cs`, `Services/AssetService.cs`, `Controllers/AssetController.cs` (contains `AssetsController`, handles both Assets and Prices endpoints)
- **API endpoints live and working**: `GET /api/assets`, `GET /api/assets/{id}`, `GET /api/assets/{id}/prices?from=&to=&limit=`

## What's not started yet

- Python write-path for backtest persistence (see "Immediate next step" above)
- C# Strategy/Backtest/Results endpoints (Backtest endpoints depend on the write-path existing first)
- Mean reversion / momentum strategies (optional stretch, not required for the v0.1 bar in `README.md`)
- Risk metrics (Phase 5): total return, annualized return, Sharpe, Sortino, VaR, CVaR
- Dashboard (Phase 7)
- CI/CD (Phase 0 stretch item)

## Reference docs, all in `docs/` unless noted

`README.md` (repo root, v0.1 definition of done) · `ARCHITECTURE.md` · `DATABASE.md` (now has full specs for all 5 tables) · `QUANT_ENGINE.md` · `ROADMAP.md` · `API.md` · `SETUP.md` · `TESTING.md` · `TODO.md` · `linkedin/QuantLab_LinkedIn_Status_Tracker.md`

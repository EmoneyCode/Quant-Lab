# QuantLab — Database Design

## Assets

| Column | Type | Description |
|---|---|---|
| id | bigint | Primary key |
| symbol | varchar | Example: AAPL |
| name | varchar | Display name |
| exchange | varchar | Exchange |
| currency | varchar | Example: USD |

## PriceBars

| Column | Type | Description |
|---|---|---|
| id | bigint | Primary key |
| asset_id | bigint | Foreign key |
| timestamp | timestamptz | UTC timestamp |
| open | numeric | Open |
| high | numeric | High |
| low | numeric | Low |
| close | numeric | Close |
| volume | numeric | Volume |

Recommended constraint:

```text
UNIQUE(asset_id, timestamp)
```

## Backtests

| Column | Type | Description |
|---|---|---|
| id | bigint | Primary key |
| asset_id | bigint | Foreign key to Assets |
| strategy_name | varchar | Name of the strategy backtested |
| start_date | timestamptz | Start of the backtested date range |
| end_date | timestamptz | End of the backtested date range |
| initial_capital | numeric | Starting cash |
| final_equity | numeric | Ending portfolio value |
| total_return | numeric | (final_equity - initial_capital) / initial_capital |
| created_at | timestamptz | When the backtest was recorded (defaults to now()) |

Recommended constraints:

```text
CHECK (initial_capital > 0)
CHECK (final_equity >= 0)
CHECK (end_date >= start_date)
```

## Trades

| Column | Type | Description |
|---|---|---|
| id | bigint | Primary key |
| backtest_id | bigint | Foreign key to Backtests |
| timestamp | timestamptz | Execution time (bar T+1's open, per the look-ahead-bias convention) |
| side | varchar | BUY or SELL |
| quantity | numeric | Shares traded |
| execution_price | numeric | Price actually paid/received, including slippage |
| commission | numeric | Commission cost charged on this trade |
| slippage | numeric | Slippage rate applied to this trade's execution price |

Recommended constraints:

```text
CHECK (side IN ('BUY', 'SELL'))
CHECK (quantity > 0)
CHECK (execution_price > 0)
CHECK (commission >= 0)
CHECK (slippage >= 0)
```

`backtest_id` should cascade on delete — a trade has no meaning independent of the backtest that produced it, unlike `price_bars`, where deleting an asset should NOT silently wipe its historical prices.

## EquityCurve

| Column | Type | Description |
|---|---|---|
| id | bigint | Primary key |
| backtest_id | bigint | Foreign key to Backtests |
| timestamp | timestamptz | Bar timestamp |
| equity | numeric | Portfolio value at this bar (cash + position marked to market) |

Recommended constraint:

```text
UNIQUE(backtest_id, timestamp)
```

Same cascade reasoning as Trades: `backtest_id` should cascade on delete.

## Data Rules

- Prices must be positive.
- High >= Open and Close.
- Low <= Open and Close.
- Volume cannot be negative.
- Duplicate `(asset_id, timestamp)` rows are rejected.
- Store timestamps in UTC.
- A backtest's `end_date` must be >= its `start_date`.
- A backtest's `initial_capital` must be positive; `final_equity` cannot be negative.
- Duplicate `(backtest_id, timestamp)` rows in EquityCurve are rejected, same idempotency principle as PriceBars.
- Deleting a Backtest cascades to its Trades and EquityCurve rows; deleting an Asset does not cascade to its PriceBars.

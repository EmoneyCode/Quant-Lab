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

Store strategy name, asset, date range, initial capital, final equity, and total return.

## Trades

Store backtest ID, timestamp, side, quantity, execution price, commission, and slippage.

## Data Rules

- Prices must be positive.
- High >= Open and Close.
- Low <= Open and Close.
- Volume cannot be negative.
- Duplicate `(asset_id, timestamp)` rows are rejected.
- Store timestamps in UTC.

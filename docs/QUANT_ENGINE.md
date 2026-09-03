# Quant Engine — Starter Specification

## Indicators

Start with:

- SMA
- EMA
- Returns
- Rolling volatility
- Standard deviation
- Correlation
- Z-score

### SMA

```text
SMA(t) = mean(Close[t-N+1 : t])
```

Requirements: configurable window, no future data, missing output until enough history exists, and deterministic tests.

## Strategy

Start with moving-average crossover:

```text
short_window = 20
long_window = 50
```

```text
short MA crosses above long MA → BUY
short MA crosses below long MA → SELL
otherwise → HOLD
```

## Backtester

Minimum state:

```text
cash
position_quantity
entry_price
portfolio_value
```

For every bar:

1. Calculate indicators using available data.
2. Generate signal.
3. Execute according to the execution model.
4. Apply transaction costs.
5. Update position.
6. Mark portfolio to market.
7. Record equity.

## Look-Ahead Bias

Use a simple convention:

```text
Bar T:   calculate signal
Bar T+1: execute at open
```

Document this assumption in every backtest.

## Costs

Support commission and slippage. For example:

```text
buy price  = market price * (1 + slippage)
sell price = market price * (1 - slippage)
```

## Metrics

Eventually calculate total return, annualized return, trade count, win rate, volatility, maximum drawdown, Sharpe, and Sortino.

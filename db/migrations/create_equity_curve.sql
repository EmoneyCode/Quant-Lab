CREATE TABLE equity_curves(
    id BIGINT generated always as identity PRIMARY KEY,
    backtest_id BIGINT NOT NULL REFERENCES backtests(id),
    timestamp TIMESTAMPTZ NOT NULL,
    equity NUMERIC(19,2) NOT NULL,

    CONSTRAINT no_duplicates UNIQUE(backtest_id, timestamp)
);
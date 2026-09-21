CREATE TABLE trades (
    id BIGINT generated always as identity primary key,
    backtest_id BIGINT NOT NULL REFERENCES backtests(id) ON DELETE CASCADE,
    timestamp TIMESTAMPTZ not null,
    side varchar(10) not null,
    quantity NUMERIC(19,2) not null,
    execution_price NUMERIC(19,2) not null,
    commission NUMERIC(19,2) not null,
    slippage NUMERIC(19,2) not null,

    CONSTRAINT side_check CHECK(side IN('BUY', 'SELL')),
    CONSTRAINT quantity_check CHECK(quantity > 0),
    CONSTRAINT execution_check CHECK(execution_price > 0),
    CONSTRAINT commission_check CHECK(commission >= 0),
    CONSTRAINT slippage_check CHECK(slippage >= 0)
);
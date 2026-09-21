CREATE TABLE backtests (
    id BIGINT generated always as identity PRIMARY KEY,
    asset_id BIGINT not null REFERENCES assets(id),
    strategy_name VARCHAR(255) not null,
    start_date timestamptz not null,
    end_date timestamptz not null,
    initial_capital numeric(19,2) not null, 
    final_equity numeric(19,2) not null, 
    total_return numeric(19,2) NOT NULL,
    created_at timestamptz default now() not null,

    CONSTRAINT initial_positive CHECK(initial_capital>0),
    CONSTRAINT final_equity_non_negative CHECK(final_equity>=0),
    constraint dates_check check(end_date>=start_date)
);
create table price_bars (
    id bigint generated always as identity primary key,
    asset_id bigint not null references assets(id),
    timestamp TIMESTAMPTZ not null,
    open numeric(19,2) not null,
    high numeric(19,2)  not null,
    low numeric(19,2)  not null,
    close numeric(19,2)  not null,
    volume numeric(19,2)  not null,

    constraint price_positive check(high>0 and open>0 and low>0 and close>0),
    constraint high_greater_equal check(high>=open and high>=close),
    constraint low_greater_equal check(low<=open and low<=close),
    constraint chk_volume_positive check(volume>=0),
    constraint no_duplicate UNIQUE(asset_id, timestamp)

) ;
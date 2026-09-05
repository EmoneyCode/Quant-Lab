CREATE TABLE assets (
    id bigint generated always as identity primary key,
    symbol varchar(10) unique not null,
    asset_name varchar(255) not null,
    exchange varchar(255) not null,
    currency varchar(3) not null
) ;
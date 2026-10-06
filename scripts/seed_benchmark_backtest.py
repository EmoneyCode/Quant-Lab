"""Seed one large synthetic backtest so C# benchmarks have a realistic payload.

    .venv/bin/python scripts/seed_benchmark_backtest.py                  # 50,000 bars
    .venv/bin/python scripts/seed_benchmark_backtest.py --bars 200000
    .venv/bin/python scripts/seed_benchmark_backtest.py --cleanup        # remove everything it created

Everything is tagged (strategy_name "benchmark_seed", asset symbol "BENCH") so cleanup
never touches real data. Prices are a seeded random walk, so runs are reproducible.
"""
import argparse
import os
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import numpy as np
import pandas as pd
import psycopg

from quant.backtester import Backtester
from quant.persistence import save_backtest

DATABASE_URL = os.environ.get("DATABASE_URL", "postgresql://quantlab:change_me@localhost:5432/quantlab")
STRATEGY_NAME = "benchmark_seed"
SYMBOL = "BENCH"
INITIAL_CAPITAL = 100_000


def get_or_create_asset(conn) -> int:
    with conn.cursor() as cur:
        cur.execute(
            """INSERT INTO assets (symbol, asset_name, exchange, currency)
               VALUES (%s, 'Benchmark Asset', 'TEST', 'USD')
               ON CONFLICT (symbol) DO NOTHING""",
            (SYMBOL,),
        )
        cur.execute("SELECT id FROM assets WHERE symbol = %s", (SYMBOL,))
        asset_id = cur.fetchone()[0]
    conn.commit()
    return asset_id


def build_bars(bars: int) -> pd.DataFrame:
    rng = np.random.default_rng(42)
    close = np.round(100 * np.exp(np.cumsum(rng.normal(0, 0.001, bars))), 2)
    open_ = np.concatenate([[100.0], close[:-1]])
    return pd.DataFrame({
        "timestamp": pd.date_range("2024-01-01", periods=bars, freq="min", tz="UTC"),
        "open": open_,
        "close": close,
    })


def build_signals(bars: int, trade_every: int) -> pd.Series:
    signals = ["HOLD"] * bars
    for n, i in enumerate(range(trade_every, bars, trade_every)):
        signals[i] = "BUY" if n % 2 == 0 else "SELL"
    return pd.Series(signals)


def seed(conn, bars: int, trade_every: int) -> None:
    asset_id = get_or_create_asset(conn)
    df = build_bars(bars)
    backtester = Backtester(INITIAL_CAPITAL, slippage=0.0005, commission_per_share=0.005, commission_minimum=1.0)
    result = backtester.run(df, build_signals(bars, trade_every))
    summary = save_backtest(conn, asset_id, STRATEGY_NAME, INITIAL_CAPITAL, df, result)
    print(f"seeded: {summary}")


def cleanup(conn) -> None:
    with conn.cursor() as cur:
        cur.execute("DELETE FROM backtests WHERE strategy_name = %s", (STRATEGY_NAME,))
        removed_backtests = cur.rowcount
        cur.execute("DELETE FROM assets WHERE symbol = %s", (SYMBOL,))
        removed_assets = cur.rowcount
    conn.commit()
    print(f"removed {removed_backtests} backtest(s) and {removed_assets} asset(s)")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--bars", type=int, default=50_000, help="equity curve length (default 50000)")
    parser.add_argument("--trade-every", type=int, default=250, help="bars between BUY/SELL signals (default 250)")
    parser.add_argument("--cleanup", action="store_true", help="delete the seeded backtests and asset, then exit")
    args = parser.parse_args()

    with psycopg.connect(DATABASE_URL) as conn:
        if args.cleanup:
            cleanup(conn)
        else:
            seed(conn, args.bars, args.trade_every)

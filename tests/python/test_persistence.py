"""
Contract for quant/persistence.py (not yet written):

    def save_backtest(conn, asset_id: int, strategy_name: str, initial_capital: float,
                       df: pd.DataFrame, backtest_result: dict) -> dict:
        ...

`backtest_result` is exactly what `Backtester.run(df, signals)` returns:
{"equity_curve": pd.Series, "trades": list[dict], "final_equity": float}.

`df` is the same OHLC dataframe passed into `run()` -- it's needed here because
`backtest_result["equity_curve"]` is indexed positionally (0..n-1), not by
timestamp, so the timestamp for each equity_curve row has to come from
`df["timestamp"]` at that same position.

Returns a summary dict: {"backtest_id": int, "trades_inserted": int, "equity_curve_inserted": int}.

Writes must be atomic: either the backtest + all its trades + all its equity_curve
rows land, or none of them do.
"""
import pandas as pd
import pytest

from quant.backtester import Backtester
from quant.persistence import save_backtest


def make_df(opens, closes, start="2024-01-01"):
    return pd.DataFrame({
        "timestamp": pd.date_range(start, periods=len(opens), tz="UTC"),
        "open": opens,
        "close": closes,
    })


@pytest.fixture
def save_backtest_tracked(db_conn):
    created_ids = []

    def _save(*args, **kwargs):
        result = save_backtest(db_conn, *args, **kwargs)
        created_ids.append(result["backtest_id"])
        return result

    yield _save

    with db_conn.cursor() as cur:
        for backtest_id in created_ids:
            cur.execute("DELETE FROM backtests WHERE id = %s", (backtest_id,))
    db_conn.commit()


def run_backtest(opens, closes, signals, initial_capital=1000):
    df = make_df(opens, closes)
    bt = Backtester(initial_capital)
    result = bt.run(df, pd.Series(signals))
    return df, result


def test_writes_backtest_row_with_correct_values(db_conn, asset_id, save_backtest_tracked):
    df, result = run_backtest(
        opens=[10, 11, 9, 12, 15, 14],
        closes=[10, 11, 9, 12, 15, 14],
        signals=["HOLD", "HOLD", "HOLD", "HOLD", "BUY", "HOLD"],
    )

    summary = save_backtest_tracked(asset_id, "ma_crossover", 1000, df, result)

    with db_conn.cursor() as cur:
        cur.execute(
            """SELECT asset_id, strategy_name, start_date, end_date, initial_capital, final_equity, total_return
               FROM backtests WHERE id = %s""",
            (summary["backtest_id"],),
        )
        row = cur.fetchone()

    assert row[0] == asset_id
    assert row[1] == "ma_crossover"
    assert row[2] == df["timestamp"].iloc[0]
    assert row[3] == df["timestamp"].iloc[-1]
    assert float(row[4]) == 1000
    assert float(row[5]) == result["final_equity"]
    expected_return = (result["final_equity"] - 1000) / 1000
    assert float(row[6]) == pytest.approx(expected_return, abs=0.01)


def test_writes_one_trade_row_per_trade_with_matching_fields(db_conn, asset_id, save_backtest_tracked):
    df, result = run_backtest(
        opens=[10, 11, 9, 12, 15, 14, 20, 15, 8, 5, 3],
        closes=[10, 11, 9, 12, 15, 14, 20, 15, 8, 5, 3],
        signals=["HOLD", "HOLD", "HOLD", "HOLD", "BUY", "HOLD", "HOLD", "HOLD", "SELL", "HOLD", "HOLD"],
    )
    assert len(result["trades"]) == 2  # sanity check on the fixture data itself

    summary = save_backtest_tracked(asset_id, "ma_crossover", 1000, df, result)
    assert summary["trades_inserted"] == 2

    with db_conn.cursor() as cur:
        cur.execute(
            "SELECT side, quantity, execution_price, commission, slippage FROM trades WHERE backtest_id = %s ORDER BY timestamp",
            (summary["backtest_id"],),
        )
        rows = cur.fetchall()

    assert len(rows) == 2
    for row, trade in zip(rows, result["trades"]):
        assert row[0] == trade["side"]
        assert float(row[1]) == trade["quantity"]
        assert float(row[2]) == trade["execution_price"]
        assert float(row[3]) == trade["commission"]
        assert float(row[4]) == trade["slippage"]


def test_writes_no_trade_rows_when_backtest_has_no_trades(db_conn, asset_id, save_backtest_tracked):
    df, result = run_backtest(
        opens=[10, 11, 9, 12],
        closes=[10, 11, 9, 12],
        signals=["HOLD", "HOLD", "HOLD", "HOLD"],
    )
    assert result["trades"] == []  # sanity check

    summary = save_backtest_tracked(asset_id, "ma_crossover", 1000, df, result)
    assert summary["trades_inserted"] == 0

    with db_conn.cursor() as cur:
        cur.execute("SELECT count(*) FROM trades WHERE backtest_id = %s", (summary["backtest_id"],))
        assert cur.fetchone()[0] == 0


def test_writes_one_equity_curve_row_per_bar_with_correct_timestamp_and_value(db_conn, asset_id, save_backtest_tracked):
    df, result = run_backtest(
        opens=[10, 11, 9, 12],
        closes=[10, 11, 9, 12],
        signals=["HOLD", "HOLD", "HOLD", "HOLD"],
    )

    summary = save_backtest_tracked(asset_id, "ma_crossover", 1000, df, result)
    assert summary["equity_curve_inserted"] == len(df)

    with db_conn.cursor() as cur:
        cur.execute(
            "SELECT timestamp, equity FROM equity_curves WHERE backtest_id = %s ORDER BY timestamp",
            (summary["backtest_id"],),
        )
        rows = cur.fetchall()

    assert len(rows) == len(df)
    for row, expected_ts, expected_equity in zip(rows, df["timestamp"], result["equity_curve"]):
        assert row[0] == expected_ts
        assert float(row[1]) == expected_equity


def test_returns_new_backtest_id_each_call(db_conn, asset_id, save_backtest_tracked):
    df, result = run_backtest(
        opens=[10, 11, 9, 12],
        closes=[10, 11, 9, 12],
        signals=["HOLD", "HOLD", "HOLD", "HOLD"],
    )

    first = save_backtest_tracked(asset_id, "ma_crossover", 1000, df, result)
    second = save_backtest_tracked(asset_id, "ma_crossover", 1000, df, result)

    assert first["backtest_id"] != second["backtest_id"]


def test_deleting_backtest_cascades_to_trades_and_equity_curves(db_conn, asset_id, save_backtest_tracked):
    df, result = run_backtest(
        opens=[10, 11, 9, 12, 15, 14],
        closes=[10, 11, 9, 12, 15, 14],
        signals=["HOLD", "HOLD", "HOLD", "HOLD", "BUY", "HOLD"],
    )

    summary = save_backtest_tracked(asset_id, "ma_crossover", 1000, df, result)
    backtest_id = summary["backtest_id"]

    with db_conn.cursor() as cur:
        cur.execute("DELETE FROM backtests WHERE id = %s", (backtest_id,))
        db_conn.commit()

        cur.execute("SELECT count(*) FROM trades WHERE backtest_id = %s", (backtest_id,))
        assert cur.fetchone()[0] == 0

        cur.execute("SELECT count(*) FROM equity_curves WHERE backtest_id = %s", (backtest_id,))
        assert cur.fetchone()[0] == 0

import pandas as pd
import pytest

from quant.backtester import Backtester


def make_df(opens, closes, start="2024-01-01"):
    return pd.DataFrame({
        "timestamp": pd.date_range(start, periods=len(opens)),
        "open": opens,
        "close": closes,
    })


def test_buy_executes_at_next_bar_open_not_signal_bar_price():
    df = make_df(
        opens=[10, 11, 9, 12, 15, 14],
        closes=[10, 11, 9, 12, 15, 14],
    )
    signals = pd.Series(["HOLD", "HOLD", "HOLD", "HOLD", "BUY", "HOLD"])
    result = Backtester(1000).run(df, signals)

    assert len(result["trades"]) == 1
    trade = result["trades"][0]
    assert trade["side"] == "BUY"
    assert trade["execution_price"] == 14  # bar 5's open, not bar 4's close of 15
    assert trade["timestamp"] == df["timestamp"].iloc[5]
    assert trade["quantity"] == 1000 // 14


def test_sell_executes_at_next_bar_open_and_records_correct_quantity():
    df = make_df(
        opens=[10, 11, 9, 12, 15, 14, 20, 15, 8, 5, 3],
        closes=[10, 11, 9, 12, 15, 14, 20, 15, 8, 5, 3],
    )
    signals = pd.Series(["HOLD", "HOLD", "HOLD", "HOLD", "BUY", "HOLD", "HOLD", "HOLD", "SELL", "HOLD", "HOLD"])
    result = Backtester(1000).run(df, signals)

    assert len(result["trades"]) == 2
    sell = result["trades"][1]
    assert sell["side"] == "SELL"
    assert sell["execution_price"] == 5  # bar 9's open
    assert sell["quantity"] == 71  # must NOT be 0 -- regression guard for the reset-before-record bug
    assert result["final_equity"] == 361


def test_equity_curve_length_matches_input_length():
    df = make_df(opens=[10, 11, 9, 12], closes=[10, 11, 9, 12])
    signals = pd.Series(["HOLD", "HOLD", "HOLD", "HOLD"])
    result = Backtester(1000).run(df, signals)
    assert len(result["equity_curve"]) == len(df)


def test_signaled_but_not_yet_executed_trade_does_not_leak_into_current_bar_equity():
    # bar 4's close is deliberately absurd -- if the not-yet-executed BUY leaked
    # into this bar's equity, it would be wildly wrong instead of unchanged cash
    df = make_df(
        opens=[10, 10, 10, 10, 10, 14],
        closes=[10, 10, 10, 10, 1000, 14],
    )
    signals = pd.Series(["HOLD", "HOLD", "HOLD", "HOLD", "BUY", "HOLD"])
    result = Backtester(1000).run(df, signals)
    assert result["equity_curve"].iloc[4] == 1000


def test_equity_reflects_new_position_starting_the_execution_bar():
    df = make_df(
        opens=[10, 10, 10, 10, 10, 14],
        closes=[10, 10, 10, 10, 1000, 14],
    )
    signals = pd.Series(["HOLD", "HOLD", "HOLD", "HOLD", "BUY", "HOLD"])
    result = Backtester(1000).run(df, signals)
    # bought 1000 // 14 = 71 shares at 14, 6 cash left, marked at bar 5's own close of 14
    assert result["equity_curve"].iloc[5] == 6 + 71 * 14


def test_no_trade_when_no_signal_fires():
    df = make_df(opens=[10, 11, 9, 12], closes=[10, 11, 9, 12])
    signals = pd.Series(["HOLD", "HOLD", "HOLD", "HOLD"])
    result = Backtester(1000).run(df, signals)
    assert result["trades"] == []
    assert result["final_equity"] == 1000
    assert result["equity_curve"].tolist() == [1000, 1000, 1000, 1000]


def test_initial_capital_is_not_mutated_across_runs():
    df = make_df(opens=[10, 11, 9, 12, 15, 14], closes=[10, 11, 9, 12, 15, 14])
    signals = pd.Series(["HOLD", "HOLD", "HOLD", "HOLD", "BUY", "HOLD"])

    bt = Backtester(1000)
    bt.run(df, signals)
    assert bt.initial_capital == 1000  # must still be the original value after run()

    # running again from the same instance must start fresh, not from leftover cash
    second_result = bt.run(df, signals)
    assert second_result["final_equity"] == bt.run(df, signals)["final_equity"]


def test_slippage_raises_buy_price_and_lowers_sell_price():
    df = make_df(
        opens=[10, 11, 9, 12, 15, 100, 20, 15, 8, 100, 3],
        closes=[10, 11, 9, 12, 15, 100, 20, 15, 8, 100, 3],
    )
    signals = pd.Series(["HOLD", "HOLD", "HOLD", "HOLD", "BUY", "HOLD", "HOLD", "HOLD", "SELL", "HOLD", "HOLD"])
    result = Backtester(1000, slippage=0.01).run(df, signals)

    buy, sell = result["trades"]
    assert buy["execution_price"] == pytest.approx(100 * 1.01)
    assert sell["execution_price"] == pytest.approx(100 * 0.99)


def test_commission_per_share_and_minimum_are_both_configurable():
    df = make_df(opens=[10, 11, 9, 12, 15, 14], closes=[10, 11, 9, 12, 15, 14])
    signals = pd.Series(["HOLD", "HOLD", "HOLD", "HOLD", "BUY", "HOLD"])

    cheap = Backtester(1000, commission_per_share=0.0, commission_minimum=0.0).run(df, signals)
    pricey = Backtester(1000, commission_per_share=0.0, commission_minimum=50.0).run(df, signals)
    assert cheap["final_equity"] != pricey["final_equity"]


def test_commission_cannot_drive_cash_negative_on_exact_division():
    # cash=1000, price=10 divides evenly, leaving nothing before the minimum
    # commission is applied -- must reserve for it before sizing the position
    df = make_df(opens=[10, 10, 10], closes=[10, 10, 10])
    signals = pd.Series(["HOLD", "BUY", "HOLD"])
    result = Backtester(1000, commission_per_share=0.005, commission_minimum=1.0).run(df, signals)
    assert result["final_equity"] >= 0


def test_trade_records_include_commission_and_slippage():
    df = make_df(opens=[10, 11, 12], closes=[10, 11, 12])
    signals = pd.Series(["HOLD", "BUY", "HOLD"])
    result = Backtester(1000, slippage=0.01, commission_per_share=0.005, commission_minimum=1.0).run(df, signals)
    trade = result["trades"][0]
    assert "commission" in trade
    assert "slippage" in trade
    assert trade["commission"] > 0
    assert trade["slippage"] == 0.01


def test_zero_cost_defaults_reproduce_original_hand_traced_result():
    # regression guard: adding commission/slippage must not change behavior
    # when both default to zero
    df = make_df(
        opens=[10, 11, 9, 12, 15, 14, 20, 15, 8, 5, 3],
        closes=[10, 11, 9, 12, 15, 14, 20, 15, 8, 5, 3],
    )
    signals = pd.Series(["HOLD", "HOLD", "HOLD", "HOLD", "BUY", "HOLD", "HOLD", "HOLD", "SELL", "HOLD", "HOLD"])
    result = Backtester(1000).run(df, signals)
    assert result["final_equity"] == 361

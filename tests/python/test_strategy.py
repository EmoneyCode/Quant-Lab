import pandas as pd

from quant.strategy import moving_average_crossover


def test_buy_fires_once_at_the_crossing_bar_not_every_bar_above():
    prices = pd.Series([10, 11, 9, 12, 15, 14, 20])
    signals = moving_average_crossover(prices, short_window=2, long_window=3)
    assert signals.tolist() == ["HOLD", "HOLD", "HOLD", "HOLD", "BUY", "HOLD", "HOLD"]


def test_sell_fires_once_when_crossing_back_down():
    prices = pd.Series([10, 11, 9, 12, 15, 14, 20, 15, 8, 5, 3])
    signals = moving_average_crossover(prices, short_window=2, long_window=3)
    assert signals.tolist() == [
        "HOLD", "HOLD", "HOLD", "HOLD", "BUY", "HOLD",
        "HOLD", "HOLD", "SELL", "HOLD", "HOLD",
    ]


def test_hold_during_insufficient_history():
    prices = pd.Series([10, 11, 9])
    signals = moving_average_crossover(prices, short_window=2, long_window=3)
    # long_sma has no value until index 2, so nothing before that can signal
    assert signals.iloc[:2].tolist() == ["HOLD", "HOLD"]


def test_exact_equality_does_not_misfire_either_signal():
    # short_sma == long_sma exactly (diff == 0.0) must resolve to HOLD,
    # not be mistaken for a cross in either direction
    prices = pd.Series([10, 11, 9, 12])
    signals = moving_average_crossover(prices, short_window=2, long_window=3)
    assert signals.iloc[2] == "HOLD"


def test_signal_index_matches_price_index():
    prices = pd.Series([10, 11, 9, 12, 15], index=pd.date_range("2024-01-01", periods=5))
    signals = moving_average_crossover(prices, short_window=2, long_window=3)
    assert list(signals.index) == list(prices.index)


def test_no_look_ahead():
    prices = pd.Series([10, 11, 9, 12, 15])
    signal_before = moving_average_crossover(prices, short_window=2, long_window=3).iloc[3]

    prices_changed_future = pd.Series([10, 11, 9, 12, 999])
    signal_after = moving_average_crossover(prices_changed_future, short_window=2, long_window=3).iloc[3]

    assert signal_before == signal_after

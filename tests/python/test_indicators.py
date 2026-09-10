import pandas as pd
import pytest

from quant.indicators import (
    ema,
    returns,
    rolling_correlation,
    rolling_std,
    rolling_volatility,
    rolling_zscore,
    sma,
)


def test_sma_matches_documented_example():
    prices = pd.Series([1, 2, 3, 4, 5])
    result = sma(prices, window=3)
    assert result.iloc[-1] == 4


def test_sma_missing_until_window_fills():
    prices = pd.Series([1, 2, 3, 4, 5])
    result = sma(prices, window=3)
    assert result.iloc[:2].isna().all()
    assert result.iloc[2:].notna().all()


def test_sma_has_no_look_ahead():
    # changing a future value must not change an already-computed SMA
    prices = pd.Series([1, 2, 3, 4, 5])
    result_before = sma(prices, window=3).iloc[2]

    prices_changed_future = pd.Series([1, 2, 3, 999, 999])
    result_after = sma(prices_changed_future, window=3).iloc[2]

    assert result_before == result_after


def test_ema_matches_hand_computed_values():
    # span=3, adjust=False -> alpha = 2/(span+1) = 0.5
    # EMA[0]=1, EMA[1]=0.5*2+0.5*1=1.5, EMA[2]=0.5*3+0.5*1.5=2.25,
    # EMA[3]=0.5*4+0.5*2.25=3.125, EMA[4]=0.5*5+0.5*3.125=4.0625
    prices = pd.Series([1, 2, 3, 4, 5])
    result = ema(prices, span=3)
    expected = [1.0, 1.5, 2.25, 3.125, 4.0625]
    for actual, exp in zip(result.tolist(), expected):
        assert actual == pytest.approx(exp)


def test_ema_has_no_look_ahead():
    prices = pd.Series([1, 2, 3, 4, 5])
    result_before = ema(prices, span=3).iloc[2]

    prices_changed_future = pd.Series([1, 2, 3, 999, 999])
    result_after = ema(prices_changed_future, span=3).iloc[2]

    assert result_before == result_after


def test_returns_matches_hand_computed_values():
    # (P_t - P_t-1) / P_t-1
    prices = pd.Series([1, 2, 3, 4, 5])
    result = returns(prices)
    expected = [None, 1.0, 0.5, 1 / 3, 0.25]
    assert result.iloc[0] != result.iloc[0]  # first value is NaN
    for actual, exp in zip(result.iloc[1:], expected[1:]):
        assert actual == pytest.approx(exp)


def test_rolling_std_matches_hand_computed_value():
    # constant values -> zero variance -> std is exactly 0
    series = pd.Series([5.0, 5.0, 5.0, 5.0])
    result = rolling_std(series, window=3)
    assert result.iloc[2] == pytest.approx(0.0)
    assert result.iloc[3] == pytest.approx(0.0)


def test_rolling_volatility_is_std_of_returns_not_prices():
    prices = pd.Series([1, 2, 3, 4, 5])
    r = returns(prices)
    assert rolling_volatility(r, window=3).equals(rolling_std(r, window=3))


def test_rolling_correlation_perfectly_inverse_series():
    series_a = pd.Series([1, 2, 3, 4, 5])
    series_b = pd.Series([5, 4, 3, 2, 1])
    result = rolling_correlation(series_a, series_b, window=3)
    assert result.iloc[2:].apply(lambda x: x == pytest.approx(-1.0)).all()


def test_rolling_correlation_perfectly_matching_series():
    series_a = pd.Series([1, 2, 3, 4, 5])
    series_b = pd.Series([2, 4, 6, 8, 10])
    result = rolling_correlation(series_a, series_b, window=3)
    assert result.iloc[2:].apply(lambda x: x == pytest.approx(1.0)).all()


def test_rolling_zscore_matches_hand_computed_values():
    # linear price sequence -> price is always exactly one rolling-std above
    # the rolling mean, within any window -> z-score is always 1.0
    prices = pd.Series([1, 2, 3, 4, 5])
    result = rolling_zscore(prices, window=3)
    assert result.iloc[:2].isna().all()
    for value in result.iloc[2:]:
        assert value == pytest.approx(1.0)


def test_rolling_zscore_has_no_look_ahead():
    prices = pd.Series([1, 2, 3, 4, 5])
    result_before = rolling_zscore(prices, window=3).iloc[2]

    prices_changed_future = pd.Series([1, 2, 3, 999, 999])
    result_after = rolling_zscore(prices_changed_future, window=3).iloc[2]

    assert result_before == result_after

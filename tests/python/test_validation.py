from io import StringIO

import pandas as pd
import pytest

from quant.validation import Validator

REQUIRED_COLUMNS = ["timestamp", "open", "high", "low", "close", "volume"]


def make_row(**overrides):
    row = {
        "timestamp": "2024-01-02",
        "open": 100.0,
        "high": 105.0,
        "low": 99.0,
        "close": 103.0,
        "volume": 1000,
    }
    row.update(overrides)
    return row


def from_csv(csv_body: str) -> pd.DataFrame:
    return pd.read_csv(StringIO(csv_body))


def test_valid_row_passes_through_clean():
    df = pd.DataFrame([make_row()])
    clean, errors = Validator.validate(df)
    assert len(clean) == 1
    assert errors.empty


def test_missing_required_column_raises():
    df = pd.DataFrame([{"timestamp": "2024-01-02", "open": 100, "high": 105, "low": 99, "volume": 1000}])
    with pytest.raises(ValueError):
        Validator.validate(df)


@pytest.mark.parametrize("column", ["open", "high", "low", "close"])
def test_negative_price_rejected(column):
    df = pd.DataFrame([make_row(**{column: -1.0})])
    clean, errors = Validator.validate(df)
    assert clean.empty
    assert f"invalid {column}" in errors.iloc[0]["error_message"]


@pytest.mark.parametrize("column", ["open", "high", "low", "close"])
def test_zero_price_rejected(column):
    # "must be positive" excludes zero, unlike volume's "cannot be negative"
    df = pd.DataFrame([make_row(**{column: 0.0})])
    clean, errors = Validator.validate(df)
    assert clean.empty
    assert f"invalid {column}" in errors.iloc[0]["error_message"]


def test_high_below_open_rejected_even_when_above_close():
    # regression case: high fails against open alone, must not require failing against close too
    df = pd.DataFrame([make_row(open=200.0, high=150.0, close=100.0, low=90.0)])
    clean, errors = Validator.validate(df)
    assert clean.empty
    assert "invalid high" in errors.iloc[0]["error_message"]


def test_high_below_close_rejected_even_when_above_open():
    df = pd.DataFrame([make_row(open=100.0, high=150.0, close=200.0, low=90.0)])
    clean, errors = Validator.validate(df)
    assert clean.empty
    assert "invalid high" in errors.iloc[0]["error_message"]


def test_low_above_open_rejected_even_when_below_close():
    df = pd.DataFrame([make_row(open=100.0, high=250.0, close=200.0, low=150.0)])
    clean, errors = Validator.validate(df)
    assert clean.empty
    assert "invalid low" in errors.iloc[0]["error_message"]


def test_low_above_close_rejected_even_when_below_open():
    df = pd.DataFrame([make_row(open=200.0, high=250.0, close=100.0, low=150.0)])
    clean, errors = Validator.validate(df)
    assert clean.empty
    assert "invalid low" in errors.iloc[0]["error_message"]


def test_high_and_low_consistent_with_open_and_close_passes():
    df = pd.DataFrame([make_row(open=100.0, high=110.0, close=105.0, low=95.0)])
    clean, errors = Validator.validate(df)
    assert len(clean) == 1
    assert errors.empty


def test_negative_volume_rejected():
    df = pd.DataFrame([make_row(volume=-1)])
    clean, errors = Validator.validate(df)
    assert clean.empty
    assert "invalid volume" in errors.iloc[0]["error_message"]


def test_zero_volume_allowed():
    # "cannot be negative" allows zero, unlike prices' "must be positive"
    df = pd.DataFrame([make_row(volume=0)])
    clean, errors = Validator.validate(df)
    assert len(clean) == 1
    assert errors.empty


def test_malformed_timestamp_rejected():
    df = pd.DataFrame([make_row(timestamp="2024-01-02X")])
    clean, errors = Validator.validate(df)
    assert clean.empty
    assert "invalid timestamp" in errors.iloc[0]["error_message"]


def test_duplicate_timestamp_keeps_first_rejects_rest():
    df = pd.DataFrame([
        make_row(timestamp="2024-01-02", close=103.0),
        make_row(timestamp="2024-01-02", close=104.0),
    ])
    clean, errors = Validator.validate(df)
    assert len(clean) == 1
    assert len(errors) == 1
    assert clean.iloc[0]["close"] == 103.0
    assert "duplicate timestamp" in errors.iloc[0]["error_message"]


def test_missing_value_in_required_column_rejected():
    csv_body = (
        "timestamp,open,high,low,close,volume\n"
        "2024-01-01,100.0,105.0,99.0,,1000\n"
        "2024-01-02,100.0,105.0,99.0,103.0,1000\n"
    )
    clean, errors = Validator.validate(from_csv(csv_body))
    assert len(clean) == 1
    assert len(errors) == 1
    assert "invalid close" in errors.iloc[0]["error_message"]


def test_multiple_violations_all_recorded_in_message():
    df = pd.DataFrame([make_row(open=-1.0, volume=-5)])
    clean, errors = Validator.validate(df)
    message = errors.iloc[0]["error_message"]
    assert "invalid open" in message
    assert "invalid volume" in message


def test_clean_rows_are_rounded_to_two_decimals():
    df = pd.DataFrame([make_row(open=100.126, high=105.994, low=98.501, close=103.507)])
    clean, errors = Validator.validate(df)
    assert errors.empty
    row = clean.iloc[0]
    assert row["open"] == pytest.approx(100.13)
    assert row["high"] == pytest.approx(105.99)
    assert row["low"] == pytest.approx(98.5)
    assert row["close"] == pytest.approx(103.51)


def test_clean_volume_is_integer_dtype():
    df = pd.DataFrame([make_row()])
    clean, errors = Validator.validate(df)
    assert clean["volume"].dtype.kind in "iu"


def test_clean_timestamp_is_parsed_to_datetime():
    df = pd.DataFrame([make_row()])
    clean, errors = Validator.validate(df)
    assert pd.api.types.is_datetime64_any_dtype(clean["timestamp"])


def test_error_message_empty_string_means_no_errors_recorded():
    df = pd.DataFrame([make_row(), make_row(timestamp="2024-01-03")])
    clean, errors = Validator.validate(df)
    assert len(clean) == 2
    assert errors.empty

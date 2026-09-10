import pandas as pd

def sma(prices: pd.Series, window: int) -> pd.Series:
    return prices.rolling(window).mean()

def ema(prices: pd.Series, span: int) -> pd.Series:
    return prices.ewm(span=span, adjust=False).mean()

def returns(prices: pd.Series) -> pd.Series:
    return prices.pct_change()

def rolling_std(returns: pd.Series, window: int) -> pd.Series:
    return returns.rolling(window).std()

def rolling_volatility(returns: pd.Series, window: int) -> pd.Series:
    return rolling_std(returns, window)

def rolling_correlation(series_a: pd.Series, series_b: pd.Series, window: int) -> pd.Series:
    return series_a.rolling(window).corr(series_b)

def rolling_zscore(prices: pd.Series, window: int) -> pd.Series:
    r_mean = prices.rolling(window).mean()
    r_std = rolling_std(prices, window)
    return (prices - r_mean)/r_std
    
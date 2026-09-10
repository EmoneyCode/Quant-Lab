import pandas as pd
import quant.indicators as ind
def moving_average_crossover(prices: pd.Series, short_window: int = 20, long_window: int = 50)->pd.Series:
    short_sma = ind.sma(prices, short_window)
    long_sma = ind.sma(prices, long_window)
    diffs = short_sma - long_sma
    series = []
    positive = False 
    for diff in diffs:
        if diff > 0.0 and not positive:
            series.append('BUY')
            positive = True
        elif diff < 0.0 and positive:
            series.append('SELL')
            positive = False
        else:
            series.append('HOLD')
    return pd.Series(series, index=prices.index, dtype="string")
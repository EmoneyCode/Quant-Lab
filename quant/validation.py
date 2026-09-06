import numpy as np
import pandas as pd
from io import StringIO
from typing import Tuple

class Validator:
    
    @classmethod
    def validate(cls, df:pd.DataFrame)->tuple[pd.DataFrame,pd.DataFrame]:
        required_columns = {'timestamp', 'open', 'high', 'low', 'close', 'volume'}
        missing = required_columns - set(df.columns)
        if missing:
            raise ValueError(f"missing the following column(s): {missing}")
        
        working_df = df.copy()
        working_df['error_message'] = ""
        
        parsed_date = pd.to_datetime(df['timestamp'], format='%Y-%m-%d', errors='coerce')
        timestamp_invalid = parsed_date.isna()
        working_df.loc[timestamp_invalid, 'error_message'] += ': invalid timestamp'
        
        timestamp_duplicate = parsed_date.duplicated(keep='first')
        working_df.loc[timestamp_duplicate, 'error_message'] += ': duplicate timestamp'
        
        open_invalid = working_df['open'].isna() | (working_df['open'] <= 0)
        working_df.loc[open_invalid, 'error_message'] += ': invalid open'
        
        high_invalid = working_df['high'].isna() | (working_df['high'] <= 0) | ((working_df['high'] < working_df['open']) |
                                                    (working_df['high'] < working_df['close']))
        working_df.loc[high_invalid, 'error_message'] += ': invalid high'
        
        low_invalid = working_df['low'].isna() | (working_df['low'] <= 0) | ((working_df['low'] > working_df['open']) |
                                                    (working_df['low'] > working_df['close']))
        working_df.loc[low_invalid, 'error_message'] += ': invalid low'
        
        close_invalid = working_df['close'].isna() | (working_df['close'] <= 0)
        working_df.loc[close_invalid, 'error_message'] += ': invalid close'
        
        volume_invalid = working_df['volume'].isna() | (working_df['volume'] < 0)
        working_df.loc[volume_invalid, 'error_message'] += ': invalid volume'
        
        is_valid = working_df['error_message'] == ""
        
        clean_df = working_df[is_valid].copy()
        if not clean_df.empty:
            clean_df['timestamp'] = parsed_date
            clean_df['open'] = clean_df['open'].round(2)
            clean_df['high'] = clean_df['high'].round(2)
            clean_df['low'] = clean_df['low'].round(2)
            clean_df['close'] = clean_df['close'].round(2)
            clean_df['volume'] = clean_df['volume'].astype(int)
            clean_df = clean_df.drop(columns=['error_message'])
            
        errors_df = working_df[~is_valid].copy()
        return (clean_df,errors_df)
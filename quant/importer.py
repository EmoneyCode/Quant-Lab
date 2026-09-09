import numpy as np
import pandas as pd
from io import StringIO
from typing import Tuple
from quant.validation import Validator
import psycopg
    
def import_csv(symbol: str, csv_path: str, conn:psycopg.Connection, name: str | None = None, 
               exchange: str = "UNKNOWN", currency: str = "UNK") -> dict:
    df = pd.read_csv(csv_path)
    clean_df, error_df = Validator.validate(df)
    symbol_clean = symbol.strip().upper()
    asset_id = ''
    # 1. look up asset by symbol; if missing, INSERT one and get its id back (INSERT ... RETURNING id)
    with conn.cursor() as cur: 
        sql = """INSERT INTO ASSETS (symbol, asset_name, exchange, currency) 
        VALUES(%s, %s, %s, %s)
        ON CONFLICT (symbol) DO NOTHING
        RETURNING id;
        """
            
        cur.execute(sql, (symbol_clean, name, exchange, currency))
        result = cur.fetchone()
        if result is not None:
            asset_id = result[0]
        else:
            select_sql = """SELECT id FROM assets WHERE symbol = %s"""
            cur.execute(select_sql, (symbol_clean,))
            row = cur.fetchone()
            if row is None:
                raise RuntimeError(f"Asset symbol {symbol_clean} failed to resolve.")
            asset_id = row[0]
    # 2. for each row in clean_df: INSERT into price_bars using that asset_id
    #    - wrap each insert in try/except to catch psycopg.errors.UniqueViolation
    #      (duplicate asset_id+timestamp) without killing the whole loop
    inserted = 0
    rejected = 0
    for row in clean_df.itertuples(index=False):
        sql = """INSERT INTO price_bars (asset_id, timestamp, open, high, low, close, volume)
                 VALUES (%s,%s,%s,%s,%s,%s,%s)"""
        try:
            with conn.cursor() as cur:
                cur.execute(sql, (asset_id, row.timestamp, row.open, row.high, row.low, row.close, row.volume))
        except psycopg.errors.UniqueViolation:
            print(f"skipping duplicate row for timestamp: {row.timestamp}")
            conn.rollback()
            rejected = 1 + rejected
    # 3. conn.commit()
        else:
            inserted = 1 + inserted
            conn.commit()
    # 4. return a dict: {"inserted": n, "validation_rejected": len(errors_df), "duplicate_rejected": n}
    return {"inserted": inserted, "validation_rejected": len(error_df), "duplicate_rejected": rejected}
        
import pandas as pd
import psycopg


def save_backtest(conn:psycopg.Connection, asset_id: int, strategy_name: str, initial_capital: float, df: pd.DataFrame, backtest_result:dict)->dict:
    trades = backtest_result["trades"]
    final_equity = backtest_result["final_equity"]
    total_return = (final_equity - initial_capital)/initial_capital
    start = df["timestamp"].iloc[0]
    end = df["timestamp"].iloc[-1]
    backtest_id = ''
    with conn.cursor() as cur:
        sql = """INSERT INTO backtests (asset_id, strategy_name, start_date, 
        end_date, initial_capital, final_equity, total_return)
        VALUES(%s, %s, %s, %s, %s, %s, %s)
        RETURNING id;
        """
        
        
        cur.execute(sql,(asset_id, strategy_name, start, end, initial_capital, final_equity, total_return))
        row = cur.fetchone()
        if row is None:
            raise RuntimeError("INSERT INTO backtests ... RETURNING id returned no row")
        backtest_id = row[0]
        
              
        for trade in trades:    
            sql = """INSERT INTO trades (backtest_id, timestamp, side, quantity, execution_price, commission, slippage)
                VALUES(%s, %s, %s, %s, %s, %s, %s);
                """
            cur.execute(sql,(backtest_id, trade["timestamp"], trade["side"], trade['quantity'], trade['execution_price'], trade['commission'], trade['slippage']))
            
        for timestamp, equity in zip(df['timestamp'], backtest_result["equity_curve"]):
            sql = """INSERT INTO equity_curves (backtest_id, timestamp, equity)
                    VALUES(%s, %s, %s);
                    """
            cur.execute(sql, (backtest_id, timestamp, equity))
    conn.commit()    
    return {"backtest_id":backtest_id, "trades_inserted": len(trades), "equity_curve_inserted":len(df)}
        
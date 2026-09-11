import pandas as pd
import quant.strategy as strat
class Backtester:
    def __init__(self, initial_capital : float, slippage : float = 0.0, commission_per_share : float = 0.0, commission_minimum : float = 0.0):
        self.initial_capital = initial_capital
        self.slippage = slippage
        self.commission_per_share = commission_per_share
        self.commission_minimum = commission_minimum
        
    def run(self, df: pd.DataFrame, signals: pd.Series)-> dict:
        holding = False
        equity_values = []
        trades = []
        cash_left_over = self.initial_capital
        quantity = 0
        for i in range(len(df)):
            current_signal = signals.iloc[i]
            equity_values.append(cash_left_over + (quantity * df['close'].iloc[i]))
            if i + 1 < len(df): 
                if current_signal == 'BUY' and not holding:
                    raw_price = df['open'].iloc[i+1]
                    execution_price = raw_price * (1 + self.slippage)
                    holding = True
                    available_cash = cash_left_over - self.commission_minimum
                    quantity = available_cash // execution_price
                    commission = max(self.commission_per_share * quantity, self.commission_minimum)
                    cash_left_over = cash_left_over - execution_price * quantity - commission
                    trades.append({
                        'timestamp':df['timestamp'].iloc[i+1],
                        'side': 'BUY',
                        'quantity' : quantity,
                        'execution_price' : execution_price,
                        'commission' : commission,
                        'slippage' : self.slippage
                    })
                elif current_signal == 'SELL' and holding:
                    holding = False
                    raw_price = df['open'].iloc[i+1]
                    execution_price = raw_price * (1 - self.slippage)
                    commission = max(self.commission_per_share * quantity, self.commission_minimum)
                    cash_left_over = cash_left_over + execution_price * quantity - commission
                    
                    trades.append({
                        'timestamp':df['timestamp'].iloc[i+1],
                        'side': 'SELL',
                        'quantity' : quantity,
                        'execution_price' : execution_price,
                        'commission' : commission,
                        'slippage' : self.slippage
                    })
                    quantity = 0
        equity_curve = pd.Series(equity_values, index = df.index)
        return {'equity_curve':equity_curve,'trades' : trades,'final_equity': cash_left_over}
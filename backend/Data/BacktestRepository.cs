using QuantLab.Api.Dtos;
using QuantLab.Api.Data;
using System;
using Dapper;
using Npgsql;

namespace QuantLab.Api.Data;

public class BacktestRepository : IBacktestRepository
{
    private readonly string _connectionString;
    public BacktestRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    public async Task<IEnumerable<BacktestSummaryDto>> GetAllAsync()
    {
        const string sql = """SELECT id, asset_id AS "AssetId", strategy_name AS "StrategyName", initial_capital AS "InitialCapital", final_equity AS "FinalEquity", total_return AS "TotalReturn", start_date AS "StartDate", end_date AS "EndDate" FROM backtests""";

        using var connection = new NpgsqlConnection(_connectionString);

        return await connection.QueryAsync<BacktestSummaryDto>(sql);
    }

    public async Task<IEnumerable<EquityPointDto>> GetEquityCurveAsync(long backtestId)
    {
        const string sql = "SELECT timestamp, equity FROM equity_curves WHERE backtest_id = @backtestId ORDER BY timestamp";

        using var connection = new NpgsqlConnection(_connectionString);

        return await connection.QueryAsync<EquityPointDto>(sql, new {backtestId = backtestId});
    }

    public async Task<BacktestSummaryDto?> GetSummaryByIdAsync(long backtestId)
    {
        const string sql = """SELECT id, asset_id AS "AssetId", strategy_name AS "StrategyName", initial_capital AS "InitialCapital", final_equity AS "FinalEquity", total_return AS "TotalReturn", start_date AS "StartDate", end_date AS "EndDate" FROM backtests WHERE id = @id""";

        using var connection = new NpgsqlConnection(_connectionString);

        return await connection.QuerySingleOrDefaultAsync<BacktestSummaryDto>(sql, new {id = backtestId});
    }

    public async Task<IEnumerable<TradeDto>> GetTradesAsync(long backtestId)
    {
        const string sql = "SELECT timestamp, side, quantity, execution_price AS \"ExecutionPrice\", commission, slippage FROM trades WHERE backtest_id = @backtestId ORDER BY timestamp";

        using var connection = new NpgsqlConnection(_connectionString);

        return await connection.QueryAsync<TradeDto>(sql, new {backtestId = backtestId});
    }
}
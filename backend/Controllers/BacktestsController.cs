using Microsoft.AspNetCore.Mvc;
using QuantLab.Api;
using QuantLab.Api.Data;
using QuantLab.Api.Dtos;
using QuantLab.Api.Services;
namespace QuantLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BacktestsController: ControllerBase
{
    private readonly BacktestService _backtestService;

    public BacktestsController(BacktestService backtestService)
    {
        _backtestService = backtestService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BacktestSummaryDto>>> GetBacktests()
    {
        var backtests = await _backtestService.GetBacktestsAsync();
        return Ok(backtests);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BacktestDetailDto>> GetBacktest(long id)
    {
        var backtest = await _backtestService.GetBacktestAsync(id);
        if (backtest == null)
        {
            return NotFound();
        }
        return Ok(backtest);
    }
}
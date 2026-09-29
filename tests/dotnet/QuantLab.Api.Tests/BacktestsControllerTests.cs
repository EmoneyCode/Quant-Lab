// Contract for backend/Controllers/BacktestsController.cs (doesn't exist yet):
//
//   [ApiController]
//   [Route("api/[controller]")]
//   public class BacktestsController : ControllerBase
//   {
//       public BacktestsController(BacktestService backtestService) { ... }
//
//       [HttpGet]
//       public async Task<ActionResult<IEnumerable<BacktestSummaryDto>>> GetBacktests();
//
//       [HttpGet("{id}")]
//       public async Task<ActionResult<BacktestDetailDto>> GetBacktest(long id);
//   }
//
// Same shape as AssetsController: GetBacktest returns 404 (NotFound()) when the
// service's GetBacktestAsync(id) comes back null, otherwise 200 with the detail DTO.

using Microsoft.AspNetCore.Mvc;
using QuantLab.Api.Controllers;
using QuantLab.Api.Data;
using QuantLab.Api.Dtos;
using QuantLab.Api.Services;

namespace QuantLab.Api.Tests;

public class BacktestsControllerTests
{
    private static BacktestSummaryDto MakeSummary(long id, long assetId = 1, string strategyName = "ma_crossover") =>
        new(id, assetId, strategyName, 1000m, 1200m, 0.2m, new DateTime(2024, 1, 1), new DateTime(2024, 1, 10));

    [Fact]
    public async Task GetBacktests_ReturnsOkWithAllBacktests()
    {
        var repository = new FakeBacktestRepository();
        repository.Seed(MakeSummary(1, strategyName: "ma_crossover"));
        repository.Seed(MakeSummary(2, strategyName: "mean_reversion"));
        var controller = new BacktestsController(new BacktestService(repository));

        var result = await controller.GetBacktests();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var backtests = Assert.IsAssignableFrom<IEnumerable<BacktestSummaryDto>>(okResult.Value);
        Assert.Equal(2, backtests.Count());
    }

    [Fact]
    public async Task GetBacktest_ReturnsOkWithDetail_WhenFound()
    {
        var repository = new FakeBacktestRepository();
        repository.Seed(MakeSummary(1, strategyName: "ma_crossover"));
        repository.SeedTrade(1, new TradeDto(new DateTime(2024, 1, 2), "BUY", 10m, 100m, 1m, 0m));
        repository.SeedEquityPoint(1, new EquityPointDto(new DateTime(2024, 1, 1), 1000m));
        var controller = new BacktestsController(new BacktestService(repository));

        var result = await controller.GetBacktest(1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var backtest = Assert.IsType<BacktestDetailDto>(okResult.Value);
        Assert.Equal("ma_crossover", backtest.StrategyName);
        Assert.Single(backtest.Trades);
        Assert.Single(backtest.EquityCurve);
    }

    [Fact]
    public async Task GetBacktest_ReturnsNotFound_WhenMissing()
    {
        var repository = new FakeBacktestRepository();
        var controller = new BacktestsController(new BacktestService(repository));

        var result = await controller.GetBacktest(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }
}

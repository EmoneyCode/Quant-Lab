using Microsoft.AspNetCore.Mvc;
using QuantLab.Api.Controllers;
using QuantLab.Api.Dtos;
using QuantLab.Api.Services;

namespace QuantLab.Api.Tests;

public class AssetsControllerTests
{
    [Fact]
    public async Task GetAssets_ReturnsOkWithAllAssets()
    {
        var repository = new FakeAssetRepository();
        repository.Seed(new AssetDto(1, "AAPL", "Apple Inc.", "NASDAQ", "USD"));
        repository.Seed(new AssetDto(2, "MSFT", "Microsoft Corp.", "NASDAQ", "USD"));
        var controller = new AssetsController(new AssetService(repository));

        var result = await controller.GetAssets();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var assets = Assert.IsAssignableFrom<IEnumerable<AssetDto>>(okResult.Value);
        Assert.Equal(2, assets.Count());
    }

    [Fact]
    public async Task GetAsset_ReturnsOkWithAsset_WhenFound()
    {
        var repository = new FakeAssetRepository();
        repository.Seed(new AssetDto(1, "AAPL", "Apple Inc.", "NASDAQ", "USD"));
        var controller = new AssetsController(new AssetService(repository));

        var result = await controller.GetAsset(1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var asset = Assert.IsType<AssetDto>(okResult.Value);
        Assert.Equal("AAPL", asset.Symbol);
    }

    [Fact]
    public async Task GetAsset_ReturnsNotFound_WhenMissing()
    {
        var repository = new FakeAssetRepository();
        var controller = new AssetsController(new AssetService(repository));

        var result = await controller.GetAsset(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }
}

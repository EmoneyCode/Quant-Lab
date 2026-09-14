using Microsoft.AspNetCore.Mvc;
using QuantLab.Api.Controllers;
using QuantLab.Api.Data;
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

    [Fact]
    public async Task GetPrices_ReturnsOkWithPrices_WhenAssetExists()
    {
        var repository = new FakeAssetRepository();
        repository.Seed(new AssetDto(1, "AAPL", "Apple Inc.", "NASDAQ", "USD"));
        repository.SeedPrice(1, new PriceBarDto(new DateTime(2024, 1, 2), 100m, 105m, 99m, 103m, 5_000_000));
        var controller = new AssetsController(new AssetService(repository));

        var result = await controller.GetPrices(1, from: null, to: null, limit: null);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var prices = Assert.IsAssignableFrom<IEnumerable<PriceBarDto>>(okResult.Value);
        Assert.Single(prices);
    }

    [Fact]
    public async Task GetPrices_ReturnsNotFound_WhenAssetDoesNotExist()
    {
        // asset 999 was never seeded -- this must 404, not just return an empty price list,
        // since "asset doesn't exist" and "asset exists but has no prices" are different things
        var repository = new FakeAssetRepository();
        var controller = new AssetsController(new AssetService(repository));

        var result = await controller.GetPrices(999, from: null, to: null, limit: null);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetPrices_ReturnsOkWithEmptyList_WhenAssetExistsButHasNoPrices()
    {
        var repository = new FakeAssetRepository();
        repository.Seed(new AssetDto(1, "AAPL", "Apple Inc.", "NASDAQ", "USD"));
        var controller = new AssetsController(new AssetService(repository));

        var result = await controller.GetPrices(1, from: null, to: null, limit: null);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var prices = Assert.IsAssignableFrom<IEnumerable<PriceBarDto>>(okResult.Value);
        Assert.Empty(prices);
    }
}

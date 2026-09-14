using QuantLab.Api.Data;
using QuantLab.Api.Dtos;
using QuantLab.Api.Services;

namespace QuantLab.Api.Tests;

public class FakeAssetRepository : IAssetRepository
{
    private readonly List<AssetDto> _assets = new();
    private readonly List<(long AssetId, PriceBarDto Price)> _prices = new();

    public void Seed(AssetDto asset) => _assets.Add(asset);
    public void SeedPrice(long assetId, PriceBarDto price) => _prices.Add((assetId, price));

    public Task<IEnumerable<AssetDto>> GetAllAsync() =>
        Task.FromResult(_assets.AsEnumerable());

    public Task<AssetDto?> GetByIdAsync(long id) =>
        Task.FromResult(_assets.FirstOrDefault(a => a.Id == id));

    // from/to/limit filtering is intentionally NOT implemented here -- that's real
    // query behavior, tested against the actual Dapper repository and Postgres instead.
    public Task<IEnumerable<PriceBarDto>> GetPricesAsync(long assetId, DateTime? from, DateTime? to, int? limit) =>
        Task.FromResult(_prices.Where(p => p.AssetId == assetId).Select(p => p.Price));
}

public class AssetServiceTests
{
    [Fact]
    public async Task GetAssetsAsync_ReturnsAllAssetsFromRepository()
    {
        var repository = new FakeAssetRepository();
        repository.Seed(new AssetDto(1, "AAPL", "Apple Inc.", "NASDAQ", "USD"));
        repository.Seed(new AssetDto(2, "MSFT", "Microsoft Corp.", "NASDAQ", "USD"));
        var service = new AssetService(repository);

        var result = (await service.GetAssetsAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, a => a.Symbol == "AAPL");
        Assert.Contains(result, a => a.Symbol == "MSFT");
    }

    [Fact]
    public async Task GetAssetAsync_ReturnsAsset_WhenFound()
    {
        var repository = new FakeAssetRepository();
        repository.Seed(new AssetDto(1, "AAPL", "Apple Inc.", "NASDAQ", "USD"));
        var service = new AssetService(repository);

        var result = await service.GetAssetAsync(1);

        Assert.NotNull(result);
        Assert.Equal("AAPL", result!.Symbol);
        Assert.Equal("Apple Inc.", result.Name);
        Assert.Equal("NASDAQ", result.Exchange);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public async Task GetAssetAsync_ReturnsNull_WhenNotFound()
    {
        var repository = new FakeAssetRepository();
        var service = new AssetService(repository);

        var result = await service.GetAssetAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPricesAsync_ReturnsPricesForGivenAsset()
    {
        var repository = new FakeAssetRepository();
        repository.Seed(new AssetDto(1, "AAPL", "Apple Inc.", "NASDAQ", "USD"));
        repository.SeedPrice(1, new PriceBarDto(new DateTime(2024, 1, 2), 100m, 105m, 99m, 103m, 5_000_000));
        repository.SeedPrice(1, new PriceBarDto(new DateTime(2024, 1, 3), 103m, 108m, 102m, 107m, 6_200_000));
        var service = new AssetService(repository);

        var result = (await service.GetPricesAsync(1, from: null, to: null, limit: null)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Close == 103m);
        Assert.Contains(result, p => p.Close == 107m);
    }
}

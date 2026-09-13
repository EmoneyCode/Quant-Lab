using QuantLab.Api.Data;
using QuantLab.Api.Dtos;
using QuantLab.Api.Services;

namespace QuantLab.Api.Tests;

public class FakeAssetRepository : IAssetRepository
{
    private readonly List<AssetDto> _assets = new();

    public void Seed(AssetDto asset) => _assets.Add(asset);

    public Task<IEnumerable<AssetDto>> GetAllAsync() =>
        Task.FromResult(_assets.AsEnumerable());

    public Task<AssetDto?> GetByIdAsync(long id) =>
        Task.FromResult(_assets.FirstOrDefault(a => a.Id == id));
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
}

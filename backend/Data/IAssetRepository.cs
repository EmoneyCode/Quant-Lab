using QuantLab.Api.Dtos;
namespace QuantLab.Api.Data;

public interface IAssetRepository
{
    public Task<IEnumerable<AssetDto>> GetAllAsync();
    public Task<AssetDto?> GetByIdAsync(long id);

    public Task<IEnumerable<PriceBarDto>> GetPricesAsync(long assetId, DateTime? from, DateTime? to, int? limit);
}
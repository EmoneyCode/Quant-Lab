using System;
using QuantLab.Api.Data;
using QuantLab.Api.Dtos;

namespace QuantLab.Api.Services;

public class AssetService
{
    private readonly IAssetRepository _assetRepository;

    public AssetService(IAssetRepository assetRepository)
    {
        _assetRepository = assetRepository;
    }

    public async Task<IEnumerable<AssetDto>> GetAssetsAsync()
    {
        return await _assetRepository.GetAllAsync();
    }

    public async Task<AssetDto?> GetAssetAsync(long Id)
    {
        return await _assetRepository.GetByIdAsync(Id);
    }

    public async Task<IEnumerable<PriceBarDto>> GetPricesAsync(long assetId, DateTime? from, DateTime? to, int? limit)
    {
        return await _assetRepository.GetPricesAsync(assetId, from, to, limit);
    }
}
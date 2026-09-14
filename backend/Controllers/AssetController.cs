using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using QuantLab.Api;
using QuantLab.Api.Data;
using QuantLab.Api.Dtos;
using QuantLab.Api.Services;
namespace QuantLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetsController : ControllerBase
{
    private readonly AssetService _assetService;

    public AssetsController(AssetService assetService)
    {
        _assetService = assetService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssetDto>>> GetAssets()
    {
        var assets = await _assetService.GetAssetsAsync();
        return Ok(assets);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AssetDto>> GetAsset(long id)
    {
        var asset = await _assetService.GetAssetAsync(id);
        if (asset == null)
        {
            return NotFound();
        }
        return Ok(asset);
    }

    [HttpGet("{id}/prices")]
    public async Task<ActionResult<IEnumerable<PriceBarDto>>> GetPrices(long id, DateTime? from, DateTime? to, int? limit)
    {
        var asset = await _assetService.GetAssetAsync(id);
        if (asset == null)
        {
            return NotFound();
        }

        var priceBar = await _assetService.GetPricesAsync(id, from, to, limit);

        return Ok(priceBar);
    }


}
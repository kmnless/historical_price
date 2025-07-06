using historical_prices.Services;
using Microsoft.AspNetCore.Mvc;

namespace historical_prices.Controllers;

[ApiController]
[Route("api/assets")]
public class AssetsController : ControllerBase
{
    private readonly AssetSyncService _assetService;
    ILogger<AssetsController> _logger;

    public AssetsController(AssetSyncService assetService, ILogger<AssetsController> logger)
    {
        _assetService = assetService;
        _logger = logger;
    }

    /// <summary>
    /// Get all market assets.
    /// </summary>
    /// <param name="refresh">If true, will sync assets from the external API before returning.</param>
    /// <returns>List of market assets.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAssets([FromQuery] bool refresh = false)
    {
        try
        {
            var result = refresh
                ? await _assetService.SyncAssetsAsync()
                : await _assetService.GetAllAssetsAsync();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting assets");
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }
}
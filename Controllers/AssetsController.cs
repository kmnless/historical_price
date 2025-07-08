using historical_prices.Services;
using Microsoft.AspNetCore.Mvc;

namespace historical_prices.Controllers;

[ApiController]
[Route("api/assets")]
public class AssetsController : ControllerBase
{
    private readonly AssetSyncService _assetService;
    private readonly ILogger<AssetsController> _logger;

    public AssetsController(AssetSyncService assetService, ILogger<AssetsController> logger)
    {
        _assetService = assetService;
        _logger = logger;
    }

    /// <summary>
    /// Get all market assets with optional filtering.
    /// </summary>
    /// <param name="refresh">If true, sync with remote API before returning.</param>
    /// <param name="provider">Optional: Filter by provider.</param>
    /// <param name="kind">Optional: Filter by kind (e.g., forex, stock).</param>
    /// <param name="symbol">Optional: Filter by symbol or part of it.</param>
    /// <returns>List of market assets.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAssets(
        [FromQuery] bool refresh = false,
        [FromQuery] string? provider = null,
        [FromQuery] string? kind = null,
        [FromQuery] string? symbol = null)
    {
        try
        {
            var assets = refresh
                ? await _assetService.SyncAssetsAsync()
                : await _assetService.GetAllAssetsAsync();

            if (!string.IsNullOrWhiteSpace(provider))
            {
                assets = assets
                    .Where(a => a.Providers.Any(p => p.Equals(provider, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(kind))
            {
                assets = assets
                    .Where(a => a.Kind != null && a.Kind.Equals(kind, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(symbol))
            {
                assets = assets
                    .Where(a => a.Symbol.Contains(symbol, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return Ok(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting assets");
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }
}
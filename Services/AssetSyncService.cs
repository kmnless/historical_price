using historical_prices.Data;
using historical_prices.Models;
using Microsoft.EntityFrameworkCore;

namespace historical_prices.Services;

public class AssetSyncService
{
    private readonly AppDbContext _db;
    private readonly FintachartsApiService _api;
    private readonly ILogger<AssetSyncService> _logger;

    public AssetSyncService(AppDbContext db, FintachartsApiService api, ILogger<AssetSyncService> logger)
    {
        _db = db;
        _api = api;
        _logger = logger;
    }

    public async Task<List<MarketAsset>> SyncAssetsAsync()
    {
        try
        {
            var instruments = await _api.GetAllInstrumentsAsync();

            var existing = await _db.MarketAssets.ToListAsync();
            var existingIds = existing.Select(e => e.InstrumentId).ToHashSet();

            var newAssets = instruments
                .Where(i => !existingIds.Contains(i.InstrumentId))
                .ToList();

            if (newAssets.Any())
            {
                _db.MarketAssets.AddRange(newAssets);
                await _db.SaveChangesAsync();
                _logger.LogInformation("Added {Count} new assets", newAssets.Count);
            }

            return existing.Concat(newAssets).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SyncAssetsAsync");
            throw;
        }
    }

    public async Task<List<MarketAsset>> GetAllAssetsAsync()
    {
        try
        {
            return await _db.MarketAssets.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during GetAllAssetsAsync");
            throw;
        }
    }
}

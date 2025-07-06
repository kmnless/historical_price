using historical_prices.Models;
using Microsoft.EntityFrameworkCore;

namespace historical_prices.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<MarketAsset> MarketAssets => Set<MarketAsset>();
    public DbSet<AssetPrice> AssetPrices => Set<AssetPrice>();
    public DbSet<PricesCachedDateRange> PricesCachedDateRanges => Set<PricesCachedDateRange>();
}

using historical_prices.Clients;
using historical_prices.Data;
using historical_prices.DTOs;
using historical_prices.Models;

namespace historical_prices.Services;

public class PriceService
{
    private readonly FintachartsRestApiClient _apiClient;
    private readonly PriceCacheService _priceCacheService;
    private readonly PriceAggregator _aggregator;
    private readonly ILogger<PriceService> _logger;

    public PriceService(FintachartsRestApiClient apiClient, ILogger<PriceService> logger, PriceCacheService priceCacheService, PriceAggregator aggregator)
    {
        _apiClient = apiClient;
        _logger = logger;
        _priceCacheService = priceCacheService;
        _aggregator = aggregator;
    }

    public async Task<List<AssetPrice>> GetPriceDateRange(Guid instrumentId, string provider, int interval,
        Periodicity periodicity, DateTimeOffset startDate, DateTimeOffset? endDate = null)
    {
        endDate ??= DateTimeOffset.UtcNow.AddMinutes(-1);
        var gaps = await _priceCacheService.GetDateGaps(instrumentId, provider, startDate, endDate.Value);
        bool newDataAppeared = false;

        foreach (var gap in gaps)
        {
            var response = await _apiClient.GetAssetPricesAsync(instrumentId, provider, interval, Periodicity.HOUR, gap.Start, gap.End);
            var prices = response.Data.Select(bar => new AssetPrice
            {
                InstrumentId = instrumentId,
                High = bar.High,
                DateTime = bar.Timestamp.UtcDateTime,
                Close = bar.Close,
                Low = bar.Low,
                Open = bar.Open,
                Provider = provider
            }).ToList();

            if (prices.Any())
                newDataAppeared = true;

            await _priceCacheService.CachePriceAsync(prices);
        }

        if (newDataAppeared)
            await _priceCacheService.CachePriceCoverage(instrumentId, provider, startDate, endDate.Value);

        var allPrices = await _priceCacheService.GetPriceAsync(instrumentId, provider, startDate, endDate);
        return _aggregator.Aggregate(allPrices, periodicity);
    }

    public async Task<AssetPrice?> GetCurrentPrice(Guid instrumentId, string provider)
    {
        var cachedPrice = await _priceCacheService.GetLatestPriceAsync(instrumentId, provider);
        if (cachedPrice != null && (DateTimeOffset.UtcNow - cachedPrice.DateTime) < TimeSpan.FromHours(2))
        {
            return cachedPrice;
        }

        var response = await _apiClient.GetLatestBarsAsync(instrumentId, provider, 1, Periodicity.HOUR, 1);

        if (response?.Data == null || !response.Data.Any())
        {
            // no data from API -> returns what we have in cache or null
            return cachedPrice;
        }

        var latestBar = response.Data.First();

        return new AssetPrice
        {
            InstrumentId = instrumentId,
            Provider = provider,
            DateTime = latestBar.Timestamp.UtcDateTime,
            Open = latestBar.Open,
            High = latestBar.High,
            Low = latestBar.Low,
            Close = latestBar.Close
        };
    }

}

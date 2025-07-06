using historical_prices.Clients;
using historical_prices.Data;
using historical_prices.DTOs;
using historical_prices.Models;
using Microsoft.EntityFrameworkCore;

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


}

using historical_prices.Clients;
using historical_prices.DTOs;
using historical_prices.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace historical_prices.Services;

public class FintachartsApiService
{
    private readonly FintachartsRestApiClient _apiClient;
    public FintachartsApiService(FintachartsRestApiClient apiClient)
    {
        _apiClient = apiClient;
    }
    public async Task<List<MarketAsset>> GetAllInstrumentsAsync()
    {
        int page = 1;
        var assets = new List<MarketAsset>();
        FintachartsInstrumentsResponseDTO response = new();
        do
        {
            response = await _apiClient.GetInstrumentsAsync(page);
            assets.AddRange(response.Data);
            page++;
        }
        while (page <= response.Paging.Pages);

        return assets;
    }
}

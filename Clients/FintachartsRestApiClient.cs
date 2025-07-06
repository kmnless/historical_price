using historical_prices.DTOs;
using historical_prices.Models;
using historical_prices.Services;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace historical_prices.Clients;
public class FintachartsRestApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;
    private readonly ILogger<FintachartsApiService> _logger;

    public FintachartsRestApiClient(HttpClient httpClient, AuthService authService, IConfiguration config, ILogger<FintachartsApiService> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(config["Fintacharts:BaseUrl"] ?? throw new ArgumentNullException("Fintacharts:BaseUrl is not configured"));
        _authService = authService;
        _logger = logger;
    }

    public async Task<BarPageDTO> GetAssetPricesAsync(Guid instrumentId, string provider, int interval, Periodicity periodicity, DateTimeOffset startDate, DateTimeOffset? endDate = null)
    {
        try
        {
            var token = await _authService.GetAccessTokenAsync();
            var urlBuilder = new StringBuilder("/api/bars/v1/bars/date-range");
            urlBuilder.Append("?");

            urlBuilder.Append($"instrumentId={instrumentId}&");
            urlBuilder.Append($"provider={provider}&");
            urlBuilder.Append($"interval={interval}&");
            urlBuilder.Append($"periodicity={periodicity.ToString().ToLower()}&");
            urlBuilder.Append($"startDate={startDate.ToString("yyyy-MM-ddTHH:mm:ss")}&");
            if (endDate is not null)
            {
                urlBuilder.Append($"endDate={endDate.Value.ToString("yyyy-MM-ddTHH:mm:ss")}&");
            }

            var url = urlBuilder.ToString().TrimEnd('&');
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to get asset prices Status: {response.StatusCode}, Response: {errorContent}");
                throw new ApplicationException("Unable to asset prices");
            }

            using var stream = await response.Content.ReadAsStreamAsync();

            //var serializer = new JsonSerializerOptions();
            //serializer.Converters.Add(new MarketAssetConverter());
            //serializer.;
            return (await JsonSerializer.DeserializeAsync<BarPageDTO>(stream, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }))!;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAssetPricesAsync");
            throw;
        }
    }
    public async Task<FintachartsInstrumentsResponseDTO> GetInstrumentsAsync(int? page = null, int? size = null, string? symbol = null, string? provider = null)
    {
        try
        {
            var token = await _authService.GetAccessTokenAsync();
            var urlBuilder = new StringBuilder("/api/instruments/v1/instruments");
            urlBuilder.Append("?");
            if(page.HasValue)
            {
                urlBuilder.Append($"page={page.Value}&");
            }
            if(size.HasValue)
            {
                urlBuilder.Append($"size={size.Value}&");
            }
            if(!string.IsNullOrWhiteSpace(symbol))
            {
                urlBuilder.Append($"symbol={Uri.EscapeDataString(symbol)}&");
            }
            if(!string.IsNullOrWhiteSpace(provider))
            {
                urlBuilder.Append($"provider={Uri.EscapeDataString(provider)}&");
            }
            var url = urlBuilder.ToString().TrimEnd('&');
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to fetch instruments. Status: {StatusCode}, Response: {Response}", response.StatusCode, errorContent);
                throw new ApplicationException("Unable to fetch instruments");
            }

            using var stream = await response.Content.ReadAsStreamAsync();

            var serializer = new JsonSerializerOptions();
            serializer.Converters.Add(new MarketAssetConverter());
            serializer.PropertyNameCaseInsensitive = true;

            return (await JsonSerializer.DeserializeAsync<FintachartsInstrumentsResponseDTO>(stream, serializer))!;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetInstrumentsAsync");
            throw;
        }
    }
    internal class MarketAssetConverter : JsonConverter<MarketAsset>
    {
        public override MarketAsset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            var asset = new MarketAsset
            {
                InstrumentId = root.GetProperty("id").GetGuid(),
                Symbol = root.GetProperty("symbol").GetString(),
                Kind = root.GetProperty("kind").GetString(),
                Description = root.GetProperty("description").GetString(),
                Currency = root.GetProperty("currency").GetString(),
                Providers = root.TryGetProperty("mappings", out var mappings)
                    ? mappings.EnumerateObject().Select(x => x.Name).ToList()
                    : new List<string>()
            };

            return asset;
        }

        public override void Write(Utf8JsonWriter writer, MarketAsset value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }

}

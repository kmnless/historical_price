namespace historical_prices.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    private string? _accessToken;
    private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;

    public AuthService(HttpClient httpClient, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        if (_accessToken != null && DateTimeOffset.UtcNow < _tokenExpiresAt)
            return _accessToken;

        try
        {
            var realm = _configuration["Fintacharts:Realm"];
            if (string.IsNullOrEmpty(realm))
            {
                _logger.LogError("Fintacharts:Realm is not configured");
                throw new ApplicationException("Missing Fintacharts realm configuration");
            }

            var baseUrl = _configuration["Fintacharts:BaseUrl"];
            if (string.IsNullOrEmpty(baseUrl))
            {
                _logger.LogError("Fintacharts:BaseUrl is not configured");
                throw new ApplicationException("Missing Fintacharts base URL configuration");
            }

            var uri = $"{baseUrl}/identity/realms/{realm}/protocol/openid-connect/token";

            var username = _configuration["Fintacharts:Username"];
            var password = _configuration["Fintacharts:Password"];

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _logger.LogError("Fintacharts credentials are not configured properly");
                throw new ApplicationException("Missing Fintacharts credentials");
            }

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("client_id", "app-cli"),
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password)
            });

            HttpResponseMessage response;

            try
            {
                response = await _httpClient.PostAsync(uri, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request to get token failed");
                throw new ApplicationException("Failed to get token from Fintacharts", ex);
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to get token from Fintacharts. Status: {response.StatusCode}, Response: {errorContent}");
                throw new ApplicationException("Failed to get token from Fintacharts");
            }

            TokenResponse? json;

            try
            {
                json = await response.Content.ReadFromJsonAsync<TokenResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize token response");
                throw new ApplicationException("Invalid token response format", ex);
            }

            if (json == null || string.IsNullOrEmpty(json.access_token))
            {
                _logger.LogError("Token response is empty or invalid");
                throw new ApplicationException("Invalid token response");
            }

            _accessToken = json.access_token;
            _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(json.expires_in - 60); // запас 60 сек

            return _accessToken;
        }
        catch (Exception)
        {
            throw;
        }
    }

    private class TokenResponse
    {
        public string access_token { get; set; } = string.Empty;
        public int expires_in { get; set; }
    }
}
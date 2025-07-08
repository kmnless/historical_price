namespace historical_prices.Models;

public class FintachartsWebSocketOptions
{
    public string BaseUrl { get; set; } = default!;
    public int ReconnectDelaySeconds { get; set; } = 5;
    public List<string> Kinds { get; set; } = new() { "ask", "bid", "last" };
}


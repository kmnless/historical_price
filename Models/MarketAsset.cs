using System.Text.Json.Serialization;

namespace historical_prices.Models;

public class MarketAsset
{
    [JsonIgnore]
    public int Id { get; set; }
    public Guid InstrumentId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public List<string> Providers { get; set; }
}

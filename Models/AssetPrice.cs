using System.Text.Json.Serialization;

namespace historical_prices.Models;

public class AssetPrice
{
    [JsonIgnore]
    public int Id { get; set; }
    public Guid InstrumentId { get; set; }
    public string Provider { get; set; } = string.Empty;

    [JsonPropertyName("o")]
    public decimal Open { get; set; }

    [JsonPropertyName("h")]
    public decimal High { get; set; }

    [JsonPropertyName("l")]
    public decimal Low { get; set; }

    [JsonPropertyName("c")]
    public decimal Close { get; set; }
    public DateTimeOffset DateTime { get; set; }
}


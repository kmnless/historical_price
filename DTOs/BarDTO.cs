using System.Text.Json.Serialization;

namespace historical_prices.DTOs;

public class BarPageDTO
{
    public List<BarDTO> Data { get; set; } 
}
public class BarDTO
{
    [JsonPropertyName("t")]
    public DateTimeOffset Timestamp { get; set; }

    [JsonPropertyName("o")]
    public decimal Open { get; set; }

    [JsonPropertyName("h")]
    public decimal High { get; set; }

    [JsonPropertyName("l")]
    public decimal Low { get; set; }

    [JsonPropertyName("c")]
    public decimal Close { get; set; }

    [JsonPropertyName("v")]
    public decimal Volume { get; set; }
}
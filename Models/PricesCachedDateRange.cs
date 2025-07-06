using System.Text.Json.Serialization;

namespace historical_prices.Models;

public class PricesCachedDateRange
{
    [JsonIgnore]
    public int Id { get; set; }
    public Guid InstrumentId { get; set; }
    public string Provider { get; set; }
    public List<string> DatePair { get; set; } 
}

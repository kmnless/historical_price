using historical_prices.Models;
using System.Text.Json.Serialization;

namespace historical_prices.DTOs;

public class FintachartsInstrumentsResponseDTO
{
    public List<MarketAsset> Data { get; set; }
    public Paging Paging { get; set; }
}
public class Paging
{
    public int Page { get; set; }
    public int Pages { get; set; }
    public int Items { get; set; }
}


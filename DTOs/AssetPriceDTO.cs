namespace historical_prices.DTOs;

public class AssetPriceDTO
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
}

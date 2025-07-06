namespace historical_prices.DTOs;

public class BarResponseDTO
{
    public List<BarDTO> Data { get; set; } = new();
}

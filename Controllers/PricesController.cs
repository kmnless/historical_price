using historical_prices.Models;
using historical_prices.Services;
using Microsoft.AspNetCore.Mvc;

namespace historical_prices.Controllers;

[ApiController]
[Route("api/prices")]
public class PricesController : ControllerBase
{
    private readonly PriceService _priceService;
    private readonly ILogger<PricesController> _logger;

    public PricesController(PriceService priceService, ILogger<PricesController> logger)
    {
        _priceService = priceService;
        _logger = logger;
    }

    /// <summary>
    /// Get price data for an instrument in a date range.
    /// </summary>
    /// <param name="instrumentId">Instrument ID (GUID)</param>
    /// <param name="provider">Provider name (e.g. oanda)</param>
    /// <param name="interval">Interval size (e.g. 1)</param>
    /// <param name="periodicity">Periodicity (minute, hour, day, week, month)</param>
    /// <param name="start">Start date</param>
    /// <param name="end">Optional end date</param>
    /// <returns>List of prices</returns>
    [HttpGet("range")]
    public async Task<IActionResult> GetPriceRange(
        [FromQuery] Guid instrumentId,
        [FromQuery] string provider,
        [FromQuery] int interval,
        [FromQuery] Periodicity periodicity,
        [FromQuery] DateTimeOffset start,
        [FromQuery] DateTimeOffset? end = null)
    {
        try
        {
            var prices = await _priceService.GetPriceDateRange(instrumentId, provider, interval, periodicity, start, end);
            return Ok(prices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching price data");
            return StatusCode(500, "Error fetching price data");
        }
    }

    /// <summary>
    /// Get current price for a specific instrument.
    /// </summary>
    /// <param name="instrumentId">Instrument ID (GUID)</param>
    /// <param name="provider">Provider name (e.g. oanda)</param>
    /// <returns>Current price</returns>
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentPrice(
        [FromQuery] Guid instrumentId,
        [FromQuery] string provider)
    {
        try
        {
            var price = await _priceService.GetCurrentPrice(instrumentId, provider);
            if (price == null)
            {
                return NotFound($"Price data not found for instrument {instrumentId} and provider {provider}.");
            }
            return Ok(price);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching current price");
            return StatusCode(500, "Error fetching current price");
        }
    }
}

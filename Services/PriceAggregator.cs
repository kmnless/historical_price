using historical_prices.Models;

namespace historical_prices.Services;

public class PriceAggregator
{
    public List<AssetPrice> Aggregate(List<AssetPrice> prices, Periodicity periodicity)
    {
        if (periodicity == Periodicity.HOUR)
            return prices.OrderBy(p => p.DateTime).ToList();

        var grouped = prices
            .GroupBy(p => TruncateToPeriod(p.DateTime, periodicity))
            .Select(group =>
            {
                var sorted = group.OrderBy(p => p.DateTime).ToList();
                return new AssetPrice
                {
                    InstrumentId = sorted[0].InstrumentId,
                    Provider = sorted[0].Provider,
                    DateTime = group.Key,
                    Open = sorted.First().Open,
                    Close = sorted.Last().Close,
                    High = group.Max(p => p.High),
                    Low = group.Min(p => p.Low)
                };
            })
            .OrderBy(p => p.DateTime)
            .ToList();

        return grouped;
    }

    private DateTimeOffset TruncateToPeriod(DateTimeOffset dateTime, Periodicity periodicity)
    {
        return periodicity switch
        {
            Periodicity.HOUR => new DateTimeOffset(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0, TimeSpan.Zero),
            Periodicity.DAY => new DateTimeOffset(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, TimeSpan.Zero),
            Periodicity.WEEK => new DateTimeOffset(dateTime.AddDays(-(int)dateTime.DayOfWeek).Date, TimeSpan.Zero),
            Periodicity.MONTH => new DateTimeOffset(dateTime.Year, dateTime.Month, 1, 0, 0, 0, TimeSpan.Zero),
            _ => dateTime
        };
    }
}
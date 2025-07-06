using historical_prices.Data;
using historical_prices.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace historical_prices.Services;

public class PriceCacheService
{
    private readonly AppDbContext _db;
    private readonly ILogger<PriceCacheService> _logger;

    public PriceCacheService(AppDbContext db, ILogger<PriceCacheService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public static List<(DateTimeOffset Start, DateTimeOffset End)> MergeIntervals(List<(DateTimeOffset Start, DateTimeOffset End)> intervals, (DateTimeOffset Start, DateTimeOffset End) added)
    {
        var newCoveredInterval = (added.Start, added.End);

        var result = new List<(DateTimeOffset Start, DateTimeOffset End)>();


        var selectedIntervals = intervals.Where(i => i.Start > added.Start && i.End < added.End)
            .ToList();

        var newLeft = intervals.FirstOrDefault(i => i.Start <= added.Start && i.End >= added.Start);
        var newRight = intervals.FirstOrDefault(i => i.Start <= added.End && i.End >= added.End);
        
        if(newLeft != default)
        {
            newCoveredInterval.Start = newLeft.Start;
            selectedIntervals.Add(newLeft);
        }
        if (newRight != default)
        {
            newCoveredInterval.End = newRight.End;
            selectedIntervals.Add(newRight);
        }

        result = intervals.Except(selectedIntervals).ToList();
        result.Add(newCoveredInterval);
        return result;

    }
    public async Task CachePriceCoverage(Guid instrumentId, string provider, DateTimeOffset requestStart, DateTimeOffset requestEnd)
    {
        var cachedRanges = await _db.PricesCachedDateRanges
            .Where(r => r.InstrumentId == instrumentId && r.Provider == provider)
            .ToListAsync();
        if (!cachedRanges.Any())
        {
            var newRange = new PricesCachedDateRange
            {
                InstrumentId = instrumentId,
                Provider = provider,
                DatePair = new List<string> { $"{requestStart.ToString("yyyy-MM-ddTHH:mm:ss")}|{requestEnd.ToString("yyyy-MM-ddTHH:mm:ss")}" }
            };
            await _db.PricesCachedDateRanges.AddAsync(newRange);
            await _db.SaveChangesAsync();
            return;
        }
        var intervals = new List<(DateTimeOffset Start, DateTimeOffset End)>();

        foreach (var range in cachedRanges)
        {
            foreach (var pair in range.DatePair)
            {
                var parts = pair.Split('|');

                if (DateTimeOffset.TryParse(parts[0], out var start) && DateTimeOffset.TryParse(parts[1], out var end))
                {
                    intervals.Add((start, end));
                }
            }
        }
        var a = MergeIntervals(intervals, (requestStart, requestEnd)).Select(b => $"{b.Start.ToString("yyyy-MM-ddTHH:mm:ss")}|{b.End.ToString("yyyy-MM-ddTHH:mm:ss")}").ToList();
        foreach(var range in cachedRanges)
        {
            range.DatePair = a;
        }
        _db.PricesCachedDateRanges.UpdateRange(cachedRanges);
        await _db.SaveChangesAsync();
    }

    public async Task<List<(DateTimeOffset Start, DateTimeOffset End)>> GetDateGaps(Guid instrumentId, string provider, DateTimeOffset requestStart, DateTimeOffset requestEnd)
    {
        var cachedRanges = await _db.PricesCachedDateRanges
            .Where(r => r.InstrumentId == instrumentId && r.Provider == provider)
            .ToListAsync();

        var intervals = new List<(DateTimeOffset Start, DateTimeOffset End)>();

        foreach (var range in cachedRanges)
        {
            foreach (var pair in range.DatePair)
            {
                var parts = pair.Split('|');

                if (DateTimeOffset.TryParse(parts[0], out var start) && DateTimeOffset.TryParse(parts[1], out var end))
                {
                    intervals.Add((start, end));
                }
            }
        }

        intervals = intervals
            .Where(i => i.End >= requestStart && i.Start <= requestEnd)
            .ToList();

        if(!intervals.Any())
        {
            return new List<(DateTimeOffset Start, DateTimeOffset End)> { (requestStart, requestEnd) };
        }

        intervals.Sort((a, b) => a.Start.CompareTo(b.Start));

        var gaps = new List<(DateTimeOffset Start, DateTimeOffset End)>();

        if(intervals.First().Start > requestStart)
        {
            gaps.Add((requestStart, intervals.First().Start));
        }
        
        var a = intervals.GetEnumerator(); 
        var b = intervals.GetEnumerator(); 


        b.MoveNext();
        while (intervals.Count > 1 && b.MoveNext() && a.MoveNext())
        {
            gaps.Add((a.Current.End, b.Current.Start));

        } 

        if (intervals.Last().End < requestEnd)
        {
            gaps.Add((intervals.Last().End, requestEnd));
        }
        return gaps;
       
    }

    public async Task CachePriceAsync(List<AssetPrice> assetPrices)
    {
        try
        {
            await _db.AssetPrices.AddRangeAsync(assetPrices);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error caching price");
        }

    }

    public async Task<List<AssetPrice>> GetPriceAsync (Guid instrumentId, string provider, DateTimeOffset startDate, DateTimeOffset? endDate = null)
    {
        var query = _db.AssetPrices.AsQueryable()
            .Where(p => p.InstrumentId == instrumentId && p.Provider == provider && p.DateTime.UtcDateTime >= startDate.UtcDateTime);
        if (endDate.HasValue)
        {
            query = query.Where(p => p.DateTime.UtcDateTime <= endDate.Value.UtcDateTime);
        }
        return await query.ToListAsync();
    }

}

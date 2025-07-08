using System.Collections.Concurrent;

namespace historical_prices.Services;

public class SubscriptionManagerService
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _subscriptions = new(); // connectionId -> instrumentIds
    private readonly ConcurrentDictionary<string, HashSet<string>> _instrumentSubscribers = new(); // instrumentId -> connectionIds
    private readonly object _lock = new();

    public void Subscribe(string connectionId, string instrumentId)
    {
        lock (_lock)
        {
            if (!_subscriptions.ContainsKey(connectionId))
                _subscriptions[connectionId] = new HashSet<string>();

            if (!_instrumentSubscribers.ContainsKey(instrumentId))
                _instrumentSubscribers[instrumentId] = new HashSet<string>();

            _subscriptions[connectionId].Add(instrumentId);
            _instrumentSubscribers[instrumentId].Add(connectionId);
        }
    }

    public void Unsubscribe(string connectionId, string instrumentId)
    {
        lock (_lock)
        {
            if (_subscriptions.TryGetValue(connectionId, out var instruments))
                instruments.Remove(instrumentId);

            if (_instrumentSubscribers.TryGetValue(instrumentId, out var connections))
                connections.Remove(connectionId);
        }
    }

    public HashSet<string> GetSubscriptions(string connectionId)
    {
        if (_subscriptions.TryGetValue(connectionId, out var subs))
            return subs;
        return new HashSet<string>();
    }

    public List<string> GetSubscribers(string instrumentId)
    {
        if (_instrumentSubscribers.TryGetValue(instrumentId, out var subs))
            return subs.ToList();
        return new List<string>();
    }

    public void RemoveConnection(string connectionId)
    {
        lock (_lock)
        {
            if (_subscriptions.TryRemove(connectionId, out var instruments))
            {
                foreach (var instrumentId in instruments)
                {
                    if (_instrumentSubscribers.TryGetValue(instrumentId, out var connSet))
                    {
                        connSet.Remove(connectionId);
                        if (connSet.Count == 0)
                            _instrumentSubscribers.TryRemove(instrumentId, out _);
                    }
                }
            }
        }
    }
}

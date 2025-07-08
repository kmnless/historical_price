using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using historical_prices.Clients;

namespace historical_prices.Services;

public class WebSocketDispatcherService
{
    private readonly ILogger<WebSocketDispatcherService> _logger;
    private readonly SubscriptionManagerService _subscriptionManager;
    private readonly FintachartsWebSocketApiClient _fintachartsClient;

    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();

    public WebSocketDispatcherService(
        ILogger<WebSocketDispatcherService> logger,
        SubscriptionManagerService subscriptionManager,
        FintachartsWebSocketApiClient fintachartsClient)
    {
        _logger = logger;
        _subscriptionManager = subscriptionManager;
        _fintachartsClient = fintachartsClient;
    }

    public async Task HandleClientAsync(WebSocket socket)
    {
        var connectionId = Guid.NewGuid().ToString();
        _connections[connectionId] = socket;

        var buffer = new byte[4096];
        var ms = new MemoryStream();

        while (socket.State == WebSocketState.Open)
        {
            ms.SetLength(0);
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    await RemoveConnectionAsync(connectionId);
                    return;
                }

                ms.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);

            ms.Seek(0, SeekOrigin.Begin);
            string json;
            using (var reader = new StreamReader(ms, Encoding.UTF8, leaveOpen: true))
            {
                json = await reader.ReadToEndAsync();
            }

            try
            {
                var command = JsonSerializer.Deserialize<SubscriptionCommand>(json);
                if (command != null && command.Subscribe && command.InstrumentId != Guid.Empty)
                {
                    _subscriptionManager.Subscribe(connectionId, command.InstrumentId.ToString());
                    _logger.LogInformation($"Connection {connectionId} subscribed to {command.InstrumentId}");

                    await _fintachartsClient.SubscribeAsync(command.InstrumentId, command.Provider, command.Kinds);
                }
                else if (command != null && !command.Subscribe && command.InstrumentId != Guid.Empty)
                {
                    _subscriptionManager.Unsubscribe(connectionId, command.InstrumentId.ToString());
                    _logger.LogInformation($"Connection {connectionId} unsubscribed from {command.InstrumentId}");

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing subscription message.");
            }
        }

        await RemoveConnectionAsync(connectionId);
    }

    public async Task BroadcastAsync(Guid instrumentId, string message)
    {
        _logger.LogInformation($"BroadcastAsync called for instrument {instrumentId}, message length {message.Length}");
        var instrumentIdStr = instrumentId.ToString();
        var subscribers = _subscriptionManager.GetSubscribers(instrumentIdStr);

        var buffer = Encoding.UTF8.GetBytes(message);
        var toRemove = new List<string>();

        foreach (var connectionId in subscribers)
        {
            if (_connections.TryGetValue(connectionId, out var socket))
            {
                if (socket.State == WebSocketState.Open)
                {
                    try
                    {
                        await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch
                    {
                        toRemove.Add(connectionId);
                    }
                }
                else
                {
                    toRemove.Add(connectionId);
                }
            }
            else
            {
                toRemove.Add(connectionId);
            }
        }

        foreach (var connId in toRemove)
        {
            await RemoveConnectionAsync(connId);
        }
    }

    private async Task RemoveConnectionAsync(string connectionId)
    {
        _subscriptionManager.RemoveConnection(connectionId);

        if (_connections.TryRemove(connectionId, out var socket))
        {
            if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
            {
                try
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
                catch { }
            }
            socket.Dispose();
        }

        _logger.LogInformation($"Connection {connectionId} removed");
    }

    private class SubscriptionCommand
    {
        [JsonPropertyName("instrumentId")]
        public Guid InstrumentId { get; set; }
        [JsonPropertyName("subscribe")]
        public bool Subscribe { get; set; }
        [JsonPropertyName("provider")]
        public string Provider { get; set; } = string.Empty;
        [JsonPropertyName("kinds")]
        public List<string> Kinds { get; set; } = new() { "ask", "bid", "last" };
    }
}

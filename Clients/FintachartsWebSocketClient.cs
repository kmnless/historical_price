using historical_prices.Models;
using historical_prices.Services;
using Microsoft.Extensions.Options;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace historical_prices.Clients;

public class FintachartsWebSocketApiClient
{
    private readonly ILogger<FintachartsWebSocketApiClient> _logger;
    private readonly AuthService _authService;
    private readonly FintachartsWebSocketOptions _options;

    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cts;

    public event Func<Guid, string, Task>? OnMessageReceived;

    public FintachartsWebSocketApiClient(ILogger<FintachartsWebSocketApiClient> logger, AuthService authService,
        IOptions<FintachartsWebSocketOptions> options)
    {
        _logger = logger;
        _authService = authService;
        _options = options.Value;
    }

    public async Task StartAsync(CancellationToken externalToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
        var token = await _authService.GetAccessTokenAsync();
        var url = $"{_options.BaseUrl}?token={token}";

        _webSocket = new ClientWebSocket();
        await _webSocket.ConnectAsync(new Uri(url), _cts.Token);
        _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
    }

    public async Task SubscribeAsync(Guid instrumentId, string provider, ICollection<string> kinds)
    {
        if (_webSocket is not { State: WebSocketState.Open })
        {
            _logger.LogWarning("WebSocket not connected. Cannot subscribe.");
            return;
        }

        var subscribeMessage = new
        {
            type = "l1-subscription",
            instrumentId = instrumentId,
            provider = provider,
            subscribe = true,
            kinds = kinds.ToList()
        };

        var json = JsonSerializer.Serialize(subscribeMessage);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        _logger.LogInformation($"Sent subscribe message: {json}");
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];

        try
        {
            while (!cancellationToken.IsCancellationRequested && _webSocket?.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogWarning("WebSocket closed.");
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                _logger.LogInformation($"Received message: {message}");

                try
                {
                    using var doc = JsonDocument.Parse(message);
                    if (doc.RootElement.TryGetProperty("instrumentId", out var idProp) &&
                        Guid.TryParse(idProp.GetString(), out var instrumentId))
                    {
                        if (OnMessageReceived != null)
                            await OnMessageReceived.Invoke(instrumentId, message);
                    }
                    else
                    {
                        _logger.LogWarning("instrumentId not found in message.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during message processing.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReceiveLoopAsync crashed.");
        }
    }

    public async Task StopAsync()
    {
        try
        {
            _cts?.Cancel();
            if (_webSocket?.State == WebSocketState.Open)
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Shutting down", CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during WebSocket shutdown.");
        }
    }
}

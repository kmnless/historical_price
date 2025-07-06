using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace historical_prices.Services;

public class FintachartsWebSocketClientService : BackgroundService
{
    private readonly ILogger<FintachartsWebSocketClientService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public FintachartsWebSocketClientService(
        ILogger<FintachartsWebSocketClientService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var authService = scope.ServiceProvider.GetRequiredService<AuthService>();

                var token = await authService.GetAccessTokenAsync();
                var url = $"wss://platform.fintacharts.com/api/streaming/ws/v1/realtime?token={token}";

                using var client = new ClientWebSocket();
                await client.ConnectAsync(new Uri(url), cancellationToken);
                _logger.LogInformation("WebSocket connected.");

                var subscribeMessage = new
                {
                    type = "l1-subscription",
                    id = Guid.NewGuid().ToString(),
                    instrumentId = "db1246ba-3bb4-4945-8012-381754baab0e",
                    provider = "oanda",
                    subscribe = true,
                    kinds = new[] { "ask", "bid", "last" }
                };

                var json = JsonSerializer.Serialize(subscribeMessage);
                var bytes = Encoding.UTF8.GetBytes(json);
                await client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
                _logger.LogInformation($"Sent subscribe message: {json}");

                var buffer = new byte[8192];
                while (client.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogWarning("WebSocket closed by server.");
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logger.LogInformation($"Received message: {message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WebSocket error, retrying in 5 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }
}

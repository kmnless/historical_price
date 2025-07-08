using historical_prices.Clients;

namespace historical_prices.Services;

public class FintachartsWebSocketHostedService : IHostedService
{
    private readonly FintachartsWebSocketApiClient _client;
    private readonly WebSocketDispatcherService _dispatcher;

    public FintachartsWebSocketHostedService(FintachartsWebSocketApiClient client, WebSocketDispatcherService dispatcher)
    {
        _client = client;
        _dispatcher = dispatcher;

        _client.OnMessageReceived += async (instrumentId, message) =>
        {
            await _dispatcher.BroadcastAsync(instrumentId, message);
        };
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _client.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.StopAsync();
    }
}

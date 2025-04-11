using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using ServiceDelivery.Api;
using ServiceDelivery.Api.Services;
using System.Threading;
using System.Threading.Tasks;

public class StaleConnectionCleanupService : BackgroundService
{
    private readonly IHubContext<NotificationsHub, INotificationClient> _hubContext;
    private readonly IConnectionManager _connectionManager;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _staleTimeout = TimeSpan.FromSeconds(40);

    public StaleConnectionCleanupService(
        IHubContext<NotificationsHub, INotificationClient> hubContext,
        IConnectionManager connectionManager)
    {
        _hubContext = hubContext;
        _connectionManager = connectionManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var staleConnections = _connectionManager.GetStaleConnections(_staleTimeout);

            foreach (var (userId, connectionId) in staleConnections)
            {
                Console.WriteLine($"Stale connection found: {connectionId} (user: {userId})");

                // Ask client to disconnect
                await _hubContext.Clients.Client(connectionId).ForceDisconnect();

                // Clean up immediately or wait for OnDisconnectedAsync
                _connectionManager.RemoveConnection(userId, connectionId);
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}

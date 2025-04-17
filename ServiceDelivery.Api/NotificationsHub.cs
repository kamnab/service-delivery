using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ServiceDelivery.Api.Services;

namespace ServiceDelivery.Api;

// [Authorize]
public class NotificationsHub : Hub<INotificationClient>
{
    private readonly IConnectionManager _connectionManager;

    public NotificationsHub(IConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public override async Task OnConnectedAsync()
    {
#if DEBUG
        // var claims = Context.User?.Claims;
        // if (claims != null)
        // {
        //     foreach (var claim in claims)
        //     {
        //         Console.WriteLine($"{claim.Type}: {claim.Value}");
        //     }
        // }
#endif

        var userId = Context.User?.FindFirst("sub")?.Value;
        var name = Context.User?.FindFirst("name")?.Value; // 'name', not 'Name'
        var email = Context.User?.FindFirst("email")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            userId = "[GUEST] " + Context.ConnectionId;
            // Unauthorized user trying to connect, disconnect immediately
            // await Clients.Client(Context.ConnectionId).ReceiveNotification("Unauthorized user.");
            // await DisconnectClient(Context.ConnectionId);
            // return;
        }

        _connectionManager.AddConnection(userId, Context.ConnectionId);
        await NotifyConnectionCountChanged(userId);

        await Clients.Client(Context.ConnectionId).ReceiveNotification($"You are connected, {name}!");
        await base.OnConnectedAsync();

    }

    // On client disconnected
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst("sub")?.Value;

        Console.WriteLine($"[{DateTime.Now:dd-MM-yy HH:mm:ss}] [{userId}] {nameof(OnDisconnectedAsync)}");

        // Check if there was an exception (unexpected disconnection)
        if (exception != null)
        {
            // Log the reason for the unexpected disconnect
            Console.WriteLine($"Client disconnected unexpectedly: {exception.Message}");
        }

        if (!string.IsNullOrEmpty(userId))
        {
            // Remove the connection from the manager
            _connectionManager.RemoveConnection(userId, Context.ConnectionId);

            await NotifyConnectionCountChanged(userId);

            // Notify remaining clients
            //await Clients.All.ReceiveNotification($"User {userId} disconnected.");
        }

        await base.OnDisconnectedAsync(exception); // Call the base method
    }

    public async Task NotifyConnectionCountChanged(string userId)
    {
        var count = _connectionManager.GetConnections(userId).Count();
        await Clients.User(userId).ConnectionCountUpdated(count);
    }

    public Task Heartbeat()
    {
        var userId = Context.User?.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Task.CompletedTask; // Don't update last seen if user is unauthorized
        }

        _connectionManager.UpdateLastSeen(userId, Context.ConnectionId);
        return Task.CompletedTask;
    }

    // You can trigger this manually when needed to disconnect a client
    private async Task DisconnectClient(string connectionId)
    {
        await Clients.Client(connectionId).ForceDisconnect();
        Context.Abort(); // Force the client to disconnect
    }


}

public interface INotificationClient
{
    Task ReceiveNotification(string message);
    Task ConnectionCountUpdated(int count);
    Task Heartbeat();
    Task ForceDisconnect(); // This is used to trigger disconnection from the server side
}

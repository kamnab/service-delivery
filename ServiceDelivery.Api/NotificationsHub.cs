using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ServiceDelivery.Api.Services;

namespace ServiceDelivery.Api;

[Authorize]
public class NotificationsHub : Hub<INotificationClient>
{
    private readonly IConnectionManager _connectionManager;

    public NotificationsHub(IConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("sub")?.Value;
        var name = Context.User?.FindFirst("name")?.Value; // 'name', not 'Name'
        var email = Context.User?.FindFirst("email")?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            _connectionManager.AddConnection(userId, Context.ConnectionId);
        }

        await Clients.Client(Context.ConnectionId).ReceiveNotification($"You are connected, {name}!");
        await base.OnConnectedAsync();
    }

    // On client disconnected
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.Identity?.Name;

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

            // Notify remaining clients
            //await Clients.All.ReceiveNotification($"User {userId} disconnected.");
        }

        await base.OnDisconnectedAsync(exception); // Call the base method
    }

}

public interface INotificationClient
{
    Task ReceiveNotification(string message);
}

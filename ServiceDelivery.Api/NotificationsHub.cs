using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using ServiceDelivery.Api.Services;

namespace ServiceDelivery.Api;

// [Authorize]
public class NotificationsHub : Hub<INotificationClient>
{
    private readonly IConnectionManager _connectionManager;
    private readonly HubDbContext _dbContext;
    public NotificationsHub(IConnectionManager connectionManager, HubDbContext dbContext)
    {
        _connectionManager = connectionManager;
        _dbContext = dbContext;
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
        var name = Context.User?.FindFirst("name")?.Value;
        var email = Context.User?.FindFirst("email")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            userId = "GUEST-" + Context.ConnectionId;
            // Unauthorized user trying to connect, disconnect immediately
            // await Clients.Client(Context.ConnectionId).ReceiveNotification("Unauthorized user.");
            // await DisconnectClient(Context.ConnectionId);
            // return;
        }

        _connectionManager.AddConnection(userId, Context.ConnectionId);
        await NotifyConnectionCountChanged(userId);

        var appsFiles = Directory.GetFiles("Infrastructure/SharedFiles/Default/Apps")
            .Select(filePath => new FileDescriptor
            {
                FileName = Path.GetFileName(filePath),
                LastModified = File.GetLastWriteTimeUtc(filePath)
            }).ToList();

        var pluginsFiles = Directory.GetFiles("Infrastructure/SharedFiles/Default/Plugins")
            .Select(filePath => new FileDescriptor
            {
                FileName = Path.GetFileName(filePath),
                LastModified = File.GetLastWriteTimeUtc(filePath)
            }).ToList();

        await Clients.Client(Context.ConnectionId)
                .OnConnected(
                    isAuthenticated: Context.User?.Identity?.IsAuthenticated == true,
                    metadata: new Dictionary<string, IEnumerable<FileDescriptor>>()
                    {
                        ["Apps"] = appsFiles,
                        ["Plugins"] = pluginsFiles
                    });

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

        _connectionManager.TryRemoveActivationKey(Context.ConnectionId, out _);
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
            var userGUEST = $"GUEST-{Context.ConnectionId}";
            var connections = _connectionManager.GetConnections(userGUEST);
            foreach (var connectionId in connections)
            {
                _connectionManager.UpdateLastSeen(userGUEST, connectionId);
            }

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

    [Authorize]
    public async Task Activating(string machineId)
    {
        var userId = Context.User?.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            Console.WriteLine($"Activating user_id: {userId}");
        }
        Console.WriteLine($"Activating user_id: {userId}, machineId: {machineId}");

        var keyExisted = await _dbContext.LicenseEntries
            .FirstOrDefaultAsync(x => x.MachineId == machineId);
        if (keyExisted == null)
        {
            // Store the key in Client
            var key = LicenseApp.GenerateKey(
                new LicenseInfo
                {
                    MachineId = machineId,
                    Expiration = DateTime.UtcNow.AddYears(1),
                    Edition = "Pro"
                }
            );

            _connectionManager.SetActivationKey(Context.ConnectionId, key);
            await Clients.Client(Context.ConnectionId).Activating(Context.User?.Identity?.IsAuthenticated == true, key);
        }
        else
        {
            await Clients.Client(Context.ConnectionId).Activated(Context.User?.Identity?.IsAuthenticated == true, keyExisted.LicenseKey);
        }
    }

    [Authorize]
    public async Task Activated(string authorizedKey)
    {
        var userId = Context.User?.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            Console.WriteLine($"Activated user_id: {userId}");
        }

        if (_connectionManager.TryGetActivationKey(Context.ConnectionId, out string? value) && value == authorizedKey)
        {
            if (LicenseApp.Validate(authorizedKey, out var license))
            {
                _dbContext.LicenseEntries.Add(new LicenseEntry
                {
                    Id = Guid.NewGuid(),
                    LicenseKey = authorizedKey,
                    MachineId = license.MachineId,
                    Expiration = license.Expiration
                });

                await _dbContext.SaveChangesAsync();
                // Store the key in Server
                await Clients.Client(Context.ConnectionId).Activated(Context.User?.Identity?.IsAuthenticated == true, authorizedKey);
                await Clients.Client(Context.ConnectionId).ReceiveNotification($"The {authorizedKey} is paired and saved to Db.");
            }
            else
            {
                await Clients.Client(Context.ConnectionId).ReceiveNotification($"Invalid license.");
            }
        }
    }

    // [Authorize(policy: "MachineValidationFilter")]
    public Task ClientResponse(string machineId, string message)
    {
        var userId = Context.User?.FindFirst("sub")?.Value;
        Console.WriteLine($"ClientResponse: user_id: {userId}, message: {message}");


        return Task.CompletedTask;
    }

    // public async Task SyncFolder()
    // {
    //     var fileList = Directory.GetFiles("Infrastructure/SharedFiles")
    //         .Select(file => new FileDescriptor
    //         {
    //             FileName = Path.GetFileName(file),
    //             LastModified = File.GetLastWriteTimeUtc(file)
    //         }).ToList();

    //     await Clients.Client(Context.ConnectionId).ReceiveFileManifest(fileList, );
    // }
}

public interface INotificationClient
{
    Task OnConnected(bool isAuthenticated, Dictionary<string, IEnumerable<FileDescriptor>> metadata);
    Task ReceiveNotification(string message);
    Task ConnectionCountUpdated(int count);
    Task Activating(bool isAuthenticated, string key);
    Task Activated(bool isAuthenticated, string confirmedKey);
    Task ForceDisconnect(); // This is used to trigger disconnection from the server side
    Task ReceiveRequest(string pluginName);
    Task ReceiveAppsManifest(IEnumerable<FileDescriptor> fileList, string hostPath = ""); // This is used to trigger disconnection from the server side
    Task ReceivePluginsManifest(IEnumerable<FileDescriptor> fileList, string hostPath = ""); // This is used to trigger disconnection from the server side
    Task ReceiveFileManifest(IEnumerable<FileDescriptor> fileList, string destination); // This is used to trigger disconnection from the server side
}

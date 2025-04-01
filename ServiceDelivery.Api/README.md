# For Controller-Based APIs:
```
var builder = WebApplication.CreateBuilder(args);

// Add OpenAPI/Swagger services
builder.Services.AddSwaggerGen(); // This is the primary service for Swagger

var app = builder.Build();

// Enable Swagger and Swagger UI
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    options.RoutePrefix = string.Empty; // Optional: Makes Swagger UI available at the root (localhost:5000)
});

app.UseHttpsRedirection();

app.Run();

```

# For Minimal APIs (If you are using them):
```
var builder = WebApplication.CreateBuilder(args);

// Add OpenAPI/Swagger services
builder.Services.AddSwaggerGen(); // This is the primary service for Swagger
builder.Services.AddEndpointsApiExplorer(); // This is for minimal APIs

var app = builder.Build();

// Expose the OpenAPI specification and Swagger UI
app.MapOpenApi(); // Exposes OpenAPI JSON for minimal APIs
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    options.RoutePrefix = string.Empty; // Optional: Makes Swagger UI available at the root (localhost:5000)
});

app.UseHttpsRedirection();

app.Run();
```

### To summarize:
- If you're using minimal APIs, you should keep builder.Services.AddEndpointsApiExplorer().
- If you're using controllers (with [ApiController]), you don't need builder.Services.AddEndpointsApiExplorer().


- dotnet add package Swashbuckle.AspNetCore
- Microsoft.AspNetCore.Authentication.JwtBearer

### Generate jwt access token

- dotnet user-jwts create -n "7298fda4-2bce-49e5-b284-39c3acc968f1"

# 1. Use an In-Memory Connection Store (Thread-Safe)
#### Step 1: Create a Connection Mapping Service IConnectionManager
#### Step 2: Register the Service in Program.cs
#### Step 3: Update NotificationsHub to Track Connections
```
public class NotificationsHub : Hub<INotificationClient>
{
    private readonly IConnectionManager _connectionManager;

    public NotificationsHub(IConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.Identity?.Name;
        if (!string.IsNullOrEmpty(userId))
        {
            _connectionManager.AddConnection(userId, Context.ConnectionId);
        }

        await Clients.Client(Context.ConnectionId).ReceiveNotification($"Thank you for connecting {userId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.Identity?.Name;
        if (!string.IsNullOrEmpty(userId))
        {
            _connectionManager.RemoveConnection(userId, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}

```

#### Step 4: Use Connection Manager in a Controller
```
[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly IHubContext<NotificationsHub, INotificationClient> _hubContext;
    private readonly IConnectionManager _connectionManager;

    public NotificationsController(IHubContext<NotificationsHub, INotificationClient> hubContext, IConnectionManager connectionManager)
    {
        _hubContext = hubContext;
        _connectionManager = connectionManager;
    }

    [HttpPost("send/{userId}")]
    public async Task<IActionResult> SendNotification(string userId, [FromBody] string message)
    {
        var connections = _connectionManager.GetConnections(userId);
        foreach (var connectionId in connections)
        {
            await _hubContext.Clients.Client(connectionId).ReceiveNotification(message);
        }

        return Ok();
    }
}
```

Minimal API to Send Notification
```

app.MapPost("/api/notifications/send/{userId}", async (string userId, string message, 
    IHubContext<NotificationsHub, INotificationClient> hubContext, 
    IConnectionManager connectionManager) =>
{
    var connections = connectionManager.GetConnections(userId);
    foreach (var connectionId in connections)
    {
        await hubContext.Clients.Client(connectionId).ReceiveNotification(message);
    }

    return Results.Ok(new { Message = "Notification sent successfully." });
});
```
## 2. Use a Persistent Store (Database)
If you need persistence across application restarts, store the connection-user mapping in a database like Redis, SQL Server, or MongoDB.

For Redis, you can use StackExchange.Redis:
```
public class RedisConnectionManager : IConnectionManager
{
    private readonly IDatabase _database;

    public RedisConnectionManager(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public void AddConnection(string userId, string connectionId)
    {
        _database.SetAdd($"connections:{userId}", connectionId);
    }

    public void RemoveConnection(string userId, string connectionId)
    {
        _database.SetRemove($"connections:{userId}", connectionId);
    }

    public IEnumerable<string> GetConnections(string userId)
    {
        return _database.SetMembers($"connections:{userId}").Select(m => m.ToString());
    }
}

```
Register Redis in Program.cs:
```
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost"));
builder.Services.AddScoped<IConnectionManager, RedisConnectionManager>();

```
#### Which One to Choose?
- Use ConcurrentDictionary if you only need to store data in memory.
- Use Redis or a database if persistence across application restarts is needed.

Would you like a database implementation example with SQL Server or MongoDB?

# Implement Group Management Functionality
1. Group Membership Management: Track users and their group memberships.

2. Adding Users to Groups: Add users to specific groups dynamically.

3. Removing Users from Groups: Remove users from groups when necessary.

4. Listing Group Members: Optionally, retrieve a list of users in a group.

5. Sending Messages to Groups: Allow sending notifications or messages to a specific group.
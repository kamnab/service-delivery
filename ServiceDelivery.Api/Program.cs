using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Validation.AspNetCore;
using ServiceDelivery.Api;
using ServiceDelivery.Api.Services;
using Talkemie.Authentication.Infrastructure.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Set the URL to use localhost on specific ports
builder.WebHost.UseUrls("http://localhost:5000", "https://localhost:5001");

var connectionString = builder.Configuration.GetConnectionString(EFSettings.DB_CONNECTION_NAME) ?? throw new InvalidOperationException($"Connection string '{EFSettings.DB_CONNECTION_NAME}' not found.");

builder.Services.AddDbContext<HubDbContext>(options =>
{
    // Configure the context to use sqlite.
    //options.UseSqlite($"Filename={Path.Combine(Path.GetTempPath(), "openiddict-velusia-server2.sqlite3")}");
    options.UseSqlServer(connectionString, x =>
    {
        x.MigrationsHistoryTable(EFSettings.DB_EFMigrationsHistory, EFSettings.DB_SCHEME);
        x.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
    });
});

// Add OpenAPI and Swagger UI support
builder.Services.AddEndpointsApiExplorer(); // Needed for minimal APIs
builder.Services.AddSwaggerGen(); // Adds full Swagger UI support

builder.Services.AddSignalR(options =>
{
    options.AddFilter<MachineValidationFilter>();

    options.KeepAliveInterval = TimeSpan.FromSeconds(15); // How often server pings the client
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30); // If no message is received in this time, disconnect
});
// builder.Services.AddHostedService<ServerTimeNotifier>();

// Register the OpenIddict validation components.
builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        // Note: the validation handler uses OpenID Connect discovery
        // to retrieve the issuer signing keys used to validate tokens.
        options.SetIssuer("https://localhost:44313/");
        options.AddAudiences("sdc-resources");

        // Register the encryption credentials. This sample uses a symmetric
        // encryption key that is shared between the server and the Api2 sample
        // (that performs local token validation instead of using introspection).
        //
        // Note: in a real world application, this encryption key should be
        // stored in a safe place (e.g in Azure KeyVault, stored as a secret).
        options
            .AddEncryptionCertificate(OpenIddictHelpers.LoadEncryptionCertificate())
            .AddSigningCertificate(OpenIddictHelpers.LoadSigningCertificate());

        // Register the System.Net.Http integration.
        options.UseSystemNetHttp();

        // Register the ASP.NET Core host.
        options.UseAspNetCore();
    });

builder.Services.AddCors();

builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
builder.Services.AddAuthorization();

// Services
builder.Services.AddSingleton<IConnectionManager, ConnectionManager>();
builder.Services.AddSingleton<IUserIdProvider, NameIdentifierProvider>();

builder.Services.AddHostedService<StaleConnectionCleanupService>();

var app = builder.Build();

// Enable Swagger and Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Minimal API v1");
        options.RoutePrefix = string.Empty; // Makes Swagger UI available at root URL, otherwise, https://localhost:5001/swagger
    });
}

app.UseCors(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.MapHub<NotificationsHub>("Notifications");

#region Minimal APIs

app.MapGet("/api/v1/connections/all", (IConnectionManager connectionManager) =>
{
    var allConnections = connectionManager.GetAllConnections();
    return Results.Ok(allConnections);
});

app.MapGet("/api/v1/connections/{userId}", (string userId, IConnectionManager connectionManager) =>
{
    var connections = connectionManager.GetConnections(userId);

    return Results.Ok(new { UserId = userId, Connections = connections });
});

// Minimal API to Send Notification
app.MapPost("/api/v1/connections/send/{userId}", async (string userId, string message,
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

app.MapPost("/api/v1/connections/close/{connectionId}", async (string connectionId, IHubContext<NotificationsHub, INotificationClient> hubContext) =>
{
    // Invoke the ForceDisconnect method in the hub
    await hubContext.Clients.Client(connectionId).ForceDisconnect();

    return Results.Ok(new { Message = $"{connectionId} is disconnecting." });
});

app.MapPost("/api/v1/conns/request/{connectionId}", async (string connectionId, string pluginName, IHubContext<NotificationsHub, INotificationClient> hubContext) =>
{
    // Invoke the ReceiveRequest method in the hub
    await hubContext.Clients.Client(connectionId).ReceiveRequest(pluginName);

    return Results.Ok(new { Message = $"Sending cmd:ReceiveRequest to client with connection: {connectionId}" });
});

app.MapPost("/deliver/apps/{fileName}", async (HttpContext context, IHubContext<NotificationsHub, INotificationClient> hubContext, string connectionId, [FromRoute] string fileName, string destPath = "") =>
{
    var path = Path.Combine("Infrastructure/SharedFiles/Default/Apps", fileName);
    if (!File.Exists(path))
    {
        context.Response.StatusCode = 404;
        return;
    }

    var boardingFile = new FileDescriptor
    {
        FileName = fileName,
        LastModified = File.GetLastWriteTimeUtc(path)
    };

    await hubContext.Clients.Client(connectionId).ReceiveAppsManifest([boardingFile], destPath);
});


app.MapPost("/deliver/plugins/{fileName}", async (HttpContext context, IHubContext<NotificationsHub, INotificationClient> hubContext, string connectionId, [FromRoute] string fileName, string destPath = "") =>
{
    var path = Path.Combine("Infrastructure/SharedFiles/Default/Plugins", fileName);
    if (!File.Exists(path))
    {
        context.Response.StatusCode = 404;
        return;
    }

    var boardingFile = new FileDescriptor
    {
        FileName = fileName,
        LastModified = File.GetLastWriteTimeUtc(path)
    };

    await hubContext.Clients.Client(connectionId).ReceivePluginsManifest([boardingFile], destPath);
});

app.MapPost("/deliver/file/{fileName}", async (HttpContext context, IHubContext<NotificationsHub, INotificationClient> hubContext,
    string connectionId,
    [FromRoute] string fileName,
    string destPath /* start from root path => ./ */) =>
{
    var path = Path.Combine("Infrastructure/SharedFiles/Default/File", fileName);
    if (!File.Exists(path))
    {
        context.Response.StatusCode = 404;
        return;
    }

    var boardingFile = new FileDescriptor
    {
        FileName = fileName,
        LastModified = File.GetLastWriteTimeUtc(path)
    };

    await hubContext.Clients.Client(connectionId).ReceiveFileManifest([boardingFile], destPath);
});

app.MapGet("/download/{machineId}/{folder}/{fileName}", async (
    [FromRoute] string machineId,
    [FromRoute] string folder,
    [FromRoute] string fileName,
    HttpContext context,
    IHubContext<NotificationsHub, INotificationClient> hubContext,
    IConnectionManager manager) =>
{
    if (!await manager.IsAuthorizedMachine(machineId))
    {
        context.Response.StatusCode = 401;
        return;
    }

    var path = Path.Combine("Infrastructure/SharedFiles/Default", folder, fileName);
    if (!File.Exists(path))
    {
        context.Response.StatusCode = 404;
        return;
    }

    context.Response.ContentType = "application/octet-stream";
    context.Response.Headers.ContentDisposition = $"attachment; filename={fileName}";
    await context.Response.SendFileAsync(path);
});
#endregion

app.Run();

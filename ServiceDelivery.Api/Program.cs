using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Validation.AspNetCore;
using ServiceDelivery.Api;
using ServiceDelivery.Api.Services;
using Talkemie.Authentication.Infrastructure.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Set the URL to use localhost on specific ports
builder.WebHost.UseUrls("http://localhost:5000", "https://localhost:5001");

// Add OpenAPI and Swagger UI support
builder.Services.AddEndpointsApiExplorer(); // Needed for minimal APIs
builder.Services.AddSwaggerGen(); // Adds full Swagger UI support

builder.Services.AddSignalR(options =>
{
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

#endregion

app.Run();

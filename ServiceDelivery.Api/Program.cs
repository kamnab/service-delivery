using System.ComponentModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using ServiceDelivery.Api;
using ServiceDelivery.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Set the URL to use localhost on specific ports
builder.WebHost.UseUrls("http://localhost:5000", "https://localhost:5001");

// Add OpenAPI and Swagger UI support
builder.Services.AddEndpointsApiExplorer(); // Needed for minimal APIs
builder.Services.AddSwaggerGen(); // Adds full Swagger UI support

builder.Services.AddSignalR();

// builder.Services.AddHostedService<ServerTimeNotifier>();
builder.Services.AddCors();

// Microsoft.AspNetCore.Authentication.JwtBearer
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

// Services
builder.Services.AddSingleton<IConnectionManager, ConnectionManager>();

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

// Microsoft.AspNetCore.Authentication.JwtBearer
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


#endregion

app.Run();

using System.ComponentModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ServiceDelivery.Api;

var builder = WebApplication.CreateBuilder(args);

// Set the URL to use localhost on specific ports
builder.WebHost.UseUrls("http://localhost:5000", "https://localhost:5001");

// Add OpenAPI and Swagger UI support
builder.Services.AddEndpointsApiExplorer(); // Needed for minimal APIs
builder.Services.AddSwaggerGen(); // Adds full Swagger UI support

builder.Services.AddSignalR();

builder.Services.AddHostedService<ServerTimeNotifier>();
builder.Services.AddCors();

// Microsoft.AspNetCore.Authentication.JwtBearer
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();


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

// Define minimal API endpoints
app.MapGet("/hello", () => "Hello, Minimal API!");
app.MapPost("/echo", (string message) => $"You said: {message}");

app.UseHttpsRedirection();

app.MapHub<NotificationsHub>("Notifications");

app.Run();

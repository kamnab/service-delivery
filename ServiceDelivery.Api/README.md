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
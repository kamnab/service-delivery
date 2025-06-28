using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Delay HTTPS configuration until cert file exists
var certPath = "/https/https.pfx";
var certPassword = "";
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(443, listenOptions =>
    {
        listenOptions.UseHttps(certPath, certPassword);
    });
    options.ListenAnyIP(80); // Optional HTTP
});


// Add services to the container.
builder.Services.AddControllersWithViews();

#region PersistKeysToFileSystem 
var keyStoragePath = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"
    ? "/app/Infrastructure/Resources" // for Docker
    : Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Resources"); // for local dev

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keyStoragePath))
    .SetApplicationName("ODI Profile Service");
#endregion

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie()
.AddOpenIdConnect(options =>
{
    options.Authority = builder.Configuration["Auth:Issuer"]; // Your Identity Server
    options.ClientId = "sdc-client-activation";
    options.ResponseType = "code";
    options.UsePkce = true;  // PKCE for public clients
    options.SaveTokens = true;

    // These are the defaults and correspond exactly to the URIs you gave
    options.CallbackPath = "/signin-oidc";
    options.SignedOutCallbackPath = "/signout-callback-oidc";

    // Scopes
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Scope.Add("offline_access");
    options.Scope.Add("sdc-api");

    // Optional: configure token validation, events, etc.
    options.Events.OnRedirectToIdentityProvider = context =>
    {
        var scheme = context.Request.Headers["X-Forwarded-Proto"].ToString() ?? "http";
        context.ProtocolMessage.RedirectUri = $"{scheme}://{context.Request.Host}{context.Options.CallbackPath}";
        return Task.CompletedTask;
    };
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddAuthorization(); // << Add this!

builder.Services.AddHttpClient(); // Required for IHttpClientFactory
builder.Services.AddScoped<ITokenRefreshService, TokenRefreshService>();
builder.Services.AddCors();

var app = builder.Build();

// Tell ASP.NET Core to use forwarded headers to detect original scheme and host
// 👇 You already have this (good!)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownProxies = { IPAddress.Parse("172.18.0.2") } // your NPM container IP
});

// ✅ Debug: Show forwarded headers & remote IP
app.Use(async (context, next) =>
{
    /*
    - If you see https, your app will know the original request scheme was HTTPS and will generate correct redirect URLs.
    - If it’s empty or http, then the problem is on NPM’s side not forwarding the header correctly.
    */
    var remoteIp = context.Connection.RemoteIpAddress;
    var proto = context.Request.Headers["X-Forwarded-Proto"].ToString();

    Console.WriteLine($"Remote IP: {remoteIp}");
    Console.WriteLine($"X-Forwarded-Proto: {proto}");

    if (context.Request.Path.StartsWithSegments("/signin-oidc"))
    {
        Console.WriteLine("=== /signin-oidc Headers ===");
        foreach (var h in context.Request.Headers)
            Console.WriteLine($"{h.Key}: {h.Value}");
    }

    var forwardedForExists = context.Request.Headers.ContainsKey("X-Forwarded-For");

    var isLocalRequest = remoteIp != null && IPAddress.IsLoopback(remoteIp);

    if (!forwardedForExists && !isLocalRequest)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsync("Direct access blocked. Use the reverse proxy.");
        return;
    }

    await next();
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseRouting();
app.UseCors(configure => configure.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

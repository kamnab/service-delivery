using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Delay HTTPS configuration until cert file exists
// var certPath = "/https/https.pfx";
// var certPassword = "";
// builder.WebHost.ConfigureKestrel(options =>
// {
//     options.ListenAnyIP(443, listenOptions =>
//     {
//         listenOptions.UseHttps(certPath, certPassword);
//     });
//     options.ListenAnyIP(80); // Optional HTTP
// });


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
    // 🔒 Fix the redirect URI to use the real domain
    options.Events.OnRedirectToIdentityProvider = context =>
    {
        var request = context.Request;

        var redirectUri = new UriBuilder
        {
            Scheme = request.Scheme,          // Should be https from proxy
            Host = request.Host.Host,         // odi-profile.codemie.dev
            Port = request.Host.Port ?? -1,   // 443 or empty
            Path = context.ProtocolMessage.RedirectUri
        };

        context.ProtocolMessage.RedirectUri = redirectUri.Uri.ToString();
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToIdentityProviderForSignOut = context =>
    {
        var request = context.Request;

        var postLogoutRedirectUri = new UriBuilder
        {
            Scheme = request.Scheme,
            Host = request.Host.Host,
            Port = request.Host.Port ?? -1,
            Path = context.Properties.RedirectUri
        };

        context.ProtocolMessage.PostLogoutRedirectUri = postLogoutRedirectUri.Uri.ToString();
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

// -------------------- FORWARDED HEADERS --------------------
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // Optionally clear known networks/proxies (for Docker environments)
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Ensure forwarded headers are processed FIRST
app.UseForwardedHeaders();

// 🔍 Debug log (remove later)
app.Use(async (context, next) =>
{
    Console.WriteLine("---- Incoming Request ----");
    Console.WriteLine("Scheme: " + context.Request.Scheme);
    Console.WriteLine("Host: " + context.Request.Host);
    Console.WriteLine("Path: " + context.Request.Path);
    Console.WriteLine("X-Forwarded-Proto: " + context.Request.Headers["X-Forwarded-Proto"]);
    await next();
});

// ✅ Debug: Show forwarded headers & remote IP
// app.Use(async (context, next) =>
// {
//     /*
//     - If you see https, your app will know the original request scheme was HTTPS and will generate correct redirect URLs.
//     - If it’s empty or http, then the problem is on NPM’s side not forwarding the header correctly.
//     */
//     var remoteIp = context.Connection.RemoteIpAddress;
//     var proto = context.Request.Headers["X-Forwarded-Proto"].ToString();

//     Console.WriteLine($"Request Scheme: {context.Request.Scheme}");
//     Console.WriteLine($"Request Host: {context.Request.Host}");
//     Console.WriteLine($"X-Forwarded-Proto: {proto}");

//     if (context.Request.Path.StartsWithSegments("/signin-oidc"))
//     {
//         Console.WriteLine("=== /signin-oidc Headers ===");
//         foreach (var h in context.Request.Headers)
//             Console.WriteLine($"{h.Key}: {h.Value}");
//     }

//     // var forwardedForExists = context.Request.Headers.ContainsKey("X-Forwarded-For");

//     // var isLocalRequest = remoteIp != null && IPAddress.IsLoopback(remoteIp);

//     // if (!forwardedForExists && !isLocalRequest)
//     // {
//     //     context.Response.StatusCode = StatusCodes.Status403Forbidden;
//     //     await context.Response.WriteAsync("Direct access blocked. Use the reverse proxy.");
//     //     return;
//     // }

//     await next();
// });

app.Use((context, next) =>
{
    var proto = context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault();
    if (!string.IsNullOrEmpty(proto))
    {
        context.Request.Scheme = proto;
    }
    return next();
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

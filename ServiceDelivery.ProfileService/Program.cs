using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
// builder.WebHost.UseUrls("https://localhost:5501");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDataProtection();

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
});

builder.Services.AddAuthorization(); // << Add this!

#region PersistKeysToFileSystem 
// var keyStoragePath = Environment.GetEnvironmentVariable("CONTAINERIZE") == "true"
//     ? "/app/Infrastructure/Resources" // for Docker
//     : Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Resources"); // for local dev

// builder.Services.AddDataProtection()
//     .PersistKeysToFileSystem(new DirectoryInfo(keyStoragePath))
//     .SetApplicationName("ODI Profile Service");
#endregion


builder.Services.AddHttpClient(); // Required for IHttpClientFactory
builder.Services.AddScoped<ITokenRefreshService, TokenRefreshService>();
builder.Services.AddCors();

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    ForwardLimit = null,   // Trust all proxies (optional, but common in Docker)
    KnownNetworks = { },   // Clear to trust any network (optional)
    KnownProxies = { }
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
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

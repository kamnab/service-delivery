using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ServiceDelivery.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#kt_wrapper");
builder.RootComponents.Add<HeadOutlet>("head::after");

#region Balosar.ServerAPI

builder.Services.AddHttpClient("Balosar.ServerAPI")
    .ConfigureHttpClient(client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))

    /*
    This is what makes the HttpClient include access tokens in its requests.
    */
    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

// Supply HttpClient instances that include access tokens when making requests to the server project.
builder.Services.AddScoped(provider =>
{
    var factory = provider.GetRequiredService<IHttpClientFactory>();
    return factory.CreateClient("Balosar.ServerAPI");
});

builder.Services.AddOidcAuthentication(options =>
{
    builder.Configuration.Bind("Oidc", options.ProviderOptions);

    // options.ProviderOptions.ClientId = "sdc-v1";
    // options.ProviderOptions.Authority = "https://localhost:44313/";
    // options.ProviderOptions.ResponseType = "code";

    // Note: response_mode=fragment is the best option for a SPA. Unfortunately, the Blazor WASM
    // authentication stack is impacted by a bug that prevents it from correctly extracting
    // authorization error responses (e.g error=access_denied responses) from the URL fragment.
    // For more information about this bug, visit https://github.com/dotnet/aspnetcore/issues/28344.
    options.ProviderOptions.ResponseMode = "query";
    options.AuthenticationPaths.RemoteRegisterPath = $"{options.ProviderOptions.Authority}Identity/Account/Register";

    // Add the "roles" (OpenIddictConstants.Scopes.Roles) scope and the "role" (OpenIddictConstants.Claims.Role) claim
    // (the same ones used in the Startup class of the Server) in order for the roles to be validated.
    // See the Counter component for an example of how to use the Authorize attribute with roles
    // options.ProviderOptions.DefaultScopes.Add("roles");
    options.UserOptions.RoleClaim = "role";

});

#endregion

builder.Services.AddScoped<SignalRService>();
builder.Services.AddScoped<NetworkStatusService>();

var host = builder.Build();

await host.RunAsync();


// dotnet add package IdentityModel --version 6.0.0

using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System;
using System.Net.Http;
using System.Threading.Tasks;

public class TokenRefreshService : ITokenRefreshService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TokenRefreshService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> TryRefreshAccessTokenAsync(HttpContext context)
    {
        var refreshToken = await context.GetTokenAsync("refresh_token");
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        var disco = await _httpClientFactory.CreateClient().GetDiscoveryDocumentAsync("https://localhost:44313");
        if (disco.IsError)
            return null;

        var response = await _httpClientFactory.CreateClient().RequestRefreshTokenAsync(new RefreshTokenRequest
        {
            Address = disco.TokenEndpoint,
            ClientId = "sdc-client-activation",
            RefreshToken = refreshToken
        });

        if (response.IsError)
            return null;

        var result = await context.AuthenticateAsync();
        result.Properties.UpdateTokenValue("access_token", response.AccessToken);
        result.Properties.UpdateTokenValue("refresh_token", response.RefreshToken);
        result.Properties.UpdateTokenValue("expires_at", DateTime.UtcNow.AddSeconds(response.ExpiresIn).ToString("o"));

        await context.SignInAsync(result.Principal, result.Properties);

        return response.AccessToken;
    }
}

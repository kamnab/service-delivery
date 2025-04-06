
# dotnet new blazorwasm -o ServiceDelivery.Client
- Microsoft.AspNetCore.SignalR.Client 


# Add openiddict for authentication

- dotnet add package Microsoft.Extensions.Http ()
- dotnet add package Microsoft.AspNetCore.Components.WebAssembly.Authentication ()

### 🔍 What Is BaseAddressAuthorizationMessageHandler?
This is what makes the HttpClient include access tokens in its requests.
BaseAddressAuthorizationMessageHandler is provided by the Blazor WebAssembly authentication library, and it’s designed specifically to:

- Check if the request is going to your app’s base URL (i.e., the API backend).
- If so, automatically:
    - Fetch a valid access token.
    - Add it to the request as an Authorization: Bearer <token> header.

So by adding this handler to a named HttpClient, you're saying:
```
"Use this handler to inject auth tokens for protected API requests."
```

### 🧠 How It Works Behind the Scenes
When a Blazor WASM app uses AddOidcAuthentication(...), it sets up the authentication system.

BaseAddressAuthorizationMessageHandler then plugs into this system and:
Knows how to request and cache tokens.
Only applies to URLs that match the configured base address.
Throws if a token is required and not available.

### 🔄 Authenticated HttpClient Request Flow
🧭 Step-by-Step Visual
[ Blazor WASM App ]
       |
       | 1. User logs in via OIDC (redirect to IdentityServer)
       v
[ IdentityServer (e.g., ASP.NET Core Identity + OpenIddict) ]
       |
       | 2. Sends back access token (in memory only in WASM)
       v
[ Blazor Auth System ]
       |
       | 3. HttpClient (with BaseAddressAuthorizationMessageHandler) makes request
       |
       v
[ BaseAddressAuthorizationMessageHandler ]
       |
       | 4. Checks base URL (must match app origin or allowed URLs)
       | 5. Gets access token from auth system
       | 6. Adds Authorization header:
       |     Authorization: Bearer eyJhbGciOi...
       v
[ API Server (e.g., https://localhost:44313) ]
       |
       | 7. Server validates token
       | 8. Returns secured data (e.g., weather forecast)


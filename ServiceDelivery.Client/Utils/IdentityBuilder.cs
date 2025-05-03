using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Collections.Generic;

public class IdentityBuilder
{
    public static ClaimsIdentity BuildIdentityFromIdToken(string idToken, string authType)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(idToken);

        var claims = new List<Claim>();

        // Add common claims
        claims.Add(new Claim(ClaimTypes.NameIdentifier, jwt.Subject));

        // "name" claim → ClaimTypes.Name
        if (jwt.Payload.TryGetValue("name", out var name))
        {
            claims.Add(new Claim(ClaimTypes.Name, name.ToString()));
        }

        // "preferred_username" → optional
        if (jwt.Payload.TryGetValue("preferred_username", out var username))
        {
            claims.Add(new Claim("preferred_username", username.ToString()));
        }

        // Add all other claims if you want
        foreach (var kvp in jwt.Payload)
        {
            // Avoid duplicates of already added standard claims
            if (kvp.Key == "sub" || kvp.Key == "name" || kvp.Key == "preferred_username")
                continue;

            claims.Add(new Claim(kvp.Key, kvp.Value.ToString()));
        }

        return new ClaimsIdentity(claims, authType);
    }
}

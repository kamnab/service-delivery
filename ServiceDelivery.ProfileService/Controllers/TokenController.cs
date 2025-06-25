using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


[Authorize]
[Route("internal/token")]
public class TokenController : Controller
{
    private readonly ITokenRefreshService _tokenRefresh;

    public TokenController(ITokenRefreshService tokenRefresh)
    {
        _tokenRefresh = tokenRefresh;
    }

    [HttpGet]
    public async Task<IActionResult> GetAccessToken()
    {
        var token = await HttpContext.GetTokenAsync("access_token");
        var expiresAt = await HttpContext.GetTokenAsync("expires_at");

        if (DateTime.TryParse(expiresAt, out var expiry) && expiry < DateTime.UtcNow)
        {
            token = await _tokenRefresh.TryRefreshAccessTokenAsync(HttpContext);
        }

        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var userId = User.FindFirst("sub")?.Value
                  ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Json(new { user_id = userId, access_token = token });
    }
}

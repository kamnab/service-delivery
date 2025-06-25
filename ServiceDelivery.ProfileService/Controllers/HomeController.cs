using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class HomeController : Controller
{
    public IActionResult Index() => View();

    [Authorize]
    public IActionResult Logout()
    {
        return SignOut("Cookies", OpenIdConnectDefaults.AuthenticationScheme);
    }

    [Authorize]
    [HttpGet("/loading")]
    public IActionResult Loading() => View();

}

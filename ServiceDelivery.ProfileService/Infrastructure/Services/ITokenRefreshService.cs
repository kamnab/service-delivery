using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

public interface ITokenRefreshService
{
    Task<string> TryRefreshAccessTokenAsync(HttpContext context);
}

/*/ Needed so SignalR knows which connections belong to which userId

📌 This tells SignalR:
“When a connection comes in, grab the sub claim from their identity and treat that as their user ID.”

Once you have that in place, Clients.User(userId) will automatically work for any connection where the authenticated user has a sub claim.
*/

using Microsoft.AspNetCore.SignalR;

public class NameIdentifierProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst("sub")?.Value;
    }
}

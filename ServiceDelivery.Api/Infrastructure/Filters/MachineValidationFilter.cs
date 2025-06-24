using Microsoft.AspNetCore.SignalR;
using ServiceDelivery.Api.Services;

public class MachineValidationFilter : IHubFilter
{
    private readonly IConnectionManager _manager;

    public MachineValidationFilter(IConnectionManager manager)
    {
        _manager = manager;
    }

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        if (invocationContext.HubMethodName == "ClientResponse")
        {
            var connectionId = invocationContext.Context.ConnectionId;
            var machineId = invocationContext.HubMethodArguments[0]?.ToString();

            if (!await _manager.IsAuthorizedMachine(machineId!))
            {
                throw new HubException("Machine not authorized.");
            }
        }

        return await next(invocationContext);
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ServiceDelivery.Api.Services;

public interface IConnectionManager
{
    void AddConnection(string userId, string connectionId);
    void RemoveConnection(string userId, string connectionId);
    IEnumerable<string> GetConnections(string userId);
    Dictionary<string, List<ConnectionInfo>> GetAllConnections();
    void UpdateLastSeen(string userId, string connectionId);
    List<(string UserId, string ConnectionId)> GetStaleConnections(TimeSpan timeout);

    void SetActivationKey(string connectionId, string key);
    bool TryGetActivationKey(string connectionId, out string key);
    bool TryRemoveActivationKey(string connectionId, out string key);
    Task<bool> IsAuthorizedMachine(string machineId);

}

public class ConnectionInfo
{
    public string ConnectionId { get; set; } = string.Empty;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow.AddHours(7);
}

public class ConnectionManager : IConnectionManager
{
    private readonly IServiceProvider _serviceProvider;

    public ConnectionManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    // Key: userId, Value: Dictionary of connectionId to ConnectionInfo
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ConnectionInfo>> _connections
        = new();

    public void AddConnection(string userId, string connectionId)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(connectionId)) return;

        var userConnections = _connections.GetOrAdd(userId, _ => new ConcurrentDictionary<string, ConnectionInfo>());
        userConnections[connectionId] = new ConnectionInfo
        {
            ConnectionId = connectionId,
            LastSeen = DateTime.UtcNow
        };
    }

    public void RemoveConnection(string userId, string connectionId)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(connectionId)) return;

        if (_connections.TryGetValue(userId, out var userConnections))
        {
            userConnections.TryRemove(connectionId, out _);

            if (userConnections.IsEmpty)
            {
                _connections.TryRemove(userId, out _);
            }
        }
    }

    public IEnumerable<string> GetConnections(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return Enumerable.Empty<string>();

        return _connections.TryGetValue(userId, out var userConnections)
            ? userConnections.Keys.ToList()
            : Enumerable.Empty<string>();
    }

    public Dictionary<string, List<ConnectionInfo>> GetAllConnections()
    {
        return _connections
            .SelectMany(kvp => kvp.Value.Values.Select(x => new { kvp.Key, kvp.Value, x.LastSeen }))
            .OrderByDescending(c => c.LastSeen)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Values
                    .Select(c => new ConnectionInfo
                    {
                        ConnectionId = c.ConnectionId,
                        LastSeen = c.LastSeen
                    }).ToList());

        // Deep copy to avoid exposing internal collections
        // return _connections.ToDictionary(
        //     kvp => kvp.Key,
        //     kvp => kvp.Value.Values
        //         .Select(c => new ConnectionInfo
        //         {
        //             ConnectionId = c.ConnectionId,
        //             LastSeen = c.LastSeen
        //         }).ToList()
        // );
    }

    public void UpdateLastSeen(string userId, string connectionId)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(connectionId)) return;

        if (_connections.TryGetValue(userId, out var userConnections) &&
            userConnections.TryGetValue(connectionId, out var conn))
        {
            conn.LastSeen = DateTime.UtcNow;
            Console.WriteLine($"userId: {userId}, connectionId: {connectionId}, lastSeen: {conn.LastSeen.AddHours(7)} (+7)");
        }
    }

    public List<(string UserId, string ConnectionId)> GetStaleConnections(TimeSpan timeout)
    {
        var staleList = new List<(string, string)>();
        var cutoff = DateTime.UtcNow - timeout;

        foreach (var (userId, userConnections) in _connections)
        {
            foreach (var (connectionId, info) in userConnections)
            {
                if (info.LastSeen < cutoff)
                {
                    staleList.Add((userId, connectionId));
                }
            }
        }

        return staleList;
    }

    private readonly ConcurrentDictionary<string, string> _activatingProgress = new();
    public void SetActivationKey(string connectionId, string key) => _activatingProgress[connectionId] = key;
    public bool TryGetActivationKey(string connectionId, out string key) => _activatingProgress.TryGetValue(connectionId, out key!);
    public bool TryRemoveActivationKey(string connectionId, out string key) => _activatingProgress.TryRemove(connectionId, out key!);
    public async Task<bool> IsAuthorizedMachine(string machineId)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HubDbContext>();
        var machineIdExisted = await dbContext.LicenseEntries.AnyAsync(x => x.MachineId == machineId);

        return machineIdExisted;
    }
}

using System;
using System.Collections.Concurrent;

namespace ServiceDelivery.Api.Services;

public interface IConnectionManager
{
    void AddConnection(string userId, string connectionId);
    void RemoveConnection(string userId, string connectionId);
    IEnumerable<string> GetConnections(string userId);
    Dictionary<string, List<ConnectionInfo>> GetAllConnections();
    void UpdateLastSeen(string userId, string connectionId);
    List<(string UserId, string ConnectionId)> GetStaleConnections(TimeSpan timeout);
}

public class ConnectionInfo
{
    public string ConnectionId { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
}

public class ConnectionManager : IConnectionManager
{
    private readonly ConcurrentDictionary<string, List<ConnectionInfo>> _connections = new();

    public void AddConnection(string userId, string connectionId)
    {
        if (string.IsNullOrEmpty(userId)) return;

        var connectionInfo = new ConnectionInfo
        {
            ConnectionId = connectionId,
            ConnectedAt = DateTime.UtcNow
        };

        _connections.AddOrUpdate(userId,
            _ => new List<ConnectionInfo> { connectionInfo },
            (_, existingConnections) =>
            {
                existingConnections.Add(connectionInfo);
                return existingConnections;
            });
    }

    public void RemoveConnection(string userId, string connectionId)
    {
        Console.WriteLine(nameof(RemoveConnection));

        if (string.IsNullOrEmpty(userId)) return;

        if (_connections.TryGetValue(userId, out var connections))
        {
            connections.RemoveAll(c => c.ConnectionId == connectionId);
            if (connections.Count == 0)
            {
                _connections.TryRemove(userId, out _);
            }
        }
    }

    public IEnumerable<string> GetConnections(string userId)
    {
        return _connections.TryGetValue(userId, out var connections)
            ? connections.Select(c => c.ConnectionId)
            : Enumerable.Empty<string>();
    }

    public Dictionary<string, List<ConnectionInfo>> GetAllConnections()
    {
        return _connections.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public void UpdateLastSeen(string userId, string connectionId)
    {
        if (string.IsNullOrEmpty(userId)) return;

        if (_connections.TryGetValue(userId, out var connections))
        {
            var con = connections.FirstOrDefault(c => c.ConnectionId == connectionId);
            if (con != null)
            {
                con.ConnectedAt = DateTime.UtcNow;
                //Console.WriteLine($"user_id: {userId}, connection_id: {connectionId}, last_seen: {con.ConnectedAt}");
            }
            else
            {
                Console.WriteLine($"user_id: {userId}, connection_id: {connectionId}, last_seen: DISCONNECTED");
            }
        }
    }

    public List<(string UserId, string ConnectionId)> GetStaleConnections(TimeSpan timeout)
    {
        var staleList = new List<(string, string)>();
        var cutoff = DateTime.UtcNow - timeout;

        foreach (var (userId, connections) in _connections)
        {
            foreach (var conn in connections)
            {
                if (conn.ConnectedAt < cutoff)
                {
                    staleList.Add((userId, conn.ConnectionId));
                }
            }
        }

        return staleList;
    }


}

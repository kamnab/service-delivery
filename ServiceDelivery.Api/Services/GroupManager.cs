using System;

namespace ServiceDelivery.Api.Services;

public class GroupManager
{
    // Dictionary to map group names to lists of connection IDs
    private readonly Dictionary<string, HashSet<string>> _groups = new Dictionary<string, HashSet<string>>();

    // Add a user to a group
    public void AddToGroup(string groupName, string connectionId)
    {
        if (!_groups.ContainsKey(groupName))
        {
            _groups[groupName] = new HashSet<string>();
        }

        _groups[groupName].Add(connectionId);
    }

    // Remove a user from a group
    public void RemoveFromGroup(string groupName, string connectionId)
    {
        if (_groups.ContainsKey(groupName))
        {
            _groups[groupName].Remove(connectionId);
            if (_groups[groupName].Count == 0)
            {
                _groups.Remove(groupName);  // Remove the group if it's empty
            }
        }
    }

    // Get all connections in a group
    public HashSet<string> GetConnectionsInGroup(string groupName)
    {
        if (_groups.ContainsKey(groupName))
        {
            return _groups[groupName];
        }
        return new HashSet<string>();
    }

    // Check if a user is in a group
    public bool IsUserInGroup(string groupName, string connectionId)
    {
        return _groups.ContainsKey(groupName) && _groups[groupName].Contains(connectionId);
    }

    // Get all group names
    public List<string> GetAllGroups()
    {
        return _groups.Keys.ToList();
    }
}


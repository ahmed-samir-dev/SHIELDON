using System.Collections.Concurrent;

namespace SHIELDON.Infrastructure.Services;

/// <summary>
/// Singleton service to track which users are currently connected via SignalR.
/// Useful for displaying Online/Offline status in the chat UI.
/// Note: In a multi-server setup (horizontal scaling), this would be replaced with Redis.
/// </summary>
public class PresenceTracker
{
    // Dictionary mapping UserId -> List of ConnectionIds (since a user can have multiple tabs open)
    private static readonly Dictionary<Guid, List<string>> OnlineUsers = new();

    /// <summary>
    /// Tracks a new connection. Returns true if the user just came online (was offline).
    /// </summary>
    public Task<bool> UserConnected(Guid userId, string connectionId)
    {
        bool isJustOnline = false;
        lock (OnlineUsers)
        {
            if (OnlineUsers.ContainsKey(userId))
            {
                OnlineUsers[userId].Add(connectionId);
            }
            else
            {
                OnlineUsers.Add(userId, new List<string> { connectionId });
                isJustOnline = true;
            }
        }
        return Task.FromResult(isJustOnline);
    }

    /// <summary>
    /// Untracks a connection. Returns true if the user is now completely offline (no more tabs open).
    /// </summary>
    public Task<bool> UserDisconnected(Guid userId, string connectionId)
    {
        bool isNowOffline = false;
        lock (OnlineUsers)
        {
            if (!OnlineUsers.ContainsKey(userId)) return Task.FromResult(isNowOffline);

            OnlineUsers[userId].Remove(connectionId);

            if (OnlineUsers[userId].Count == 0)
            {
                OnlineUsers.Remove(userId);
                isNowOffline = true;
            }
        }
        return Task.FromResult(isNowOffline);
    }

    /// <summary>
    /// Gets all currently online user IDs.
    /// </summary>
    public Task<Guid[]> GetOnlineUsers()
    {
        Guid[] onlineUsers;
        lock (OnlineUsers)
        {
            onlineUsers = OnlineUsers.Keys.ToArray();
        }
        return Task.FromResult(onlineUsers);
    }
}

using System.Collections.Concurrent;

namespace VinhkhanhTour.API.Services
{
    public class ActiveAppUser
    {
        public string SessionId { get; set; } = "";
        public string Username { get; set; } = "";
        public bool IsAnonymous { get; set; }
        public DateTime LastPing { get; set; }
    }

    public class AppUserTrackingService
    {
        private readonly ConcurrentDictionary<string, ActiveAppUser> _activeUsers = new();

        public void Ping(string sessionId, string username, bool isAnonymous)
        {
            if (string.IsNullOrEmpty(sessionId)) return;

            var user = new ActiveAppUser
            {
                SessionId = sessionId,
                Username = username,
                IsAnonymous = isAnonymous,
                LastPing = DateTime.UtcNow
            };

            _activeUsers[sessionId] = user;
        }

        public void Remove(string sessionId)
        {
            if (!string.IsNullOrEmpty(sessionId))
            {
                _activeUsers.TryRemove(sessionId, out _);
            }
        }

        public List<ActiveAppUser> GetActiveUsers()
        {
            // Remove users who haven't pinged in the last 2 minutes
            var cutoff = DateTime.UtcNow.AddMinutes(-2);
            var inactiveKeys = _activeUsers.Where(kv => kv.Value.LastPing < cutoff).Select(kv => kv.Key).ToList();
            
            foreach (var key in inactiveKeys)
            {
                _activeUsers.TryRemove(key, out _);
            }

            return _activeUsers.Values.OrderByDescending(u => u.LastPing).ToList();
        }
    }
}

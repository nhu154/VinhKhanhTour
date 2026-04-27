using System.Collections.Concurrent;

namespace VinhkhanhTour.API.Services
{
    public class ActiveAppUser
    {
        public string SessionId { get; set; } = "";
        public string Username { get; set; } = "";
        public bool IsAnonymous { get; set; }
        public DateTime LastPing { get; set; }

        // Vị trí GPS realtime (null nếu chưa có)
        public double? Lat { get; set; }
        public double? Lng { get; set; }
        public DateTime? LastLocationTime { get; set; }
    }

    public class AppUserTrackingService
    {
        private readonly ConcurrentDictionary<string, ActiveAppUser> _activeUsers = new();

       
        private static readonly TimeSpan ActiveTimeout = TimeSpan.FromMinutes(10);

        public void Ping(string sessionId, string username, bool isAnonymous,
                         double? lat = null, double? lng = null)
        {
            if (string.IsNullOrEmpty(sessionId)) return;

           
            _activeUsers.TryGetValue(sessionId, out var existing);

            var hasNewLocation = lat.HasValue && lat != 0 && lng.HasValue && lng != 0;

            var user = new ActiveAppUser
            {
                SessionId = sessionId,
                Username = username,
                IsAnonymous = isAnonymous,
                LastPing = DateTime.UtcNow,
                Lat = hasNewLocation ? lat : existing?.Lat,
                Lng = hasNewLocation ? lng : existing?.Lng,
                LastLocationTime = hasNewLocation ? DateTime.UtcNow : existing?.LastLocationTime
            };

            _activeUsers[sessionId] = user;
        }

        public void Remove(string sessionId)
        {
            if (!string.IsNullOrEmpty(sessionId))
                _activeUsers.TryRemove(sessionId, out _);
        }

       
        public void ClearAll()
        {
            _activeUsers.Clear();
        }

        public List<ActiveAppUser> GetActiveUsers()
        {
            // Chỉ xóa tự động nếu không ping trong ActiveTimeout (10 phút)
            // → Đủ để chịu đựng mất mạng ngắn (rút dây, chuyển wifi, nền app)
            var cutoff = DateTime.UtcNow - ActiveTimeout;
            foreach (var key in _activeUsers.Where(kv => kv.Value.LastPing < cutoff)
                                             .Select(kv => kv.Key).ToList())
                _activeUsers.TryRemove(key, out _);

            return _activeUsers.Values.OrderByDescending(u => u.LastPing).ToList();
        }

        public List<ActiveAppUser> GetUsersWithLocation()
        {
            return GetActiveUsers()
                .Where(u => u.Lat.HasValue && u.Lng.HasValue
                         && u.Lat != 0 && u.Lng != 0)
                .ToList();
        }
    }
}
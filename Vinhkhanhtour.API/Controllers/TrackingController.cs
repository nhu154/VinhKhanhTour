using Microsoft.AspNetCore.Mvc;
using VinhkhanhTour.API.Services;

namespace VinhkhanhTour.API.Controllers
{
    public class PingRequest
    {
        public string SessionId { get; set; } = "";
        public string Username { get; set; } = "";
        public bool IsAnonymous { get; set; }

        // GPS realtime — tùy chọn, app gửi kèm nếu có
        public double? Lat { get; set; }
        public double? Lng { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class TrackingController : ControllerBase
    {
        private readonly AppUserTrackingService _tracker;

        public TrackingController(AppUserTrackingService tracker)
        {
            _tracker = tracker;
        }

        // POST: api/tracking/ping
        // App gửi ping định kỳ, kèm lat/lng nếu có GPS
        [HttpPost("ping")]
        public IActionResult Ping([FromBody] PingRequest req)
        {
            if (string.IsNullOrEmpty(req.SessionId))
                return BadRequest("SessionId is required");

            _tracker.Ping(req.SessionId, req.Username, req.IsAnonymous, req.Lat, req.Lng);
            return Ok(new { message = "Pinged successfully" });
        }

        // GET: api/tracking/online-users
        [HttpGet("online-users")]
        public IActionResult GetOnlineUsers()
        {
            var activeUsers = _tracker.GetActiveUsers();
            var result = activeUsers.Select(u => new
            {
                isAnonymous = u.IsAnonymous,
                username = u.Username,
                loginTime = ((DateTimeOffset)u.LastPing).ToUnixTimeMilliseconds()
            });
            return Ok(result);
        }

        // GET: api/tracking/live-locations
        // Trả về vị trí GPS hiện tại của những user đang online (có GPS)
        // Dành cho heatmap realtime trên CMS
        [HttpGet("live-locations")]
        public IActionResult GetLiveLocations()
        {
            var usersWithLocation = _tracker.GetUsersWithLocation();

            var result = usersWithLocation.Select((u, i) => new
            {
                id = u.SessionId.Substring(0, Math.Min(8, u.SessionId.Length)),
                label = u.IsAnonymous
                                ? $"Khách #{i + 1}"
                                : (string.IsNullOrEmpty(u.Username) ? $"Người dùng #{i + 1}" : u.Username),
                isAnonymous = u.IsAnonymous,
                lat = u.Lat,
                lng = u.Lng,
                // Số giây kể từ lần cập nhật vị trí cuối
                secondsAgo = u.LastLocationTime.HasValue
                                ? (int)(DateTime.UtcNow - u.LastLocationTime.Value).TotalSeconds
                                : 999,
                lastPing = ((DateTimeOffset)u.LastPing).ToUnixTimeMilliseconds()
            });

            return Ok(result);
        }

        // DELETE: api/tracking/online-users/{sessionId}
        [HttpDelete("online-users/{sessionId}")]
        public IActionResult EndSession(string sessionId)
        {
            _tracker.Remove(sessionId);
            return Ok();
        }

        // DELETE: api/tracking/clear
        // Xóa toàn bộ session — dùng để reset ghost sessions khi test/debug
        [HttpDelete("clear")]
        public IActionResult ClearAllSessions()
        {
            _tracker.ClearAll();
            return Ok(new { message = "All sessions cleared" });
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using VinhkhanhTour.API.Services;

namespace VinhkhanhTour.API.Controllers
{
    public class PingRequest
    {
        public string SessionId { get; set; } = "";
        public string Username { get; set; } = "";
        public bool IsAnonymous { get; set; }
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
        [HttpPost("ping")]
        public IActionResult Ping([FromBody] PingRequest req)
        {
            if (string.IsNullOrEmpty(req.SessionId))
            {
                return BadRequest("SessionId is required");
            }

            _tracker.Ping(req.SessionId, req.Username, req.IsAnonymous);
            return Ok(new { message = "Pinged successfully" });
        }

        // GET: api/tracking/online-users
        [HttpGet("online-users")]
        public IActionResult GetOnlineUsers()
        {
            var activeUsers = _tracker.GetActiveUsers();
            
            // Format to match the frontend expectations closely
            var result = activeUsers.Select(u => new
            {
                isAnonymous = u.IsAnonymous,
                username = u.Username,
                // Time elapsed in milliseconds 
                // However, since JS uses Date.now(), we'll return loginTime as standard Unix Timestamp format (milliseconds)
                loginTime = ((DateTimeOffset)u.LastPing).ToUnixTimeMilliseconds()
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
    }
}

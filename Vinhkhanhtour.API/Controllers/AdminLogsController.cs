using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Dapper;

namespace VinhkhanhTour.API.Controllers
{
    public class AdminLogRequest
    {
        public string Action { get; set; } = "";
        public string Target { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Details { get; set; } = "";
    }

    [ApiController]
    [Route("api/[controller]")]
    public class AdminLogsController : ControllerBase
    {
        private readonly string _conn;

        public AdminLogsController(IConfiguration config)
        {
            _conn = config.GetConnectionString("DefaultConnection")!;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int limit = 200)
        {
            using var db = new MySqlConnection(_conn);
            var logs = await db.QueryAsync(@"
                SELECT * FROM admin_logs
                ORDER BY Timestamp DESC 
                LIMIT @limit", new { limit });
            return Ok(logs);
        }

        [HttpPost("user-activity")]
        public async Task<IActionResult> LogActivity([FromBody] AdminLogRequest req)
        {
            using var db = new MySqlConnection(_conn);
            await db.ExecuteAsync(@"
                INSERT INTO admin_logs (Action, Target, UserName, Details, Timestamp)
                VALUES (@Action, @Target, @UserName, @Details, NOW())",
                new { req.Action, req.Target, req.UserName, req.Details });
            return Ok(new { message = "Logged successfully" });
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> Clear()
        {
            using var db = new MySqlConnection(_conn);
            await db.ExecuteAsync("DELETE FROM admin_logs WHERE Id > 0");
            return Ok(new { message = "Đã xóa toàn bộ lịch sử thao tác" });
        }
    }
}

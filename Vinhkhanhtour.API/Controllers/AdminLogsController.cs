using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Dapper;

namespace VinhkhanhTour.API.Controllers
{
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
        public async Task<IActionResult> GetAll([FromQuery] int limit = 100)
        {
            using var db = new MySqlConnection(_conn);
            var logs = await db.QueryAsync(@"
                SELECT * FROM admin_logs 
                ORDER BY Timestamp DESC 
                LIMIT @limit", new { limit });
            return Ok(logs);
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

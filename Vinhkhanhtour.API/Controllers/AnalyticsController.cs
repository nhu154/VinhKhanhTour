using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Dapper;

namespace VinhkhanhTour.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly string _conn;

        public AnalyticsController(IConfiguration config)
        {
            _conn = config.GetConnectionString("DefaultConnection")!;
        }

        // GET: api/analytics
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            using var db = new MySqlConnection(_conn);
            var list = await db.QueryAsync(@"
                SELECT 
                    a.Id,
                    a.RestaurantId,
                    COALESCE(r.Name, 'Không rõ') as RestaurantName,
                    a.EventType,
                    a.Timestamp
                FROM analytics a
                LEFT JOIN restaurants r ON a.RestaurantId = r.Id
                ORDER BY a.Timestamp DESC
                LIMIT 500");
            return Ok(list);
        }

        // GET: api/analytics/stats - tổng hợp thống kê
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            using var db = new MySqlConnection(_conn);

            var totalVisits = await db.QueryFirstOrDefaultAsync<int>(
                "SELECT COUNT(*) FROM analytics");

            var todayVisits = await db.QueryFirstOrDefaultAsync<int>(
                "SELECT COUNT(*) FROM analytics WHERE DATE(Timestamp) = CURDATE()");

            var weekVisits = await db.QueryFirstOrDefaultAsync<int>(
                "SELECT COUNT(*) FROM analytics WHERE Timestamp >= DATE_SUB(NOW(), INTERVAL 7 DAY)");

            var topPoi = await db.QueryAsync(@"
                SELECT r.Name, COUNT(*) as VisitCount
                FROM analytics a
                LEFT JOIN restaurants r ON a.RestaurantId = r.Id
                GROUP BY a.RestaurantId, r.Name
                ORDER BY VisitCount DESC
                LIMIT 5");

            var byDay = await db.QueryAsync(@"
                SELECT DATE(Timestamp) as Day, COUNT(*) as Count
                FROM analytics
                WHERE Timestamp >= DATE_SUB(NOW(), INTERVAL 7 DAY)
                GROUP BY DATE(Timestamp)
                ORDER BY Day");

            return Ok(new
            {
                totalVisits,
                todayVisits,
                weekVisits,
                topPoi,
                byDay
            });
        }

        // POST: api/analytics
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AnalyticsRequest req)
        {
            using var db = new MySqlConnection(_conn);
            await db.ExecuteAsync(
                "INSERT INTO analytics (RestaurantId, EventType) VALUES (@RestaurantId, @EventType)",
                new { req.RestaurantId, req.EventType });
            return Ok(new { message = "Ghi thành công" });
        }

        // DELETE: api/analytics/clear
        [HttpDelete("clear")]
        public async Task<IActionResult> Clear()
        {
            using var db = new MySqlConnection(_conn);
            await db.ExecuteAsync("DELETE FROM analytics WHERE Id > 0");
            return Ok(new { message = "Đã xóa toàn bộ analytics" });
        }
    }

    public class AnalyticsRequest
    {
        public int RestaurantId { get; set; }
        public string EventType { get; set; } = "";
    }
}
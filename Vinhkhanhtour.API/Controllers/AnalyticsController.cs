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
                SELECT a.*, r.Name as RestaurantName 
                FROM analytics a
                LEFT JOIN restaurants r ON a.RestaurantId = r.Id
                ORDER BY a.Timestamp DESC
                LIMIT 100");
            return Ok(list);
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
    }

    public class AnalyticsRequest
    {
        public int RestaurantId { get; set; }
        public string EventType { get; set; } = "";
    }
}
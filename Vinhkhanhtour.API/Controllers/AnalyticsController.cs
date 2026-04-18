using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Dapper;

namespace VinhkhanhTour.API.Controllers
{
    public class AnalyticsRequest
    {
        public int? RestaurantId { get; set; }  // nullable: GPS point không cần restaurant
        public string EventType { get; set; } = "";
        public double Value { get; set; }
        public double Lat  { get; set; }
        public double Lng  { get; set; }
    }

    public class GpsPointRequest
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }

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
                    a.Value,
                    a.Lat,
                    a.Lng,
                    a.Timestamp
                FROM analytics a
                LEFT JOIN restaurants r ON a.RestaurantId = r.Id
                WHERE a.EventType != 'gps_point'
                ORDER BY a.Timestamp DESC
                LIMIT 500");
            return Ok(list);
        }

        // GET: api/analytics/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            using var db = new MySqlConnection(_conn);

            var totalVisits = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM analytics WHERE EventType != 'gps_point'");
            var todayVisits = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM analytics WHERE DATE(Timestamp) = CURDATE() AND EventType != 'gps_point'");
            var weekVisits = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM analytics WHERE Timestamp >= DATE_SUB(NOW(), INTERVAL 7 DAY) AND EventType != 'gps_point'");

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

            return Ok(new { totalVisits, todayVisits, weekVisits, topPoi, byDay });
        }

        // GET: api/analytics/avg-duration
        // Trả về thời gian nghe trung bình (giây) cho mỗi POI
        [HttpGet("avg-duration")]
        public async Task<IActionResult> GetAvgDuration()
        {
            using var db = new MySqlConnection(_conn);

            var rows = await db.QueryAsync(@"
                SELECT
                    a.RestaurantId                          AS PoiId,
                    COALESCE(r.Name, CONCAT('POI #', a.RestaurantId)) AS PoiName,
                    COUNT(*)                                AS PlayCount,
                    ROUND(AVG(a.Value), 1)                  AS AvgSeconds,
                    ROUND(SUM(a.Value), 1)                  AS TotalSeconds
                FROM analytics a
                LEFT JOIN restaurants r ON a.RestaurantId = r.Id
                WHERE a.EventType LIKE 'audio_%'
                  AND a.Value >= 3
                GROUP BY a.RestaurantId, r.Name
                ORDER BY AvgSeconds DESC");

            return Ok(rows);
        }

        // GET: api/analytics/heatmap?days=30&layer=combined
        // Trả về danh sách tọa độ GPS ẩn danh để vẽ heatmap
        [HttpGet("heatmap")]
        public async Task<IActionResult> GetHeatmap([FromQuery] int days = 30, [FromQuery] string layer = "combined")
        {
            if (days < 1 || days > 365) days = 30;

            using var db = new MySqlConnection(_conn);

            string eventTypeFilter = layer switch
            {
                "checkin" => "AND EventType = 'poi_visit'",
                "view" => "AND EventType LIKE 'audio_%'",
                _ => "AND EventType != 'gps_point'" // "combined" loại bỏ gps_point để tránh lặp 8 lần trên 1 tương tác
            };

            // Bao gồm tất cả events có tọa độ GPS hợp lệ trong phạm vi Việt Nam
            // Group theo 3 chữ số thập phân (~111m) để gom các cụm lại rộng hơn, tránh 1 tương tác rải thành 4 cụm
            var sql = $@"
                SELECT
                    ROUND(Lat, 3) AS lat,
                    ROUND(Lng, 3) AS lng,
                    COUNT(*)      AS weight
                FROM analytics
                WHERE Lat <> 0 AND Lng <> 0
                  AND Lat BETWEEN 5 AND 25
                  AND Lng BETWEEN 100 AND 115
                  AND Timestamp >= DATE_SUB(NOW(), INTERVAL @days DAY)
                  {eventTypeFilter}
                GROUP BY ROUND(Lat, 3), ROUND(Lng, 3)
                ORDER BY weight DESC
                LIMIT 2000";

            var points = await db.QueryAsync(sql, new { days });

            return Ok(points);
        }

        // POST: api/analytics
        // Nhận Value (duration), Lat, Lng từ MAUI app
        // RestaurantId có thể null với event gps_point
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AnalyticsRequest req)
        {
            using var db = new MySqlConnection(_conn);

            // Gựi trói FK: gps_point không có restaurant
            if (req.EventType == "gps_point" || req.RestaurantId == null || req.RestaurantId == 0)
            {
                await db.ExecuteAsync(@"
                    INSERT INTO analytics (RestaurantId, EventType, Value, Lat, Lng, Timestamp)
                    VALUES (NULL, @EventType, @Value, @Lat, @Lng, NOW())",
                    new { req.EventType, Value = req.Value, Lat = req.Lat, Lng = req.Lng });
            }
            else
            {
                await db.ExecuteAsync(@"
                    INSERT INTO analytics (RestaurantId, EventType, Value, Lat, Lng, Timestamp)
                    VALUES (@RestaurantId, @EventType, @Value, @Lat, @Lng, NOW())",
                    new { req.RestaurantId, req.EventType, Value = req.Value, Lat = req.Lat, Lng = req.Lng });
            }
            return Ok(new { message = "Ghi thành công" });
        }

        // POST: api/analytics/gps
        // Endpoint rêng cho GPS tracking ẩn danh (không cần RestaurantId)
        [HttpPost("gps")]
        public async Task<IActionResult> AddGpsPoint([FromBody] GpsPointRequest req)
        {
            if (req.Lat == 0 && req.Lng == 0) return BadRequest("Lat/Lng invalid");
            using var db = new MySqlConnection(_conn);
            await db.ExecuteAsync(@"
                INSERT INTO analytics (RestaurantId, EventType, Value, Lat, Lng, Timestamp)
                VALUES (NULL, 'gps_point', 0, @Lat, @Lng, NOW())",
                new { req.Lat, req.Lng });
            return Ok(new { message = "GPS đã ghi" });
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
}
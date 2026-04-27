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
        public string Username { get; set; } = "";
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
        private readonly VinhkhanhTour.API.Services.AppUserTrackingService _tracker;

        public AnalyticsController(IConfiguration config, VinhkhanhTour.API.Services.AppUserTrackingService tracker)
        {
            _conn = config.GetConnectionString("DefaultConnection")!;
            _tracker = tracker;
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
                    a.Username,
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

            // Đếm lượt khách ghé thăm (Dựa trên số lần Mở App / Đăng nhập)
            var totalVisits = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM analytics WHERE EventType = 'app_login'");
            var todayVisits = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM analytics WHERE DATE(Timestamp) = CURDATE() AND EventType = 'app_login'");
            var weekVisits = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM analytics WHERE Timestamp >= DATE_SUB(NOW(), INTERVAL 7 DAY) AND EventType = 'app_login'");

            // Top POI vẫn dựa trên các tương tác thực (không phải gps_point)
            var topPoi = await db.QueryAsync(@"
                SELECT r.Name, COUNT(*) as VisitCount
                FROM analytics a
                LEFT JOIN restaurants r ON a.RestaurantId = r.Id
                WHERE a.EventType != 'gps_point' AND a.RestaurantId IS NOT NULL
                GROUP BY a.RestaurantId, r.Name
                ORDER BY VisitCount DESC
                LIMIT 5");

            // Biểu đồ theo ngày: đếm lượt tương tác thực (chỉ đếm poi_visit để 1 lần click = 1 lượt)
            var byDay = await db.QueryAsync(@"
                SELECT DATE(Timestamp) as Day, COUNT(*) as Count
                FROM analytics
                WHERE Timestamp >= DATE_SUB(NOW(), INTERVAL 7 DAY)
                  AND EventType != 'gps_point'
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

        // GET: api/analytics/heatmap?days=30
        // Trả về danh sách tọa độ GPS ẩn danh để vẽ heatmap (dữ liệu người dùng di chuyển)
        [HttpGet("heatmap")]
        public async Task<IActionResult> GetHeatmap([FromQuery] int days = 30)
        {
            if (days < 1 || days > 365) days = 30;

            using var db = new MySqlConnection(_conn);

            // Group theo 4 chữ số thập phân (~11m) để thấy rõ vệt đường đi của người dùng
            var sql = $@"
                SELECT
                    ROUND(Lat, 4) AS lat,
                    ROUND(Lng, 4) AS lng,
                    COUNT(*)      AS weight
                FROM analytics
                WHERE Lat <> 0 AND Lng <> 0
                  AND Lat BETWEEN 5 AND 25
                  AND Lng BETWEEN 100 AND 115
                  AND Timestamp >= DATE_SUB(NOW(), INTERVAL @days DAY)
                  AND EventType = 'gps_point'
                GROUP BY ROUND(Lat, 4), ROUND(Lng, 4)
                ORDER BY weight DESC
                LIMIT 5000";

            var points = await db.QueryAsync(sql, new { days });

            return Ok(points);
        }

        // GET: api/analytics/heatmap-live
        // Trả về dữ liệu heatmap từ các người dùng đang online (realtime)
        [HttpGet("heatmap-live")]
        public IActionResult GetHeatmapLive()
        {
            var liveUsers = _tracker.GetUsersWithLocation();
            var points = liveUsers.Select(u => new
            {
                lat = u.Lat,
                lng = u.Lng,
                weight = 1.0
            }).ToList();

            return Ok(new { success = true, data = points });
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
                    INSERT INTO analytics (RestaurantId, EventType, Value, Lat, Lng, Username, Timestamp)
                    VALUES (NULL, @EventType, @Value, @Lat, @Lng, @Username, NOW())",
                    new { req.EventType, Value = req.Value, Lat = req.Lat, Lng = req.Lng, req.Username });
            }
            else
            {
                await db.ExecuteAsync(@"
                    INSERT INTO analytics (RestaurantId, EventType, Value, Lat, Lng, Username, Timestamp)
                    VALUES (@RestaurantId, @EventType, @Value, @Lat, @Lng, @Username, NOW())",
                    new { req.RestaurantId, req.EventType, Value = req.Value, Lat = req.Lat, Lng = req.Lng, req.Username });
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
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Dapper;
using System.Text.Json;
using VinhkhanhTour.API.Services;

namespace VinhkhanhTour.API.Controllers
{
    public class ApprovalRequestDto
    {
        public int? Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public int? LocationId { get; set; }
        public string LocationName { get; set; } = "";
        public string Action { get; set; } = "";
        public string RequestData { get; set; } = "{}";
        public string OldData { get; set; } = "{}";
        public string Status { get; set; } = "pending";
        public string? AdminNote { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }

    public class ReviewDto
    {
        public string Status { get; set; } = "";
        public string? AdminNote { get; set; }
        public int ReviewedBy { get; set; }
    }

    // DTO type-safe cho RegisterLocation (thay dynamic)
    public class RegisterLocationDto
    {
        public int UserId { get; set; }
        public int LocationId { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class ApprovalsController : ControllerBase
    {
        private readonly string _conn;
        private readonly ImageService _img;

        public ApprovalsController(IConfiguration config, ImageService img)
        {
            _conn = config.GetConnectionString("DefaultConnection")!;
            _img = img;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string status = "")
        {
            using var db = new MySqlConnection(_conn);
            var sql = "SELECT * FROM approval_requests";
            if (!string.IsNullOrEmpty(status))
                sql += " WHERE Status=@status";
            sql += " ORDER BY CreatedAt DESC";
            var list = await db.QueryAsync(sql, string.IsNullOrEmpty(status) ? null : new { status });
            return Ok(list);
        }

        [HttpGet("count/pending")]
        public async Task<IActionResult> CountPending()
        {
            using var db = new MySqlConnection(_conn);
            var count = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM approval_requests WHERE Status='pending'");
            return Ok(new { count });
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            using var db = new MySqlConnection(_conn);
            var list = await db.QueryAsync(
                "SELECT * FROM approval_requests WHERE UserId=@userId ORDER BY CreatedAt DESC",
                new { userId });
            return Ok(list);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ApprovalRequestDto body)
        {
            using var db = new MySqlConnection(_conn);
            var id = await db.ExecuteScalarAsync<int>(@"
                INSERT INTO approval_requests
                (UserId, UserName, LocationId, LocationName, Action, RequestData, OldData, Status)
                VALUES (@UserId, @UserName, @LocationId, @LocationName, @Action, @RequestData, @OldData, 'pending');
                SELECT LAST_INSERT_ID();", body);
            return Ok(new { message = "Đã gửi yêu cầu, chờ admin duyệt", id });
        }

        [HttpPut("{id}/review")]
        public async Task<IActionResult> Review(int id, [FromBody] ReviewDto body)
        {
            using var db = new MySqlConnection(_conn);
            var req = await db.QueryFirstOrDefaultAsync<ApprovalRequestDto>(
                "SELECT * FROM approval_requests WHERE Id=@id", new { id });
            if (req == null) return NotFound();

            await db.ExecuteAsync(@"
                UPDATE approval_requests
                SET Status=@Status, AdminNote=@AdminNote, ReviewedAt=NOW(), ReviewedBy=@ReviewedBy
                WHERE Id=@id",
                new { body.Status, body.AdminNote, body.ReviewedBy, id });

            if (body.Status == "approved")
            {
                try { await ApplyApprovedData(db, req); }
                catch (Exception ex)
                {
                    return Ok(new { message = $"Đã duyệt nhưng lỗi khi áp dụng: {ex.Message}" });
                }
            }

            return Ok(new { message = body.Status == "approved" ? "✅ Đã duyệt và áp dụng!" : "❌ Đã từ chối" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            using var db = new MySqlConnection(_conn);
            await db.ExecuteAsync("DELETE FROM approval_requests WHERE Id=@id", new { id });
            return Ok(new { message = "Đã xóa" });
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearAll()
        {
            using var db = new MySqlConnection(_conn);
            await db.ExecuteAsync("DELETE FROM approval_requests");
            return Ok(new { message = "Đã dọn sạch toàn bộ danh sách phê duyệt" });
        }

        [HttpGet("my-locations/{userId}")]
        public async Task<IActionResult> GetMyLocations(int userId)
        {
            using var db = new MySqlConnection(_conn);
            var list = await db.QueryAsync(@"
                SELECT r.* FROM restaurants r
                JOIN user_locations ul ON ul.LocationId = r.Id
                WHERE ul.UserId = @userId", new { userId });
            return Ok(list);
        }

        // ĐÃ SỬA: dùng DTO type-safe thay vì dynamic
        [HttpPost("register-location")]
        public async Task<IActionResult> RegisterLocation([FromBody] RegisterLocationDto body)
        {
            using var db = new MySqlConnection(_conn);
            var exists = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM user_locations WHERE UserId=@userId AND LocationId=@locationId",
                new { body.UserId, body.LocationId });
            if (exists > 0)
                return BadRequest(new { message = "Bạn đã đăng ký quán này rồi" });

            await db.ExecuteAsync(
                "INSERT INTO user_locations (UserId, LocationId) VALUES (@UserId, @LocationId)",
                new { body.UserId, body.LocationId });
            return Ok(new { message = "Đã đăng ký, chờ admin xác nhận" });
        }

        private async Task ApplyApprovedData(MySqlConnection db, ApprovalRequestDto req)
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(req.RequestData)!;
            string GetStr(string key) => data.TryGetValue(key, out var v) ? v.GetString() ?? "" : "";
            double GetDbl(string key, double def = 0) => data.TryGetValue(key, out var v) && v.TryGetDouble(out var d) ? d : def;
            int GetInt(string key, int def = 0) => data.TryGetValue(key, out var v) && v.TryGetInt32(out var i) ? i : def;

            var imgUrl = _img.SaveIfBase64(GetStr("ImageUrl"), "poi");

            if (req.Action == "create_poi")
            {
                await db.ExecuteAsync(@"
                    INSERT INTO restaurants
                    (Name,Description,Category,Latitude,Longitude,Address,ImageUrl,Rating,OpenHours,
                     TtsScript,TtsScriptEn,TtsScriptZh,Radius,IsAdsPopup,AudioUrl,IsFavorite,Translations)
                    VALUES
                    (@Name,@Desc,@Cat,@Lat,@Lng,@Addr,@Img,@Rating,@Hours,
                     @Vi,@En,@Zh,@Radius,0,'','0','{}')",
                    new
                    {
                        Name = GetStr("Name"),
                        Desc = GetStr("Description"),
                        Cat = GetStr("Category"),
                        Lat = GetDbl("Latitude"),
                        Lng = GetDbl("Longitude"),
                        Addr = GetStr("Address"),
                        Img = imgUrl,
                        Rating = GetDbl("Rating", 4.0),
                        Hours = GetStr("OpenHours"),
                        Vi = GetStr("TtsScript"),
                        En = GetStr("TtsScriptEn"),
                        Zh = GetStr("TtsScriptZh"),
                        Radius = GetInt("Radius", 50)
                    });

                var newId = await db.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID()");
                await db.ExecuteAsync(
                    "INSERT IGNORE INTO user_locations (UserId,LocationId) VALUES (@u,@l)",
                    new { u = req.UserId, l = newId });
            }
            else if (req.LocationId.HasValue)
            {
                await db.ExecuteAsync(@"
                    UPDATE restaurants SET
                    Name=@Name, Description=@Desc, Category=@Cat,
                    Latitude=@Lat, Longitude=@Lng, Address=@Addr,
                    ImageUrl=@Img, Rating=@Rating, OpenHours=@Hours,
                    TtsScript=@Vi, TtsScriptEn=@En, TtsScriptZh=@Zh,
                    Radius=@Radius, AudioUrl=@Audio
                    WHERE Id=@Id",
                    new
                    {
                        Name = GetStr("Name"),
                        Desc = GetStr("Description"),
                        Cat = GetStr("Category"),
                        Lat = GetDbl("Latitude"),
                        Lng = GetDbl("Longitude"),
                        Addr = GetStr("Address"),
                        Img = imgUrl,
                        Rating = GetDbl("Rating", 4.0),
                        Hours = GetStr("OpenHours"),
                        Vi = GetStr("TtsScript"),
                        En = GetStr("TtsScriptEn"),
                        Zh = GetStr("TtsScriptZh"),
                        Radius = GetInt("Radius", 50),
                        Audio = GetStr("AudioUrl"),
                        Id = req.LocationId.Value
                    });
            }
        }
    }
}

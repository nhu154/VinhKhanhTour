using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Dapper;
using System.Text.Json;

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
        public string Status { get; set; } = ""; // approved / rejected
        public string? AdminNote { get; set; }
        public int ReviewedBy { get; set; } = 1;
    }

    [ApiController]
    [Route("api/[controller]")]
    public class ApprovalsController : ControllerBase
    {
        private readonly string _conn;
        private readonly string _uploadDir;

        public ApprovalsController(IConfiguration config, IWebHostEnvironment env)
        {
            _conn = config.GetConnectionString("DefaultConnection")!;
            _uploadDir = Path.Combine(
                env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                "uploads");
            Directory.CreateDirectory(_uploadDir);
        }

        // GET: api/approvals — lấy tất cả (admin)
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

        // GET: api/approvals/count/pending — đếm badge
        [HttpGet("count/pending")]
        public async Task<IActionResult> CountPending()
        {
            using var db = new MySqlConnection(_conn);
            var count = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM approval_requests WHERE Status='pending'");
            return Ok(new { count });
        }

        // GET: api/approvals/user/{userId} — lấy requests của 1 user
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            using var db = new MySqlConnection(_conn);
            var list = await db.QueryAsync(
                "SELECT * FROM approval_requests WHERE UserId=@userId ORDER BY CreatedAt DESC",
                new { userId });
            return Ok(list);
        }

        // POST: api/approvals — chủ quán gửi yêu cầu
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

        // PUT: api/approvals/{id}/review — admin duyệt/từ chối
        [HttpPut("{id}/review")]
        public async Task<IActionResult> Review(int id, [FromBody] ReviewDto body)
        {
            using var db = new MySqlConnection(_conn);

            // Lấy request
            var req = await db.QueryFirstOrDefaultAsync<ApprovalRequestDto>(
                "SELECT * FROM approval_requests WHERE Id=@id", new { id });
            if (req == null) return NotFound();

            // Cập nhật trạng thái
            await db.ExecuteAsync(@"
                UPDATE approval_requests
                SET Status=@Status, AdminNote=@AdminNote, ReviewedAt=NOW(), ReviewedBy=@ReviewedBy
                WHERE Id=@id",
                new { body.Status, body.AdminNote, body.ReviewedBy, id });

            // Nếu approved → áp dụng data vào restaurants
            if (body.Status == "approved")
            {
                try
                {
                    await ApplyApprovedData(db, req);
                }
                catch (Exception ex)
                {
                    return Ok(new { message = $"Đã duyệt nhưng lỗi khi áp dụng: {ex.Message}" });
                }
            }

            return Ok(new { message = body.Status == "approved" ? "✅ Đã duyệt và áp dụng!" : "❌ Đã từ chối" });
        }

        // DELETE: api/approvals/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            using var db = new MySqlConnection(_conn);
            await db.ExecuteAsync("DELETE FROM approval_requests WHERE Id=@id", new { id });
            return Ok(new { message = "Đã xóa" });
        }

        // GET: api/approvals/my-locations/{userId}
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

        // POST: api/approvals/register-location — chủ quán đăng ký quán
        [HttpPost("register-location")]
        public async Task<IActionResult> RegisterLocation([FromBody] dynamic body)
        {
            using var db = new MySqlConnection(_conn);
            int userId = (int)body.UserId;
            int locationId = (int)body.LocationId;

            // Kiểm tra đã đăng ký chưa
            var exists = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM user_locations WHERE UserId=@userId AND LocationId=@locationId",
                new { userId, locationId });
            if (exists > 0)
                return BadRequest(new { message = "Bạn đã đăng ký quán này rồi" });

            await db.ExecuteAsync(
                "INSERT INTO user_locations (UserId, LocationId) VALUES (@userId, @locationId)",
                new { userId, locationId });
            return Ok(new { message = "Đã đăng ký, chờ admin xác nhận" });
        }

        // ── Áp dụng data đã được duyệt vào DB ──
        private async Task ApplyApprovedData(MySqlConnection db, ApprovalRequestDto req)
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(req.RequestData)!;
            string GetStr(string key) => data.TryGetValue(key, out var v) ? v.GetString() ?? "" : "";
            double GetDbl(string key, double def = 0) => data.TryGetValue(key, out var v) && v.TryGetDouble(out var d) ? d : def;
            int GetInt(string key, int def = 0) => data.TryGetValue(key, out var v) && v.TryGetInt32(out var i) ? i : def;

            // Xử lý ảnh base64 nếu có
            var imgUrl = GetStr("ImageUrl");
            if (!string.IsNullOrEmpty(imgUrl) && imgUrl.Contains("base64,"))
                imgUrl = SaveBase64Image(imgUrl);

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

                // Gán quán mới cho chủ quán
                var newId = await db.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID()");
                await db.ExecuteAsync(
                    "INSERT IGNORE INTO user_locations (UserId,LocationId) VALUES (@u,@l)",
                    new { u = req.UserId, l = newId });
            }
            else if (req.LocationId.HasValue)
            {
                // update_info / update_audio / update_image
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

        private string SaveBase64Image(string base64)
        {
            try
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    base64, @"data:image/(?<ext>.*?);base64,(?<data>.*)");
                if (!match.Success) return base64;
                var ext = match.Groups["ext"].Value.Replace("jpeg", "jpg");
                var bytes = Convert.FromBase64String(match.Groups["data"].Value);
                if (bytes.Length > 3 * 1024 * 1024) return "";
                var fileName = $"poi_{Guid.NewGuid():N}.{ext}";
                System.IO.File.WriteAllBytes(Path.Combine(_uploadDir, fileName), bytes);
                return $"uploads/{fileName}";
            }
            catch { return base64; }
        }
    }
}
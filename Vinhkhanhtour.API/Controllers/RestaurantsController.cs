using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Dapper;
using System.Text.RegularExpressions;

namespace VinhkhanhTour.API.Controllers
{
    public class RestaurantDto
    {
        public int? Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Address { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public double Rating { get; set; }
        public string OpenHours { get; set; } = "";
        public string AudioFile { get; set; } = "";
        public string TtsScript { get; set; } = "";
        public string TtsScriptEn { get; set; } = "";
        public string TtsScriptZh { get; set; } = "";
        public string Translations { get; set; } = "{}";
        public int Radius { get; set; }
        public bool IsAdsPopup { get; set; }
        public string AudioUrl { get; set; } = "";
    }

    [ApiController]
    [Route("api/[controller]")]
    public class RestaurantsController : ControllerBase
    {
        private readonly string _conn;
        private readonly string _uploadDir;

        public RestaurantsController(IConfiguration config, IWebHostEnvironment env)
        {
            _conn = config.GetConnectionString("DefaultConnection")!;
            _uploadDir = Path.Combine(env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads");
            // Tự tạo thư mục uploads nếu chưa có
            Directory.CreateDirectory(_uploadDir);
        }

        // GET: api/restaurants
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            using var db = new MySqlConnection(_conn);
            var list = await db.QueryAsync("SELECT * FROM restaurants");
            return Ok(list);
        }

        // GET: api/restaurants/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            using var db = new MySqlConnection(_conn);
            var item = await db.QueryFirstOrDefaultAsync(
                "SELECT * FROM restaurants WHERE Id=@Id", new { Id = id });
            if (item == null) return NotFound();
            return Ok(item);
        }

        // POST: api/restaurants/upload-image — upload ảnh riêng (multipart/form-data)
        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return BadRequest(new { message = "Không có file" });

            if (imageFile.Length > 3 * 1024 * 1024)
                return BadRequest(new { message = "File quá lớn (max 3MB)" });

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                return BadRequest(new { message = "Chỉ chấp nhận JPG, PNG, WebP" });

            var fileName = $"poi_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(_uploadDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await imageFile.CopyToAsync(stream);

            return Ok(new { url = $"uploads/{fileName}", message = "Upload thành công" });
        }

        // POST: api/restaurants
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RestaurantDto body)
        {
            body.ImageUrl = SaveBase64Image(body.ImageUrl);

            using var db = new MySqlConnection(_conn);
            var id = await db.ExecuteScalarAsync<int>(@"
                INSERT INTO restaurants 
                (Name, Description, Category, Latitude, Longitude, Address, ImageUrl, Rating, OpenHours, AudioFile, TtsScript, TtsScriptEn, TtsScriptZh, Translations, Radius, IsAdsPopup, AudioUrl)
                VALUES
                (@Name, @Description, @Category, @Latitude, @Longitude, @Address, @ImageUrl, @Rating, @OpenHours, @AudioFile, @TtsScript, @TtsScriptEn, @TtsScriptZh, @Translations, @Radius, @IsAdsPopup, @AudioUrl);
                SELECT LAST_INSERT_ID();",
                body);
            return Ok(new { message = "Thêm thành công", id });
        }

        // PUT: api/restaurants/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] RestaurantDto body)
        {
            body.ImageUrl = SaveBase64Image(body.ImageUrl);
            body.Id = id;
            using var db = new MySqlConnection(_conn);
            await db.ExecuteAsync(@"
                UPDATE restaurants SET
                Name=@Name, Description=@Description, Category=@Category,
                Latitude=@Latitude, Longitude=@Longitude, Address=@Address,
                ImageUrl=@ImageUrl, Rating=@Rating, OpenHours=@OpenHours,
                AudioFile=@AudioFile, TtsScript=@TtsScript,
                TtsScriptEn=@TtsScriptEn, TtsScriptZh=@TtsScriptZh, Translations=@Translations,
                Radius=@Radius, IsAdsPopup=@IsAdsPopup, AudioUrl=@AudioUrl
                WHERE Id=@Id",
                body);
            return Ok(new { message = "Cập nhật thành công" });
        }

        // DELETE: api/restaurants/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            using var db = new MySqlConnection(_conn);
            await db.ExecuteAsync("DELETE FROM restaurants WHERE Id=@Id", new { Id = id });
            return Ok(new { message = "Xóa thành công" });
        }

        // Lưu ảnh base64 thành file, trả về đường dẫn tương đối
        private string SaveBase64Image(string imageData)
        {
            if (string.IsNullOrEmpty(imageData)) return imageData;

            // Nếu đã là URL/path (không phải base64) → giữ nguyên
            if (!imageData.Contains("base64,")) return imageData;

            try
            {
                var match = Regex.Match(imageData, @"data:image/(?<ext>.*?);base64,(?<data>.*)");
                if (!match.Success) return imageData;

                var ext = match.Groups["ext"].Value.Replace("jpeg", "jpg");
                var data = Convert.FromBase64String(match.Groups["data"].Value);

                // Giới hạn 3MB
                if (data.Length > 3 * 1024 * 1024) return "";

                var fileName = $"poi_{Guid.NewGuid():N}.{ext}";
                var filePath = Path.Combine(_uploadDir, fileName);
                System.IO.File.WriteAllBytes(filePath, data);

                return $"uploads/{fileName}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SaveImage] Error: {ex.Message}");
                return imageData; // fallback giữ nguyên
            }
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Dapper;

namespace VinhkhanhTour.API.Controllers
{
    /// <summary>
    /// Quản lý file audio .mp3 cho POI.
    /// Upload file → lưu vào wwwroot/uploads/audio/ → cập nhật AudioUrl trong bảng restaurants.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AudioController : ControllerBase
    {
        private readonly string _conn;
        private readonly string _uploadDir;
        private const long MaxBytes = 10 * 1024 * 1024; // 10 MB

        public AudioController(IConfiguration config, IWebHostEnvironment env)
        {
            _conn = config.GetConnectionString("DefaultConnection")!;
            _uploadDir = Path.Combine(
                env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                "uploads", "audio");
            Directory.CreateDirectory(_uploadDir);
        }

        /// <summary>
        /// POST api/audio/upload
        /// Body: multipart/form-data với field "audioFile" và "poiId"
        /// Lưu file → cập nhật AudioUrl trong bảng restaurants → trả về URL
        /// </summary>
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile audioFile, [FromForm] int poiId)
        {
            if (audioFile == null || audioFile.Length == 0)
                return BadRequest(new { message = "Không có file" });

            if (audioFile.Length > MaxBytes)
                return BadRequest(new { message = "File quá lớn (tối đa 10MB)" });

            var ext = Path.GetExtension(audioFile.FileName).ToLowerInvariant();
            if (ext != ".mp3" && ext != ".m4a" && ext != ".wav" && ext != ".ogg")
                return BadRequest(new { message = "Chỉ chấp nhận MP3, M4A, WAV, OGG" });

            // Xóa file audio cũ của POI này nếu có
            if (poiId > 0)
            {
                using var db = new MySqlConnection(_conn);
                var oldUrl = await db.ExecuteScalarAsync<string>(
                    "SELECT AudioUrl FROM restaurants WHERE Id=@Id", new { Id = poiId });

                if (!string.IsNullOrEmpty(oldUrl) && oldUrl.StartsWith("uploads/audio/"))
                {
                    var oldPath = Path.Combine(
                        Path.GetDirectoryName(_uploadDir)!, // wwwroot
                        oldUrl.Replace("/", Path.DirectorySeparatorChar.ToString()));
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }
            }

            // Lưu file mới
            var fileName = $"audio_{(poiId > 0 ? poiId.ToString() : Guid.NewGuid().ToString("N"))}" +
                           $"_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(_uploadDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await audioFile.CopyToAsync(stream);

            var relativeUrl = $"uploads/audio/{fileName}";

            // Cập nhật AudioUrl trong bảng restaurants nếu có poiId
            if (poiId > 0)
            {
                using var db = new MySqlConnection(_conn);
                await db.ExecuteAsync(
                    "UPDATE restaurants SET AudioUrl=@url WHERE Id=@Id",
                    new { url = relativeUrl, Id = poiId });
            }

            return Ok(new
            {
                message = "Upload thành công",
                url = relativeUrl,
                poiId
            });
        }

        /// <summary>
        /// DELETE api/audio/{poiId}
        /// Xóa file audio và xóa AudioUrl khỏi database
        /// </summary>
        [HttpDelete("{poiId}")]
        public async Task<IActionResult> Delete(int poiId)
        {
            using var db = new MySqlConnection(_conn);
            var audioUrl = await db.ExecuteScalarAsync<string>(
                "SELECT AudioUrl FROM restaurants WHERE Id=@Id", new { Id = poiId });

            if (!string.IsNullOrEmpty(audioUrl) && audioUrl.StartsWith("uploads/audio/"))
            {
                var filePath = Path.Combine(
                    Path.GetDirectoryName(_uploadDir)!,
                    audioUrl.Replace("/", Path.DirectorySeparatorChar.ToString()));
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }

            await db.ExecuteAsync(
                "UPDATE restaurants SET AudioUrl='' WHERE Id=@Id",
                new { Id = poiId });

            return Ok(new { message = "Đã xóa audio" });
        }
    }
}
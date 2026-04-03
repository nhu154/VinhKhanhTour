using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Dapper;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System;
using System.Globalization;

namespace VinhkhanhTour.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ToursController : ControllerBase
    {
        private readonly string _conn;
        private readonly string _uploadDir;

        public ToursController(IConfiguration config, IWebHostEnvironment env)
        {
            _conn = config.GetConnectionString("DefaultConnection")!;
            _uploadDir = Path.Combine(env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads");
            Directory.CreateDirectory(_uploadDir);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            using var db = new MySqlConnection(_conn);
            var list = await db.QueryAsync(@"
                SELECT Id, Name, 
                       COALESCE(NameEn, '') as NameEn,
                       COALESCE(NameZh, '') as NameZh,
                       COALESCE(Description, '') as Description,
                       COALESCE(DescEn, '') as DescEn,
                       COALESCE(DescZh, '') as DescZh,
                       COALESCE(Duration, '45 phút') as Duration,
                       COALESCE(Rating, 4.0) as Rating,
                       COALESCE(Emoji, '🍜') as Emoji,
                       COALESCE(ImageUrl, '') as ImageUrl,
                       COALESCE(IsActive, 1) as IsActive,
                       COALESCE(Pois, '[]') as Pois
                FROM tours ORDER BY Id");
            return Ok(list);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Dictionary<string, object> body)
        {
            using var db = new MySqlConnection(_conn);
            var imgUrl = body.GetValueOrDefault("ImageUrl", "")?.ToString();
            imgUrl = SaveBase64Image(imgUrl);

            await db.ExecuteAsync(@"
                INSERT INTO tours (Name, NameEn, NameZh, Description, DescEn, DescZh, Duration, Rating, Emoji, ImageUrl, IsActive, Pois)
                VALUES (@Name, @NameEn, @NameZh, @Description, @DescEn, @DescZh, @Duration, @Rating, @Emoji, @ImageUrl, @IsActive, @Pois)",
                new
                {
                    Name = body.GetValueOrDefault("Name", "")?.ToString(),
                    NameEn = body.GetValueOrDefault("NameEn", "")?.ToString(),
                    NameZh = body.GetValueOrDefault("NameZh", "")?.ToString(),
                    Description = body.GetValueOrDefault("Description", "")?.ToString(),
                    DescEn = body.GetValueOrDefault("DescEn", "")?.ToString(),
                    DescZh = body.GetValueOrDefault("DescZh", "")?.ToString(),
                    Duration = body.GetValueOrDefault("Duration", "45 phút")?.ToString(),
                    Rating = double.TryParse(body.GetValueOrDefault("Rating", 4.0)?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var r) ? r : 4.0,
                    Emoji = body.GetValueOrDefault("Emoji", "🍜")?.ToString(),
                    ImageUrl = imgUrl,
                    IsActive = true,
                    Pois = body.GetValueOrDefault("Pois", "[]")?.ToString()
                });
            return Ok(new { message = "Tạo tour thành công" });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Dictionary<string, object> body)
        {
            using var db = new MySqlConnection(_conn);
            var imgUrl = body.GetValueOrDefault("ImageUrl", "")?.ToString();
            imgUrl = SaveBase64Image(imgUrl);

            await db.ExecuteAsync(@"
                UPDATE tours SET
                Name=@Name, NameEn=@NameEn, NameZh=@NameZh,
                Description=@Description, DescEn=@DescEn, DescZh=@DescZh,
                Duration=@Duration, Rating=@Rating, Emoji=@Emoji,
                ImageUrl=@ImageUrl, Pois=@Pois
                WHERE Id=@Id",
                new
                {
                    Id = id,
                    Name = body.GetValueOrDefault("Name", "")?.ToString(),
                    NameEn = body.GetValueOrDefault("NameEn", "")?.ToString(),
                    NameZh = body.GetValueOrDefault("NameZh", "")?.ToString(),
                    Description = body.GetValueOrDefault("Description", "")?.ToString(),
                    DescEn = body.GetValueOrDefault("DescEn", "")?.ToString(),
                    DescZh = body.GetValueOrDefault("DescZh", "")?.ToString(),
                    Duration = body.GetValueOrDefault("Duration", "45 phút")?.ToString(),
                    Rating = double.TryParse(body.GetValueOrDefault("Rating", 4.0)?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var r) ? r : 4.0,
                    Emoji = body.GetValueOrDefault("Emoji", "🍜")?.ToString(),
                    ImageUrl = imgUrl,
                    Pois = body.GetValueOrDefault("Pois", "[]")?.ToString()
                });
            return Ok(new { message = "Cập nhật tour thành công" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            using var db = new MySqlConnection(_conn);
            await db.ExecuteAsync("DELETE FROM tours WHERE Id=@Id", new { Id = id });
            return Ok(new { message = "Xóa tour thành công" });
        }

        private string SaveBase64Image(string imageData)
        {
            if (string.IsNullOrEmpty(imageData)) return imageData;

            if (!imageData.Contains("base64,")) return imageData;

            try
            {
                var match = Regex.Match(imageData, @"data:image/(?<ext>.*?);base64,(?<data>.*)");
                if (!match.Success) return imageData;

                var ext = match.Groups["ext"].Value.Replace("jpeg", "jpg");
                var data = Convert.FromBase64String(match.Groups["data"].Value);

                if (data.Length > 3 * 1024 * 1024) return ""; // 3MB limit

                var fileName = $"tour_{Guid.NewGuid():N}.{ext}";
                var filePath = Path.Combine(_uploadDir, fileName);
                System.IO.File.WriteAllBytes(filePath, data);

                return $"uploads/{fileName}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SaveImage] Error: {ex.Message}");
                return imageData;
            }
        }
    }
}
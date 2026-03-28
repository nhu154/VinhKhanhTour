using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Dapper;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VinhkhanhTour.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ToursController : ControllerBase
    {
        private readonly string _conn;

        public ToursController(IConfiguration config)
        {
            _conn = config.GetConnectionString("DefaultConnection")!;
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
                    Rating = double.TryParse(body.GetValueOrDefault("Rating", 4.0)?.ToString(), out var r) ? r : 4.0,
                    Emoji = body.GetValueOrDefault("Emoji", "🍜")?.ToString(),
                    ImageUrl = body.GetValueOrDefault("ImageUrl", "")?.ToString(),
                    IsActive = true,
                    Pois = body.GetValueOrDefault("Pois", "[]")?.ToString()
                });
            return Ok(new { message = "Tạo tour thành công" });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Dictionary<string, object> body)
        {
            using var db = new MySqlConnection(_conn);
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
                    Rating = double.TryParse(body.GetValueOrDefault("Rating", 4.0)?.ToString(), out var r) ? r : 4.0,
                    Emoji = body.GetValueOrDefault("Emoji", "🍜")?.ToString(),
                    ImageUrl = body.GetValueOrDefault("ImageUrl", "")?.ToString(),
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
    }
}
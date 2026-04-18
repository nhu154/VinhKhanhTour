using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Dapper;
using System.Globalization;
using VinhkhanhTour.API.Services;

namespace VinhkhanhTour.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ToursController : ControllerBase
    {
        private readonly string _conn;
        private readonly ImageService _img;
        private readonly LogService _log;

        public ToursController(IConfiguration config, ImageService img, LogService log)
        {
            _conn = config.GetConnectionString("DefaultConnection")!;
            _img = img;
            _log = log;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            using var db = new MySqlConnection(_conn);
            var list = await db.QueryAsync(@"
                SELECT Id, Name,
                       COALESCE(NameEn, '') as NameEn,
                       COALESCE(NameZh, '') as NameZh,
                       COALESCE(NameJa, '') as NameJa,
                       COALESCE(NameKo, '') as NameKo,
                       COALESCE(Description, '') as Description,
                       COALESCE(DescEn, '') as DescEn,
                       COALESCE(DescZh, '') as DescZh,
                       COALESCE(DescJa, '') as DescJa,
                       COALESCE(DescKo, '') as DescKo,
                       COALESCE(Duration, '45 phút') as Duration,
                       COALESCE(Rating, 4.0) as Rating,
                       COALESCE(Emoji, '🍜') as Emoji,
                       COALESCE(ImageUrl, '') as ImageUrl,
                       COALESCE(IsActive, 1) as IsActive,
                       COALESCE(Pois, '[]') as Pois
                FROM tours ORDER BY Id");
            return Ok(list);
        }

        // ĐÃ THÊM: GET /{id} — lấy 1 tour theo ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            using var db = new MySqlConnection(_conn);
            var tour = await db.QueryFirstOrDefaultAsync(@"
                SELECT Id, Name,
                       COALESCE(NameEn, '') as NameEn,
                       COALESCE(NameZh, '') as NameZh,
                       COALESCE(NameJa, '') as NameJa,
                       COALESCE(NameKo, '') as NameKo,
                       COALESCE(Description, '') as Description,
                       COALESCE(DescEn, '') as DescEn,
                       COALESCE(DescZh, '') as DescZh,
                       COALESCE(DescJa, '') as DescJa,
                       COALESCE(DescKo, '') as DescKo,
                       COALESCE(Duration, '45 phút') as Duration,
                       COALESCE(Rating, 4.0) as Rating,
                       COALESCE(Emoji, '🍜') as Emoji,
                       COALESCE(ImageUrl, '') as ImageUrl,
                       COALESCE(IsActive, 1) as IsActive,
                       COALESCE(Pois, '[]') as Pois
                FROM tours WHERE Id=@Id", new { Id = id });
            if (tour == null) return NotFound();
            return Ok(tour);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Dictionary<string, object> body)
        {
            using var db = new MySqlConnection(_conn);
            var imgUrl = _img.SaveIfBase64(body.GetValueOrDefault("ImageUrl", "")?.ToString(), "tour");

            await db.ExecuteAsync(@"
                INSERT INTO tours (Name, NameEn, NameZh, NameJa, NameKo, Description, DescEn, DescZh, DescJa, DescKo, Duration, Rating, Emoji, ImageUrl, IsActive, Pois)
                VALUES (@Name, @NameEn, @NameZh, @NameJa, @NameKo, @Description, @DescEn, @DescZh, @DescJa, @DescKo, @Duration, @Rating, @Emoji, @ImageUrl, @IsActive, @Pois)",
                new
                {
                    Name = body.GetValueOrDefault("Name", "")?.ToString(),
                    NameEn = body.GetValueOrDefault("NameEn", "")?.ToString(),
                    NameZh = body.GetValueOrDefault("NameZh", "")?.ToString(),
                    NameJa = body.GetValueOrDefault("NameJa", "")?.ToString(),
                    NameKo = body.GetValueOrDefault("NameKo", "")?.ToString(),
                    Description = body.GetValueOrDefault("Description", "")?.ToString(),
                    DescEn = body.GetValueOrDefault("DescEn", "")?.ToString(),
                    DescZh = body.GetValueOrDefault("DescZh", "")?.ToString(),
                    DescJa = body.GetValueOrDefault("DescJa", "")?.ToString(),
                    DescKo = body.GetValueOrDefault("DescKo", "")?.ToString(),
                    Duration = body.GetValueOrDefault("Duration", "45 phút")?.ToString(),
                    Rating = double.TryParse(body.GetValueOrDefault("Rating", 4.0)?.ToString(),
                                    NumberStyles.Any, CultureInfo.InvariantCulture, out var r) ? r : 4.0,
                    Emoji = body.GetValueOrDefault("Emoji", "🍜")?.ToString(),
                    ImageUrl = imgUrl,
                    IsActive = true,
                    Pois = body.GetValueOrDefault("Pois", "[]")?.ToString()
                });
            var name = body.GetValueOrDefault("Name", "")?.ToString();
            await _log.LogAction(Request, "CREATE_TOUR", name);
            return Ok(new { message = "Tạo tour thành công" });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Dictionary<string, object> body)
        {
            using var db = new MySqlConnection(_conn);
            var imgUrl = _img.SaveIfBase64(body.GetValueOrDefault("ImageUrl", "")?.ToString(), "tour");

            await db.ExecuteAsync(@"
                UPDATE tours SET
                Name=@Name, NameEn=@NameEn, NameZh=@NameZh, NameJa=@NameJa, NameKo=@NameKo,
                Description=@Description, DescEn=@DescEn, DescZh=@DescZh, DescJa=@DescJa, DescKo=@DescKo,
                Duration=@Duration, Rating=@Rating, Emoji=@Emoji,
                ImageUrl=@ImageUrl, Pois=@Pois
                WHERE Id=@Id",
                new
                {
                    Id = id,
                    Name = body.GetValueOrDefault("Name", "")?.ToString(),
                    NameEn = body.GetValueOrDefault("NameEn", "")?.ToString(),
                    NameZh = body.GetValueOrDefault("NameZh", "")?.ToString(),
                    NameJa = body.GetValueOrDefault("NameJa", "")?.ToString(),
                    NameKo = body.GetValueOrDefault("NameKo", "")?.ToString(),
                    Description = body.GetValueOrDefault("Description", "")?.ToString(),
                    DescEn = body.GetValueOrDefault("DescEn", "")?.ToString(),
                    DescZh = body.GetValueOrDefault("DescZh", "")?.ToString(),
                    DescJa = body.GetValueOrDefault("DescJa", "")?.ToString(),
                    DescKo = body.GetValueOrDefault("DescKo", "")?.ToString(),
                    Duration = body.GetValueOrDefault("Duration", "45 phút")?.ToString(),
                    Rating = double.TryParse(body.GetValueOrDefault("Rating", 4.0)?.ToString(),
                                    NumberStyles.Any, CultureInfo.InvariantCulture, out var r) ? r : 4.0,
                    Emoji = body.GetValueOrDefault("Emoji", "🍜")?.ToString(),
                    ImageUrl = imgUrl,
                    Pois = body.GetValueOrDefault("Pois", "[]")?.ToString()
                });
            var tourName = body.GetValueOrDefault("Name", "")?.ToString();
            await _log.LogAction(Request, "UPDATE_TOUR", tourName);
            return Ok(new { message = "Cập nhật tour thành công" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            using var db = new MySqlConnection(_conn);
            var name = await db.ExecuteScalarAsync<string>("SELECT Name FROM tours WHERE Id=@Id", new { Id = id });
            await db.ExecuteAsync("DELETE FROM tours WHERE Id=@Id", new { Id = id });
            await _log.LogAction(Request, "DELETE_TOUR", name ?? $"Tour {id}");
            return Ok(new { message = "Xóa tour thành công" });
        }
    }
}
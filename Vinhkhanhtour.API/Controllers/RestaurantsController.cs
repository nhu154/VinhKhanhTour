using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Dapper;
using VinhkhanhTour.API.Services;

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
        private readonly ImageService _img;
        private readonly LogService _log;

        public RestaurantsController(IConfiguration config, ImageService img, LogService log)
        {
            _conn = config.GetConnectionString("DefaultConnection")!;
            _img = img;
            _log = log;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            using var db = new MySqlConnection(_conn);
            var list = await db.QueryAsync("SELECT * FROM restaurants");
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            using var db = new MySqlConnection(_conn);
            var item = await db.QueryFirstOrDefaultAsync(
                "SELECT * FROM restaurants WHERE Id=@Id", new { Id = id });
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(IFormFile imageFile)
        {
            var (ok, result) = await _img.SaveUploadedFileAsync(imageFile);
            if (!ok) return BadRequest(new { message = result });
            return Ok(new { url = result, message = "Upload thành công" });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RestaurantDto body)
        {
            body.ImageUrl = _img.SaveIfBase64(body.ImageUrl, "poi");
            using var db = new MySqlConnection(_conn);
            var id = await db.ExecuteScalarAsync<int>(@"
                INSERT INTO restaurants
                (Name, Description, Category, Latitude, Longitude, Address, ImageUrl, Rating, OpenHours, AudioFile, TtsScript, TtsScriptEn, TtsScriptZh, Translations, Radius, IsAdsPopup, AudioUrl)
                VALUES
                (@Name, @Description, @Category, @Latitude, @Longitude, @Address, @ImageUrl, @Rating, @OpenHours, @AudioFile, @TtsScript, @TtsScriptEn, @TtsScriptZh, @Translations, @Radius, @IsAdsPopup, @AudioUrl);
                SELECT LAST_INSERT_ID();",
                body);
            await _log.LogAction(Request, "CREATE_POI", body.Name);
            return Ok(new { message = "Thêm thành công", id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] RestaurantDto body)
        {
            body.ImageUrl = _img.SaveIfBase64(body.ImageUrl, "poi");
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
            await _log.LogAction(Request, "UPDATE_POI", body.Name);
            return Ok(new { message = "Cập nhật thành công" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            using var db = new MySqlConnection(_conn);
            var name = await db.ExecuteScalarAsync<string>("SELECT Name FROM restaurants WHERE Id=@Id", new { Id = id });
            await db.ExecuteAsync("DELETE FROM restaurants WHERE Id=@Id", new { Id = id });
            await _log.LogAction(Request, "DELETE_POI", name ?? $"POI {id}");
            return Ok(new { message = "Xóa thành công" });
        }
    }
}

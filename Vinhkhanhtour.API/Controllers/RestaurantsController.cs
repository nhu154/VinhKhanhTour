using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Dapper;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace VinhkhanhTour.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RestaurantsController : ControllerBase
    {
        private readonly string _conn;

        public RestaurantsController(IConfiguration config)
        {
            _conn = config.GetConnectionString("DefaultConnection")!;
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

        // POST: api/restaurants
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Dictionary<string, object> body)
        {
            if (body.ContainsKey("ImageUrl")) body["ImageUrl"] = SaveImage(body["ImageUrl"]?.ToString() ?? "");

            using var db = new MySqlConnection(_conn);
            await db.ExecuteAsync(@"
                INSERT INTO restaurants 
                (Name, Description, Category, Latitude, Longitude, Address, ImageUrl, Rating, OpenHours, AudioFile, TtsScript, TtsScriptEn, TtsScriptZh, Radius, IsAdsPopup, AudioUrl)
                VALUES
                (@Name, @Description, @Category, @Latitude, @Longitude, @Address, @ImageUrl, @Rating, @OpenHours, @AudioFile, @TtsScript, @TtsScriptEn, @TtsScriptZh, @Radius, @IsAdsPopup, @AudioUrl)",
                body);
            return Ok(new { message = "Thêm thành công" });
        }

        // PUT: api/restaurants/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Dictionary<string, object> body)
        {
            if (body.ContainsKey("ImageUrl")) body["ImageUrl"] = SaveImage(body["ImageUrl"]?.ToString() ?? "");
            body["Id"] = id;
            using var db = new MySqlConnection(_conn);
            await db.ExecuteAsync(@"
                UPDATE restaurants SET
                Name=@Name, Description=@Description, Category=@Category,
                Latitude=@Latitude, Longitude=@Longitude, Address=@Address,
                ImageUrl=@ImageUrl, Rating=@Rating, OpenHours=@OpenHours,
                AudioFile=@AudioFile, TtsScript=@TtsScript,
                TtsScriptEn=@TtsScriptEn, TtsScriptZh=@TtsScriptZh,
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

        private string SaveImage(string base64)
        {
            if (string.IsNullOrEmpty(base64) || !base64.Contains("base64,")) return base64;
            try {
                var match = Regex.Match(base64, @"data:image/(?<ext>.*?);base64,(?<data>.*)");
                var ext = match.Groups["ext"].Value;
                var data = Convert.FromBase64String(match.Groups["data"].Value);
                var fileName = $"poi_{Guid.NewGuid():N}.{ext}";
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName);
                System.IO.File.WriteAllBytes(path, data);
                return $"uploads/{fileName}";
            } catch { return base64; }
        }
    }
}
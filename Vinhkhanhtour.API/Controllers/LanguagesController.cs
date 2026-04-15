using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Dapper;

namespace VinhkhanhTour.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LanguagesController : ControllerBase
    {
        private readonly string _conn;

        public LanguagesController(IConfiguration config)
        {
            _conn = config.GetConnectionString("DefaultConnection")!;
        }

        // Đảm bảo bảng tồn tại
        private async Task EnsureTableAsync(MySqlConnection db)
        {
            await db.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS app_languages (
                    Code        VARCHAR(10)  NOT NULL PRIMARY KEY,
                    Name        VARCHAR(100) NOT NULL,
                    Flag        VARCHAR(10)  NOT NULL DEFAULT '🌐',
                    IsDefault   TINYINT(1)   NOT NULL DEFAULT 0,
                    SortOrder   INT          NOT NULL DEFAULT 99
                )");

            // Seed 3 ngôn ngữ mặc định nếu bảng trống
            var count = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM app_languages");
            if (count == 0)
            {
                await db.ExecuteAsync(@"
                    INSERT INTO app_languages (Code, Name, Flag, IsDefault, SortOrder) VALUES
                    ('vi', 'Tiếng Việt', '🇻🇳', 1, 0),
                    ('en', 'English',    '🇺🇸', 0, 1),
                    ('zh', '中文',        '🇨🇳', 0, 2)");
            }
        }

        // GET api/languages
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            using var db = new MySqlConnection(_conn);
            await EnsureTableAsync(db);
            var list = await db.QueryAsync(
                "SELECT Code, Name, Flag, IsDefault, SortOrder FROM app_languages ORDER BY SortOrder, Code");
            return Ok(list);
        }

        // POST api/languages  — thêm hoặc cập nhật ngôn ngữ
        [HttpPost]
        public async Task<IActionResult> Upsert([FromBody] LanguageDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Code và Name không được để trống" });

            dto.Code = dto.Code.Trim().ToLower();

            using var db = new MySqlConnection(_conn);
            await EnsureTableAsync(db);

            // Đếm để xác định SortOrder
            var maxOrder = await db.ExecuteScalarAsync<int>("SELECT COALESCE(MAX(SortOrder),0) FROM app_languages");

            await db.ExecuteAsync(@"
                INSERT INTO app_languages (Code, Name, Flag, IsDefault, SortOrder)
                VALUES (@Code, @Name, @Flag, 0, @SortOrder)
                ON DUPLICATE KEY UPDATE Name = @Name, Flag = @Flag",
                new { dto.Code, dto.Name, dto.Flag, SortOrder = maxOrder + 1 });

            return Ok(new { message = $"Đã lưu ngôn ngữ {dto.Flag} {dto.Name}" });
        }

        // DELETE api/languages/{code}
        [HttpDelete("{code}")]
        public async Task<IActionResult> Delete(string code)
        {
            code = code.Trim().ToLower();
            if (code is "vi" or "en" or "zh")
                return BadRequest(new { message = "Không thể xóa ngôn ngữ mặc định (VI/EN/ZH)" });

            using var db = new MySqlConnection(_conn);
            await EnsureTableAsync(db);
            var affected = await db.ExecuteAsync("DELETE FROM app_languages WHERE Code = @code", new { code });
            if (affected == 0) return NotFound(new { message = "Ngôn ngữ không tồn tại" });
            return Ok(new { message = $"Đã xóa ngôn ngữ {code.ToUpper()}" });
        }
    }

    public class LanguageDto
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Flag { get; set; } = "🌐";
    }
}

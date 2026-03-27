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
            var list = await db.QueryAsync("SELECT * FROM tours");
            return Ok(list);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Dictionary<string, object> body)
        {
            using var db = new MySqlConnection(_conn);
            await db.ExecuteAsync(@"
                INSERT INTO tours (Name, Description, ImageUrl, Pois)
                VALUES (@Name, @Description, @ImageUrl, @Pois)", 
                body);
            return Ok(new { message = "Tạo tour thành công" });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Dictionary<string, object> body)
        {
            body["Id"] = id;
            using var db = new MySqlConnection(_conn);
            await db.ExecuteAsync(@"
                UPDATE tours SET Name=@Name, Description=@Description, ImageUrl=@ImageUrl, Pois=@Pois
                WHERE Id=@Id", 
                body);
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

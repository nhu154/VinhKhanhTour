using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Dapper;

namespace VinhkhanhTour.API.Controllers
{
    public class UserDto
    {
        public int? Id { get; set; }
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "user";
    }

    public class LoginDto
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly string _conn;
        public AuthController(IConfiguration config)
        {
            _conn = config.GetConnectionString("DefaultConnection")!;
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto body)
        {
            // Hardcode fallback admin
            if (body.Username == "admin" && (body.Password == "admin123" || body.Password == "admin"))
                return Ok(new { success = true, role = "admin", fullName = "Administrator" });

            using var db = new MySqlConnection(_conn);
            var user = await db.QueryFirstOrDefaultAsync<UserDto>(
                "SELECT * FROM users WHERE Username=@u AND Password=@p",
                new { u = body.Username, p = body.Password });

            if (user == null) return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu" });
            return Ok(new { success = true, id = user.Id, role = (user.Role ?? "user").ToLower().Trim(), fullName = user.FullName });
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserDto body)
        {
            if (string.IsNullOrEmpty(body.Username) || string.IsNullOrEmpty(body.Password))
                return BadRequest(new { message = "Username và Password không được trống" });

            using var db = new MySqlConnection(_conn);
            var exists = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM users WHERE Username=@u", new { u = body.Username });
            if (exists > 0) return BadRequest(new { message = "Username đã tồn tại" });

            await db.ExecuteAsync(
                "INSERT INTO users (Username, Password, FullName, Role) VALUES (@Username, @Password, @FullName, @Role)",
                body);
            return Ok(new { message = "Đăng ký thành công" });
        }

        // GET: api/auth/users — lấy danh sách users (không trả password)
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            using var db = new MySqlConnection(_conn);
            var users = await db.QueryAsync(@"
                SELECT Id, Username, FullName, Role,
                       CreatedAt
                FROM users ORDER BY Id");
            return Ok(users);
        }

        // PUT: api/auth/users/{id}
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserDto body)
        {
            using var db = new MySqlConnection(_conn);

            if (!string.IsNullOrEmpty(body.Password))
            {
                await db.ExecuteAsync(@"
                    UPDATE users SET FullName=@FullName, Role=@Role, Password=@Password
                    WHERE Id=@Id",
                    new { body.FullName, body.Role, body.Password, Id = id });
            }
            else
            {
                await db.ExecuteAsync(@"
                    UPDATE users SET FullName=@FullName, Role=@Role
                    WHERE Id=@Id",
                    new { body.FullName, body.Role, Id = id });
            }
            return Ok(new { message = "Cập nhật thành công" });
        }

        // DELETE: api/auth/users/{id}
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            using var db = new MySqlConnection(_conn);
            await db.ExecuteAsync("DELETE FROM users WHERE Id=@Id", new { Id = id });
            return Ok(new { message = "Đã xóa người dùng" });
        }
    }
}
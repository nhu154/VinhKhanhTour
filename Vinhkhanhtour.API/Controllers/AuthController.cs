using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Dapper;
using VinhkhanhTour.API.Services;

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

    public class ChangePasswordDto
    {
        public int UserId { get; set; }
        public string OldPassword { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly string _conn;
        private readonly LogService _log;

        public AuthController(IConfiguration config, LogService log)
        {
            _conn = config.GetConnectionString("DefaultConnection")!;
            _log = log;
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto body)
        {
            if (string.IsNullOrEmpty(body.Username) || string.IsNullOrEmpty(body.Password))
                return BadRequest(new { message = "Username và Password không được trống" });

            using var db = new MySqlConnection(_conn);
            var user = await db.QueryFirstOrDefaultAsync<UserDto>(
                "SELECT * FROM users WHERE Username=@u",
                new { u = body.Username });

            if (user == null)
                return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu" });

            bool passwordOk;
            if (user.Password.StartsWith("$2"))
            {
                passwordOk = BCrypt.Net.BCrypt.Verify(body.Password, user.Password);
            }
            else
            {
                passwordOk = user.Password == body.Password;
                if (passwordOk)
                {
                    var newHash = BCrypt.Net.BCrypt.HashPassword(body.Password);
                    await db.ExecuteAsync(
                        "UPDATE users SET Password=@p WHERE Id=@id",
                        new { p = newHash, id = user.Id });
                }
            }

            if (!passwordOk)
                return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu" });

            await _log.LogAction(user.Id, user.FullName, "LOGIN", "CMS");

            return Ok(new
            {
                success = true,
                id = user.Id,
                role = (user.Role ?? "user").ToLower().Trim(),
                fullName = user.FullName
            });
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

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(body.Password);
            await db.ExecuteAsync(
                "INSERT INTO users (Username, Password, FullName, Role) VALUES (@Username, @Password, @FullName, @Role)",
                new { body.Username, Password = hashedPassword, body.FullName, body.Role });

            await _log.LogAction(Request, "REGISTER_USER", body.Username);
            return Ok(new { message = "Đăng ký thành công" });
        }

        // PUT: api/auth/change-password
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto body)
        {
            if (body.UserId <= 0 || string.IsNullOrEmpty(body.OldPassword) || string.IsNullOrEmpty(body.NewPassword))
                return BadRequest(new { message = "Thiếu thông tin bắt buộc" });

            if (body.NewPassword.Length < 6)
                return BadRequest(new { message = "Mật khẩu mới phải có ít nhất 6 ký tự" });

            using var db = new MySqlConnection(_conn);
            var user = await db.QueryFirstOrDefaultAsync<UserDto>(
                "SELECT * FROM users WHERE Id=@Id", new { Id = body.UserId });

            if (user == null)
                return NotFound(new { message = "Người dùng không tồn tại" });

            // Kiểm tra mật khẩu cũ
            bool oldOk = user.Password.StartsWith("$2")
                ? BCrypt.Net.BCrypt.Verify(body.OldPassword, user.Password)
                : user.Password == body.OldPassword;

            if (!oldOk)
                return Unauthorized(new { message = "Mật khẩu cũ không đúng" });

            var newHash = BCrypt.Net.BCrypt.HashPassword(body.NewPassword);
            await db.ExecuteAsync(
                "UPDATE users SET Password=@p WHERE Id=@Id",
                new { p = newHash, Id = body.UserId });

            await _log.LogAction(Request, "CHANGE_PASSWORD", user.Username);
            return Ok(new { message = "Đổi mật khẩu thành công" });
        }

        // GET: api/auth/users
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            using var db = new MySqlConnection(_conn);
            var users = await db.QueryAsync(@"
                SELECT Id, Username, FullName, Role, CreatedAt
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
                var newHash = BCrypt.Net.BCrypt.HashPassword(body.Password);
                await db.ExecuteAsync(@"
                    UPDATE users SET FullName=@FullName, Role=@Role, Password=@Password WHERE Id=@Id",
                    new { body.FullName, body.Role, Password = newHash, Id = id });
            }
            else
            {
                await db.ExecuteAsync(@"
                    UPDATE users SET FullName=@FullName, Role=@Role WHERE Id=@Id",
                    new { body.FullName, body.Role, Id = id });
            }
            await _log.LogAction(Request, "UPDATE_USER", body.Username ?? $"ID {id}");
            return Ok(new { message = "Cập nhật thành công" });
        }

        // DELETE: api/auth/users/{id}
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            using var db = new MySqlConnection(_conn);
            var user = await db.QueryFirstOrDefaultAsync<UserDto>("SELECT Username FROM users WHERE Id=@Id", new { Id = id });
            await db.ExecuteAsync("DELETE FROM users WHERE Id=@Id", new { Id = id });
            await _log.LogAction(Request, "DELETE_USER", user?.Username ?? $"User {id}");
            return Ok(new { message = "Đã xóa người dùng" });
        }
    }
}
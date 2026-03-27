using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Dapper;

namespace VinhkhanhTour.API.Controllers
{
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
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            using var db = new MySqlConnection(_conn);
            var user = await db.QueryFirstOrDefaultAsync(
                "SELECT * FROM users WHERE Username=@Username AND Password=@Password",
                new { req.Username, req.Password });

            if (user == null)
                return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu" });

            return Ok(new { message = "Đăng nhập thành công", user });
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] LoginRequest req)
        {
            using var db = new MySqlConnection(_conn);

            var exists = await db.QueryFirstOrDefaultAsync(
                "SELECT Id FROM users WHERE Username=@Username",
                new { req.Username });

            if (exists != null)
                return BadRequest(new { message = "Tài khoản đã tồn tại" });

            await db.ExecuteAsync(
                "INSERT INTO users (Username, Password, FullName, Role) VALUES (@Username, @Password, @FullName, 'user')",
                new { req.Username, req.Password, FullName = req.Username });

            return Ok(new { message = "Đăng ký thành công" });
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }
}
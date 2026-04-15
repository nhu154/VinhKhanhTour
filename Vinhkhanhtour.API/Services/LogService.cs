using MySql.Data.MySqlClient;
using Dapper;

namespace VinhkhanhTour.API.Services
{
    public class LogService
    {
        private readonly string _conn;

        public LogService(IConfiguration config)
        {
            _conn = config.GetConnectionString("DefaultConnection")!;
        }

        public async Task LogAction(int? userId, string userName, string action, string target, string details = null)
        {
            try
            {
                using var db = new MySqlConnection(_conn);
                await db.ExecuteAsync(@"
                    INSERT INTO admin_logs (UserId, UserName, Action, Target, Details, Timestamp)
                    VALUES (@userId, @userName, @action, @target, @details, NOW())",
                    new { userId, userName, action, target, details });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LogAction Error: {ex.Message}");
            }
        }

        public async Task LogAction(HttpRequest request, string action, string target, string details = null)
        {
            var userIdStr = request.Headers["X-Admin-Id"].ToString();
            var userName = request.Headers["X-Admin-Name"].ToString();
            if (string.IsNullOrEmpty(userName)) userName = "Hệ thống";
            
            int? userId = null;
            if (int.TryParse(userIdStr, out int uid)) userId = uid;

            await LogAction(userId, userName, action, target, details);
        }
    }
}

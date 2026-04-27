namespace VinhKhanhTour.Services
{
    /// <summary>
    /// Singleton quản lý phiên đăng nhập của người dùng.
    /// Dùng Preferences để persist qua các lần mở app.
    /// </summary>
    public class UserSession
    {
        private static UserSession? _instance;
        public static UserSession Instance => _instance ??= new UserSession();

        private const string KEY_USERNAME = "session_username";
        private const string KEY_FULLNAME = "session_fullname";
        private const string KEY_IS_GUEST = "session_is_guest";
        private const string KEY_LOGGED_IN = "session_logged_in";

        private UserSession() { }

        // ── Properties ──────────────────────────────────────────────────────

        public bool IsLoggedIn => Preferences.Get(KEY_LOGGED_IN, false);
        public bool IsGuest => Preferences.Get(KEY_IS_GUEST, true);
        public string Username => Preferences.Get(KEY_USERNAME, "");
        public string FullName => Preferences.Get(KEY_FULLNAME, "Du khách");

        /// <summary>User đã đăng nhập và KHÔNG phải khách ẩn danh</summary>
        public bool IsAuthenticatedUser => IsLoggedIn && !IsGuest;

        /// <summary>Xác định người dùng đã bắt đầu tour (vào bản đồ) hay chưa</summary>
        public bool IsTourActive { get; set; } = false;

        /// <summary>Vị trí GPS cuối cùng — cập nhật bởi MapPage, dùng để ping lên server</summary>
        public double LastLat { get; set; } = 0;
        public double LastLng { get; set; } = 0;

        // ── Methods ─────────────────────────────────────────────────────────

        public void LoginAsUser(string username, string fullName)
        {
            Preferences.Set(KEY_USERNAME, username);
            Preferences.Set(KEY_FULLNAME, string.IsNullOrWhiteSpace(fullName) ? username : fullName);
            Preferences.Set(KEY_IS_GUEST, false);
            Preferences.Set(KEY_LOGGED_IN, true);
            _ = Task.Run(async () => {
                await App.SendHeartbeatAsync();
                await AnalyticsService.RecordAppLoginAsync();
            });
        }

        public void LoginAsGuest()
        {
            Preferences.Set(KEY_USERNAME, "guest");
            Preferences.Set(KEY_FULLNAME, "Du khách");
            Preferences.Set(KEY_IS_GUEST, true);
            Preferences.Set(KEY_LOGGED_IN, true);
            _ = Task.Run(async () => {
                await App.SendHeartbeatAsync();
                await AnalyticsService.RecordAppLoginAsync();
            });
        }

        public void Logout()
        {
            var sessionId = Preferences.Get("device_session_id", "");
            if (!string.IsNullOrEmpty(sessionId))
            {
                _ = Task.Run(async () => await ApiService.Instance.EndActiveStatusAsync(sessionId));
                // Xóa session ID — tránh ghost session khi mở lại app
                Preferences.Remove("device_session_id");
            }

            Preferences.Remove(KEY_USERNAME);
            Preferences.Remove(KEY_FULLNAME);
            Preferences.Remove(KEY_IS_GUEST);
            Preferences.Remove(KEY_LOGGED_IN);
            IsTourActive = false;
        }
    }
}
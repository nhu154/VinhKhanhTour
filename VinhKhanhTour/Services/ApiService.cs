using System.Net.Http.Json;
using VinhKhanhTour.Models;
using VinhKhanhTour.Helpers;

namespace VinhKhanhTour.Services
{
    public class ApiService
    {
        private static ApiService? _instance;
        public static ApiService Instance => _instance ??= new ApiService();

        private readonly HttpClient _http;

        // ── FIX: Đọc BASE_URL từ Config để dễ thay đổi, không cần sửa nhiều chỗ ──
        // Trong Config.cs, thêm:  public const string ApiBaseUrl = "http://192.168.1.29:5256/api";
        // Nếu chưa có, fallback về địa chỉ mặc định
        private static string BASE
        {
            get
            {
                string baseUrl = Config.ApiBaseUrl;

                // ── HỖ TRỢ MÁY ẢO ──
                // Nếu là máy ảo Android, ta chuyển IP LAN thành 10.0.2.2 để có thể gọi về localhost máy tính
                if (DeviceInfo.DeviceType == DeviceType.Virtual && DeviceInfo.Platform == DevicePlatform.Android)
                {
                    // Tự động thay thế IP LAN bằng địa chỉ loopback của máy ảo
                    baseUrl = baseUrl.Replace("192.168.1.34", "10.0.2.2");
                }

                return baseUrl.TrimEnd('/');
            }
        }

        private ApiService()
        {
            _http = new HttpClient
            {
                // ── FIX: Giảm timeout xuống 5s để không bị treo quá lâu khi offline ──
                Timeout = TimeSpan.FromSeconds(5)
            };
        }

        // ── Kiểm tra kết nối API ──────────────────────────────────────────────
        public async Task<bool> PingAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var res = await _http.GetAsync($"{BASE}/restaurants?limit=1", cts.Token);
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // ── Normalize image URL ───────────────────────────────────────────────
        public static string NormalizeImageUrl(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return "";

            // Thay thế các biến thể localhost → địa chỉ IP thực
            var baseHost = BASE.Replace("/api", "");
            imageUrl = imageUrl
                .Replace("localhost:5256", new Uri(baseHost).Host + ":" + (new Uri(baseHost).Port))
                .Replace("127.0.0.1:5256", new Uri(baseHost).Host + ":" + (new Uri(baseHost).Port))
                .Replace("10.0.2.2:5256", new Uri(baseHost).Host + ":" + (new Uri(baseHost).Port));

            if (imageUrl.StartsWith("http://") || imageUrl.StartsWith("https://"))
                return imageUrl;

            return baseHost.TrimEnd('/') + "/" + imageUrl.TrimStart('/');
        }

        // ── Restaurants ───────────────────────────────────────────────────────
        public async Task<List<Restaurant>> GetRestaurantsAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var list = await _http.GetFromJsonAsync<List<Restaurant>>($"{BASE}/restaurants", cts.Token);
                if (list != null)
                    foreach (var r in list)
                        r.ImageUrl = NormalizeImageUrl(r.ImageUrl);
                return list ?? [];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] GetRestaurants: {ex.Message}");
                return [];
            }
        }

        public async Task<Restaurant?> GetRestaurantByIdAsync(int id)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var r = await _http.GetFromJsonAsync<Restaurant>($"{BASE}/restaurants/{id}", cts.Token);
                if (r != null)
                    r.ImageUrl = NormalizeImageUrl(r.ImageUrl);
                return r;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] GetRestaurantById: {ex.Message}");
                return null;
            }
        }

        // ── Analytics ─────────────────────────────────────────────────────────
        public async Task<bool> PostAnalyticAsync(int restaurantId, string eventType = "visit",
            double lat = 0, double lng = 0, double value = 0.0)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var res = await _http.PostAsJsonAsync($"{BASE}/analytics", new
                {
                    RestaurantId = restaurantId,
                    EventType = eventType,
                    Value = value,
                    Lat = lat,
                    Lng = lng
                }, cts.Token);
                return res.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] PostAnalytic: {ex.Message}");
                return false;
            }
        }

        // ── Bookings ──────────────────────────────────────────────────────────
        public async Task<bool> PostBookingAsync(Booking booking)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                var res = await _http.PostAsJsonAsync($"{BASE}/bookings", new
                {
                    RestaurantId = booking.RestaurantId,
                    RestaurantName = booking.RestaurantName,
                    CustomerName = booking.CustomerName,
                    CustomerPhone = booking.CustomerPhone,
                    GuestCount = booking.GuestCount,
                    BookingDate = booking.BookingDate,
                    BookingTime = booking.BookingTime,
                    Note = booking.Note,
                    PaymentMethod = booking.PaymentMethod,
                    PaymentStatus = booking.PaymentStatus,
                    DepositAmount = booking.DepositAmount,
                    Status = booking.Status,
                    BookingCode = booking.BookingCode
                }, cts.Token);
                return res.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] PostBooking: {ex.Message}");
                return false;
            }
        }

        // ── GPS / Heatmap ─────────────────────────────────────────────────────
        public async Task PostGpsPointAsync(double lat, double lng)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
                await _http.PostAsJsonAsync($"{BASE}/analytics", new
                {
                    RestaurantId = 0,
                    EventType = "gps_point",
                    Value = 0.0,
                    Lat = lat,
                    Lng = lng,
                    Username = UserSession.Instance.Username
                }, cts.Token);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] PostGpsPoint: {ex.Message}");
            }
        }

        // ── Tracking ────────────────────────────────────────────────────────
        public async Task PingActiveStatusAsync(string sessionId, string username, bool isAnonymous, double? lat = null, double? lng = null)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await _http.PostAsJsonAsync($"{BASE}/tracking/ping", new
                {
                    SessionId = sessionId,
                    Username = username,
                    IsAnonymous = isAnonymous,
                    Lat = lat,
                    Lng = lng
                }, cts.Token);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] PingActiveStatus: {ex.Message}");
            }
        }

        public async Task EndActiveStatusAsync(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) return;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await _http.DeleteAsync($"{BASE}/tracking/online-users/{sessionId}", cts.Token);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] EndActiveStatus: {ex.Message}");
            }
        }

        public async Task LogUserActivityAsync(string action, string target, string details = "")
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await _http.PostAsJsonAsync($"{BASE}/AdminLogs/user-activity", new
                {
                    Action = action,
                    Target = target,
                    UserName = UserSession.Instance.Username,
                    Details = details
                }, cts.Token);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] LogUserActivity: {ex.Message}");
            }
        }

        // ── Auth ──────────────────────────────────────────────────────────────
        public async Task<LoginResult?> LoginWithDetailsAsync(string username, string password)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var res = await _http.PostAsJsonAsync($"{BASE}/auth/login", new
                {
                    Username = username,
                    Password = password
                }, cts.Token);
                if (!res.IsSuccessStatusCode) return null;

                var json = await res.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(
                    cancellationToken: cts.Token);
                return new LoginResult
                {
                    Success = true,
                    FullName = json.TryGetProperty("fullName", out var fn) ? fn.GetString() ?? "" : "",
                    Role = json.TryGetProperty("role", out var r) ? r.GetString() ?? "user" : "user",
                    Id = json.TryGetProperty("id", out var id) ? id.GetInt32() : 0
                };
            }
            catch { return null; }
        }

        public async Task<bool> LoginAsync(string username, string password)
            => (await LoginWithDetailsAsync(username, password))?.Success == true;

        public async Task<bool> RegisterAsync(string username, string password, string fullName)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var res = await _http.PostAsJsonAsync($"{BASE}/auth/register", new
                {
                    Username = username,
                    Password = password,
                    FullName = fullName
                }, cts.Token);
                return res.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // ── Tours ─────────────────────────────────────────────────────────────
        public async Task<List<ApiTour>> GetToursAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var list = await _http.GetFromJsonAsync<List<ApiTour>>($"{BASE}/tours", cts.Token);
                if (list != null)
                    foreach (var t in list)
                        t.ImageUrl = NormalizeImageUrl(t.ImageUrl);
                return list ?? [];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] GetTours: {ex.Message}");
                return [];
            }
        }

        // ── Languages ─────────────────────────────────────────────────────────
        public async Task<List<AppLanguage>> GetLanguagesAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var list = await _http.GetFromJsonAsync<List<AppLanguage>>($"{BASE}/languages", cts.Token);
                return list ?? DefaultLanguages();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] GetLanguages: {ex.Message}");
                return DefaultLanguages();
            }
        }

        private static List<AppLanguage> DefaultLanguages() =>
        [
            new AppLanguage { Code = "vi", Name = "Tiếng Việt", Flag = "🇻🇳", IsDefault = true },
            new AppLanguage { Code = "en", Name = "English",     Flag = "🇺🇸", IsDefault = false },
            new AppLanguage { Code = "zh", Name = "中文",          Flag = "🇨🇳", IsDefault = false },
        ];

        public async Task ClearAnalyticsOnServerAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _http.DeleteAsync($"{BASE}/analytics/clear", cts.Token);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] ClearAnalytics: {ex.Message}");
            }
        }

        // ── Device Management (Quản lý thiết bị) ─────────────────────────────

        /// <summary>Đăng ký thiết bị khi đăng nhập</summary>
        public async Task<DeviceRegisterResult> RegisterDeviceAsync(int userId, string deviceId,
            string deviceName, string platform)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var res = await _http.PostAsJsonAsync($"{BASE}/devices/register", new
                {
                    UserId = userId,
                    DeviceId = deviceId,
                    DeviceName = deviceName,
                    Platform = platform
                }, cts.Token);

                var json = await res.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(
                    cancellationToken: cts.Token);

                if (res.IsSuccessStatusCode)
                {
                    return new DeviceRegisterResult
                    {
                        Success = true,
                        Message = json.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "",
                        IsNew = json.TryGetProperty("isNew", out var n) && n.GetBoolean()
                    };
                }

                // Conflict = đã đạt giới hạn
                if ((int)res.StatusCode == 409)
                {
                    return new DeviceRegisterResult
                    {
                        Success = false,
                        LimitReached = true,
                        Message = json.TryGetProperty("message", out var m2) ? m2.GetString() ?? "" : ""
                    };
                }

                return new DeviceRegisterResult { Success = false, Message = "Lỗi đăng ký thiết bị" };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] RegisterDevice: {ex.Message}");
                return new DeviceRegisterResult { Success = false, Message = ex.Message };
            }
        }

        /// <summary>Gửi heartbeat cho thiết bị (gọi định kỳ)</summary>
        public async Task<bool> DeviceHeartbeatAsync(int userId, string deviceId)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var res = await _http.PutAsJsonAsync($"{BASE}/devices/heartbeat", new
                {
                    UserId = userId,
                    DeviceId = deviceId
                }, cts.Token);

                if (!res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(
                        cancellationToken: cts.Token);
                    bool revoked = json.TryGetProperty("revoked", out var r) && r.GetBoolean();
                    if (revoked)
                    {
                        System.Diagnostics.Debug.WriteLine("[ApiService] Device has been revoked!");
                        return false; // Thiết bị bị thu hồi → cần logout
                    }
                }

                return res.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] DeviceHeartbeat: {ex.Message}");
                return true; // Lỗi mạng → giữ nguyên, không ép logout
            }
        }

        /// <summary>Thu hồi thiết bị</summary>
        public async Task<bool> RevokeDeviceAsync(int userId, string deviceId)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var res = await _http.DeleteAsync(
                    $"{BASE}/devices/{userId}/{Uri.EscapeDataString(deviceId)}", cts.Token);
                return res.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        /// <summary>Lấy dữ liệu Heatmap người dùng thực tế (live)</summary>
        public async Task<List<HeatmapPoint>> GetLiveHeatmapDataAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var res = await _http.GetFromJsonAsync<HeatmapResponse>($"{BASE}/analytics/heatmap-live", cts.Token);
                return res?.Data ?? [];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] GetLiveHeatmap: {ex.Message}");
                return [];
            }
        }

    }

    // ── Data models ───────────────────────────────────────────────────────────

    public class HeatmapResponse
    {
        public bool Success { get; set; }
        public List<HeatmapPoint> Data { get; set; } = [];
    }

    public class HeatmapPoint
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
        public double Weight { get; set; } = 1.0;
    }

    public class DeviceRegisterResult
    {
        public bool Success { get; set; }
        public bool LimitReached { get; set; }
        public bool IsNew { get; set; }
        public string Message { get; set; } = "";
    }



    public class LoginResult
    {
        public bool Success { get; set; }
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "user";
        public int Id { get; set; }
    }

    public class ApiTour
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string NameEn { get; set; } = "";
        public string NameZh { get; set; } = "";
        public string NameJa { get; set; } = "";
        public string NameKo { get; set; } = "";
        public string Description { get; set; } = "";
        public string DescEn { get; set; } = "";
        public string DescZh { get; set; } = "";
        public string DescJa { get; set; } = "";
        public string DescKo { get; set; } = "";
        public string Duration { get; set; } = "45 phút";
        public double Rating { get; set; } = 4.0;
        public string Emoji { get; set; } = "🍜";
        public string ImageUrl { get; set; } = "";
        public string Pois { get; set; } = "[]";

        public List<int> GetRestaurantIds()
        {
            if (string.IsNullOrWhiteSpace(Pois) || Pois == "[]") return [];
            try
            {
                if (Pois.Trim().StartsWith('['))
                    return System.Text.Json.JsonSerializer.Deserialize<List<int>>(Pois) ?? [];

                return Pois.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
                           .Where(id => id > 0).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiTour] Parse Pois Error: {ex.Message}");
                return [];
            }
        }

        public string GetName(string lang) => lang switch
        {
            "en" => !string.IsNullOrEmpty(NameEn) ? NameEn : Name,
            "zh" => !string.IsNullOrEmpty(NameZh) ? NameZh : (!string.IsNullOrEmpty(NameEn) ? NameEn : Name),
            "ja" => !string.IsNullOrEmpty(NameJa) ? NameJa : (!string.IsNullOrEmpty(NameEn) ? NameEn : Name),
            "ko" => !string.IsNullOrEmpty(NameKo) ? NameKo : (!string.IsNullOrEmpty(NameEn) ? NameEn : Name),
            _ => Name
        };

        public string GetDescription(string lang) => lang switch
        {
            "en" => !string.IsNullOrEmpty(DescEn) ? DescEn : Description,
            "zh" => !string.IsNullOrEmpty(DescZh) ? DescZh : (!string.IsNullOrEmpty(DescEn) ? DescEn : Description),
            "ja" => !string.IsNullOrEmpty(DescJa) ? DescJa : (!string.IsNullOrEmpty(DescEn) ? DescEn : Description),
            "ko" => !string.IsNullOrEmpty(DescKo) ? DescKo : (!string.IsNullOrEmpty(DescEn) ? DescEn : Description),
            _ => Description
        };

        public string GetDuration(string lang) => lang switch
        {
            "en" => Duration.Replace("phút", "min"),
            "zh" => Duration.Replace("phút", "分钟"),
            "ja" => Duration.Replace("phút", "分"),
            "ko" => Duration.Replace("phút", "분"),
            _ => Duration
        };
    }

    public class AppLanguage
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Flag { get; set; } = "🌐";
        public bool IsDefault { get; set; }
        public int SortOrder { get; set; }
    }
}
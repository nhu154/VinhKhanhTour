using System.Net.Http.Json;
using VinhKhanhTour.Models;

namespace VinhKhanhTour.Services
{
    public class ApiService
    {
        private static ApiService? _instance;
        public static ApiService Instance => _instance ??= new ApiService();

        private readonly HttpClient _http;
        private const string BASE = "http://10.0.2.2:5256/api";

        private ApiService()
        {
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        }

        public async Task<List<Restaurant>> GetRestaurantsAsync()
        {
            try
            {
                var list = await _http.GetFromJsonAsync<List<Restaurant>>($"{BASE}/restaurants");
                return list ?? new List<Restaurant>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] GetRestaurants: {ex.Message}");
                return new List<Restaurant>();
            }
        }

        public async Task PostAnalyticAsync(int restaurantId, string eventType = "visit")
        {
            try
            {
                await _http.PostAsJsonAsync($"{BASE}/analytics", new
                {
                    RestaurantId = restaurantId,
                    EventType = eventType
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] PostAnalytic: {ex.Message}");
            }
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                var res = await _http.PostAsJsonAsync($"{BASE}/auth/login", new
                {
                    Username = username,
                    Password = password
                });
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RegisterAsync(string username, string password, string fullName)
        {
            try
            {
                var res = await _http.PostAsJsonAsync($"{BASE}/auth/register", new
                {
                    Username = username,
                    Password = password,
                    FullName = fullName
                });
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<ApiTour>> GetToursAsync()
        {
            try
            {
                var list = await _http.GetFromJsonAsync<List<ApiTour>>($"{BASE}/tours");
                return list ?? new List<ApiTour>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] GetTours: {ex.Message}");
                return new List<ApiTour>();
            }
        }

        public async Task ClearAnalyticsOnServerAsync()
        {
            try
            {
                await _http.DeleteAsync($"{BASE}/analytics/clear");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] ClearAnalytics: {ex.Message}");
            }
        }
    }

    public class ApiTour
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string NameEn { get; set; } = "";
        public string NameZh { get; set; } = "";
        public string Description { get; set; } = "";
        public string DescEn { get; set; } = "";
        public string DescZh { get; set; } = "";
        public string Duration { get; set; } = "45 phút";
        public double Rating { get; set; } = 4.0;
        public string Emoji { get; set; } = "🍜";
        public string ImageUrl { get; set; } = "";
        public string Pois { get; set; } = "[]";

        public List<int> GetRestaurantIds()
        {
            if (string.IsNullOrWhiteSpace(Pois) || Pois == "[]") return new List<int>();
            try 
            { 
                // Xử lý cả trường hợp Pois là JSON array [1,2,3] hoặc chuỗi phân tách bởi dấu phẩy
                if (Pois.Trim().StartsWith("["))
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<int>>(Pois) ?? new List<int>(); 
                }
                else
                {
                    return Pois.Split(',', StringSplitOptions.RemoveEmptyEntries)
                               .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
                               .Where(id => id > 0)
                               .ToList();
                }
            }
            catch (Exception ex) 
            { 
                System.Diagnostics.Debug.WriteLine($"[ApiTour] Parse Pois Error: {ex.Message} (Data: {Pois})");
                return new List<int>(); 
            }
        }

        public string GetName(string lang) => lang switch
        {
            "en" => !string.IsNullOrEmpty(NameEn) ? NameEn : Name,
            "zh" => !string.IsNullOrEmpty(NameZh) ? NameZh : Name,
            _ => Name
        };

        public string GetDescription(string lang) => lang switch
        {
            "en" => !string.IsNullOrEmpty(DescEn) ? DescEn : Description,
            "zh" => !string.IsNullOrEmpty(DescZh) ? DescZh : Description,
            _ => Description
        };

        public string GetDuration(string lang) => lang switch
        {
            "en" => Duration.Replace("phút", "min"),
            "zh" => Duration.Replace("phút", "分钟"),
            _ => Duration
        };
    }
}
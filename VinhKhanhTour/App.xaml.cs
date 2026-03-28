using VinhKhanhTour.Services;
using VinhKhanhTour.Models;

namespace VinhKhanhTour
{
    public partial class App : Application
    {
        public static DatabaseService Database { get; private set; } = null!;

        public App()
        {
            InitializeComponent();
            Database = new DatabaseService();
            Task.Run(async () => await InitializeSampleData()).GetAwaiter().GetResult();
            MainPage = new NavigationPage(new LoginPage());
        }

        private async Task InitializeSampleData()
        {
            // ── Bước 1: Thử load từ API ───────────────────────────
            try
            {
                var apiList = await ApiService.Instance.GetRestaurantsAsync();
                if (apiList.Count > 0)
                {
                    var oldList = await Database.GetRestaurantsAsync();
                    foreach (var old in oldList)
                        await Database.DeleteRestaurantAsync(old.Id);

                    foreach (var r in apiList)
                        await Database.SaveRestaurantAsync(r);

                    System.Diagnostics.Debug.WriteLine($"[App] ✅ Loaded {apiList.Count} restaurants from API");
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] ⚠️ API unavailable, using local seed: {ex.Message}");
            }

            // ── Bước 2: Fallback seed data local ─────────────────
            var restaurants = await Database.GetRestaurantsAsync();
            if (restaurants.Count == 0)
            {
                await Database.SaveRestaurantAsync(new Restaurant
                {
                    Name = "Ốc Oanh",
                    Description = "Ốc nướng tiêu, ốc hấp sả đặc sản",
                    Category = "Quán ăn",
                    Latitude = 10.760825909365554,
                    Longitude = 106.70331368648232,
                    Address = "Vĩnh Khánh, Phường 8, Quận 4",
                    Rating = 4.3,
                    OpenHours = "16:00 - 23:00",
                    ImageUrl = "oc_oanh.jpg",
                    TtsScript = "Chào mừng bạn đến với Ốc Oanh - linh hồn ẩm thực của phố Vĩnh Khánh.",
                    TtsScriptEn = "Welcome to Oc Oanh, the culinary soul of Vinh Khanh street.",
                    TtsScriptZh = "欢迎来到'欧莺海鲜'。"
                });
                await Database.SaveRestaurantAsync(new Restaurant
                {
                    Name = "Ốc Sáu Nở",
                    Description = "Ốc tươi ngon đa dạng",
                    Category = "Quán ăn",
                    Latitude = 10.761090779311022,
                    Longitude = 106.70289908345818,
                    Address = "Vĩnh Khánh, Phường 8, Quận 4",
                    Rating = 4.4,
                    OpenHours = "15:00 - 23:00",
                    ImageUrl = "oc_sauno.jpg",
                    TtsScript = "Bạn đang đến gần quán Ốc Sáu Nở - thiên đường hải sản tươi sống.",
                    TtsScriptEn = "You are approaching Sau No Snail Restaurant.",
                    TtsScriptZh = "您正在靠近'六绽海鲜'。"
                });
                await Database.SaveRestaurantAsync(new Restaurant
                {
                    Name = "Ốc Thảo",
                    Description = "Ốc các loại chế biến đa dạng",
                    Category = "Quán ăn",
                    Latitude = 10.761758951046252,
                    Longitude = 106.70235823553499,
                    Address = "Vĩnh Khánh, Phường 8, Quận 4",
                    Rating = 4.2,
                    OpenHours = "16:00 - 22:30",
                    ImageUrl = "oc_thao.jpg",
                    TtsScript = "Phía trước là Ốc Thảo với bí quyết biến tấu gia vị độc đáo.",
                    TtsScriptEn = "Just ahead is Oc Thao with unique spice blends.",
                    TtsScriptZh = "前方是'草海鲜'。"
                });
            }
        }
    }
}
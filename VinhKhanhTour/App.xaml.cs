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
            MainPage = new NavigationPage(new LoginPage());
            // Load data sau khi UI khởi động - không block main thread
            _ = Task.Run(async () => await InitializeSampleData());
        }

        private async Task InitializeSampleData()
        {
            // ── Bước 1: Thử load từ API ───────────────────────────
            try
            {
                var apiList = await ApiService.Instance.GetRestaurantsAsync();
                if (apiList.Count > 0)
                {
                    // Xóa hết SQLite local để đồng bộ ID với MySQL
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
                System.Diagnostics.Debug.WriteLine($"[App] ⚠️ API unavailable: {ex.Message}");
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
                await Database.SaveRestaurantAsync(new Restaurant
                {
                    Name = "Lãng Quán",
                    Description = "Quán ăn phong cách trẻ trung",
                    Category = "Quán ăn",
                    Latitude = 10.761281731910726,
                    Longitude = 106.70537328006456,
                    Address = "Vĩnh Khánh, Phường 8, Quận 4",
                    Rating = 4.5,
                    OpenHours = "17:00 - 23:00",
                    ImageUrl = "langquan.jpeg",
                    TtsScript = "Lãng Quán - không gian trẻ trung giữa lòng phố ẩm thực Vĩnh Khánh.",
                    TtsScriptEn = "Lang Quan - a vibrant space in the heart of Vinh Khanh food street.",
                    TtsScriptZh = "浪漫小馆"
                });
                await Database.SaveRestaurantAsync(new Restaurant
                {
                    Name = "Ớt Xiêm Quán",
                    Description = "Đồ ăn cay nồng đậm đà",
                    Category = "Quán ăn",
                    Latitude = 10.761345472033696,
                    Longitude = 106.70569016657214,
                    Address = "Vĩnh Khánh, Phường 8, Quận 4",
                    Rating = 4.4,
                    OpenHours = "11:00 - 21:00",
                    ImageUrl = "otxiemquan.jpg",
                    TtsScript = "Ớt Xiêm Quán đang ở ngay trước mắt bạn!",
                    TtsScriptEn = "Ot Xiem Quan is right in front of you!",
                    TtsScriptZh = "朝天椒餐厅就在您眼前"
                });
                await Database.SaveRestaurantAsync(new Restaurant
                {
                    Name = "Bún Cá Châu Đốc - Dì Tư",
                    Description = "Bún cá Châu Đốc chính gốc",
                    Category = "Quán ăn",
                    Latitude = 10.761060311531145,
                    Longitude = 106.70668201075144,
                    Address = "Vĩnh Khánh, Phường 8, Quận 4",
                    Rating = 4.6,
                    OpenHours = "06:00 - 14:00",
                    ImageUrl = "buncachaudoc.jpg",
                    TtsScript = "Mùi mắm cá linh đặc trưng từ Bún Cá Châu Đốc Dì Tư chắc hẳn đã lan tỏa đến bạn.",
                    TtsScriptEn = "The distinctive aroma from Auntie Tu Chau Doc Fish Noodle Soup must have reached you.",
                    TtsScriptZh = "四姨朱笃鱼头米粉的独特香气想必已经飘到您身边"
                });
                await Database.SaveRestaurantAsync(new Restaurant
                {
                    Name = "Chilli Lẩu Nướng Quán",
                    Description = "Lẩu nướng buffet giá sinh viên",
                    Category = "Quán ăn",
                    Latitude = 10.760840551806485,
                    Longitude = 106.70405082000606,
                    Address = "Vĩnh Khánh, Phường 8, Quận 4",
                    Rating = 4.3,
                    OpenHours = "10:00 - 23:00",
                    ImageUrl = "chililaunuong.jpg",
                    TtsScript = "Chào bạn đến với Chilli Lẩu Nướng. Buffet thịt nướng và lẩu đang chờ đón bạn.",
                    TtsScriptEn = "Welcome to Chilli Hotpot and BBQ.",
                    TtsScriptZh = "欢迎来到辣椒火锅烤肉"
                });
                await Database.SaveRestaurantAsync(new Restaurant
                {
                    Name = "Thế Giới Bò",
                    Description = "Các món bò đa dạng chất lượng",
                    Category = "Quán ăn",
                    Latitude = 10.764267370582093,
                    Longitude = 106.70118183588556,
                    Address = "Vĩnh Khánh, Phường 8, Quận 4",
                    Rating = 4.5,
                    OpenHours = "10:00 - 22:00",
                    ImageUrl = "thegioibo.jpg",
                    TtsScript = "Bạn đã đặt chân đến Thế Giới Bò!",
                    TtsScriptEn = "You have set foot in The World of Beef!",
                    TtsScriptZh = "您已踏入牛肉世界"
                });
                await Database.SaveRestaurantAsync(new Restaurant
                {
                    Name = "Cơm Cháy Kho Quẹt",
                    Description = "Cơm cháy giòn rụm kho quẹt đậm đà",
                    Category = "Quán ăn",
                    Latitude = 10.760625291110975,
                    Longitude = 106.70371667475501,
                    Address = "Vĩnh Khánh, Phường 8, Quận 4",
                    Rating = 4.4,
                    OpenHours = "10:00 - 21:00",
                    ImageUrl = "comchaykhoquet.jpg",
                    TtsScript = "Âm thanh rôm rốp giòn vang của Cơm Cháy Kho Quẹt đang thu hút sự chú ý của bạn.",
                    TtsScriptEn = "The crunchy crackling sound of Com Chay Kho Quet is catching your attention.",
                    TtsScriptZh = "锅巴蘸酱酥脆诱人的声音正在吸引您的注意"
                });
                await Database.SaveRestaurantAsync(new Restaurant
                {
                    Name = "Bò Lá Lốt Cô Út",
                    Description = "Bò lá lốt nướng thơm ngon",
                    Category = "Quán ăn",
                    Latitude = 10.761278781423528,
                    Longitude = 106.70529381362458,
                    Address = "Vĩnh Khánh, Phường 8, Quận 4",
                    Rating = 4.7,
                    OpenHours = "15:00 - 23:00",
                    ImageUrl = "bolalotcout.jpg",
                    TtsScript = "Hương thơm quyến rũ này đi ra từ quán Bò Lá Lốt Cô Út.",
                    TtsScriptEn = "This charming aroma is wafting from Auntie Ut Grilled Beef in Lolot Leaves.",
                    TtsScriptZh = "这迷人香气正是来自幺姑蒌叶烤牛肉"
                });
                await Database.SaveRestaurantAsync(new Restaurant
                {
                    Name = "Bún Thịt Nướng Cô Nga",
                    Description = "Bún thịt nướng đặc biệt",
                    Category = "Quán ăn",
                    Latitude = 10.760883450920542,
                    Longitude = 106.70674182239293,
                    Address = "Vĩnh Khánh, Phường 8, Quận 4",
                    Rating = 4.5,
                    OpenHours = "06:00 - 20:00",
                    ImageUrl = "bunthitnuongconga.jpg",
                    TtsScript = "Bạn đang đứng trước Bún Thịt Nướng Cô Nga.",
                    TtsScriptEn = "You are standing in front of Ms. Nga Grilled Pork Noodles.",
                    TtsScriptZh = "您现在正站在阿娥烤肉米粉门前"
                });
            }
        }
    }
}
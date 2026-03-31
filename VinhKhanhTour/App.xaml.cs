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
            _ = Task.Run(async () => await InitializeSampleData());
        }

        private async Task InitializeSampleData()
        {
            // ── Bước 1: Thử load từ API ──
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
                System.Diagnostics.Debug.WriteLine($"[App] ⚠️ API unavailable: {ex.Message}");
            }

            // ── Bước 2: Fallback seed data local ──
            var restaurants = await Database.GetRestaurantsAsync();
            if (restaurants.Count == 0)
            {
                await Database.SaveRestaurantAsync(new Restaurant
                {
                    Name = "Ốc Oanh",
                    Description = "Ốc nướng tiêu, ốc hấp sả — linh hồn ẩm thực phố Vĩnh Khánh",
                    Category = "Quán ăn",
                    Latitude = 10.760825909365554,
                    Longitude = 106.70331368648232,
                    Address = "Vĩnh Khánh, Phường 8, Quận 4",
                    Rating = 4.3,
                    OpenHours = "16:00 - 23:00",
                    ImageUrl = "https://images.unsplash.com/photo-1559329007-40df8a9345d8?auto=format&fit=crop&w=600&q=80",
                    TtsScript = "Chào mừng bạn đến với Ốc Oanh — linh hồn phố ẩm thực Vĩnh Khánh hơn mười năm qua! Quán nổi tiếng với ốc hương nướng tiêu xanh, ốc mỡ hấp sả gừng và ốc len xào dừa béo ngậy. Mỗi con ốc tươi chọn lọc từ miền Tây mỗi sáng. Thực đơn hơn hai mươi món, nước chấm bí truyền độc quyền. Mở cửa từ bốn giờ chiều đến mười một giờ đêm. Đặt chỗ sớm kẻo hết!",
                    TtsScriptEn = "Welcome to Oc Oanh — the soul of Vinh Khanh food street for over a decade! Famous for pepper-grilled rock snails, lemongrass-steamed butter snails, and rich coconut-sauteed mud creepers, all hand-picked fresh every morning from the Mekong Delta. Menu features twenty shellfish preparations with a secret house dipping sauce. Open four in the afternoon until eleven at night. Book early — always packed!",
                    TtsScriptZh = "欢迎来到欧莺——永庆街美食灵魂超过十年！以胡椒烤香螺、香茅蒸蜗牛和椰汁炒泥螺著称，每日清晨从湄公河精选食材。菜单超过二十种贝类料理，搭配秘制蘸酱。下午四点至晚上十一点营业，请提前预订！"
                });
                await Database.SaveRestaurantAsync(new Restaurant
                {
                    Name = "Ốc Sáu Nở",
                    Description = "Thiên đường hải sản tươi sống, ốc đa dạng chế biến độc đáo",
                    Category = "Quán ăn",
                    Latitude = 10.761090779311022,
                    Longitude = 106.70289908345818,
                    Address = "Vĩnh Khánh, Phường 8, Quận 4",
                    Rating = 4.4,
                    OpenHours = "15:00 - 23:00",
                    ImageUrl = "https://images.unsplash.com/photo-1565680018434-b513d5e5fd47?auto=format&fit=crop&w=600&q=80",
                    TtsScript = "Bạn đang đến Ốc Sáu Nở — biểu tượng hải sản Sài Gòn nhiều thập kỷ! Thử ngay ốc bươu nhồi thịt hấp gừng, ốc nhảy xào me chua ngọt và mâm ốc tổng hợp năm loại đặc biệt. Nước chấm bí truyền gia truyền là linh hồn của quán. Ngồi vỉa hè Vĩnh Khánh, nhâm nhi ly mát cùng mâm ốc bốc khói — trải nghiệm Sài Gòn đích thực. Mở từ ba giờ chiều đến mười một giờ đêm!",
                    TtsScriptEn = "Approaching Oc Sau No — a Saigon seafood icon for decades! Must-try: ginger-steamed stuffed snails, tamarind-glazed jumping snails, and the five-variety shellfish platter. Their secret dipping sauce keeps regulars coming back. Sitting on the lively Vinh Khanh sidewalk with an ice-cold drink alongside steaming shellfish — authentic Saigon street food. Open three in the afternoon until eleven at night!",
                    TtsScriptZh = "欢迎来到六绽海鲜——西贡数十年的海鲜传奇！必点：姜汁蒸肉馅田螺、酸甜罗望子炒蜗牛和五种贝类拼盘。秘制蘸酱代代相传，令老顾客念念不忘。坐在永庆街边，冷饮配热螺，尽享西贡街头真味。每天下午三点至晚上十一点营业！"
                });
                await Database.SaveRestaurantAsync(new Restaurant
                {
                    Name = "Ốc Thảo",
                    Description = "Ốc các loại chế biến đa dạng, gia vị biến tấu độc đáo",
                    Category = "Quán ăn",
                    Latitude = 10.761758951046252,
                    Longitude = 106.70235823553499,
                    Address = "Vĩnh Khánh, Phường 8, Quận 4",
                    Rating = 4.2,
                    OpenHours = "16:00 - 22:30",
                    ImageUrl = "https://images.unsplash.com/photo-1601050690597-df0568f70950?auto=format&fit=crop&w=600&q=80",
                    TtsScript = "Phía trước là Ốc Thảo — một trong những quán ốc lâu đời nhất Vĩnh Khánh! Điểm đặc biệt là gia vị độc đáo kết hợp truyền thống Nam Bộ và sáng tạo hiện đại. Thử ốc nướng muối ớt giòn tan, ốc sò hấp bia thơm nồng và ốc de nấu tiêu đen đậm đà. Chị Thảo luôn đích thân đứng bếp đảm bảo hương vị chuẩn nhất. Không khí thân thiện, giá hợp lý. Mở từ bốn giờ chiều đến mười giờ rưỡi tối!",
                    TtsScriptEn = "Just ahead is Oc Thao — one of Vinh Khanh's oldest and most beloved shellfish spots! Known for blending traditional Southern spices with modern creativity. Try salt-chili grilled snails, beer-steamed clams, and savory black pepper mud snails. Ms. Thao personally oversees every dish. Friendly atmosphere, affordable prices. Open four in the afternoon until half past ten at night!",
                    TtsScriptZh = "前方是草海鲜——永庆街历史最悠久的贝壳餐厅之一！以传统南越香料与现代创意的独特融合闻名。必尝盐辣烤螺、啤酒蒸蛤蜊和黑椒焖泥螺。草姐每天亲自掌厨，品质始终如一。氛围友好，价格亲民。下午四点至晚上十点半营业！"
                });
                await Database.SaveRestaurantAsync(new Restaurant
                {
                    Name = "Lãng Quán",
                    Description = "Không gian trẻ trung sáng tạo, ẩm thực đường phố Sài Gòn đa dạng",
                    Category = "Quán ăn",
                    Latitude = 10.761281731910726,
                    Longitude = 106.70537328006456,
                    Address = "Vĩnh Khánh, Phường 8, Quận 4",
                    Rating = 4.5,
                    OpenHours = "17:00 - 23:00",
                    ImageUrl = "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=600&q=80",
                    TtsScript = "Chào bạn đến Lãng Quán — không gian ẩm thực trẻ trung sáng tạo giữa phố Vĩnh Khánh! Không chỉ là quán ăn — đây là nơi bạn bè hội tụ và tạo kỷ niệm. Thực đơn đa dạng từ ăn vặt Sài Gòn đến lẩu riêu cua, bò lúc lắc tiêu xanh, gà nướng mật ong. Cuối tuần có âm nhạc live tạo không gian lãng mạn. Phục vụ từ năm giờ chiều đến mười một giờ đêm. Hẹn gặp bạn tại Lãng Quán!",
                    TtsScriptEn = "Welcome to Lang Quan — a vibrant creative dining space in the heart of Vinh Khanh! More than a restaurant — a gathering place for friends and memories. The menu spans Saigon street snacks to crab roe hotpot, green pepper beef, and honey-glazed grilled chicken. Weekend evenings feature live acoustic music. Open five in the afternoon until eleven at night. See you at Lang Quan!",
                    TtsScriptZh = "欢迎来到浪漫小馆——永庆街充满青春创意的餐饮空间！不只是餐厅，更是朋友聚会创造回忆之地。菜单从西贡小吃到蟹黄火锅、青椒牛肉、蜂蜜烤鸡应有尽有。周末有现场原声音乐，浪漫氛围满满。下午五点至晚上十一点营业，期待与您相聚！"
                });
                await Database.SaveRestaurantAsync(new Restaurant
                {
                    Name = "Ớt Xiêm Quán",
                    Description = "Thiên đường đồ ăn cay nồng phong cách Khmer Nam Bộ",
                    Category = "Quán ăn",
                    Latitude = 10.761345472033696,
                    Longitude = 106.70569016657214,
                    Address = "Vĩnh Khánh, Phường 8, Quận 4",
                    Rating = 4.4,
                    OpenHours = "11:00 - 21:00",
                    ImageUrl = "https://images.unsplash.com/photo-1583608205776-bfd35f0d9f83?auto=format&fit=crop&w=600&q=80",
                    TtsScript = "Ớt Xiêm Quán đang hiện ra trước mắt bạn — thiên đường của những tín đồ vị cay kiểu Khmer Nam Bộ! Ớt xiêm xanh nhỏ nhưng cực cay là linh hồn mọi món. Đừng bỏ qua gà nướng mật ong giòn vàng, tôm rang muối ớt giòn rụm và lẩu thái chua cay đậm vị. Nhân viên sẵn sàng tư vấn mức cay phù hợp. Chuẩn bị khăn giấy và chinh phục ẩm thực cay nhất Vĩnh Khánh! Mở từ mười một giờ sáng đến chín giờ tối.",
                    TtsScriptEn = "Ot Xiem Quan is right before you — paradise for lovers of bold Khmer-style fiery flavors! The small but ferocious Siam chili pepper defines every dish. Don't miss honey-glazed chili grilled chicken, crunchy salt-chili shrimp, and the sour-spicy Thai-style hotpot. Staff always help you find your ideal heat level. Open eleven in the morning until nine at night.",
                    TtsScriptZh = "朝天椒餐厅就在眼前——辣味爱好者的天堂，以越南南部高棉风味为灵感！个头小却无比辣的暹罗朝天椒贯穿所有菜肴。必尝蜂蜜烤鸡、椒盐炸虾和酸辣泰式火锅。员工随时协助您选择合适辣度。备好纸巾，挑战永庆街最辣美食！上午十一点至晚上九点营业。"
                });
                await Database.SaveRestaurantAsync(new Restaurant
                {
                    Name = "Bún Cá Châu Đốc - Dì Tư",
                    Description = "Bún cá Châu Đốc chính gốc An Giang, nước lèo mắm ruốc đặc trưng",
                    Category = "Quán ăn",
                    Latitude = 10.761060311531145,
                    Longitude = 106.70668201075144,
                    Address = "Vĩnh Khánh, Phường 8, Quận 4",
                    Rating = 4.6,
                    OpenHours = "06:00 - 14:00",
                    ImageUrl = "https://images.unsplash.com/photo-1562802378-063ec186a863?auto=format&fit=crop&w=600&q=80",
                    TtsScript = "Mùi mắm cá linh và ruốc Châu Đốc chính gốc đang lan tỏa từ bếp Dì Tư! Đây là địa chỉ hiếm tại Sài Gòn giữ nguyên hương vị bún cá An Giang. Nước lèo hầm cá lóc tươi cùng sả gừng tạo mùi thơm đặc trưng. Chan vào tô bún, thêm rau đắng, bắp chuối và cá chiên vàng — bữa sáng hoàn hảo! Mở từ sáu giờ sáng đến hai giờ chiều. Đến sớm kẻo hết!",
                    TtsScriptEn = "The sweet aroma of linh fish paste and authentic Chau Doc shrimp paste drifts from Auntie Tu's kitchen! One of Saigon's rare spots preserving the original An Giang fish noodle flavors. The broth is slow-simmered with fresh snakehead fish, lemongrass and ginger. Ladled over soft rice noodles with bitter greens, banana blossom and crispy fish — the perfect breakfast! Open six in the morning until two in the afternoon. Come early!",
                    TtsScriptZh = "四姨厨房飘来正宗朱笃虾酱的甘美香气！西贡难得保留安江原味鱼粉的珍贵地址。汤底以新鲜乌鱼、香茅姜慢火熬制，香气独特无误。浇在米粉上，配苦菜、芭蕉花和炸鱼片——完美早餐！早上六点至下午两点营业，早来早得，卖完即止！"
                });
                await Database.SaveRestaurantAsync(new Restaurant
                {
                    Name = "Chilli Lẩu Nướng Quán",
                    Description = "Buffet lẩu nướng giá sinh viên, thực đơn phong phú hàng chục món",
                    Category = "Quán ăn",
                    Latitude = 10.760840551806485,
                    Longitude = 106.70405082000606,
                    Address = "Vĩnh Khánh, Phường 8, Quận 4",
                    Rating = 4.3,
                    OpenHours = "10:00 - 23:00",
                    ImageUrl = "https://images.unsplash.com/photo-1547592166-23ac45744acd?auto=format&fit=crop&w=600&q=80",
                    TtsScript = "Chào mừng đến Chilli Lẩu Nướng — buffet lẩu nướng tự chọn hấp dẫn giữa phố Vĩnh Khánh! Tự do chọn bò Mỹ thái mỏng, hải sản tươi và rau củ đa dạng để nướng và nhúng lẩu. Nồi lẩu thái chua cay hầm xương cả ngày là điểm nhấn đặc biệt. Giá hợp lý, không gian rộng rãi, thân thiện với sinh viên. Mở từ mười giờ sáng đến mười một giờ đêm mỗi ngày. Rủ bạn bè đến cùng!",
                    TtsScriptEn = "Welcome to Chilli Hotpot and BBQ — an all-you-can-eat buffet in the heart of Vinh Khanh! Choose from premium US beef, fresh shrimp, crab, squid, and seasonal vegetables to grill and dip in broth. The signature sour-spicy Thai hotpot, simmered all day, is the star of every visit. Student-friendly prices, spacious seating. Open ten in the morning until eleven at night, seven days a week. Bring your crew!",
                    TtsScriptZh = "欢迎来到辣椒火锅烤肉——永庆街物超所值的自助烤涮体验！自由挑选优质美国牛肉、新鲜海鲜和各色蔬菜，自助烤涮随心所欲。全天慢火熬制的酸辣泰式火锅是每次必点亮点。价格亲民，空间宽敞，深受学生喜爱。每天上午十点至晚上十一点，欢迎携友同来！"
                });
                await Database.SaveRestaurantAsync(new Restaurant
                {
                    Name = "Thế Giới Bò",
                    Description = "Vương quốc các món bò, từ truyền thống đến sáng tạo hiện đại",
                    Category = "Quán ăn",
                    Latitude = 10.764267370582093,
                    Longitude = 106.70118183588556,
                    Address = "Vĩnh Khánh, Phường 8, Quận 4",
                    Rating = 4.5,
                    OpenHours = "10:00 - 22:00",
                    ImageUrl = "https://images.unsplash.com/photo-1544025162-d76538b2a621?auto=format&fit=crop&w=600&q=80",
                    TtsScript = "Bạn đã đến Thế Giới Bò — vương quốc của tín đồ thịt bò tại Vĩnh Khánh! Gần ba mươi món bò từ truyền thống Việt đến sáng tạo Tây phương. Nổi bật là bò tái chanh lá quế, bò nhúng giấm sôi sùng sục và bò lúc lắc tiêu đen với khoai tây chiên. Nguyên liệu nhập nguồn uy tín, đảm bảo sạch tươi. Không gian industrial hiện đại, ánh đèn warm tone. Mở từ mười giờ sáng đến mười giờ tối!",
                    TtsScriptEn = "You have arrived at The World of Beef — the ultimate beef kingdom on Vinh Khanh street! Nearly thirty preparations spanning Vietnamese tradition to Western fusion. Standouts include rare beef with lime and basil, bubbling vinegar fondue, and irresistible black pepper stir-fry with crispy fries. All beef sourced from trusted suppliers. Modern industrial decor, warm inviting lighting. Open ten in the morning until ten at night!",
                    TtsScriptZh = "您已踏入牛肉世界——永庆街牛肉爱好者的终极王国！近三十种料理从越式传统到西式创新应有尽有。亮点包括柠檬香草腌牛肉、醋涮牛肉火锅和黑椒炒牛肉配薯条。食材来自信誉供应商，品质有保障。工业风装潢，灯光温馨。每天上午十点至晚上十点营业！"
                });
                await Database.SaveRestaurantAsync(new Restaurant
                {
                    Name = "Cơm Cháy Kho Quet",
                    Description = "Cơm cháy giòn rụm từ nồi đất, kho quẹt thịt ba chỉ đậm đà Nam Bộ",
                    Category = "Quán ăn",
                    Latitude = 10.760625291110975,
                    Longitude = 106.70371667475501,
                    Address = "Vĩnh Khánh, Phường 8, Quận 4",
                    Rating = 4.4,
                    OpenHours = "10:00 - 21:00",
                    ImageUrl = "https://images.unsplash.com/photo-1516684669134-de6f7a3d4780?auto=format&fit=crop&w=600&q=80",
                    TtsScript = "Tiếng giòn rụm và mùi khói than — dấu hiệu bạn đang đến Cơm Cháy Kho Quẹt! Cơm cháy giòn tan từ đáy nồi đất nung, ăn kèm kho quẹt thịt ba chỉ và tép bạc đậm đà. Sự kết hợp cơm cháy giòn và kho quẹt ngọt mặn tạo hương vị mộc mạc khó quên. Còn có trứng ốp la, tôm kho tàu và canh chua cá kèo Nam Bộ. Mở từ mười giờ sáng đến chín giờ tối. Ghé ngay kẻo hết!",
                    TtsScriptEn = "That crackling sound and charcoal aroma mean you are near Com Chay Kho Quet! Crispy rice scraped from clay pot bottoms, served with thick pork belly and shrimp braising sauce. The contrast of crunchy rice with savory-sweet sauce creates a humble yet unforgettable flavor. Also serving fried eggs, caramelized braised shrimp, and Southern Vietnamese sour catfish soup. Open ten in the morning until nine at night. Come before the crispy rice runs out!",
                    TtsScriptZh = "酥脆声和炭火烟香——您正在靠近锅巴蘸酱！从陶土锅底精心刮下的酥脆锅巴，搭配五花肉银虾浓稠蘸酱。酥脆锅巴与咸甜蘸酱的完美对比，朴实而令人难忘。另有荷包蛋、焦糖焖虾和南越酸鱼汤。上午十点至晚上九点营业，锅巴卖完即止！"
                });
                await Database.SaveRestaurantAsync(new Restaurant
                {
                    Name = "Bò Lá Lốt Cô Út",
                    Description = "Bò lá lốt nướng than hoa thơm ngon, gia truyền hơn 15 năm",
                    Category = "Quán ăn",
                    Latitude = 10.761278781423528,
                    Longitude = 106.70529381362458,
                    Address = "Vĩnh Khánh, Phường 8, Quận 4",
                    Rating = 4.7,
                    OpenHours = "15:00 - 23:00",
                    ImageUrl = "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?auto=format&fit=crop&w=600&q=80",
                    TtsScript = "Hương thơm nồng nàn trong không khí — đó là mùi bò lá lốt nướng than của Cô Út! Hơn mười lăm năm, Cô Út nhóm bếp than mỗi ngày tạo ra những cuộn bò lá lốt vàng ươm thơm phức. Thịt bò xay pha sả tiêu tỏi và mắm ruốc, cuộn lá lốt tươi nướng đỏ than. Chấm nước mắm chua ngọt gừng hoặc muối tiêu chanh — mỗi miếng đều tuyệt vời. Còn có nem nướng sả và bún thịt nướng. Mở từ ba giờ chiều đến mười một giờ đêm!",
                    TtsScriptEn = "That seductive aroma in the evening air is Auntie Ut's famous lolot leaf beef rolls! For over fifteen years she lights the charcoal grill daily, crafting golden fragrant beef rolls nobody stops at just one. Ground beef seasoned with lemongrass, pepper, garlic and shrimp paste, wrapped in fresh lolot leaves, grilled over glowing charcoal. Dip in ginger fish sauce or salt-pepper-lime. Also serving lemongrass sausage and grilled pork noodles. Open three until eleven at night!",
                    TtsScriptZh = "空气中弥漫着迷人香气——那是幺姑著名蒌叶烤牛肉卷的气息！十五年来每天点燃炭炉，精心制作金黄牛肉卷，令人无法只吃一串。牛肉末调入香茅、胡椒、大蒜和虾酱，紧裹新鲜蒌叶，炭火烤制。蘸姜汁鱼露或椒盐柠檬汁，每口都是享受。另有香茅烤肠和烤肉米粉。下午三点至晚上十一点营业！"
                });
                await Database.SaveRestaurantAsync(new Restaurant
                {
                    Name = "Bún Thịt Nướng Cô Nga",
                    Description = "Bún thịt nướng sả mật ong đặc biệt, gia truyền hơn 20 năm",
                    Category = "Quán ăn",
                    Latitude = 10.760883450920542,
                    Longitude = 106.70674182239293,
                    Address = "Vĩnh Khánh, Phường 8, Quận 4",
                    Rating = 4.5,
                    OpenHours = "06:00 - 20:00",
                    ImageUrl = "https://images.unsplash.com/photo-1565299507177-b0ac66763828?auto=format&fit=crop&w=600&q=80",
                    TtsScript = "Bạn đang đứng trước Bún Thịt Nướng Cô Nga — tiệm bún yêu thích Vĩnh Khánh hơn hai mươi năm! Cô Nga ướp thịt bằng sả mật ong ngũ vị hương và nước cốt dừa, nướng than đến vàng thơm. Tô bún hoàn hảo: bún trắng mềm, thịt nướng thái lát, chả giò giòn và nước mắm chua ngọt. Giá bình dân, phục vụ nhanh và luôn tươi cười. Mở từ sáu giờ sáng đến tám giờ tối. Đến sớm thưởng thức bữa sáng hoàn hảo!",
                    TtsScriptEn = "You are at Ms. Nga's Grilled Pork Noodles — Vinh Khanh's beloved bun thit nuong stall for over twenty years! Ms. Nga marinates pork daily with lemongrass, honey, five-spice and coconut milk, grilled over charcoal until golden. A perfect bowl: soft rice noodles, sliced grilled pork, crispy spring rolls, fresh herbs, and sweet-sour fish sauce. Affordable, fast service, warm smiles. Open six in the morning until eight at night. Come early for the best breakfast on Vinh Khanh!",
                    TtsScriptZh = "您正站在阿娥烤肉米粉前——永庆街深受喜爱超过二十年的米粉摊！阿娥每天用香茅、蜂蜜、五香粉和椰奶腌制猪肉，炭火烤至金黄焦香。完美一碗：柔软米粉、薄切烤肉、酥脆炸春卷和酸甜鱼露。价格亲民，服务快捷，笑容温暖。早上六点至晚上八点营业，早来享用永庆最美早餐！"
                });
            }
        }
    }
}
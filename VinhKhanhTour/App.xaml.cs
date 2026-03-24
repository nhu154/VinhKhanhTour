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

            // Seed data đồng bộ trước khi mở MainPage
            Task.Run(async () => await InitializeSampleData()).GetAwaiter().GetResult();

            MainPage = new NavigationPage(new LoginPage());
        }

        private async Task InitializeSampleData()
        {
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
                    TtsScript = "Chào mừng bạn đến với Ốc Oanh - linh hồn ẩm thực của phố Vĩnh Khánh. Ngay lúc này, hương thơm nức mũi của món Ốc hương nướng tiêu xanh và Ốc hấp sả trứ danh chắc chắn sẽ đánh thức mọi giác quan của bạn. Hãy dừng chân và trải nghiệm tinh hoa ẩm thực đường phố nơi đây nhé.",
                    TtsScriptEn = "Welcome to Oc Oanh, the culinary soul of Vinh Khanh street. Right now, the irresistible aroma of our signature green pepper roasted snails and lemongrass steamed snails will surely awaken all your senses. Take a moment to stop by and experience the vibrant essence of authentic street food.",
                    TtsScriptZh = "欢迎来到‘欧莺海鲜’，这里是永庆街的灵魂美食地标。此刻，招牌青胡椒烤螺和香茅蒸螺的迷人香气定能唤醒您的所有感官。请停下脚步，尽情体验这里最地道的街头美食精髓吧。"
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
                    TtsScript = "Bạn đang đến gần quán Ốc Sáu Nở. Đây là thiên đường hải sản tươi sống với thực đơn vô cùng phong phú. Giữa nhịp sống hối hả, một đĩa ốc đậm đà nêm nếm hoàn hảo cùng ly bia mát lạnh tại đây sẽ là điểm nhấn tuyệt vời cho buổi tối của bạn.",
                    TtsScriptEn = "You are approaching Sau No Snail Restaurant, a true paradise of fresh seafood with an incredibly diverse menu. Amidst the city's hustle, a perfectly seasoned plate of snails paired with a cold beer here will be the absolute highlight of your evening.",
                    TtsScriptZh = "您正在靠近‘六绽海鲜’。这是一个拥有丰富菜单的新鲜海鲜天堂。在喧嚣的城市中，一盘调味完美的美味海鲜，配上一杯冰镇啤酒，绝对是您今晚最棒的享受。"
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
                    TtsScript = "Phía trước là Ốc Thảo. Không ồn ào nhưng lại đặc biệt níu chân thực khách bởi bí quyết biến tấu gia vị độc đáo trong từng chảo ốc. Nước xốt béo ngậy chấm cùng bánh mì giòn tan tại đây là một trải nghiệm ẩm thực bạn không thể bỏ lỡ.",
                    TtsScriptEn = "Just ahead is Oc Thao. Though not overwhelming, it captivates food lovers with its unique spice blends and masterful preparations. Their rich, creamy sauce served with crispy baguette is a culinary experience you simply cannot miss.",
                    TtsScriptZh = "前方是‘草海鲜’。虽然不喧哗，但凭借极其独特的海鲜烹调秘方吸引了无数食客。浓郁的酱汁搭配酥脆的法式长棍面包，是您绝不能错过的美食体验。"
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
                    OpenHours = "10:00 - 22:00",
                    ImageUrl = "langquan.jpeg",
                    TtsScript = "Bạn vừa bước vào không gian của Lãng Quán. Đúng như tên gọi, quán mang đến một bầu không khí trẻ trung, phóng khoáng và cực kỳ thư giãn. Nơi đây là điểm hẹn lý tưởng để bạn bè cùng nhau lai rai và kể những câu chuyện đời thường.",
                    TtsScriptEn = "You've just entered the atmosphere of Lang Quan. True to its name, this place offers a youthful, open, and incredibly relaxing vibe. It is the perfect gathering spot for friends to unwind, share food, and tell everyday stories.",
                    TtsScriptZh = "您刚进入‘浪漫小馆’的氛围中。正如其名，这里提供了一种年轻、奔放且极度放松的环境。这里是朋友们相聚、品尝美食和分享日常故事的完美聚会场所。"
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
                    TtsScript = "Dừng chân lại một chút nào, Ớt Xiêm Quán đang ở ngay trước mắt bạn! Điểm nhấn của quán là những món ăn mang đậm vị cay nồng xé lưỡi, kích thích vị giác tột độ. Nếu bạn là một tín đồ của ẩm thực cay nóng, đây chính xác là thánh địa dành cho bạn.",
                    TtsScriptEn = "Hold on a second, Ot Xiem Quan is right in front of you! The highlight here is the intensely spicy and flavorful dishes designed to perfectly excite your taste buds. If you are a true lover of fiery cuisine, this is absolutely your sanctuary.",
                    TtsScriptZh = "稍作停留，‘朝天椒餐厅’就在您眼前！这里的最大亮点是极其辛辣入味的菜肴，能极大限度地刺激您的味蕾。如果您是辛辣美食的忠实粉丝，这里绝对是您的圣地。"
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
                    TtsScript = "Mùi mắm cá linh đặc trưng từ Bún Cá Châu Đốc Dì Tư chắc hẳn đã lan tỏa đến bạn. Lấy cảm hứng từ tinh hoa ẩm thực miền Tây sông nước, một tô bún cá nóng hổi với nước lèo vàng ươm, ngọt thanh sẽ làm ấm lòng bất kỳ ai.",
                    TtsScriptEn = "The distinctive aroma from Auntie Tu's Chau Doc Fish Noodle Soup must have reached you by now. Inspired by the culinary essence of the Mekong Delta, a steaming bowl of fish noodles with its golden, sweet-savory broth will warm anyone's heart.",
                    TtsScriptZh = "来自四姨‘朱笃鱼头米粉’的独特香气想必已经飘到您的身边了。灵感来源于湄公河三角洲的美食精髓，一碗热腾腾的鱼肉米粉，配上金黄清甜的汤底，定能温暖您的心。"
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
                    TtsScript = "Chào bạn đến với Chilli Lẩu Nướng. Một bữa tiệc buffet thịt nướng xèo xèo trên lửa đỏ cùng nồi lẩu nghi ngút khói đang chờ đón bạn. Với mức giá cực kỳ sinh viên nhưng chất lượng lại vô cùng trọn vẹn, đây là lựa chọn tuyệt vời cho nhóm bạn đông người.",
                    TtsScriptEn = "Welcome to Chilli Hotpot and BBQ. A sizzling barbecue feast over red flames and a steaming hotpot are waiting for you. With an incredibly student-friendly price but uncompromising quality, this is the ultimate choice for large groups of friends.",
                    TtsScriptZh = "欢迎来到‘辣椒火锅烤肉’。红火上的咝咝烤肉盛宴和热腾腾的火锅正等着您。这里价格极其亲民，但质量却毫不妥协，绝对是大型朋友聚会的绝佳选择。"
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
                    TtsScript = "Xin chào, bạn đã đặt chân đến Thế Giới Bò! Từ bò né xèo xèo đến bò nướng tảng mọng nước, mọi món ăn đều được chế biến từ những thớ thịt tươi ngon nhất. Hãy chuẩn bị sẵn sàng để trải nghiệm một bữa tiệc đạm đà và đẳng cấp.",
                    TtsScriptEn = "Hello there, you have set foot in The World of Beef! From sizzling beefsteak to juicy grilled slabs, every dish is crafted from the absolute freshest cuts of meat. Get ready to experience a rich and highly premium meat feast.",
                    TtsScriptZh = "您好，您已踏入‘牛肉世界’！从铁板牛肉到鲜嫩多汁的烤肉排，每一道菜都选用最优质的新鲜牛肉烹制。请准备好体验一场浓郁且高端的肉类盛宴。"
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
                    TtsScript = "Âm thanh rôm rốp giòn vang của Cơm Cháy Kho Quẹt đang thu hút sự chú ý của bạn. Món ăn dân dã, gợi nhớ ký ức tuổi thơ này kết hợp cùng thố kho quẹt kẹo kẹo, mặn ngọt cay cay, đảm bảo sẽ khiến bạn gắp không ngừng đũa.",
                    TtsScriptEn = "The crunchy, crackling sound of Com Chay Kho Quet is catching your attention. This rustic dish, evoking sweet childhood memories, is paired with a caramelized, sweet, savory, and spicy clay pot dip that guarantees you won't be able to stop eating.",
                    TtsScriptZh = "‘锅巴蘸酱’酥脆诱人的声音正在吸引您的注意。这道唤起童年记忆的乡村美食，搭配着装在砂锅里的咸甜香辣的浓郁秘制蘸酱，保证让您回味无穷，停不下筷子。"
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
                    TtsScript = "Hương thơm nức nở quyến rũ này đi ra từ quán Bò Lá Lốt Cô Út. Những cuốn thịt bò tẩm ướp đậm đà, cuộn gọn trong lá lốt xanh cởi mở cháy xèo xèo trên bếp than hồng. Cuốn cùng bánh tráng, rau rừng và mắm nêm, quả là một mỹ vị nhân gian.",
                    TtsScriptEn = "This incredibly charming aroma is wafting from Auntie Ut's Grilled Beef in Lolot Leaves. Intensely marinated beef wrapped in fresh green leaves, sizzling perfectly on hot charcoal. Rolled with rice paper, wild herbs, and fermented anchovy sauce, it is truly a heavenly delicacy.",
                    TtsScriptZh = "这迷人且极其浓郁的香气正是来自‘幺姑蒌叶烤牛肉’。腌制入味的牛肉被新鲜的蒌叶紧紧包裹，在炭火上烤得咝咝作响。用米纸卷上野菜，蘸满特制鱼露，简直是人间美味。"
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
                    TtsScript = "Bạn đang đứng trước Bún Thịt Nướng Cô Nga. Một tô bún mát lạnh kết hợp cùng thịt nướng cháy cạnh thơm lừng, chả giò giòn tan và nước mắm chua ngọt pha chế theo công thức gia truyền. Một món ăn thanh mát nhưng lưu luyến vô cùng.",
                    TtsScriptEn = "You are standing in front of Ms. Nga's Grilled Pork Noodles. A refreshing bowl of rice noodles combined with perfectly charred, fragrant grilled pork, crispy spring rolls, and a sweet-and-sour fish sauce made from an heirloom recipe. A light yet unforgettably enduring dish.",
                    TtsScriptZh = "您现在正站在‘阿娥烤肉米粉’门前。一碗清爽的米粉，搭配烤得焦香四溢的猪肉、酥脆的春卷，以及用祖传秘方调制的酸甜鱼露。这是一道清淡却令人久久难以忘怀的绝妙佳肴。"
                });
            }
        }
    }
}
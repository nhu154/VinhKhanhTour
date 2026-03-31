-- MySQL dump 10.13  Distrib 8.0.45, for Win64 (x86_64)
--
-- Host: localhost    Database: vinhkhanhtour_db
-- ------------------------------------------------------
-- Server version	8.0.45

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `analytics`
--

DROP TABLE IF EXISTS `analytics`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `analytics` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `RestaurantId` int DEFAULT NULL,
  `EventType` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Timestamp` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  KEY `RestaurantId` (`RestaurantId`),
  CONSTRAINT `analytics_ibfk_1` FOREIGN KEY (`RestaurantId`) REFERENCES `restaurants` (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=37 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `analytics`
--

LOCK TABLES `analytics` WRITE;
/*!40000 ALTER TABLE `analytics` DISABLE KEYS */;
INSERT INTO `analytics` VALUES (7,6,'poi_visit','2026-03-29 20:32:53'),(8,6,'geofence_enter','2026-03-29 20:32:58'),(9,6,'poi_visit','2026-03-29 20:37:54'),(10,6,'geofence_enter','2026-03-29 20:38:09'),(22,1,'geofence_enter','2026-03-31 22:54:22'),(23,2,'click','2026-03-31 22:54:22'),(24,3,'geofence_enter','2026-03-31 22:54:22'),(25,1,'geofence_enter','2026-03-31 22:54:30'),(26,2,'click','2026-03-31 22:54:30'),(27,3,'geofence_enter','2026-03-31 22:54:30'),(28,1,'geofence_enter','2026-03-31 22:54:41'),(29,2,'click','2026-03-31 22:54:41'),(30,3,'geofence_enter','2026-03-31 22:54:41'),(31,1,'geofence_enter','2026-03-31 23:00:15'),(32,2,'click','2026-03-31 23:00:15'),(33,3,'geofence_enter','2026-03-31 23:00:15'),(34,1,'geofence_enter','2026-03-31 23:02:20'),(35,2,'click','2026-03-31 23:02:20'),(36,3,'geofence_enter','2026-03-31 23:02:20');
/*!40000 ALTER TABLE `analytics` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `restaurants`
--

DROP TABLE IF EXISTS `restaurants`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `restaurants` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(200) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Description` text COLLATE utf8mb4_unicode_ci,
  `Category` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Latitude` double DEFAULT NULL,
  `Longitude` double DEFAULT NULL,
  `Address` varchar(300) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `ImageUrl` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Rating` double DEFAULT '0',
  `OpenHours` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `IsFavorite` tinyint(1) DEFAULT '0',
  `AudioFile` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `TtsScript` text COLLATE utf8mb4_unicode_ci,
  `TtsScriptEn` text COLLATE utf8mb4_unicode_ci,
  `TtsScriptZh` text COLLATE utf8mb4_unicode_ci,
  `Radius` int DEFAULT '50',
  `IsAdsPopup` tinyint(1) DEFAULT '0',
  `AudioUrl` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT '',
  `Translations` text COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `restaurants`
--

LOCK TABLES `restaurants` WRITE;
/*!40000 ALTER TABLE `restaurants` DISABLE KEYS */;
INSERT INTO `restaurants` VALUES (1,'Ốc Oanh','Chào mừng bạn đến với Ốc Oanh - linh hồn ẩm thực của phố Vĩnh Khánh.','Quán ăn',10.7608259,106.7033137,'Vĩnh Khánh, Phường 8, Quận 4','oc_oanh.jpg',4.3,'16:00 - 23:00',0,'','Chào mừng bạn đến Ốc Oanh — linh hồn phố ẩm thực Vĩnh Khánh hơn mười năm qua! Quán nổi tiếng với ốc hương nướng tiêu xanh, ốc mỡ hấp sả gừng và ốc len xào dừa béo ngậy. Mỗi con ốc tươi chọn lọc từ miền Tây mỗi sáng, đảm bảo chất lượng tuyệt đối. Thực đơn hơn hai mươi món, nước chấm bí truyền độc quyền. Mở cửa từ bốn giờ chiều đến mười một giờ đêm. Đặt chỗ sớm kẻo hết!','Welcome to Oc Oanh — the soul of Vinh Khanh food street for over a decade! Famous for pepper-grilled rock snails, lemongrass-steamed butter snails, and rich coconut-sauteed mud creepers, all hand-picked fresh every morning from the Mekong Delta. Our menu features twenty shellfish preparations with a secret house dipping sauce. Open four in the afternoon until eleven at night. Book early — always packed!','欢迎来到\'欧莺\'——永庆街美食灵魂超过十年！以胡椒烤香螺、香茅蒸蜗牛和椰汁炒泥螺著称，每日清晨从湄公河精选食材。菜单超过二十种贝类料理，搭配秘制蘸酱。下午四点至晚上十一点营业，请提前预订！',50,0,'','{}'),(2,'Ốc Sáu Nở','Ốc tươi ngon đa dạng','Quán ăn',10.761090779311022,106.70289908345818,'Vĩnh Khánh, Phường 8, Quận 4','oc_sauno.jpg',4.4,'15:00 - 23:00',0,'','Bạn đang đến Ốc Sáu Nở — biểu tượng hải sản Sài Gòn nhiều thập kỷ! Thử ngay ốc bươu nhồi thịt hấp gừng, ốc nhảy xào me chua ngọt và mâm ốc tổng hợp năm loại đặc biệt. Nước chấm bí truyền gia truyền là linh hồn của quán. Ngồi vỉa hè Vĩnh Khánh, nhâm nhi ly mát cùng mâm ốc bốc khói — trải nghiệm Sài Gòn đích thực. Mở từ ba giờ chiều đến mười một giờ đêm!','Approaching Oc Sau No — a Saigon seafood icon for decades! Must-try: ginger-steamed stuffed snails, tamarind-glazed jumping snails, and the five-variety shellfish platter. Their secret dipping sauce keeps regulars coming back. Sitting on the lively Vinh Khanh sidewalk with an ice-cold drink alongside steaming shellfish — this is authentic Saigon street food. Open three in the afternoon until eleven at night!','欢迎来到\'六绽海鲜\'——西贡数十年的海鲜传奇！必点：姜汁蒸肉馅田螺、酸甜罗望子炒蜗牛和五种贝类拼盘。秘制蘸酱代代相传，令老顾客念念不忘。坐在永庆街边，冷饮配热螺，尽享西贡街头真味。每天下午三点至晚上十一点营业！',50,0,'','{}'),(3,'Ốc Thảo','Ốc các loại chế biến đa dạng','Quán ăn',10.761758951046252,106.70235823553499,'Vĩnh Khánh, Phường 8, Quận 4','oc_thao.jpg',4.2,'16:00 - 22:30',0,'','Phía trước là Ốc Thảo — một trong những quán ốc lâu đời nhất Vĩnh Khánh! Điểm đặc biệt là gia vị độc đáo kết hợp truyền thống Nam Bộ và sáng tạo hiện đại. Thử ốc nướng muối ớt giòn tan, ốc sò hấp bia thơm nồng và ốc de nấu tiêu đen đậm đà. Chị Thảo luôn đích thân đứng bếp đảm bảo hương vị chuẩn nhất. Không khí thân thiện, giá hợp lý. Mở từ bốn giờ chiều đến mười giờ rưỡi tối!','Just ahead is Oc Thao — one of Vinh Khanh\'s oldest and most beloved shellfish spots! Known for blending traditional Southern spices with modern creativity. Try salt-chili grilled snails, beer-steamed clams, and savory black pepper mud snails. Ms. Thao personally oversees every dish from the kitchen. Friendly atmosphere, affordable prices. Open four in the afternoon until half past ten at night!','前方是\'草海鲜\'——永庆街历史最悠久的贝壳餐厅之一！以传统南越香料与现代创意的独特融合闻名。必尝盐辣烤螺、啤酒蒸蛤蜊和黑椒焖泥螺。草姐每天亲自掌厨，品质始终如一。氛围友好，价格亲民。下午四点至晚上十点半营业！',50,0,'','{}'),(4,'Lãng Quán','Quán ăn phong cách trẻ trung','Quán ăn',10.761281731910726,106.70537328006456,'Vĩnh Khánh, Phường 8, Quận 4','langquan.jpeg',4.5,'17:00 - 23:00',0,'','Chào bạn đến Lãng Quán — không gian ẩm thực trẻ trung sáng tạo giữa phố Vĩnh Khánh! Không chỉ là quán ăn — đây là nơi bạn bè hội tụ và tạo kỷ niệm. Thực đơn đa dạng từ ăn vặt Sài Gòn đến lẩu riêu cua, bò lúc lắc tiêu xanh, gà nướng mật ong. Cuối tuần có âm nhạc live tạo không gian lãng mạn. Phục vụ từ năm giờ chiều đến mười một giờ đêm. Hẹn gặp bạn tại Lãng Quán!','Welcome to Lang Quan — a vibrant creative dining space in the heart of Vinh Khanh! More than a restaurant — a gathering place for friends and memories. The menu spans Saigon street snacks to crab roe hotpot, green pepper beef, and honey-glazed grilled chicken. Weekend evenings feature live acoustic music for a romantic atmosphere. Open five in the afternoon until eleven at night. See you at Lang Quan!','欢迎来到\'浪漫小馆\'——永庆街充满青春创意的餐饮空间！不只是餐厅，更是朋友聚会创造回忆之地。菜单从西贡小吃到蟹黄火锅、青椒牛肉、蜂蜜烤鸡应有尽有。周末有现场原声音乐，浪漫氛围满满。下午五点至晚上十一点营业，期待与您相聚！',50,0,'','{}'),(5,'Ớt Xiêm Quán','Đồ ăn cay nồng đậm đà','Quán ăn',10.761345472033696,106.70569016657214,'Vĩnh Khánh, Phường 8, Quận 4','otxiemquan.jpg',4.4,'11:00 - 21:00',0,'','Ớt Xiêm Quán đang hiện ra trước mắt bạn — thiên đường của những tín đồ vị cay kiểu Khmer Nam Bộ! Ớt xiêm xanh nhỏ nhưng cực cay là linh hồn mọi món. Đừng bỏ qua gà nướng mật ong giòn vàng, tôm rang muối ớt giòn rụm và lẩu thái chua cay đậm vị. Nhân viên sẵn sàng tư vấn mức cay phù hợp. Chuẩn bị khăn giấy và chinh phục ẩm thực cay nhất Vĩnh Khánh! Mở từ mười một giờ sáng đến chín giờ tối.','Ot Xiem Quan is right before you — paradise for lovers of bold Khmer-style fiery flavors! The small but ferocious Siam chili pepper defines every dish. Don\'t miss honey-glazed chili grilled chicken, crunchy salt-chili shrimp, and the sour-spicy Thai-style hotpot. Staff always help you find your ideal heat level. Grab your tissues and conquer the spiciest food on Vinh Khanh street! Open eleven in the morning until nine at night.','朝天椒餐厅就在眼前——辣味爱好者的天堂，以越南南部高棉风味为灵感！个头小却无比辣的暹罗朝天椒贯穿所有菜肴。必尝蜂蜜烤鸡、椒盐炸虾和酸辣泰式火锅。员工随时协助您选择合适辣度。备好纸巾，挑战永庆街最辣美食！上午十一点至晚上九点营业。',50,0,'','{}'),(6,'Bún Cá Châu Đốc - Dì Tư','Bún cá Châu Đốc chính gốc','Quán ăn',10.761060311531145,106.70668201075144,'Vĩnh Khánh, Phường 8, Quận 4','buncachaudoc.jpg',4.6,'06:00 - 14:00',0,'','Mùi mắm cá linh và ruốc Châu Đốc chính gốc đang lan tỏa từ bếp Dì Tư! Đây là địa chỉ hiếm tại Sài Gòn giữ nguyên hương vị bún cá An Giang. Nước lèo hầm cá lóc tươi cùng sả gừng tạo mùi thơm đặc trưng. Chan vào tô bún, thêm rau đắng, bắp chuối và cá chiên vàng — bữa sáng hoàn hảo! Mở từ sáu giờ sáng đến hai giờ chiều. Đến sớm kẻo hết!','The sweet aroma of linh fish paste and authentic Chau Doc shrimp paste drifts from Auntie Tu\'s kitchen! One of Saigon\'s rare spots preserving the original An Giang fish noodle flavors. The broth is slow-simmered with fresh snakehead fish, lemongrass and ginger. Ladled over soft rice noodles with bitter greens, banana blossom and crispy fish — the perfect breakfast! Open six in the morning until two in the afternoon. Come early before it sells out!','四姨厨房飘来正宗朱笃虾酱的甘美香气！西贡难得保留安江原味鱼粉的珍贵地址。汤底以新鲜乌鱼、香茅姜慢火熬制，香气独特无误。浇在米粉上，配苦菜、芭蕉花和炸鱼片——完美早餐！早上六点至下午两点营业，早来早得，卖完即止！',50,0,'','{}'),(7,'Chilli Lẩu Nướng Quán','Lẩu nướng buffet giá sinh viên','Quán ăn',10.760840551806485,106.70405082000606,'Vĩnh Khánh, Phường 8, Quận 4','chililaunuong.jpg',4.3,'10:00 - 23:00',0,'','Chào mừng đến Chilli Lẩu Nướng — buffet lẩu nướng tự chọn hấp dẫn giữa phố Vĩnh Khánh! Tự do chọn bò Mỹ thái mỏng, hải sản tươi và rau củ đa dạng để nướng và nhúng lẩu. Nồi lẩu thái chua cay hầm xương cả ngày là điểm nhấn đặc biệt. Giá hợp lý, không gian rộng rãi, thân thiện với sinh viên. Mở từ mười giờ sáng đến mười một giờ đêm mỗi ngày. Rủ bạn bè đến cùng!','Welcome to Chilli Hotpot and BBQ — an all-you-can-eat buffet in the heart of Vinh Khanh! Choose from premium US beef, fresh shrimp, crab, squid, and seasonal vegetables to grill and dip in broth. The signature sour-spicy Thai hotpot, simmered all day, is the star of every visit. Student-friendly prices, spacious seating. Open ten in the morning until eleven at night, seven days a week. Bring your crew!','欢迎来到辣椒火锅烤肉——永庆街物超所值的自助烤涮体验！自由挑选优质美国牛肉、新鲜海鲜和各色蔬菜，自助烤涮随心所欲。全天慢火熬制的酸辣泰式火锅是每次必点亮点。价格亲民，空间宽敞，深受学生喜爱。每天上午十点至晚上十一点，欢迎携友同来！',50,0,'','{}'),(8,'Thế Giới Bò','Các món bò đa dạng chất lượng','Quán ăn',10.764267370582093,106.70118183588556,'Vĩnh Khánh, Phường 8, Quận 4','thegioibo.jpg',4.5,'10:00 - 22:00',0,'','Bạn đã đến Thế Giới Bò — vương quốc của tín đồ thịt bò tại Vĩnh Khánh! Gần ba mươi món bò từ truyền thống Việt đến sáng tạo Tây phương. Nổi bật là bò tái chanh lá quế, bò nhúng giấm sôi sùng sục và bò lúc lắc tiêu đen với khoai tây chiên. Nguyên liệu nhập nguồn uy tín, đảm bảo sạch tươi. Không gian industrial hiện đại, ánh đèn warm tone. Mở từ mười giờ sáng đến mười giờ tối!','You\'ve arrived at The World of Beef — the ultimate beef kingdom on Vinh Khanh street! Nearly thirty preparations spanning Vietnamese tradition to Western fusion. Standouts include rare beef with lime and basil, bubbling vinegar fondue, and irresistible black pepper stir-fry with crispy fries. All beef sourced from trusted suppliers for guaranteed freshness. Modern industrial decor, warm inviting lighting. Open ten in the morning until ten at night!','您已踏入\'牛肉世界\'——永庆街牛肉爱好者的终极王国！近三十种料理从越式传统到西式创新应有尽有。亮点包括柠檬香草腌牛肉、醋涮牛肉火锅和黑椒炒牛肉配薯条。食材来自信誉供应商，品质有保障。工业风装潢，灯光温馨。每天上午十点至晚上十点营业！',50,0,'','{}'),(9,'Cơm Cháy Kho Quẹt','Cơm cháy giòn rụm kho quẹt đậm đà','Quán ăn',10.760625291110975,106.70371667475501,'Vĩnh Khánh, Phường 8, Quận 4','comchaykhoquet.jpg',4.4,'10:00 - 21:00',0,'','Tiếng giòn rụm và mùi khói than — dấu hiệu bạn đang đến Cơm Cháy Kho Quẹt! Cơm cháy giòn tan từ đáy nồi đất nung, ăn kèm kho quẹt thịt ba chỉ và tép bạc đậm đà. Sự kết hợp cơm cháy giòn và kho quẹt ngọt mặn tạo hương vị mộc mạc khó quên. Còn có trứng ốp la, tôm kho tàu và canh chua cá kèo Nam Bộ. Mở từ mười giờ sáng đến chín giờ tối. Ghé ngay kẻo hết!','That crackling sound and charcoal aroma mean you\'re near Com Chay Kho Quet! Crispy rice scraped from clay pot bottoms, served with thick pork belly and shrimp braising sauce. The contrast of crunchy rice with savory-sweet sauce creates a humble yet unforgettable flavor. Also serving fried eggs, caramelized braised shrimp, and Southern Vietnamese sour catfish soup. Open ten in the morning until nine at night. Come before the crispy rice runs out!','酥脆声和炭火烟香——您正在靠近\'锅巴蘸酱\'！从陶土锅底精心刮下的酥脆锅巴，搭配五花肉银虾浓稠蘸酱。酥脆锅巴与咸甜蘸酱的完美对比，朴实而令人难忘。另有荷包蛋、焦糖焖虾和南越酸鱼汤。上午十点至晚上九点营业，锅巴卖完即止！',50,0,'','{}'),(10,'Bò Lá Lốt Cô Út','Bò lá lốt nướng thơm ngon','Quán ăn',10.761278781423528,106.70529381362458,'Vĩnh Khánh, Phường 8, Quận 4','bolalotcout.jpg',4.7,'15:00 - 23:00',0,'','Hương thơm nồng nàn trong không khí — đó là mùi bò lá lốt nướng than của Cô Út! Hơn mười lăm năm, Cô Út nhóm bếp than mỗi ngày tạo ra những cuộn bò lá lốt vàng ươm thơm phức. Thịt bò xay pha sả tiêu tỏi và mắm ruốc, cuộn lá lốt tươi nướng đỏ than. Chấm nước mắm chua ngọt gừng hoặc muối tiêu chanh — mỗi miếng đều tuyệt vời. Còn có nem nướng sả và bún thịt nướng. Mở từ ba giờ chiều đến mười một giờ đêm!','That seductive aroma in the evening air is Auntie Ut\'s famous lolot leaf beef rolls! For over fifteen years she lights the charcoal grill daily, crafting golden fragrant beef rolls nobody stops at just one. Ground beef seasoned with lemongrass, pepper, garlic and shrimp paste, wrapped in fresh lolot leaves, grilled over glowing charcoal. Dip in ginger fish sauce or salt-pepper-lime — extraordinary every bite. Also serving lemongrass sausage and grilled pork noodles. Open three until eleven at night!','空气中弥漫着迷人香气——那是幺姑著名蒌叶烤牛肉卷的气息！十五年来每天点燃炭炉，精心制作金黄牛肉卷，令人无法只吃一串。牛肉末调入香茅、胡椒、大蒜和虾酱，紧裹新鲜蒌叶，炭火烤制。蘸姜汁鱼露或椒盐柠檬汁，每口都是享受。另有香茅烤肠和烤肉米粉。下午三点至晚上十一点营业！',50,0,'','{}'),(11,'Bún Thịt Nướng Cô Nga','lê đỗ tâm uyên lê đỗ tâm uyên lê đỗ tâm uyên lê đỗ tâm uyên lê đỗ tâm uyên lê đỗ tâm uyên lê đỗ tâm uyên lê đỗ tâm uyên lê đỗ tâm uyên lê đỗ tâm uyên','Quán ăn',10.7608835,106.7067418,'Vĩnh Khánh, Phường 8, Quận 4','bunthitnuongconga.jpg',4.5,'06:00 - 20:00',0,'','Bạn đang đứng trước Bún Thịt Nướng Cô Nga — tiệm bún yêu thích Vĩnh Khánh hơn hai mươi năm! Cô Nga ướp thịt bằng sả mật ong ngũ vị hương và nước cốt dừa, nướng than đến vàng thơm. Tô bún hoàn hảo: bún trắng mềm, thịt nướng thái lát, chả giò giòn và nước mắm chua ngọt. Giá bình dân, phục vụ nhanh và luôn tươi cười. Mở từ sáu giờ sáng đến tám giờ tối. Đến sớm thưởng thức bữa sáng hoàn hảo!','You\'re at Ms. Nga\'s Grilled Pork Noodles — Vinh Khanh\'s beloved bun thit nuong stall for over twenty years! Ms. Nga marinates pork daily with lemongrass, honey, five-spice and coconut milk, grilled over charcoal until golden. A perfect bowl: soft rice noodles, sliced grilled pork, crispy spring rolls, fresh herbs, and sweet-sour fish sauce. Affordable, fast service, warm smiles. Open six in the morning until eight at night. Come early for the best breakfast on Vinh Khanh!','您正站在阿娥烤肉米粉前——永庆街深受喜爱超过二十年的米粉摊！阿娥每天用香茅、蜂蜜、五香粉和椰奶腌制猪肉，炭火烤至金黄焦香。完美一碗：柔软米粉、薄切烤肉、酥脆炸春卷和酸甜鱼露。价格亲民，服务快捷，笑容温暖。早上六点至晚上八点营业，早来享用永庆最美早餐！',50,0,'','{}');
/*!40000 ALTER TABLE `restaurants` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tours`
--

DROP TABLE IF EXISTS `tours`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tours` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(200) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Description` text COLLATE utf8mb4_unicode_ci,
  `ImageUrl` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `IsActive` tinyint(1) DEFAULT '1',
  `Pois` text COLLATE utf8mb4_unicode_ci,
  `NameEn` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `NameZh` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `DescEn` text COLLATE utf8mb4_unicode_ci,
  `DescZh` text COLLATE utf8mb4_unicode_ci,
  `Duration` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Rating` double DEFAULT '4',
  `Emoji` varchar(10) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=30 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tours`
--

LOCK TABLES `tours` WRITE;
/*!40000 ALTER TABLE `tours` DISABLE KEYS */;
INSERT INTO `tours` VALUES (1,'Tour Ăn Ốc','3 quán ốc ngon nổi tiếng nhất phố Vĩnh Khánh','https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=600&q=80',1,'[1,2,3]','Shellfish Tour','贝类美食游','3 famous shellfish restaurants','3家知名贝类餐厅','45 phút',4.4,'?'),(2,'Tour Ăn Nướng','Lẩu nướng, bò lá lốt - những món nướng đỉnh nhất Vĩnh Khánh','https://images.unsplash.com/photo-1544025162-81111421550a?auto=format&fit=crop&w=600&q=80',1,'[7,8,10]','BBQ Tour','烧烤美食游','BBQ hotpot, grilled beef in pepper leaves','烧烤，胡椒叶牛肉','60 phút',4.5,'?'),(3,'Tour Ăn Vặt','Cơm cháy kho quẹt, bún thịt nướng - ăn vặt đặc trưng Sài Gòn','https://images.unsplash.com/photo-1574484284002-952d92456975?auto=format&fit=crop&w=600&q=80',1,'[9,11]','Snack Tour','小吃游','Crispy rice with dipping sauce, grilled pork noodles','锅巴蘸酱，烤猪肉米线','40 phút',4.3,'?'),(4,'Tour Đặc Sản','Bún cá Châu Đốc, Lãng Quán, Ớt Xiêm - đặc sản không thể bỏ qua','https://images.unsplash.com/photo-1555939594-58d7cb561ad1?auto=format&fit=crop&w=600&q=80',1,'[4,5,6]','Specialty Tour','特产美食游','Chau Doc fish noodle soup and local specialties','朱笃鱼米线及当地特色美食','50 phút',4.6,'⭐');
/*!40000 ALTER TABLE `tours` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `users` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Username` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Password` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `FullName` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Role` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT 'user',
  `CreatedAt` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `Username` (`Username`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES (1,'admin','admin123','Quản trị viên','admin','2026-03-31 23:02:46'),(2,'nhu1542005','nhu1542005','nhu1542005','user','2026-03-31 23:02:46');
/*!40000 ALTER TABLE `users` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `visit_history`
--

DROP TABLE IF EXISTS `visit_history`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `visit_history` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `RestaurantId` int DEFAULT NULL,
  `RestaurantName` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `VisitedAt` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  KEY `RestaurantId` (`RestaurantId`),
  CONSTRAINT `visit_history_ibfk_1` FOREIGN KEY (`RestaurantId`) REFERENCES `restaurants` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `visit_history`
--

LOCK TABLES `visit_history` WRITE;
/*!40000 ALTER TABLE `visit_history` DISABLE KEYS */;
/*!40000 ALTER TABLE `visit_history` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-03-31 23:39:11

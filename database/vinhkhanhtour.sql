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
-- Table structure for table `admin_logs`
--

DROP TABLE IF EXISTS `admin_logs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `admin_logs` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` int DEFAULT NULL,
  `UserName` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Action` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Target` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Details` text COLLATE utf8mb4_unicode_ci,
  `Timestamp` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=116 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `admin_logs`
--

LOCK TABLES `admin_logs` WRITE;
/*!40000 ALTER TABLE `admin_logs` DISABLE KEYS */;
INSERT INTO `admin_logs` VALUES (86,1,'Quản trị viên','LOGIN','CMS',NULL,'2026-04-21 22:14:22'),(87,1,'Quản trị viên','LOGIN','CMS',NULL,'2026-04-21 22:20:41'),(88,1,'Quản trị viên','LOGIN','CMS',NULL,'2026-04-21 22:47:44'),(89,1,'Quản trị viên','LOGIN','CMS',NULL,'2026-04-21 22:55:34'),(90,1,'Quản trị viên','LOGIN','CMS',NULL,'2026-04-21 23:04:27'),(91,1,'Quản trị viên','LOGIN','CMS',NULL,'2026-04-21 23:08:32'),(92,1,'Quản trị viên','LOGIN','CMS',NULL,'2026-04-21 23:16:44'),(93,1,'Quản trị viên','LOGIN','CMS',NULL,'2026-04-21 23:33:34'),(94,12,'trinh tam nhu','LOGIN','CMS',NULL,'2026-04-21 23:40:21'),(95,12,'trinh tam nhu','LOGIN','CMS',NULL,'2026-04-21 23:55:19'),(96,1,'Quản trị viên','LOGIN','CMS',NULL,'2026-04-22 00:05:07'),(97,13,'nhu','LOGIN','CMS',NULL,'2026-04-22 01:01:39'),(98,13,'nhu','LOGIN','CMS',NULL,'2026-04-22 01:09:30'),(99,12,'trinh tam nhu','LOGIN','CMS',NULL,'2026-04-22 01:15:34'),(100,13,'nhu','LOGIN','CMS',NULL,'2026-04-22 01:34:23'),(101,13,'nhu','LOGIN','CMS',NULL,'2026-04-22 02:34:28'),(102,1,'admin','CREATE_TOUR','nhu',NULL,'2026-04-22 02:44:26'),(103,1,'admin','UPDATE_TOUR','nhu',NULL,'2026-04-22 02:44:39'),(104,1,'admin','DELETE_TOUR','nhu',NULL,'2026-04-22 02:46:40'),(105,3,'Chủ quán Ốc Oanh','LOGIN','CMS',NULL,'2026-04-22 02:47:55'),(106,1,'Quản trị viên','LOGIN','CMS',NULL,'2026-04-22 03:04:15'),(107,1,'Quản trị viên','LOGIN','CMS',NULL,'2026-04-22 03:26:54'),(108,1,'Quản trị viên','LOGIN','CMS',NULL,'2026-04-22 03:44:17'),(109,1,'Quản trị viên','LOGIN','CMS',NULL,'2026-04-22 03:48:19'),(110,1,'Quản trị viên','LOGIN','CMS',NULL,'2026-04-22 05:19:47'),(111,13,'nhu','LOGIN','CMS',NULL,'2026-04-22 05:45:39'),(112,1,'Quản trị viên','LOGIN','CMS',NULL,'2026-04-22 07:06:21'),(113,1,'Quản trị viên','LOGIN','CMS',NULL,'2026-04-22 07:08:51'),(114,13,'nhu','LOGIN','CMS',NULL,'2026-04-22 07:11:55'),(115,1,'Quản trị viên','LOGIN','CMS',NULL,'2026-04-22 07:19:03');
/*!40000 ALTER TABLE `admin_logs` ENABLE KEYS */;
UNLOCK TABLES;

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
  `Value` double NOT NULL DEFAULT '0' COMMENT 'Duration tính bằng giây khi EventType = audio_*',
  `Lat` double NOT NULL DEFAULT '0' COMMENT 'Vĩ độ GPS tại thời điểm ghi event (0 = không có)',
  `Lng` double NOT NULL DEFAULT '0' COMMENT 'Kinh độ GPS tại thời điểm ghi event (0 = không có)',
  `Username` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `RestaurantId` (`RestaurantId`),
  KEY `idx_analytics_latlng` (`Lat`,`Lng`),
  KEY `idx_analytics_eventtype` (`EventType`),
  CONSTRAINT `analytics_ibfk_1` FOREIGN KEY (`RestaurantId`) REFERENCES `restaurants` (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=3119 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `analytics`
--

LOCK TABLES `analytics` WRITE;
/*!40000 ALTER TABLE `analytics` DISABLE KEYS */;
INSERT INTO `analytics` VALUES (2826,NULL,'app_login','2026-04-22 02:13:45',0,0,0,NULL),(2827,NULL,'app_login','2026-04-22 02:27:23',0,0,0,NULL),(2828,1,'click','2026-04-22 02:28:00',0,0,0,NULL),(2829,NULL,'app_login','2026-04-22 02:28:55',0,0,0,NULL),(2830,NULL,'gps_point','2026-04-22 02:30:27',0,10.7834928,106.6285314,NULL),(2833,NULL,'gps_point','2026-04-22 02:30:55',0,10.7835063,106.6285454,NULL),(2834,1,'click','2026-04-22 02:33:03',0,0,0,NULL),(2835,1,'click','2026-04-22 02:33:09',0,0,0,NULL),(2836,2,'click','2026-04-22 02:33:17',0,0,0,NULL),(2837,NULL,'app_login','2026-04-22 02:34:29',0,0,0,NULL),(2838,1,'click','2026-04-22 02:35:05',0,0,0,NULL),(2839,NULL,'gps_point','2026-04-22 02:35:11',0,10.7834986,106.6285385,NULL),(2840,4,'click','2026-04-22 02:36:04',0,0,0,NULL),(2841,NULL,'gps_point','2026-04-22 02:36:29',0,10.7835187,106.6285598,NULL),(2842,NULL,'gps_point','2026-04-22 02:36:30',0,10.7835042,106.628544,NULL),(2843,NULL,'gps_point','2026-04-22 02:36:59',0,10.7835042,106.628544,NULL),(2851,NULL,'gps_point','2026-04-22 02:37:29',0,10.7835042,106.628544,NULL),(2853,NULL,'gps_point','2026-04-22 02:37:59',0,10.7835042,106.628544,NULL),(2854,NULL,'gps_point','2026-04-22 02:38:29',0,10.7835042,106.628544,NULL),(2855,NULL,'gps_point','2026-04-22 02:38:59',0,10.7835042,106.628544,NULL),(2856,NULL,'gps_point','2026-04-22 02:39:29',0,10.7835042,106.628544,NULL),(2858,NULL,'gps_point','2026-04-22 02:43:48',0,10.783508,106.6285398,NULL),(2859,NULL,'gps_point','2026-04-22 02:43:59',0,10.783508,106.6285398,NULL),(2861,NULL,'gps_point','2026-04-22 02:44:29',0,10.783508,106.6285398,NULL),(2862,NULL,'gps_point','2026-04-22 02:45:29',0,10.7835069,106.6285236,NULL),(2863,NULL,'gps_point','2026-04-22 07:23:56',0,10.7791733,106.6845261,'tamnhu'),(2864,NULL,'gps_point','2026-04-22 07:23:57',0,10.7791656,106.6845447,'tamnhu'),(2865,NULL,'gps_point','2026-04-22 07:24:10',0,10.7791506,106.6845695,'tamnhu'),(2866,NULL,'gps_point','2026-04-22 07:24:11',0,10.779071,106.684524,'tamnhu'),(2867,NULL,'gps_point','2026-04-22 07:24:12',0,10.7790038,106.684405,'tamnhu'),(2868,NULL,'gps_point','2026-04-22 07:24:13',0,10.7789776,106.684315,'tamnhu'),(2869,NULL,'gps_point','2026-04-22 07:24:14',0,10.7789801,106.6842692,'tamnhu'),(2870,NULL,'gps_point','2026-04-22 07:24:14',0,10.7792056,106.6845986,'tamnhu'),(2871,NULL,'gps_point','2026-04-22 07:24:17',0,10.7792092,106.6846327,'tamnhu'),(2872,NULL,'gps_point','2026-04-22 07:24:20',0,10.779205,106.6845975,'tamnhu'),(2873,NULL,'gps_point','2026-04-22 07:24:26',0,10.779205,106.6845975,'tamnhu'),(2874,NULL,'gps_point','2026-04-22 07:24:55',0,10.761634412837973,106.70238141820536,'tamnhu'),(2875,NULL,'gps_point','2026-04-22 07:24:56',0,10.761634412837973,106.70238141820536,'tamnhu'),(2876,NULL,'gps_point','2026-04-22 07:24:59',0,10.7792046,106.6846096,'tamnhu'),(2877,NULL,'gps_point','2026-04-22 07:25:01',0,10.761682300198181,106.70232520110108,'tamnhu'),(2878,NULL,'gps_point','2026-04-22 07:25:05',0,10.7792023,106.6846054,'tamnhu'),(2879,NULL,'gps_point','2026-04-22 07:25:08',0,10.761611028750549,106.70241333083754,'tamnhu'),(2880,NULL,'gps_point','2026-04-22 07:25:10',0,10.7792023,106.6846054,'tamnhu'),(2881,NULL,'gps_point','2026-04-22 07:25:11',0,10.761624078462264,106.70239774846823,'tamnhu'),(2882,NULL,'gps_point','2026-04-22 07:25:16',0,10.779201,106.6846093,'tamnhu'),(2883,NULL,'gps_point','2026-04-22 07:25:26',0,10.779201,106.6846093,'tamnhu'),(2884,NULL,'gps_point','2026-04-22 07:25:56',0,10.779201,106.6846093,'tamnhu'),(2885,NULL,'gps_point','2026-04-22 07:25:59',0,10.7792088,106.6845921,'tamnhu'),(2886,NULL,'gps_point','2026-04-22 07:26:20',0,10.7792179,106.684569,'tamnhu'),(2887,NULL,'gps_point','2026-04-22 07:26:26',0,10.7792179,106.684569,'tamnhu'),(2888,NULL,'gps_point','2026-04-22 07:26:31',0,10.7792025,106.684597,'tamnhu'),(2889,NULL,'gps_point','2026-04-22 07:26:42',0,10.7792172,106.684572,'tamnhu'),(2890,3,'poi_audio_started_vi','2026-04-22 07:26:43',0,0,0,''),(2891,NULL,'gps_point','2026-04-22 07:26:56',0,10.7792172,106.684572,'tamnhu'),(2892,3,'audio_vi','2026-04-22 07:27:03',20.6323398,0,0,''),(2893,NULL,'gps_point','2026-04-22 07:27:14',0,10.7792078,106.6845899,'tamnhu'),(2894,NULL,'gps_point','2026-04-22 07:27:26',0,10.7792078,106.6845899,'tamnhu'),(2895,NULL,'gps_point','2026-04-22 07:27:56',0,10.7792078,106.6845899,'tamnhu'),(2896,NULL,'gps_point','2026-04-22 07:28:26',0,10.7792078,106.6845899,'tamnhu'),(2897,NULL,'gps_point','2026-04-22 07:28:50',0,10.761774360945905,106.70250895183082,'tamnhu'),(2898,NULL,'gps_point','2026-04-22 07:28:50',0,10.7792075,106.684596,'tamnhu'),(2899,NULL,'gps_point','2026-04-22 07:28:51',0,10.761648883085176,106.70239757628971,'tamnhu'),(2900,NULL,'gps_point','2026-04-22 07:28:55',0,10.7792075,106.684596,'tamnhu'),(2901,NULL,'gps_point','2026-04-22 07:28:56',0,10.7792075,106.684596,'tamnhu'),(2902,NULL,'gps_point','2026-04-22 07:28:56',0,10.761392157194894,106.70287072449158,'tamnhu'),(2903,NULL,'gps_point','2026-04-22 07:29:01',0,10.761012628260168,106.70298677042967,'tamnhu'),(2904,NULL,'gps_point','2026-04-22 07:29:01',0,10.7792026,106.6845867,'tamnhu'),(2905,NULL,'gps_point','2026-04-22 07:29:03',0,10.760997570859073,106.70298421595464,'tamnhu'),(2906,NULL,'gps_point','2026-04-22 07:29:06',0,10.7792025,106.6845865,'tamnhu'),(2907,NULL,'gps_point','2026-04-22 07:29:07',0,10.760935544718002,106.7029653280707,'tamnhu'),(2908,NULL,'gps_point','2026-04-22 07:29:09',0,10.76093420850772,106.70337349483233,'tamnhu'),(2909,NULL,'gps_point','2026-04-22 07:29:12',0,10.7792072,106.6845999,'tamnhu'),(2910,NULL,'gps_point','2026-04-22 07:29:12',0,10.760577409709814,106.70333932379381,'tamnhu'),(2911,NULL,'gps_point','2026-04-22 07:29:17',0,10.7792072,106.6846001,'tamnhu'),(2912,NULL,'gps_point','2026-04-22 07:29:26',0,10.7792072,106.6846001,'tamnhu'),(2913,NULL,'gps_point','2026-04-22 07:29:28',0,10.76081832801093,106.70318350022345,'tamnhu'),(2914,NULL,'gps_point','2026-04-22 07:29:33',0,10.7792023,106.6845968,'tamnhu'),(2915,NULL,'gps_point','2026-04-22 07:29:34',0,10.761469924038204,106.70285692106476,'tamnhu'),(2916,NULL,'gps_point','2026-04-22 07:29:37',0,10.761763230044245,106.70249967463849,'tamnhu'),(2917,NULL,'gps_point','2026-04-22 07:29:38',0,10.7792023,106.6845966,'tamnhu'),(2918,NULL,'gps_point','2026-04-22 07:29:43',0,10.76156497499146,106.70264987836379,'tamnhu'),(2919,NULL,'gps_point','2026-04-22 07:29:45',0,10.7792033,106.6845973,'tamnhu'),(2920,NULL,'gps_point','2026-04-22 07:29:45',0,10.761722073307608,106.70256353675984,'tamnhu'),(2921,NULL,'gps_point','2026-04-22 07:29:49',0,10.7792033,106.6845973,'tamnhu'),(2922,NULL,'gps_point','2026-04-22 07:29:49',0,10.761647288502038,106.702594190583,'tamnhu'),(2923,NULL,'gps_point','2026-04-22 07:29:54',0,10.7792017,106.6845867,'tamnhu'),(2924,NULL,'gps_point','2026-04-22 07:29:55',0,10.761743153586085,106.70252879577663,'tamnhu'),(2925,NULL,'gps_point','2026-04-22 07:29:56',0,10.761743153586085,106.70252879577663,'tamnhu'),(2926,NULL,'gps_point','2026-04-22 07:29:59',0,10.7792017,106.6845865,'tamnhu'),(2927,NULL,'gps_point','2026-04-22 07:30:00',0,10.761782302682285,106.70258397264195,'tamnhu'),(2928,NULL,'gps_point','2026-04-22 07:30:05',0,10.7792042,106.6845989,'tamnhu'),(2929,NULL,'gps_point','2026-04-22 07:30:06',0,10.761722575176885,106.7025568951043,'tamnhu'),(2930,NULL,'gps_point','2026-04-22 07:30:10',0,10.7792042,106.6845991,'tamnhu'),(2931,NULL,'gps_point','2026-04-22 07:30:13',0,10.761729601990146,106.7025236868061,'tamnhu'),(2932,NULL,'gps_point','2026-04-22 07:30:16',0,10.761074629320747,106.70284790297073,'tamnhu'),(2933,NULL,'gps_point','2026-04-22 07:30:16',0,10.7792058,106.6845911,'tamnhu'),(2934,NULL,'gps_point','2026-04-22 07:30:17',0,10.76099201130038,106.70299187011626,'tamnhu'),(2935,NULL,'gps_point','2026-04-22 07:30:21',0,10.7792058,106.6845909,'tamnhu'),(2936,NULL,'gps_point','2026-04-22 07:30:22',0,10.760952553536692,106.70337025811256,'tamnhu'),(2937,NULL,'gps_point','2026-04-22 07:30:25',0,10.76065156227984,106.70364110512857,'tamnhu'),(2938,NULL,'gps_point','2026-04-22 07:30:26',0,10.76065156227984,106.70364110512857,'tamnhu'),(2939,NULL,'gps_point','2026-04-22 07:30:27',0,10.7792016,106.6846058,'tamnhu'),(2940,1,'poi_audio_started_vi','2026-04-22 07:30:32',0,0,0,''),(2941,1,'audio_vi','2026-04-22 07:30:34',2.3874622,0,0,''),(2942,2,'poi_audio_started_vi','2026-04-22 07:30:34',0,0,0,''),(2943,NULL,'gps_point','2026-04-22 07:30:56',0,10.7792016,106.6846058,'tamnhu'),(2944,2,'audio_vi','2026-04-22 07:30:59',24.2734028,0,0,''),(2945,NULL,'gps_point','2026-04-22 07:31:09',0,10.7792101,106.6845844,'tamnhu'),(2946,NULL,'gps_point','2026-04-22 07:31:20',0,10.7792043,106.6846072,'tamnhu'),(2947,NULL,'gps_point','2026-04-22 07:31:26',0,10.7792043,106.6846072,'tamnhu'),(2948,NULL,'gps_point','2026-04-22 07:34:26',0,10.7792043,106.6846072,'tamnhu'),(2949,NULL,'app_login','2026-04-22 07:36:49',0,0,0,''),(2950,NULL,'gps_point','2026-04-22 07:37:12',0,10.7792031,106.6846035,'guest'),(2951,1,'click','2026-04-22 07:37:15',0,0,0,''),(2952,NULL,'gps_point','2026-04-22 07:37:20',0,10.7792051,106.6845984,'guest'),(2953,NULL,'gps_point','2026-04-22 07:37:50',0,10.7792051,106.6845984,'guest'),(2954,NULL,'gps_point','2026-04-22 07:37:50',0,10.761743732660694,106.70240359057514,'guest'),(2955,NULL,'gps_point','2026-04-22 07:37:51',0,10.7791994,106.6846119,'guest'),(2956,3,'poi_audio_started_vi','2026-04-22 07:37:51',0,0,0,''),(2957,3,'poi_visit','2026-04-22 07:37:51',0,10.761743732660694,106.70240359057514,''),(2958,3,'audio_vi','2026-04-22 07:38:13',22.2405628,0,0,''),(2959,NULL,'gps_point','2026-04-22 07:38:20',0,10.760890636972103,106.70191951993749,'guest'),(2960,NULL,'gps_point','2026-04-22 07:38:20',0,10.760890636972103,106.70191951993749,'guest'),(2961,NULL,'gps_point','2026-04-22 07:38:23',0,10.7792023,106.6846096,'guest'),(2962,NULL,'gps_point','2026-04-22 07:38:26',0,10.761725729172381,106.70239436080479,'guest'),(2963,3,'poi_audio_started_vi','2026-04-22 07:38:28',0,0,0,''),(2964,NULL,'gps_point','2026-04-22 07:38:28',0,10.7792012,106.6846094,'guest'),(2965,3,'audio_vi','2026-04-22 07:38:34',6.5158114,0,0,''),(2966,NULL,'gps_point','2026-04-22 07:38:50',0,10.7792012,106.6846094,'guest'),(2967,NULL,'gps_point','2026-04-22 07:39:19',0,10.7792012,106.6846094,'guest'),(2968,NULL,'gps_point','2026-04-22 07:39:50',0,10.7792012,106.6846094,'guest'),(2969,NULL,'gps_point','2026-04-22 07:40:20',0,10.7792012,106.6846094,'guest'),(2970,NULL,'gps_point','2026-04-22 07:40:50',0,10.7792012,106.6846094,'guest'),(2971,1,'click','2026-04-22 07:51:44',0,0,0,''),(2972,2,'click','2026-04-22 07:55:24',0,0,0,''),(2973,NULL,'app_login','2026-04-22 09:34:11',0,0,0,''),(2974,NULL,'app_login','2026-04-22 09:34:35',0,0,0,''),(2975,NULL,'gps_point','2026-04-22 09:35:11',0,10.761858333333333,106.702235,'guest'),(2976,NULL,'gps_point','2026-04-22 09:35:41',0,10.761858333333333,106.702235,'guest'),(2977,NULL,'gps_point','2026-04-22 09:36:11',0,10.761858333333333,106.702235,'guest'),(2978,NULL,'gps_point','2026-04-22 09:36:41',0,10.761858333333333,106.702235,'guest'),(2979,NULL,'gps_point','2026-04-22 09:37:11',0,10.761858333333333,106.702235,'guest'),(2980,NULL,'gps_point','2026-04-22 09:37:41',0,10.761858333333333,106.702235,'guest'),(2981,NULL,'gps_point','2026-04-22 09:38:11',0,10.761858333333333,106.702235,'guest'),(2982,NULL,'gps_point','2026-04-22 09:38:41',0,10.761858333333333,106.702235,'guest'),(2983,NULL,'gps_point','2026-04-22 09:39:11',0,10.761858333333333,106.702235,'guest'),(2984,NULL,'gps_point','2026-04-22 09:39:41',0,10.761858333333333,106.702235,'guest'),(2985,NULL,'gps_point','2026-04-22 09:40:11',0,10.761858333333333,106.702235,'guest'),(2986,NULL,'gps_point','2026-04-22 09:40:41',0,10.761858333333333,106.702235,'guest'),(2987,NULL,'gps_point','2026-04-22 09:41:11',0,10.761858333333333,106.702235,'guest'),(2988,NULL,'gps_point','2026-04-22 09:41:41',0,10.761858333333333,106.702235,'guest'),(2989,NULL,'gps_point','2026-04-22 09:42:11',0,10.761858333333333,106.702235,'guest'),(2990,NULL,'gps_point','2026-04-22 09:42:41',0,10.761858333333333,106.702235,'guest'),(2991,NULL,'gps_point','2026-04-22 09:43:11',0,10.761858333333333,106.702235,'guest'),(2992,NULL,'gps_point','2026-04-22 09:43:41',0,10.761858333333333,106.702235,'guest'),(2993,NULL,'gps_point','2026-04-22 09:44:11',0,10.761858333333333,106.702235,'guest'),(2994,NULL,'gps_point','2026-04-22 09:44:41',0,10.761858333333333,106.702235,'guest'),(2995,NULL,'gps_point','2026-04-22 09:45:11',0,10.761858333333333,106.702235,'guest'),(2996,NULL,'gps_point','2026-04-22 09:45:41',0,10.761858333333333,106.702235,'guest'),(2997,NULL,'gps_point','2026-04-22 09:46:11',0,10.761858333333333,106.702235,'guest'),(2998,NULL,'gps_point','2026-04-22 09:46:41',0,10.761858333333333,106.702235,'guest'),(2999,NULL,'gps_point','2026-04-22 09:47:11',0,10.761858333333333,106.702235,'guest'),(3000,NULL,'gps_point','2026-04-22 09:47:41',0,10.761858333333333,106.702235,'guest'),(3001,NULL,'gps_point','2026-04-22 09:48:11',0,10.761858333333333,106.702235,'guest'),(3002,NULL,'gps_point','2026-04-22 09:48:41',0,10.761858333333333,106.702235,'guest'),(3003,NULL,'gps_point','2026-04-22 09:49:11',0,10.761858333333333,106.702235,'guest'),(3004,NULL,'gps_point','2026-04-22 09:49:41',0,10.761858333333333,106.702235,'guest'),(3005,NULL,'gps_point','2026-04-22 09:50:11',0,10.761858333333333,106.702235,'guest'),(3006,NULL,'gps_point','2026-04-22 09:50:41',0,10.761858333333333,106.702235,'guest'),(3007,NULL,'gps_point','2026-04-22 09:51:11',0,10.761858333333333,106.702235,'guest'),(3008,NULL,'gps_point','2026-04-22 10:08:20',0,10.761858333333333,106.702235,'guest'),(3009,NULL,'gps_point','2026-04-22 10:08:28',0,10.761858333333333,106.702235,'guest'),(3010,NULL,'gps_point','2026-04-22 10:08:58',0,10.761858333333333,106.702235,'guest'),(3011,NULL,'gps_point','2026-04-22 10:09:28',0,10.761858333333333,106.702235,'guest'),(3012,NULL,'gps_point','2026-04-22 10:09:58',0,10.761858333333333,106.702235,'guest'),(3013,NULL,'gps_point','2026-04-22 10:10:28',0,10.761858333333333,106.702235,'guest'),(3014,NULL,'gps_point','2026-04-22 10:10:58',0,10.761858333333333,106.702235,'guest'),(3015,NULL,'gps_point','2026-04-22 10:11:28',0,10.761858333333333,106.702235,'guest'),(3016,NULL,'gps_point','2026-04-22 10:11:58',0,10.761858333333333,106.702235,'guest'),(3017,NULL,'gps_point','2026-04-22 10:12:28',0,10.761858333333333,106.702235,'guest'),(3018,NULL,'gps_point','2026-04-22 10:12:58',0,10.761858333333333,106.702235,'guest'),(3019,NULL,'gps_point','2026-04-22 10:13:28',0,10.761858333333333,106.702235,'guest'),(3020,NULL,'gps_point','2026-04-22 10:13:58',0,10.761858333333333,106.702235,'guest'),(3021,NULL,'gps_point','2026-04-22 10:14:28',0,10.761858333333333,106.702235,'guest'),(3022,NULL,'gps_point','2026-04-22 10:14:58',0,10.761858333333333,106.702235,'guest'),(3023,NULL,'gps_point','2026-04-22 10:15:28',0,10.761858333333333,106.702235,'guest'),(3024,NULL,'gps_point','2026-04-22 10:15:58',0,10.761858333333333,106.702235,'guest'),(3025,NULL,'gps_point','2026-04-22 10:16:28',0,10.761858333333333,106.702235,'guest'),(3026,NULL,'gps_point','2026-04-22 10:16:58',0,10.761858333333333,106.702235,'guest'),(3027,NULL,'gps_point','2026-04-22 10:17:28',0,10.761858333333333,106.702235,'guest'),(3028,NULL,'gps_point','2026-04-22 10:17:58',0,10.761858333333333,106.702235,'guest'),(3029,NULL,'gps_point','2026-04-22 10:18:28',0,10.761858333333333,106.702235,'guest'),(3030,NULL,'gps_point','2026-04-22 10:18:58',0,10.761858333333333,106.702235,'guest'),(3031,NULL,'gps_point','2026-04-22 10:19:28',0,10.761858333333333,106.702235,'guest'),(3032,NULL,'gps_point','2026-04-22 10:19:58',0,10.761858333333333,106.702235,'guest'),(3033,NULL,'gps_point','2026-04-22 10:20:28',0,10.761858333333333,106.702235,'guest'),(3034,NULL,'gps_point','2026-04-22 10:20:58',0,10.761858333333333,106.702235,'guest'),(3035,NULL,'gps_point','2026-04-22 10:21:28',0,10.761858333333333,106.702235,'guest'),(3036,NULL,'gps_point','2026-04-22 10:21:58',0,10.761858333333333,106.702235,'guest'),(3037,NULL,'gps_point','2026-04-22 10:22:28',0,10.761858333333333,106.702235,'guest'),(3038,NULL,'gps_point','2026-04-22 10:22:58',0,10.761858333333333,106.702235,'guest'),(3039,NULL,'gps_point','2026-04-22 10:23:28',0,10.761858333333333,106.702235,'guest'),(3040,NULL,'gps_point','2026-04-22 10:23:58',0,10.761858333333333,106.702235,'guest'),(3041,NULL,'gps_point','2026-04-22 10:24:28',0,10.761858333333333,106.702235,'guest'),(3042,NULL,'gps_point','2026-04-22 10:24:58',0,10.761858333333333,106.702235,'guest'),(3043,NULL,'gps_point','2026-04-22 10:25:28',0,10.761858333333333,106.702235,'guest'),(3044,NULL,'gps_point','2026-04-22 10:25:58',0,10.761858333333333,106.702235,'guest'),(3045,NULL,'gps_point','2026-04-22 10:26:28',0,10.761858333333333,106.702235,'guest'),(3046,NULL,'gps_point','2026-04-22 10:26:58',0,10.761858333333333,106.702235,'guest'),(3047,NULL,'gps_point','2026-04-22 10:27:28',0,10.761858333333333,106.702235,'guest'),(3048,NULL,'gps_point','2026-04-22 10:27:58',0,10.761858333333333,106.702235,'guest'),(3049,NULL,'gps_point','2026-04-22 10:28:28',0,10.761858333333333,106.702235,'guest'),(3050,NULL,'gps_point','2026-04-22 10:28:58',0,10.761858333333333,106.702235,'guest'),(3051,NULL,'gps_point','2026-04-22 10:29:28',0,10.761858333333333,106.702235,'guest'),(3052,NULL,'gps_point','2026-04-22 10:29:58',0,10.761858333333333,106.702235,'guest'),(3053,NULL,'gps_point','2026-04-22 10:30:28',0,10.761858333333333,106.702235,'guest'),(3054,NULL,'gps_point','2026-04-22 10:30:58',0,10.761858333333333,106.702235,'guest'),(3055,NULL,'gps_point','2026-04-22 10:31:28',0,10.761858333333333,106.702235,'guest'),(3056,NULL,'gps_point','2026-04-22 10:31:58',0,10.761858333333333,106.702235,'guest'),(3057,NULL,'gps_point','2026-04-22 10:32:28',0,10.761858333333333,106.702235,'guest'),(3058,NULL,'gps_point','2026-04-22 10:32:58',0,10.761858333333333,106.702235,'guest'),(3059,NULL,'gps_point','2026-04-22 10:33:28',0,10.761858333333333,106.702235,'guest'),(3060,NULL,'gps_point','2026-04-22 10:33:58',0,10.761858333333333,106.702235,'guest'),(3061,NULL,'gps_point','2026-04-22 10:34:28',0,10.761858333333333,106.702235,'guest'),(3062,NULL,'gps_point','2026-04-22 10:34:58',0,10.761858333333333,106.702235,'guest'),(3063,NULL,'gps_point','2026-04-22 10:35:28',0,10.761858333333333,106.702235,'guest'),(3064,NULL,'gps_point','2026-04-22 10:35:58',0,10.761858333333333,106.702235,'guest'),(3065,NULL,'gps_point','2026-04-22 10:36:28',0,10.761858333333333,106.702235,'guest'),(3066,NULL,'gps_point','2026-04-22 10:36:58',0,10.761858333333333,106.702235,'guest'),(3067,NULL,'gps_point','2026-04-22 10:37:28',0,10.761858333333333,106.702235,'guest'),(3068,NULL,'gps_point','2026-04-22 10:37:58',0,10.761858333333333,106.702235,'guest'),(3069,NULL,'gps_point','2026-04-22 10:38:28',0,10.761858333333333,106.702235,'guest'),(3070,NULL,'gps_point','2026-04-22 10:38:58',0,10.761858333333333,106.702235,'guest'),(3071,NULL,'gps_point','2026-04-22 10:39:28',0,10.761858333333333,106.702235,'guest'),(3072,NULL,'gps_point','2026-04-22 10:39:58',0,10.761858333333333,106.702235,'guest'),(3073,NULL,'gps_point','2026-04-22 10:40:28',0,10.761858333333333,106.702235,'guest'),(3074,NULL,'gps_point','2026-04-22 10:40:58',0,10.761858333333333,106.702235,'guest'),(3075,NULL,'gps_point','2026-04-22 10:41:28',0,10.761858333333333,106.702235,'guest'),(3076,NULL,'gps_point','2026-04-22 10:41:58',0,10.761858333333333,106.702235,'guest'),(3077,NULL,'gps_point','2026-04-22 10:42:28',0,10.761858333333333,106.702235,'guest'),(3078,NULL,'gps_point','2026-04-22 10:42:58',0,10.761858333333333,106.702235,'guest'),(3079,NULL,'gps_point','2026-04-22 10:43:28',0,10.761858333333333,106.702235,'guest'),(3080,NULL,'gps_point','2026-04-22 10:43:58',0,10.761858333333333,106.702235,'guest'),(3081,NULL,'gps_point','2026-04-22 10:44:28',0,10.761858333333333,106.702235,'guest'),(3082,NULL,'gps_point','2026-04-22 10:44:58',0,10.761858333333333,106.702235,'guest'),(3083,NULL,'gps_point','2026-04-22 10:45:28',0,10.761858333333333,106.702235,'guest'),(3084,NULL,'gps_point','2026-04-22 10:45:58',0,10.761858333333333,106.702235,'guest'),(3085,NULL,'gps_point','2026-04-22 10:46:28',0,10.761858333333333,106.702235,'guest'),(3086,NULL,'gps_point','2026-04-22 10:46:58',0,10.761858333333333,106.702235,'guest'),(3087,NULL,'gps_point','2026-04-22 10:47:28',0,10.761858333333333,106.702235,'guest'),(3088,NULL,'gps_point','2026-04-22 10:47:58',0,10.761858333333333,106.702235,'guest'),(3089,NULL,'gps_point','2026-04-22 10:48:28',0,10.761858333333333,106.702235,'guest'),(3090,NULL,'gps_point','2026-04-22 10:48:58',0,10.761858333333333,106.702235,'guest'),(3091,NULL,'gps_point','2026-04-22 10:49:28',0,10.761858333333333,106.702235,'guest'),(3092,NULL,'gps_point','2026-04-22 10:49:58',0,10.761858333333333,106.702235,'guest'),(3093,NULL,'gps_point','2026-04-22 10:50:28',0,10.761858333333333,106.702235,'guest'),(3094,NULL,'gps_point','2026-04-22 10:50:58',0,10.761858333333333,106.702235,'guest'),(3095,NULL,'gps_point','2026-04-22 10:51:28',0,10.761858333333333,106.702235,'guest'),(3096,NULL,'gps_point','2026-04-22 10:51:58',0,10.761858333333333,106.702235,'guest'),(3097,NULL,'gps_point','2026-04-22 10:52:28',0,10.761858333333333,106.702235,'guest'),(3098,NULL,'gps_point','2026-04-22 10:52:58',0,10.761858333333333,106.702235,'guest'),(3099,NULL,'gps_point','2026-04-22 10:53:28',0,10.761858333333333,106.702235,'guest'),(3100,NULL,'gps_point','2026-04-22 10:53:58',0,10.761858333333333,106.702235,'guest'),(3101,NULL,'gps_point','2026-04-22 10:54:28',0,10.761858333333333,106.702235,'guest'),(3102,NULL,'gps_point','2026-04-22 10:54:58',0,10.761858333333333,106.702235,'guest'),(3103,NULL,'gps_point','2026-04-22 10:55:28',0,10.761858333333333,106.702235,'guest'),(3104,NULL,'gps_point','2026-04-22 10:55:58',0,10.761858333333333,106.702235,'guest'),(3105,NULL,'gps_point','2026-04-22 10:56:28',0,10.761858333333333,106.702235,'guest'),(3106,NULL,'gps_point','2026-04-22 10:56:58',0,10.761858333333333,106.702235,'guest'),(3107,NULL,'gps_point','2026-04-22 10:57:28',0,10.761858333333333,106.702235,'guest'),(3108,NULL,'gps_point','2026-04-22 10:57:58',0,10.761858333333333,106.702235,'guest'),(3109,NULL,'gps_point','2026-04-22 10:58:28',0,10.761858333333333,106.702235,'guest'),(3110,NULL,'gps_point','2026-04-22 10:58:58',0,10.761858333333333,106.702235,'guest'),(3111,NULL,'gps_point','2026-04-22 10:59:28',0,10.761858333333333,106.702235,'guest'),(3112,NULL,'gps_point','2026-04-22 10:59:58',0,10.761858333333333,106.702235,'guest'),(3113,NULL,'gps_point','2026-04-22 11:00:28',0,10.761858333333333,106.702235,'guest'),(3114,NULL,'gps_point','2026-04-22 11:00:58',0,10.761858333333333,106.702235,'guest'),(3115,NULL,'gps_point','2026-04-22 11:01:28',0,10.761858333333333,106.702235,'guest'),(3116,NULL,'gps_point','2026-04-22 11:01:58',0,10.761858333333333,106.702235,'guest'),(3117,NULL,'gps_point','2026-04-22 11:02:28',0,10.761858333333333,106.702235,'guest'),(3118,NULL,'gps_point','2026-04-22 11:02:58',0,10.761858333333333,106.702235,'guest');
/*!40000 ALTER TABLE `analytics` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `app_languages`
--

DROP TABLE IF EXISTS `app_languages`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `app_languages` (
  `Code` varchar(10) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Flag` varchar(10) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 0xF09F8C90,
  `IsDefault` tinyint(1) NOT NULL DEFAULT '0',
  `SortOrder` int NOT NULL DEFAULT '99',
  PRIMARY KEY (`Code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `app_languages`
--

LOCK TABLES `app_languages` WRITE;
/*!40000 ALTER TABLE `app_languages` DISABLE KEYS */;
INSERT INTO `app_languages` VALUES ('en','English','??',0,1),('vi','Tiếng Việt','??',1,0),('zh','中文','??',0,2);
/*!40000 ALTER TABLE `app_languages` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `approval_requests`
--

DROP TABLE IF EXISTS `approval_requests`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `approval_requests` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `UserName` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `LocationId` int NOT NULL,
  `LocationName` varchar(200) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `Action` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL,
  `RequestData` longtext COLLATE utf8mb4_unicode_ci NOT NULL,
  `OldData` longtext COLLATE utf8mb4_unicode_ci,
  `Status` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT 'pending',
  `AdminNote` text COLLATE utf8mb4_unicode_ci,
  `Note` text COLLATE utf8mb4_unicode_ci,
  `CreatedAt` datetime DEFAULT CURRENT_TIMESTAMP,
  `ReviewedAt` datetime DEFAULT NULL,
  `ReviewedBy` int DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `approval_requests`
--

LOCK TABLES `approval_requests` WRITE;
/*!40000 ALTER TABLE `approval_requests` DISABLE KEYS */;
/*!40000 ALTER TABLE `approval_requests` ENABLE KEYS */;
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
  `QrCode` varchar(64) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Mã QR duy nhất cho mỗi địa điểm, dùng để check-in',
  PRIMARY KEY (`Id`),
  UNIQUE KEY `QrCode` (`QrCode`),
  KEY `idx_restaurants_qrcode` (`QrCode`)
) ENGINE=InnoDB AUTO_INCREMENT=16 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `restaurants`
--

LOCK TABLES `restaurants` WRITE;
/*!40000 ALTER TABLE `restaurants` DISABLE KEYS */;
INSERT INTO `restaurants` VALUES (1,'Ốc Oanh',' Ốc Oanh — linh hồn phố ẩm thực Vĩnh Khánh từ lâu đời','Quán ăn',10.7609469,106.7033042,'Vĩnh Khánh, Phường 8, Quận 4','uploads/poi_c8a6c420015b4893ba4a29fca9b6da87.jpg',4.3,'16:00 - 23:00',0,'','Chào mừng bạn đến Ốc Oanh — linh hồn phố ẩm thực Vĩnh Khánh hơn mười năm qua! Quán nổi tiếng với ốc hương nướng tiêu xanh, ốc mỡ hấp sả gừng và ốc len xào dừa béo ngậy. Mỗi con ốc tươi chọn lọc từ miền Tây mỗi sáng, đảm bảo chất lượng tuyệt đối. Thực đơn hơn hai mươi món, nước chấm bí truyền độc quyền. Mở cửa từ bốn giờ chiều đến mười một giờ đêm. Đặt chỗ sớm kẻo hết!','Welcome to Oc Oanh - the soul of Vinh Khanh culinary street for more than ten years! The restaurant is famous for grilled green pepper snails, steamed fatty snails with lemongrass and ginger, and fatty snails stir-fried with coconut. Each fresh snail is selected from the West every morning, ensuring absolute quality. Menu of more than twenty dishes, exclusive esoteric dipping sauce. Open from four in the afternoon to eleven at night. Book early before it runs out!','欢迎来到 Oc Oanh - 十多年来永庆美食街的灵魂！餐厅以烤青椒田螺、香茅生姜蒸田螺、椰子炒田螺而闻名。每只新鲜蜗牛都是每天早上从西方精选而来，保证绝对的品质。二十多道菜肴的菜单，独家秘制蘸酱。营业时间为下午四点至晚上十一点。售完为止请尽早预订！',50,0,'','{}','vkt_f89a3fd3d0ea9dc2'),(2,'Ốc Sáu Nở','Ốc Sáu Nở — thiên đường hải sản tươi sống với đa dạng món ốc chế biến độc đáo, biểu tượng ẩm thực Sài Gòn nhiều thập kỷ','Quán ăn',10.7609854,106.7029098,'Vĩnh Khánh, Phường 8, Quận 4','uploads/poi_e0f679bebae6499d928efd3ef79b9fad.jpg',4.4,'15:00 - 23:00',0,'','Bạn đang đến Ốc Sáu Nở — biểu tượng hải sản Sài Gòn nhiều thập kỷ! Thử ngay ốc bươu nhồi thịt hấp gừng, ốc nhảy xào me chua ngọt và mâm ốc tổng hợp năm loại đặc biệt. Nước chấm bí truyền gia truyền là linh hồn của quán. Ngồi vỉa hè Vĩnh Khánh, nhâm nhi ly mát cùng mâm ốc bốc khói — trải nghiệm Sài Gòn đích thực. Mở từ ba giờ chiều đến mười một giờ đêm!','Approaching Oc Sau No — a Saigon seafood icon for decades! Must-try: ginger-steamed stuffed snails, tamarind-glazed jumping snails, and the five-variety shellfish platter. Their secret dipping sauce keeps regulars coming back. Sitting on the lively Vinh Khanh sidewalk with an ice-cold drink alongside steaming shellfish — this is authentic Saigon street food. Open three in the afternoon until eleven at night!','欢迎来到\'六绽海鲜\'——西贡数十年的海鲜传奇！必点：姜汁蒸肉馅田螺、酸甜罗望子炒蜗牛和五种贝类拼盘。秘制蘸酱代代相传，令老顾客念念不忘。坐在永庆街边，冷饮配热螺，尽享西贡街头真味。每天下午三点至晚上十一点营业！',50,0,'','{}','vkt_3055dfb4d47d4a72'),(3,'Ốc Thảo','Ốc Thảo — quán ốc gia vị biến tấu độc đáo, một trong những quán lâu đời và được yêu thích nhất phố Vĩnh Khánh','Quán ăn',10.7617199,106.7023681,'Vĩnh Khánh, Phường 8, Quận 4','uploads/poi_c2a2404fbac34d7585ea9bbe297452af.jpg',4.2,'16:00 - 22:30',0,'','Phía trước là Ốc Thảo — một trong những quán ốc lâu đời nhất Vĩnh Khánh! Điểm đặc biệt là gia vị độc đáo kết hợp truyền thống Nam Bộ và sáng tạo hiện đại. Thử ốc nướng muối ớt giòn tan, ốc sò hấp bia thơm nồng và ốc de nấu tiêu đen đậm đà. Chị Thảo luôn đích thân đứng bếp đảm bảo hương vị chuẩn nhất. Không khí thân thiện, giá hợp lý. Mở từ bốn giờ chiều đến mười giờ rưỡi tối!','Just ahead is Oc Thao — one of Vinh Khanh\'s oldest and most beloved shellfish spots! Known for blending traditional Southern spices with modern creativity. Try salt-chili grilled snails, beer-steamed clams, and savory black pepper mud snails. Ms. Thao personally oversees every dish from the kitchen. Friendly atmosphere, affordable prices. Open four in the afternoon until half past ten at night!','前方是\'草海鲜\'——永庆街历史最悠久的贝壳餐厅之一！以传统南越香料与现代创意的独特融合闻名。必尝盐辣烤螺、啤酒蒸蛤蜊和黑椒焖泥螺。草姐每天亲自掌厨，品质始终如一。氛围友好，价格亲民。下午四点至晚上十点半营业！',50,0,'','{}','vkt_7a351a7502a22517'),(4,'Lãng Quán','Lãng Quán — không gian ẩm thực trẻ trung sáng tạo với thực đơn đa dạng từ ốc, lẩu đến đồ nướng và nhạc sống cuối tuần.\' WHERE Name = \'Lãng Quán\';','Quán ăn',10.7612817,106.7053733,'Vĩnh Khánh, Phường 8, Quận 4','uploads/poi_4b1c258bca154e839ad7993a0fe66e22.jpg',4.5,'17:00 - 23:00',0,'','Chào bạn đến Lãng Quán — không gian ẩm thực trẻ trung sáng tạo giữa phố Vĩnh Khánh! Không chỉ là quán ăn — đây là nơi bạn bè hội tụ và tạo kỷ niệm. Thực đơn đa dạng từ ăn vặt Sài Gòn đến lẩu riêu cua, bò lúc lắc tiêu xanh, gà nướng mật ong. Cuối tuần có âm nhạc live tạo không gian lãng mạn. Phục vụ từ năm giờ chiều đến mười một giờ đêm. Hẹn gặp bạn tại Lãng Quán!','Welcome to Lang Quan — a vibrant creative dining space in the heart of Vinh Khanh! More than a restaurant — a gathering place for friends and memories. The menu spans Saigon street snacks to crab roe hotpot, green pepper beef, and honey-glazed grilled chicken. Weekend evenings feature live acoustic music for a romantic atmosphere. Open five in the afternoon until eleven at night. See you at Lang Quan!','欢迎来到\'浪漫小馆\'——永庆街充满青春创意的餐饮空间！不只是餐厅，更是朋友聚会创造回忆之地。菜单从西贡小吃到蟹黄火锅、青椒牛肉、蜂蜜烤鸡应有尽有。周末有现场原声音乐，浪漫氛围满满。下午五点至晚上十一点营业，期待与您相聚！',50,0,'','{}','vkt_b42ffc8b7491cae5'),(5,'Ớt Xiêm Quán','Ớt Xiêm Quán — thiên đường đồ ăn cay nồng phong cách Khmer Nam Bộ, điểm đến không thể bỏ qua cho tín đồ ghiền cay','Quán ăn',10.7613455,106.7056902,'Vĩnh Khánh, Phường 8, Quận 4','uploads/poi_e0884ceb10434af398ae22196e50ac12.jpg',4.4,'11:00 - 21:00',0,'','Ớt Xiêm Quán đang hiện ra trước mắt bạn — thiên đường của những tín đồ vị cay kiểu Khmer Nam Bộ! Ớt xiêm xanh nhỏ nhưng cực cay là linh hồn mọi món. Đừng bỏ qua gà nướng mật ong giòn vàng, tôm rang muối ớt giòn rụm và lẩu thái chua cay đậm vị. Nhân viên sẵn sàng tư vấn mức cay phù hợp. Chuẩn bị khăn giấy và chinh phục ẩm thực cay nhất Vĩnh Khánh! Mở từ mười một giờ sáng đến chín giờ tối.','Ot Xiem Quan is right before you — paradise for lovers of bold Khmer-style fiery flavors! The small but ferocious Siam chili pepper defines every dish. Don\'t miss honey-glazed chili grilled chicken, crunchy salt-chili shrimp, and the sour-spicy Thai-style hotpot. Staff always help you find your ideal heat level. Grab your tissues and conquer the spiciest food on Vinh Khanh street! Open eleven in the morning until nine at night.','朝天椒餐厅就在眼前——辣味爱好者的天堂，以越南南部高棉风味为灵感！个头小却无比辣的暹罗朝天椒贯穿所有菜肴。必尝蜂蜜烤鸡、椒盐炸虾和酸辣泰式火锅。员工随时协助您选择合适辣度。备好纸巾，挑战永庆街最辣美食！上午十一点至晚上九点营业。',50,0,'','{}','vkt_e6aff6b269d727f0'),(6,'Bún Cá Châu Đốc - Dì Tư','Bún Cá Châu Đốc Dì Tư — bún cá chính gốc An Giang với nước lèo mắm ruốc đậm đà, hương vị miền Tây hiếm có giữa Sài Gòn','Quán ăn',10.7610603,106.706682,'Vĩnh Khánh, Phường 8, Quận 4','uploads/poi_51bfa22a55de4e10b16f9af0b4d19449.jpg',4.6,'06:00 - 14:00',0,'','Mùi mắm cá linh và ruốc Châu Đốc chính gốc đang lan tỏa từ bếp Dì Tư! Đây là địa chỉ hiếm tại Sài Gòn giữ nguyên hương vị bún cá An Giang. Nước lèo hầm cá lóc tươi cùng sả gừng tạo mùi thơm đặc trưng. Chan vào tô bún, thêm rau đắng, bắp chuối và cá chiên vàng — bữa sáng hoàn hảo! Mở từ sáu giờ sáng đến hai giờ chiều. Đến sớm kẻo hết!','The sweet aroma of linh fish paste and authentic Chau Doc shrimp paste drifts from Auntie Tu\'s kitchen! One of Saigon\'s rare spots preserving the original An Giang fish noodle flavors. The broth is slow-simmered with fresh snakehead fish, lemongrass and ginger. Ladled over soft rice noodles with bitter greens, banana blossom and crispy fish — the perfect breakfast! Open six in the morning until two in the afternoon. Come early before it sells out!','四姨厨房飘来正宗朱笃虾酱的甘美香气！西贡难得保留安江原味鱼粉的珍贵地址。汤底以新鲜乌鱼、香茅姜慢火熬制，香气独特无误。浇在米粉上，配苦菜、芭蕉花和炸鱼片——完美早餐！早上六点至下午两点营业，早来早得，卖完即止！',50,0,'','{}','vkt_258a113b56015270'),(7,'Chilli Lẩu Nướng Quán','Chilli Lẩu Nướng Quán — buffet lẩu nướng tự chọn giá sinh viên với thực đơn phong phú hàng chục món ăn đậm đà','Quán ăn',10.7608406,106.7040508,'Vĩnh Khánh, Phường 8, Quận 4','uploads/poi_a3ed31d6f7634e06937aaf18cc31a8c7.jpg',4.3,'10:00 - 23:00',0,'','Chào mừng đến Chilli Lẩu Nướng — buffet lẩu nướng tự chọn hấp dẫn giữa phố Vĩnh Khánh! Tự do chọn bò Mỹ thái mỏng, hải sản tươi và rau củ đa dạng để nướng và nhúng lẩu. Nồi lẩu thái chua cay hầm xương cả ngày là điểm nhấn đặc biệt. Giá hợp lý, không gian rộng rãi, thân thiện với sinh viên. Mở từ mười giờ sáng đến mười một giờ đêm mỗi ngày. Rủ bạn bè đến cùng!','Welcome to Chilli Hotpot and BBQ — an all-you-can-eat buffet in the heart of Vinh Khanh! Choose from premium US beef, fresh shrimp, crab, squid, and seasonal vegetables to grill and dip in broth. The signature sour-spicy Thai hotpot, simmered all day, is the star of every visit. Student-friendly prices, spacious seating. Open ten in the morning until eleven at night, seven days a week. Bring your crew!','欢迎来到辣椒火锅烤肉——永庆街物超所值的自助烤涮体验！自由挑选优质美国牛肉、新鲜海鲜和各色蔬菜，自助烤涮随心所欲。全天慢火熬制的酸辣泰式火锅是每次必点亮点。价格亲民，空间宽敞，深受学生喜爱。每天上午十点至晚上十一点，欢迎携友同来！',50,0,'','{}','vkt_32e38a719c813012'),(8,'Thế Giới Bò','Thế Giới Bò — vương quốc các món bò từ truyền thống đến sáng tạo hiện đại với gần ba mươi cách chế biến','Quán ăn',10.7642674,106.7011818,'Vĩnh Khánh, Phường 8, Quận 4','uploads/poi_24a35b56b2d04d8f96f53c5d3bcc091e.jpg',4.5,'10:00 - 22:00',0,'','Bạn đã đến Thế Giới Bò — vương quốc của tín đồ thịt bò tại Vĩnh Khánh! Gần ba mươi món bò từ truyền thống Việt đến sáng tạo Tây phương. Nổi bật là bò tái chanh lá quế, bò nhúng giấm sôi sùng sục và bò lúc lắc tiêu đen với khoai tây chiên. Nguyên liệu nhập nguồn uy tín, đảm bảo sạch tươi. Không gian industrial hiện đại, ánh đèn warm tone. Mở từ mười giờ sáng đến mười giờ tối!','You\'ve arrived at The World of Beef — the ultimate beef kingdom on Vinh Khanh street! Nearly thirty preparations spanning Vietnamese tradition to Western fusion. Standouts include rare beef with lime and basil, bubbling vinegar fondue, and irresistible black pepper stir-fry with crispy fries. All beef sourced from trusted suppliers for guaranteed freshness. Modern industrial decor, warm inviting lighting. Open ten in the morning until ten at night!','您已踏入\'牛肉世界\'——永庆街牛肉爱好者的终极王国！近三十种料理从越式传统到西式创新应有尽有。亮点包括柠檬香草腌牛肉、醋涮牛肉火锅和黑椒炒牛肉配薯条。食材来自信誉供应商，品质有保障。工业风装潢，灯光温馨。每天上午十点至晚上十点营业！',50,0,'','{}','vkt_c06850df0ba1cb8e'),(9,'Cơm Cháy Kho Quẹt','Bò Lá Lốt Cô Út — bò lá lốt nướng than hoa thơm lừng gia truyền hơn mười lăm năm, món không thể bỏ khi ghé Vĩnh Khánh','Quán ăn',10.7606253,106.7037167,'Vĩnh Khánh, Phường 8, Quận 4','uploads/poi_69d7fa8b4dd141efa88dafe5fd936b13.jpg',4.4,'10:00 - 21:00',0,'','Tiếng giòn rụm và mùi khói than — dấu hiệu bạn đang đến Cơm Cháy Kho Quẹt! Cơm cháy giòn tan từ đáy nồi đất nung, ăn kèm kho quẹt thịt ba chỉ và tép bạc đậm đà. Sự kết hợp cơm cháy giòn và kho quẹt ngọt mặn tạo hương vị mộc mạc khó quên. Còn có trứng ốp la, tôm kho tàu và canh chua cá kèo Nam Bộ. Mở từ mười giờ sáng đến chín giờ tối. Ghé ngay kẻo hết!','That crackling sound and charcoal aroma mean you\'re near Com Chay Kho Quet! Crispy rice scraped from clay pot bottoms, served with thick pork belly and shrimp braising sauce. The contrast of crunchy rice with savory-sweet sauce creates a humble yet unforgettable flavor. Also serving fried eggs, caramelized braised shrimp, and Southern Vietnamese sour catfish soup. Open ten in the morning until nine at night. Come before the crispy rice runs out!','酥脆声和炭火烟香——您正在靠近\'锅巴蘸酱\'！从陶土锅底精心刮下的酥脆锅巴，搭配五花肉银虾浓稠蘸酱。酥脆锅巴与咸甜蘸酱的完美对比，朴实而令人难忘。另有荷包蛋、焦糖焖虾和南越酸鱼汤。上午十点至晚上九点营业，锅巴卖完即止！',50,0,'','{}','vkt_ea68857a534096b6'),(10,'Bò Lá Lốt Cô Út','Bò Lá Lốt Cô Út — bò lá lốt nướng than hoa thơm lừng gia truyền hơn mười lăm năm, món không thể bỏ khi ghé Vĩnh Khánh','Quán ăn',10.7612788,106.7052938,'Vĩnh Khánh, Phường 8, Quận 4','uploads/poi_8119d4aba4fd411189e7c8fbdb7c707f.jpg',4.7,'15:00 - 23:00',0,'','Hương thơm nồng nàn trong không khí — đó là mùi bò lá lốt nướng than của Cô Út! Hơn mười lăm năm, Cô Út nhóm bếp than mỗi ngày tạo ra những cuộn bò lá lốt vàng ươm thơm phức. Thịt bò xay pha sả tiêu tỏi và mắm ruốc, cuộn lá lốt tươi nướng đỏ than. Chấm nước mắm chua ngọt gừng hoặc muối tiêu chanh — mỗi miếng đều tuyệt vời. Còn có nem nướng sả và bún thịt nướng. Mở từ ba giờ chiều đến mười một giờ đêm!','That seductive aroma in the evening air is Auntie Ut\'s famous lolot leaf beef rolls! For over fifteen years she lights the charcoal grill daily, crafting golden fragrant beef rolls nobody stops at just one. Ground beef seasoned with lemongrass, pepper, garlic and shrimp paste, wrapped in fresh lolot leaves, grilled over glowing charcoal. Dip in ginger fish sauce or salt-pepper-lime — extraordinary every bite. Also serving lemongrass sausage and grilled pork noodles. Open three until eleven at night!','空气中弥漫着迷人香气——那是幺姑著名蒌叶烤牛肉卷的气息！十五年来每天点燃炭炉，精心制作金黄牛肉卷，令人无法只吃一串。牛肉末调入香茅、胡椒、大蒜和虾酱，紧裹新鲜蒌叶，炭火烤制。蘸姜汁鱼露或椒盐柠檬汁，每口都是享受。另有香茅烤肠和烤肉米粉。下午三点至晚上十一点营业！',50,0,'','{}','vkt_9367e4539c480b46'),(11,'Bún Thịt Nướng Cô Nga','Bún Thịt Nướng Cô Nga — bún thịt nướng sả mật ong gia truyền hơn hai mươi năm, thơm ngon từ sáng sớm đến chiều tối','Quán ăn',10.7608835,106.7067418,'Vĩnh Khánh, Phường 8, Quận 4','uploads/poi_f92b5571042c4f8797e3fd8726d09910.jpg',4.5,'06:00 - 20:00',0,'','Bạn đang đứng trước Bún Thịt Nướng Cô Nga — tiệm bún yêu thích Vĩnh Khánh hơn hai mươi năm! Cô Nga ướp thịt bằng sả mật ong ngũ vị hương và nước cốt dừa, nướng than đến vàng thơm. Tô bún hoàn hảo: bún trắng mềm, thịt nướng thái lát, chả giò giòn và nước mắm chua ngọt. Giá bình dân, phục vụ nhanh và luôn tươi cười. Mở từ sáu giờ sáng đến tám giờ tối. Đến sớm thưởng thức bữa sáng hoàn hảo!','You\'re at Ms. Nga\'s Grilled Pork Noodles — Vinh Khanh\'s beloved bun thit nuong stall for over twenty years! Ms. Nga marinates pork daily with lemongrass, honey, five-spice and coconut milk, grilled over charcoal until golden. A perfect bowl: soft rice noodles, sliced grilled pork, crispy spring rolls, fresh herbs, and sweet-sour fish sauce. Affordable, fast service, warm smiles. Open six in the morning until eight at night. Come early for the best breakfast on Vinh Khanh!','您正站在阿娥烤肉米粉前——永庆街深受喜爱超过二十年的米粉摊！阿娥每天用香茅、蜂蜜、五香粉和椰奶腌制猪肉，炭火烤至金黄焦香。完美一碗：柔软米粉、薄切烤肉、酥脆炸春卷和酸甜鱼露。价格亲民，服务快捷，笑容温暖。早上六点至晚上八点营业，早来享用永庆最美早餐！',50,0,'','{}','vkt_ac4573eefa50b51c');
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
  `NameJa` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `DescEn` text COLLATE utf8mb4_unicode_ci,
  `DescZh` text COLLATE utf8mb4_unicode_ci,
  `DescJa` text COLLATE utf8mb4_unicode_ci,
  `Duration` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Rating` double DEFAULT '4',
  `Emoji` varchar(10) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `NameKo` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `DescKo` text COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=34 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tours`
--

LOCK TABLES `tours` WRITE;
/*!40000 ALTER TABLE `tours` DISABLE KEYS */;
INSERT INTO `tours` VALUES (1,'Tour Ăn Ốc','3 quán ốc ngon nổi tiếng nhất phố Vĩnh Khánh','uploads/tour_dab426ae68404abeaa6b937cd29b24dc.jpg',1,'[1,2,3]','Tour An Oc','Tour An Oc','Tour An Oc','The 3 most famous delicious snail restaurants on Vinh Khanh street','永庆街上最著名的 3 家蜗牛餐厅','ヴィンカイン通りの最も有名なおいしいカタツムリレストラン 3 軒','45 phút',4.4,'?','',''),(2,'Tour Ăn Nướng','Lẩu nướng, bò lá lốt - những món nướng đỉnh nhất Vĩnh Khánh','uploads/tour_56c8ba4c78ce4240897272011222d574.jpg',1,'[4,7,8]','Tour An Nuong','Tour An Nuong','Tour An Nuong','Grilled hot pot, beef with betel leaves - the best grilled dishes in Vinh Khanh','烤火锅、槟榔叶牛肉——永庆最好的烧烤菜肴','鍋のグリル、牛肉のキンマの葉添え - ヴィン カインで最高のグリル料理','60 phút',4.4,'?','',''),(3,'Tour Ăn Vặt','Cơm cháy kho quẹt, bún thịt nướng - ăn vặt đặc trưng Sài Gòn','uploads/tour_99b4fd7fc33d46a897c8e00971082e88.jpg',1,'[9,11,10]','Tour An Vat','Tour An Vat','Tour An Vat','Braised scorched rice, grilled meat vermicelli - typical Saigon snacks','红烧饭、烤肉粉丝——典型的西贡小吃','おこげご飯、焼き肉春雨 - 典型的なサイゴンの軽食','40 phút',4.3,'?','',''),(4,'Tour Đặc Sản','Bún cá Châu Đốc, Lãng Quán, Ớt Xiêm - đặc sản không thể bỏ qua','uploads/tour_27176e04fe80494789842dd165397529.jpg',1,'[6]','Tour Dac San','Tour Dac San','Tour Dac San','Chau Doc fish noodle soup, Lang Quan, Siamese chili - specialties not to be missed','朱笃鱼面汤、郎泉、暹罗辣椒——不容错过的特色菜','チャウドックのフィッシュヌードルスープ、ランクアン、シャムチリ - 見逃せない名物料理','50 phút',4.6,'⭐','','');
/*!40000 ALTER TABLE `tours` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `user_devices`
--

DROP TABLE IF EXISTS `user_devices`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user_devices` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `DeviceId` varchar(255) NOT NULL,
  `DeviceName` varchar(255) DEFAULT '',
  `Platform` varchar(50) DEFAULT '',
  `LastActiveAt` datetime DEFAULT CURRENT_TIMESTAMP,
  `RegisteredAt` datetime DEFAULT CURRENT_TIMESTAMP,
  `IsActive` tinyint(1) DEFAULT '1',
  PRIMARY KEY (`Id`),
  UNIQUE KEY `idx_user_device_unique` (`UserId`,`DeviceId`),
  KEY `idx_user_devices_user` (`UserId`),
  KEY `idx_user_devices_device` (`DeviceId`),
  CONSTRAINT `user_devices_ibfk_1` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `user_devices`
--

LOCK TABLES `user_devices` WRITE;
/*!40000 ALTER TABLE `user_devices` DISABLE KEYS */;
INSERT INTO `user_devices` VALUES (1,13,'Android_67fa30f4f1204d1f82aa23ac4040fc30','sdk_gphone_x86_64 (Android)','Android','2026-04-21 16:21:42','2026-04-21 16:16:41',1),(2,12,'Android_c5cf3bc60e144670969026146fb642d7','SM-A566B (Android)','Android','2026-04-21 16:21:44','2026-04-21 16:17:37',1);
/*!40000 ALTER TABLE `user_devices` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `user_live_locations`
--

DROP TABLE IF EXISTS `user_live_locations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user_live_locations` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `Latitude` double NOT NULL,
  `Longitude` double NOT NULL,
  `UpdatedAt` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `uk_userid` (`UserId`),
  KEY `idx_updated_at` (`UpdatedAt` DESC),
  CONSTRAINT `user_live_locations_ibfk_1` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=185 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `user_live_locations`
--

LOCK TABLES `user_live_locations` WRITE;
/*!40000 ALTER TABLE `user_live_locations` DISABLE KEYS */;
/*!40000 ALTER TABLE `user_live_locations` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `user_locations`
--

DROP TABLE IF EXISTS `user_locations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user_locations` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `LocationId` int NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UserId` (`UserId`,`LocationId`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `user_locations`
--

LOCK TABLES `user_locations` WRITE;
/*!40000 ALTER TABLE `user_locations` DISABLE KEYS */;
INSERT INTO `user_locations` VALUES (1,3,1),(2,4,4);
/*!40000 ALTER TABLE `user_locations` ENABLE KEYS */;
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
) ENGINE=InnoDB AUTO_INCREMENT=14 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES (1,'admin','$2a$11$RLxmRCA5jVdOb37AbTwG1utHCQpZsr2MogLXkY/DBsnOh1Kqp.Nym','Quản trị viên','admin','2026-03-31 23:02:46'),(3,'ocoanh','$2a$11$6Vy5r7SNuvDIdEHp11smeegdwpjZQjRNfxvc8R4nqZ3YxlB1BVOsm','Chủ quán Ốc Oanh','owner','2026-04-02 18:07:09'),(4,'langquan','$2a$11$w5OmhMSO7E58uEAd3WXfTe5cdTLdrJrat3WHArmK1vVE3MPxJb2Zy','Chủ quán Lãng Quán','owner','2026-04-02 18:07:09'),(12,'nhu1542005#','$2a$11$4o1AE1EsXBRXFr7uwhF/muKno4Jx2qSoZpUucRSQz5ODCNPBBMWP.','trinh tam nhu','user','2026-04-07 23:09:41'),(13,'tamnhu','$2a$11$H/PVyNxxEy5.y6ozlqgM1OdNP/llfcO0R7YJVUH7QuJ4.W6WeS6r2','nhu','user','2026-04-15 23:05:29');
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
  `Username` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
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

-- Dump completed on 2026-04-27 20:03:35

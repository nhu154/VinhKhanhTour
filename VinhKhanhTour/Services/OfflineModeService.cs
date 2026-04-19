using System.Text;
using System.Text.Json;
using VinhKhanhTour.Models;

namespace VinhKhanhTour.Services
{
    /// <summary>
    /// Quản lý chức năng offline:
    ///   1. Pre-cache audio MP3 (Google TTS) cho tất cả POI × tất cả ngôn ngữ
    ///   2. Pre-warm Google Maps tiles bằng cách tải ngầm URL tiles khu vực Vĩnh Khánh
    ///   3. Thống kê trạng thái cache để hiển thị trong OfflineDownloadPage
    /// </summary>
    public class OfflineModeService
    {
        // ── Singleton ──────────────────────────────────────────────────────────
        private static OfflineModeService? _instance;
        public static OfflineModeService Instance => _instance ??= new OfflineModeService();

        // ── Events ────────────────────────────────────────────────────────────
        /// <summary>Phát ra khi tiến độ tải audio thay đổi (0.0 → 1.0)</summary>
        public event Action<double>? AudioCacheProgressChanged;

        /// <summary>Phát ra khi tải xong tất cả audio</summary>
        public event Action? AudioCacheCompleted;

        /// <summary>Phát ra khi pre-warm map tiles xong</summary>
        public event Action? MapTilesWarmed;

        // ── State ─────────────────────────────────────────────────────────────
        public bool IsAudioCaching { get; private set; }
        public bool IsAudioCached { get; private set; }
        public int TotalAudioFiles { get; private set; }
        public int CachedAudioFiles { get; private set; }
        public bool IsMapTilesWarmed { get; private set; }

        private CancellationTokenSource? _audioCts;

        // ── Google TTS config (sao chép từ AudioService để pre-cache độc lập) ─
        private const string GOOGLE_TTS_API_KEY = "AIzaSyAMX0XgjmNv2O4Twk_CBBmjzDwopqtuexE";
        private const string GOOGLE_TTS_URL =
            "https://texttospeech.googleapis.com/v1/text:synthesize?key=" + GOOGLE_TTS_API_KEY;

        private static readonly JsonSerializerOptions _jsonOpts =
            new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private static readonly Dictionary<string, (string voice, string langCode, string gender)> VoiceMap = new()
        {
            ["vi"] = ("vi-VN-Wavenet-A", "vi-VN", "FEMALE"),
            ["en"] = ("en-US-Wavenet-F", "en-US", "FEMALE"),
            ["zh"] = ("cmn-CN-Wavenet-A", "cmn-CN", "FEMALE"),
            ["ja"] = ("ja-JP-Wavenet-D", "ja-JP", "FEMALE"),
            ["ko"] = ("ko-KR-Wavenet-A", "ko-KR", "FEMALE"),
            ["fr"] = ("fr-FR-Wavenet-C", "fr-FR", "FEMALE"),
            ["th"] = ("th-TH-Neural2-C", "th-TH", "FEMALE"),
        };

        // ── Khu vực Vĩnh Khánh để pre-warm Google Maps tiles ──────────────────
        // Bao phủ toàn bộ phố Vĩnh Khánh Quận 4, zoom 14-18
        private const double VK_LAT_MIN = 10.758;
        private const double VK_LAT_MAX = 10.766;
        private const double VK_LNG_MIN = 106.699;
        private const double VK_LNG_MAX = 106.710;

        private const string PREF_AUDIO_CACHED    = "offline_audio_cached";
        private const string PREF_MAP_WARMED      = "offline_map_warmed";
        private const string PREF_LAST_CACHE_DATE = "offline_last_cache_date";

        private OfflineModeService()
        {
            IsAudioCached  = Preferences.Default.Get(PREF_AUDIO_CACHED, false);
            IsMapTilesWarmed = Preferences.Default.Get(PREF_MAP_WARMED, false);
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  Pre-cache audio — tải Google TTS MP3 cho tất cả POI × tất cả ngôn ngữ
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tự động pre-cache nếu chưa cache lần nào và đang online.
        /// Gọi từ App.xaml.cs sau khi khởi động xong.
        /// </summary>
        public async Task AutoPreCacheIfNeededAsync()
        {
            if (!OfflineService.Instance.IsOnline) return;
            if (IsAudioCached && IsMapTilesWarmed) return;

            // Delay nhỏ để app khởi động xong
            await Task.Delay(3000);

            if (!IsAudioCached)
                _ = PreCacheAllAudioAsync();

            if (!IsMapTilesWarmed)
                _ = PreWarmMapTilesAsync();
        }

        /// <summary>
        /// Pre-cache tất cả audio cho POI. Gọi thủ công từ OfflineDownloadPage.
        /// </summary>
        public async Task PreCacheAllAudioAsync(CancellationToken externalToken = default)
        {
            if (IsAudioCaching) return;
            if (!OfflineService.Instance.IsOnline)
            {
                System.Diagnostics.Debug.WriteLine("[OfflineModeService] Offline — không thể pre-cache audio");
                return;
            }

            IsAudioCaching = true;
            _audioCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            var token = _audioCts.Token;

            try
            {
                var pois = await App.Database.GetRestaurantsAsync();
                var langs = await ApiService.Instance.GetLanguagesAsync();
                if (langs.Count == 0) langs = DefaultLanguages();

                // Danh sách công việc: mỗi cặp (POI, lang) là 1 file MP3
                var jobs = new List<(Restaurant poi, string lang, string script)>();
                foreach (var poi in pois)
                {
                    foreach (var lang in langs)
                    {
                        var script = GetScript(poi, lang.Code);
                        if (!string.IsNullOrWhiteSpace(script))
                            jobs.Add((poi, lang.Code, script));
                    }
                }

                TotalAudioFiles  = jobs.Count;
                CachedAudioFiles = 0;

                System.Diagnostics.Debug.WriteLine(
                    $"[OfflineModeService] Bắt đầu pre-cache {jobs.Count} audio files ({pois.Count} POI × {langs.Count} ngôn ngữ)");

                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };

                for (int i = 0; i < jobs.Count; i++)
                {
                    if (token.IsCancellationRequested) break;
                    var (poi, lang, script) = jobs[i];

                    try
                    {
                        await PreCacheSingleAsync(http, poi.Id, lang, script, token);
                        CachedAudioFiles++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[OfflineModeService] ⚠️ Lỗi cache POI {poi.Id} [{lang}]: {ex.Message}");
                    }

                    double progress = (double)(i + 1) / jobs.Count;
                    AudioCacheProgressChanged?.Invoke(progress);

                    // Delay nhỏ để không spam API
                    await Task.Delay(200, token).ContinueWith(_ => { });
                }

                if (!token.IsCancellationRequested)
                {
                    IsAudioCached = true;
                    Preferences.Default.Set(PREF_AUDIO_CACHED, true);
                    Preferences.Default.Set(PREF_LAST_CACHE_DATE, DateTime.Now.ToString("O"));
                    AudioCacheCompleted?.Invoke();
                    System.Diagnostics.Debug.WriteLine(
                        $"[OfflineModeService] ✅ Pre-cache audio hoàn thành: {CachedAudioFiles}/{TotalAudioFiles} files");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OfflineModeService] Pre-cache lỗi: {ex.Message}");
            }
            finally
            {
                IsAudioCaching = false;
                _audioCts?.Dispose();
                _audioCts = null;
            }
        }

        public void CancelPreCache()
        {
            _audioCts?.Cancel();
        }

        /// <summary>
        /// Tải 1 file MP3 TTS cho POI + ngôn ngữ. Lưu vào CacheDirectory.
        /// Cùng naming convention với AudioService để dùng chung cache.
        /// </summary>
        private async Task PreCacheSingleAsync(HttpClient http, int poiId, string lang, string script, CancellationToken token)
        {
            // Hash script để tạo tên file giống AudioService (dùng chung cache)
            byte[] hashBytes = System.Security.Cryptography.MD5.HashData(
                Encoding.UTF8.GetBytes(script + lang));
            string hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
            var filePath = Path.Combine(FileSystem.CacheDirectory, $"gtts_{poiId}_{lang}_{hash}.mp3");

            if (File.Exists(filePath))
            {
                System.Diagnostics.Debug.WriteLine($"[OfflineModeService] ✓ Đã có: POI {poiId} [{lang}]");
                return; // Đã cache
            }

            string voiceName, langCode, gender;
            if (VoiceMap.TryGetValue(lang, out var v))
                (voiceName, langCode, gender) = v;
            else
            {
                var upper = lang.Length == 2 ? lang.ToUpper() : "";
                langCode  = lang.Length == 2 ? $"{lang}-{upper}" : lang;
                voiceName = lang.Length == 2 ? $"{lang}-{upper}-Wavenet-A" : $"{lang}-Wavenet-A";
                gender    = "FEMALE";
            }

            var requestBody = new
            {
                input = new { text = script },
                voice = new { languageCode = langCode, name = voiceName, ssmlGender = gender },
                audioConfig = new { audioEncoding = "MP3", speakingRate = 0.95, pitch = 0.0 }
            };

            var json    = JsonSerializer.Serialize(requestBody, _jsonOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await http.PostAsync(GOOGLE_TTS_URL, content, token);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(token);
            using var doc = JsonDocument.Parse(responseJson);
            var audioBase64 = doc.RootElement.GetProperty("audioContent").GetString()
                ?? throw new Exception("audioContent trống");

            await File.WriteAllBytesAsync(filePath, Convert.FromBase64String(audioBase64), token);
            System.Diagnostics.Debug.WriteLine($"[OfflineModeService] ⬇️ Đã cache: POI {poiId} [{lang}] → {Path.GetFileName(filePath)}");
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  Pre-warm Google Maps tiles — tải ngầm tiles khu vực Vĩnh Khánh
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tải ngầm các tile URLs của Google Maps khu vực Vĩnh Khánh zoom 14-18.
        /// WebView sẽ dùng HTTP cache để trả kết quả offline sau này.
        /// Đây là "gợi ý" để pre-populate WebView cache; tile thực tế sẽ được cache
        /// lần đầu user mở bản đồ. Phương pháp này cache header và tile metadata.
        /// </summary>
        public async Task PreWarmMapTilesAsync()
        {
            if (!OfflineService.Instance.IsOnline) return;
            if (IsMapTilesWarmed) return;

            try
            {
                System.Diagnostics.Debug.WriteLine("[OfflineModeService] Bắt đầu pre-warm map tiles...");

                // Google Maps tiles URL pattern
                // Zoom 14-16 cho toàn khu vực, zoom 17-18 chỉ tải trung tâm
                var tilesToFetch = new List<(int zoom, int x, int y)>();

                for (int zoom = 14; zoom <= 18; zoom++)
                {
                    double latStep = zoom <= 16 ? 0.002 : 0.001;
                    double lngStep = zoom <= 16 ? 0.003 : 0.0015;

                    double latMin = zoom <= 16 ? VK_LAT_MIN : 10.760;
                    double latMax = zoom <= 16 ? VK_LAT_MAX : 10.764;
                    double lngMin = zoom <= 16 ? VK_LNG_MIN : 106.701;
                    double lngMax = zoom <= 16 ? VK_LNG_MAX : 106.708;

                    for (double lat = latMin; lat <= latMax; lat += latStep)
                    {
                        for (double lng = lngMin; lng <= lngMax; lng += lngStep)
                        {
                            var (x, y) = LatLngToTileXY(lat, lng, zoom);
                            if (!tilesToFetch.Any(t => t.zoom == zoom && t.x == x && t.y == y))
                                tilesToFetch.Add((zoom, x, y));
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine(
                    $"[OfflineModeService] Pre-warm {tilesToFetch.Count} Google Maps tiles...");

                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                int fetched = 0;

                foreach (var (zoom, x, y) in tilesToFetch)
                {
                    if (!OfflineService.Instance.IsOnline) break;

                    // Google Maps Static Tile URL (không cần API key cho roadmap tiles cơ bản)
                    // Dùng Google Maps tile endpoint để pre-populate WebView cache
                    var tileUrl = $"https://mt0.google.com/vt/lyrs=m&x={x}&y={y}&z={zoom}";
                    try
                    {
                        var response = await http.GetAsync(tileUrl);
                        if (response.IsSuccessStatusCode)
                            fetched++;
                    }
                    catch { /* Bỏ qua tile lỗi */ }

                    await Task.Delay(50); // Rate limit nhẹ
                }

                IsMapTilesWarmed = true;
                Preferences.Default.Set(PREF_MAP_WARMED, true);
                MapTilesWarmed?.Invoke();
                System.Diagnostics.Debug.WriteLine(
                    $"[OfflineModeService] ✅ Pre-warm map tiles xong: {fetched}/{tilesToFetch.Count} tiles");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OfflineModeService] Pre-warm lỗi: {ex.Message}");
            }
        }

        // ── Chuyển lat/lng sang tile coordinates (Slippy Map) ─────────────────
        private static (int x, int y) LatLngToTileXY(double lat, double lng, int zoom)
        {
            int n = 1 << zoom;
            int x = (int)Math.Floor((lng + 180.0) / 360.0 * n);
            double latRad = lat * Math.PI / 180.0;
            int y = (int)Math.Floor((1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * n);
            return (x, y);
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  Helpers
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>Lấy TTS script đúng ngôn ngữ từ POI</summary>
        private static string GetScript(Restaurant poi, string lang) => lang switch
        {
            "vi" => !string.IsNullOrWhiteSpace(poi.TtsScript) ? poi.TtsScript : poi.GetTtsScript(),
            "en" => !string.IsNullOrWhiteSpace(poi.TtsScriptEn) ? poi.TtsScriptEn : poi.GetTtsScript(),
            "zh" => !string.IsNullOrWhiteSpace(poi.TtsScriptZh) ? poi.TtsScriptZh : poi.GetTtsScript(),
            _ => GetDynamicScript(poi, lang)
        };

        private static string GetDynamicScript(Restaurant poi, string lang)
        {
            if (string.IsNullOrWhiteSpace(poi.Translations) || poi.Translations == "{}") return "";
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(poi.Translations);
                if (dict != null && dict.TryGetValue(lang, out var langData) &&
                    langData.TryGetValue("tts", out var tts) && !string.IsNullOrWhiteSpace(tts))
                    return tts;
            }
            catch { }
            return "";
        }

        private static List<AppLanguage> DefaultLanguages() =>
        [
            new AppLanguage { Code = "vi", Name = "Tiếng Việt", Flag = "🇻🇳" },
            new AppLanguage { Code = "en", Name = "English",    Flag = "🇺🇸" },
            new AppLanguage { Code = "zh", Name = "中文",         Flag = "🇨🇳" },
        ];

        // ── Thống kê cache ─────────────────────────────────────────────────────

        public CacheStats GetCacheStats()
        {
            var cacheDir = FileSystem.CacheDirectory;
            var mp3Files = Directory.Exists(cacheDir)
                ? Directory.GetFiles(cacheDir, "gtts_*.mp3")
                : [];

            long totalBytes = mp3Files.Sum(f =>
            {
                try { return new FileInfo(f).Length; } catch { return 0L; }
            });

            string lastCacheDate = Preferences.Default.Get(PREF_LAST_CACHE_DATE, "");
            DateTime? lastDate = null;
            if (!string.IsNullOrEmpty(lastCacheDate) &&
                DateTime.TryParse(lastCacheDate, out var d))
                lastDate = d;

            return new CacheStats
            {
                AudioFileCount  = mp3Files.Length,
                TotalBytes      = totalBytes,
                IsAudioReady    = IsAudioCached,
                IsMapReady      = IsMapTilesWarmed,
                LastCacheDate   = lastDate
            };
        }

        /// <summary>Xóa toàn bộ cache audio đã tải về</summary>
        public void ClearAudioCache()
        {
            var cacheDir = FileSystem.CacheDirectory;
            if (!Directory.Exists(cacheDir)) return;

            foreach (var f in Directory.GetFiles(cacheDir, "gtts_*.mp3"))
            {
                try { File.Delete(f); } catch { }
            }

            IsAudioCached = false;
            CachedAudioFiles = 0;
            Preferences.Default.Remove(PREF_AUDIO_CACHED);
            Preferences.Default.Remove(PREF_LAST_CACHE_DATE);
            System.Diagnostics.Debug.WriteLine("[OfflineModeService] 🗑️ Đã xóa cache audio");
        }
    }

    /// <summary>Thống kê trạng thái cache offline</summary>
    public class CacheStats
    {
        public int AudioFileCount  { get; set; }
        public long TotalBytes     { get; set; }
        public bool IsAudioReady   { get; set; }
        public bool IsMapReady     { get; set; }
        public DateTime? LastCacheDate { get; set; }

        public string TotalSizeDisplay => TotalBytes switch
        {
            0 => "0 KB",
            < 1024 * 1024 => $"{TotalBytes / 1024.0:F1} KB",
            _ => $"{TotalBytes / (1024.0 * 1024.0):F1} MB"
        };

        public string LastCacheDateDisplay => LastCacheDate.HasValue
            ? LastCacheDate.Value.ToString("dd/MM/yyyy HH:mm")
            : "Chưa tải";
    }
}

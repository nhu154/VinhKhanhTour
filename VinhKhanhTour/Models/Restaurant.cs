using SQLite;

namespace VinhKhanhTour.Models
{
    [Table("restaurants")]
    public class Restaurant
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Address { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public double Rating { get; set; }
        public string OpenHours { get; set; } = string.Empty;
        
        public bool IsFavorite { get; set; }

        // ── Thuyết minh tự động ────────────────────────────────────────────────

        /// <summary>
        /// Tên file audio trong Resources/Raw, ví dụ: "thuyet_minh_oc_oanh.mp3".
        /// Để trống nếu chỉ dùng TTS.
        /// </summary>
        public string AudioFile { get; set; } = string.Empty;

        /// <summary>
        /// Văn bản đọc bằng TTS tiếng Việt.
        /// </summary>
        public string TtsScript { get; set; } = string.Empty;
        
        /// <summary>
        /// Văn bản đọc bằng TTS tiếng Anh.
        /// </summary>
        public string TtsScriptEn { get; set; } = string.Empty;

        /// <summary>
        /// Văn bản đọc bằng TTS tiếng Trung.
        /// </summary>
        public string TtsScriptZh { get; set; } = string.Empty;

        /// <summary>
        /// Chuỗi JSON chứa Tên &amp; Bản thuyết minh của các ngôn ngữ tự định nghĩa (Nhật, Hàn, Pháp...).
        /// Định dạng: {"ja":{"name":"...","tts":"..."},"ko":{"name":"...","tts":"..."}}
        /// </summary>
        public string Translations { get; set; } = "{}";


        public string GetDescription(string lang)
        {
            if (lang == "en" && !string.IsNullOrWhiteSpace(TtsScriptEn)) return TtsScriptEn;
            if (lang == "zh" && !string.IsNullOrWhiteSpace(TtsScriptZh)) return TtsScriptZh;
            
            // Check dynamic langs
            if (!string.IsNullOrWhiteSpace(Translations) && Translations != "{}")
            {
                try {
                    var extraLangs = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>>(Translations);
                    if (extraLangs != null && extraLangs.TryGetValue(lang, out var langData)) {
                        if (langData.TryGetValue("tts", out var dynamicTts) && !string.IsNullOrWhiteSpace(dynamicTts)) return dynamicTts;
                    }
                } catch { }
            }
            return Description;
        }

        public string GetName(string lang)
        {
            if (lang == "en") return Name; // Should have NameEn in DB, but currently Name is unified
            
            // Check dynamic langs
            if (!string.IsNullOrWhiteSpace(Translations) && Translations != "{}")
            {
                try {
                    var extraLangs = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>>(Translations);
                    if (extraLangs != null && extraLangs.TryGetValue(lang, out var langData)) {
                        if (langData.TryGetValue("name", out var dynamicName) && !string.IsNullOrWhiteSpace(dynamicName)) return dynamicName;
                    }
                } catch { }
            }
            return Name;
        }

        // ── Helper ────────────────────────────────────────────────────────────

        /// <summary>
        /// Trả về script đọc TTS dựa trên ngôn ngữ hiện tại của AudioService.
        /// </summary>
        public string GetTtsScript()
        {
            string currentLang = VinhKhanhTour.Services.AudioService.Instance.CurrentLanguage;

            if (currentLang == "en" && !string.IsNullOrWhiteSpace(TtsScriptEn)) return TtsScriptEn;
            if (currentLang == "zh" && !string.IsNullOrWhiteSpace(TtsScriptZh)) return TtsScriptZh;
            if (currentLang == "vi" && !string.IsNullOrWhiteSpace(TtsScript)) return TtsScript;

            // Đọc ngôn ngữ động (Nhật, Hàn, Pháp...)
            if (!string.IsNullOrWhiteSpace(Translations) && Translations != "{}")
            {
                try
                {
                    var extraLangs = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>>(Translations);
                    if (extraLangs != null && extraLangs.TryGetValue(currentLang, out var langData))
                    {
                        if (langData.TryGetValue("tts", out var dynamicTts) && !string.IsNullOrWhiteSpace(dynamicTts))
                        {
                            return dynamicTts;
                        }
                    }
                }
                catch { /* Ignore JSON parse errors */ }
            }

            // Fallback (Vietnamese / Any)
            if (!string.IsNullOrWhiteSpace(TtsScript))
                return TtsScript;

            var parts = new List<string>();
            parts.Add($"Bạn đang đến gần {Name}.");
            if (!string.IsNullOrWhiteSpace(Description))
                parts.Add(Description);
            if (Rating > 0)
                parts.Add($"Được đánh giá {Rating} sao.");
            if (!string.IsNullOrWhiteSpace(OpenHours))
                parts.Add($"Giờ mở cửa: {OpenHours}.");
            return string.Join(" ", parts);
        }
    }
}
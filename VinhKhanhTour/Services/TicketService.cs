using System.Linq;
using VinhKhanhTour.Models;

namespace VinhKhanhTour.Services
{
    /// <summary>
    /// Quản lý vé premium và các tính năng được mở khoá.
    /// Dùng Preferences để lưu trạng thái qua các lần mở app.
    /// </summary>
    public class TicketService
    {
        private static TicketService? _instance;
        public static TicketService Instance => _instance ??= new TicketService();

        // ── Helpers: keys scoped per user ─────────────────────────
        private static string U() => UserSession.Instance.Username ?? "guest";

        private static string KEY_TICKET_TYPE   => $"ticket_type_{U()}";
        private static string KEY_TICKET_EXPIRY => $"ticket_expiry_{U()}";
        private static string KEY_TICKET_CODE   => $"ticket_code_{U()}";
        private static string KEY_BADGES_JSON   => $"user_badges_{U()}";
        private static string KEY_POINTS        => $"user_points_{U()}";
        private static string KEY_JOURNAL_JSON  => $"food_journal_{U()}";
        private static string KEY_OFFLINE_MAP   => $"offline_map_{U()}";

        private TicketService() { }

        // ── Trạng thái vé ──────────────────────────────────────────

        public string TicketType => Preferences.Get(KEY_TICKET_TYPE, "none"); // none | day | full

        public bool HasValidTicket
        {
            get
            {
                var type = TicketType;
                if (type == "none") return false;
                if (type == "full") return true;
                // day ticket: check expiry
                var expiryTicks = Preferences.Get(KEY_TICKET_EXPIRY, 0L);
                return expiryTicks > DateTime.Now.Ticks;
            }
        }

        public bool IsFullTicket => TicketType == "full";

        public DateTime? TicketExpiry
        {
            get
            {
                var ticks = Preferences.Get(KEY_TICKET_EXPIRY, 0L);
                return ticks > 0 ? new DateTime(ticks) : null;
            }
        }

        public string TicketCode => Preferences.Get(KEY_TICKET_CODE, "");

        // ── Mua vé (demo — không có payment gateway thật) ──────────

        /// <summary>Kích hoạt vé sau khi thanh toán thành công.</summary>
        public TicketInfo ActivateTicket(string ticketType)
        {
            var code = GenerateTicketCode(ticketType);
            var expiry = ticketType == "day" ? DateTime.Now.AddHours(24) : DateTime.MaxValue;

            Preferences.Set(KEY_TICKET_TYPE, ticketType);
            Preferences.Set(KEY_TICKET_EXPIRY, expiry.Ticks);
            Preferences.Set(KEY_TICKET_CODE, code);

            // System.Diagnostics.Debug.WriteLine($"[TicketService] Activated {ticketType} ticket: {code}");
            return new TicketInfo { Type = ticketType, Code = code, Expiry = expiry };
        }

        public void RevokeTicket()
        {
            Preferences.Remove(KEY_TICKET_TYPE);
            Preferences.Remove(KEY_TICKET_EXPIRY);
            Preferences.Remove(KEY_TICKET_CODE);
        }

        // ── Tính năng được unlock ──────────────────────────────────

        /// <summary>Audio guide chuyên sâu (người thật đọc)</summary>
        public bool CanAccessPremiumAudio => HasValidTicket;

        /// <summary>Bản đồ offline download</summary>
        public bool CanDownloadOfflineMap => HasValidTicket;

        /// <summary>Cẩm nang đầy đủ (menu giá, tip)</summary>
        public bool CanAccessFullGuide => HasValidTicket;

        /// <summary>AR/Photo frame kỷ niệm</summary>
        public bool CanAccessPhotoFrame => HasValidTicket;

        /// <summary>Tour cá nhân hóa</summary>
        public bool CanAccessPersonalTour => HasValidTicket;

        /// <summary>Xuất PDF nhật ký ẩm thực (chỉ full ticket)</summary>
        public bool CanExportJournal => IsFullTicket;

        public bool IsOfflineMapDownloaded => Preferences.Get(KEY_OFFLINE_MAP, false);

        public void SetOfflineMapDownloaded(bool value) =>
            Preferences.Set(KEY_OFFLINE_MAP, value);





        // ── Nhật ký ẩm thực ────────────────────────────────────────

        public List<JournalEntry> GetJournalEntries()
        {
            var json = Preferences.Get(KEY_JOURNAL_JSON, "[]");
            try { return System.Text.Json.JsonSerializer.Deserialize<List<JournalEntry>>(json) ?? new(); }
            catch { return new(); }
        }

        public void AddJournalEntry(JournalEntry entry)
        {
            var entries = GetJournalEntries();
            entries.Insert(0, entry); // mới nhất lên đầu
            if (entries.Count > 50) entries = entries.Take(50).ToList();
            Preferences.Set(KEY_JOURNAL_JSON, System.Text.Json.JsonSerializer.Serialize(entries));
            AddPoints(10);
        }

        // ── Helpers ────────────────────────────────────────────────

        private string GenerateTicketCode(string type)
        {
            var prefix = type == "full" ? "VKF" : "VKD";
            return $"{prefix}-{DateTime.Now:yyyyMMdd}-{new Random().Next(10000, 99999)}";
        }

        // ── Points & Badges ────────────────────────────────────────

        public int Points => Preferences.Get(KEY_POINTS, 0);

        public void AddPoints(int pts)
        {
            var current = Points;
            Preferences.Set(KEY_POINTS, current + pts);
        }

        public List<Badge> GetUnlockedBadges()
        {
            var json = Preferences.Get(KEY_BADGES_JSON, "[]");
            try { return System.Text.Json.JsonSerializer.Deserialize<List<Badge>>(json) ?? new(); }
            catch { return new(); }
        }

        public bool HasBadge(string badgeId)
        {
            return GetUnlockedBadges().Any(b => b.Id == badgeId);
        }

        public Badge? UnlockBadge(Badge badge)
        {
            if (HasBadge(badge.Id)) return null;

            var badges = GetUnlockedBadges();
            badges.Add(badge);
            Preferences.Set(KEY_BADGES_JSON, System.Text.Json.JsonSerializer.Serialize(badges));
            AddPoints(badge.Points);
            return badge;
        }
    }

    // ── Data classes ───────────────────────────────────────────────

    public class TicketInfo
    {
        public string Type { get; set; } = "none";
        public string Code { get; set; } = "";
        public DateTime Expiry { get; set; }

        public string TypeDisplay => Type switch
        {
            "day" => "🎫 Vé 1 ngày",
            "full" => "🏆 Vé trọn gói",
            _ => "Miễn phí"
        };

        public string PriceDisplay => Type switch
        {
            "day" => "29.000đ",
            "full" => "79.000đ",
            _ => "0đ"
        };
    }

    public class JournalEntry
    {
        public int RestaurantId { get; set; }
        public string RestaurantName { get; set; } = "";
        public string Note { get; set; } = "";
        public int Rating { get; set; } = 5;
        public string Emoji { get; set; } = "🍽️";
        public DateTime VisitedAt { get; set; } = DateTime.Now;
    }
}
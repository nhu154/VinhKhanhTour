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

            // Tặng điểm ban đầu khi mua vé
            AddPoints(ticketType == "full" ? 500 : 100);

            // Tặng badge "Người ủng hộ"
            UnlockBadge(BadgeDefinitions.Supporter);

            System.Diagnostics.Debug.WriteLine($"[TicketService] Activated {ticketType} ticket: {code}");
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

        // ── Điểm thưởng ────────────────────────────────────────────

        public int Points => Preferences.Get(KEY_POINTS, 0);

        public void AddPoints(int amount)
        {
            var current = Points;
            Preferences.Set(KEY_POINTS, current + amount);
        }

        // ── Huy hiệu ───────────────────────────────────────────────

        public List<BadgeInfo> GetUnlockedBadges()
        {
            var json = Preferences.Get(KEY_BADGES_JSON, "[]");
            try { return System.Text.Json.JsonSerializer.Deserialize<List<BadgeInfo>>(json) ?? new(); }
            catch { return new(); }
        }

        public bool HasBadge(string badgeId)
            => GetUnlockedBadges().Any(b => b.Id == badgeId);

        public BadgeInfo? UnlockBadge(BadgeInfo badge)
        {
            if (HasBadge(badge.Id)) return null; // đã có rồi
            var badges = GetUnlockedBadges();
            badge.UnlockedAt = DateTime.Now;
            badges.Add(badge);
            Preferences.Set(KEY_BADGES_JSON, System.Text.Json.JsonSerializer.Serialize(badges));
            AddPoints(badge.Points);
            System.Diagnostics.Debug.WriteLine($"[TicketService] Badge unlocked: {badge.Name}");
            return badge;
        }

        /// <summary>Check-in quán → có thể unlock badge mới</summary>
        public BadgeInfo? CheckInRestaurant(int restaurantId, int totalVisited)
        {
            AddPoints(20);

            // Badge theo số quán đã ghé
            if (totalVisited >= 1 && !HasBadge("first_visit"))
                return UnlockBadge(BadgeDefinitions.FirstVisit);
            if (totalVisited >= 3 && !HasBadge("explorer"))
                return UnlockBadge(BadgeDefinitions.Explorer);
            if (totalVisited >= 5 && !HasBadge("snail_king"))
                return UnlockBadge(BadgeDefinitions.SnailKing);
            if (totalVisited >= 8 && !HasBadge("foodie_pro"))
                return UnlockBadge(BadgeDefinitions.FoodiePro);
            if (totalVisited >= 10 && !HasBadge("vinh_khanh_legend"))
                return UnlockBadge(BadgeDefinitions.VinhKhanhLegend);

            return null;
        }

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

    public class BadgeInfo
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Emoji { get; set; } = "🏅";
        public string Color { get; set; } = "#1565C0";
        public int Points { get; set; } = 50;
        public DateTime UnlockedAt { get; set; }
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

    // ── Badge Definitions ──────────────────────────────────────────

    public static class BadgeDefinitions
    {
        public static BadgeInfo Supporter => new() { Id = "supporter", Name = "Người ủng hộ", Description = "Đã mua vé trải nghiệm", Emoji = "💎", Color = "#9C27B0", Points = 100 };
        public static BadgeInfo FirstVisit => new() { Id = "first_visit", Name = "Khởi đầu", Description = "Ghé thăm quán đầu tiên", Emoji = "🌟", Color = "#FF9800", Points = 50 };
        public static BadgeInfo Explorer => new() { Id = "explorer", Name = "Nhà thám hiểm", Description = "Đã ghé 3 quán", Emoji = "🗺️", Color = "#2196F3", Points = 80 };
        public static BadgeInfo SnailKing => new() { Id = "snail_king", Name = "Vua Ốc", Description = "Đã ghé 5 quán ốc", Emoji = "🐚", Color = "#4CAF50", Points = 120 };
        public static BadgeInfo FoodiePro => new() { Id = "foodie_pro", Name = "Foodie Pro", Description = "Đã ghé 8 quán khác nhau", Emoji = "👨‍🍳", Color = "#E91E63", Points = 150 };
        public static BadgeInfo VinhKhanhLegend => new() { Id = "vinh_khanh_legend", Name = "Huyền thoại VK", Description = "Đã ghé tất cả 10 quán", Emoji = "🏆", Color = "#FF5722", Points = 300 };
        public static BadgeInfo NightOwl => new() { Id = "night_owl", Name = "Cú đêm", Description = "Check-in sau 10 giờ tối", Emoji = "🦉", Color = "#3F51B5", Points = 60 };
        public static BadgeInfo Photographer => new() { Id = "photographer", Name = "Nhiếp ảnh gia", Description = "Lưu ảnh kỷ niệm", Emoji = "📸", Color = "#00BCD4", Points = 70 };
        public static BadgeInfo Reviewer => new() { Id = "reviewer", Name = "Nhà phê bình", Description = "Viết 5 nhật ký ẩm thực", Emoji = "✍️", Color = "#607D8B", Points = 90 };

        public static List<BadgeInfo> All => new()
        {
            Supporter, FirstVisit, Explorer, SnailKing, FoodiePro,
            VinhKhanhLegend, NightOwl, Photographer, Reviewer
        };
    }
}
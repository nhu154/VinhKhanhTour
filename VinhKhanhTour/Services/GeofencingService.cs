using VinhKhanhTour.Models;

namespace VinhKhanhTour.Services
{
    public class GeofencingService
    {
        private const double EARTH_RADIUS_METERS = 6371000;

        // ── Bán kính vào/ra ───────────────────────────────────────────
        private const double ENTER_RADIUS_METERS = 5;    // Vào trong 5m → trigger audio
        private const double EXIT_RADIUS_METERS = 20;   // Ra ngoài 20m → reset, cho phép phát lại

        // ── Cooldown chỉ áp dụng khi đã nghe rồi quay lại liền ──────
        private const int COOLDOWN_SECONDS = 30; // Chờ 30s trước khi báo "đã nghe rồi"

        // ── Trạng thái từng POI ───────────────────────────────────────
        private readonly Dictionary<int, DateTime> _lastTriggered = [];
        private readonly HashSet<int> _insideGeofence = []; // POI user đang ở trong

        public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EARTH_RADIUS_METERS * c;
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

        /// <summary>
        /// Trả về POI cần phát audio (nếu có), null nếu không cần phát.
        /// Logic:
        ///   - Vào trong 5m  → phát (lần đầu hoặc sau khi đã ra ngoài 20m)
        ///   - Ra ngoài 20m  → reset trạng thái, cho phép phát lại khi vào lại
        ///   - Đang trong 5–20m → không làm gì (vùng đệm)
        /// </summary>
        public async Task<Restaurant?> CheckNearbyRestaurant(double userLat, double userLon)
        {
            var restaurants = await App.Database.GetRestaurantsAsync();

            // ── Bước 1: Kiểm tra POI nào user đã ra ngoài 20m → reset ──
            foreach (var r in restaurants)
            {
                if (!_insideGeofence.Contains(r.Id)) continue;

                double dist = CalculateDistance(userLat, userLon, r.Latitude, r.Longitude);
                if (dist > EXIT_RADIUS_METERS)
                {
                    _insideGeofence.Remove(r.Id);
                    System.Diagnostics.Debug.WriteLine(
                        $"[Geofencing] Ra khỏi {r.Name} ({dist:F1}m > {EXIT_RADIUS_METERS}m) → reset");
                }
            }

            // ── Bước 2: Tìm POI gần nhất trong bán kính 5m chưa được trigger ──
            Restaurant? nearest = null;
            double minDist = double.MaxValue;

            foreach (var r in restaurants)
            {
                double dist = CalculateDistance(userLat, userLon, r.Latitude, r.Longitude);

                if (dist > ENTER_RADIUS_METERS) continue;          // Chưa đủ gần (> 5m)
                if (_insideGeofence.Contains(r.Id)) continue;      // Đang ở trong rồi, không trigger lại

                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = r;
                }
            }

            if (nearest == null) return null;

            // ── Bước 3: Kiểm tra cooldown (đã nghe gần đây chưa) ──────
            bool alreadyHeard = _lastTriggered.TryGetValue(nearest.Id, out var lastTime)
                                && (DateTime.Now - lastTime).TotalSeconds < COOLDOWN_SECONDS;

            // Đánh dấu user đang ở trong geofence này
            _insideGeofence.Add(nearest.Id);

            if (alreadyHeard)
            {
                // Thông báo "đã nghe rồi" → trả về null nhưng bắn event riêng
                AlreadyHeardTriggered?.Invoke(nearest);
                System.Diagnostics.Debug.WriteLine(
                    $"[Geofencing] {nearest.Name} đã nghe {(DateTime.Now - lastTime).TotalSeconds:F0}s trước → báo đã nghe");
                return null;
            }

            // Lần đầu hoặc đã ra ngoài đủ lâu → phát audio
            _lastTriggered[nearest.Id] = DateTime.Now;
            System.Diagnostics.Debug.WriteLine(
                $"[Geofencing] Trigger audio: {nearest.Name} ({minDist:F1}m)");
            return nearest;
        }

        /// <summary>
        /// Bắn khi user vào lại POI đã nghe trước đó (trong cooldown).
        /// UI lắng nghe event này để hiện thông báo "Đã nghe rồi – Phát lại?"
        /// </summary>
        public event Action<Restaurant>? AlreadyHeardTriggered;
    }
}
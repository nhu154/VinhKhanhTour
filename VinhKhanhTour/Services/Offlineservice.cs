using VinhKhanhTour.Models;

namespace VinhKhanhTour.Services
{
    /// <summary>
    /// Quản lý trạng thái mạng và đồng bộ dữ liệu offline → online.
    ///
    /// THAY ĐỔI:
    ///   - Thêm IsApiReachable: phân biệt "có WiFi nhưng server down" vs "mất mạng hoàn toàn"
    ///   - Thêm RefreshAsync(): để UI gọi thủ công khi cần làm mới dữ liệu
    ///   - Thêm ConnectivityStatusChanged event trả về ConnectivityStatus thay vì bool đơn giản
    ///   - Khi online trở lại: tự sync bookings + analytics + làm mới danh sách nhà hàng
    /// </summary>
    public class OfflineService
    {
        private static OfflineService? _instance;
        public static OfflineService Instance => _instance ??= new OfflineService();

        // ── Trạng thái mạng ───────────────────────────────────────────────────

        /// <summary>Có kết nối internet (theo hệ thống)</summary>
        public bool IsOnline { get; private set; }

        /// <summary>API server có phản hồi không (ping thực tế)</summary>
        public bool IsApiReachable { get; private set; }

        /// <summary>Phát ra trạng thái kết nối mỗi khi thay đổi</summary>
        public event Action<ConnectivityStatus>? StatusChanged;

        // Cache danh sách nhà hàng mới nhất (để các trang dùng lại không cần gọi lại)
        public event Action<List<Restaurant>>? RestaurantsRefreshed;

        private OfflineService()
        {
            IsOnline = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
            IsApiReachable = false; // chưa ping, giả định false cho an toàn
            Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
        }

        // ── Kết nối thay đổi ─────────────────────────────────────────────────

        private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
        {
            bool wasOnline = IsOnline;
            IsOnline = e.NetworkAccess == NetworkAccess.Internet;

            System.Diagnostics.Debug.WriteLine(
                $"[OfflineService] Network: {(IsOnline ? "ONLINE" : "OFFLINE")}");

            if (!IsOnline)
            {
                IsApiReachable = false;
                StatusChanged?.Invoke(ConnectivityStatus.Offline);
                return;
            }

            // Vừa online: ping API để xác nhận
            IsApiReachable = await ApiService.Instance.PingAsync();
            StatusChanged?.Invoke(IsApiReachable
                ? ConnectivityStatus.OnlineApiReachable
                : ConnectivityStatus.OnlineApiUnreachable);

            if (!wasOnline && IsOnline && IsApiReachable)
            {
                // Vừa có mạng lại → sync + refresh
                await SyncPendingBookingsAsync();
                await SyncPendingAnalyticsAsync();
                await RefreshRestaurantsAsync();
            }
        }

        // ── Ping thủ công ─────────────────────────────────────────────────────

        /// <summary>
        /// Kiểm tra lại kết nối thủ công.
        /// UI gọi khi người dùng bấm "Thử lại" hoặc khi pull-to-refresh.
        /// </summary>
        public async Task<ConnectivityStatus> CheckConnectivityAsync()
        {
            IsOnline = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
            if (!IsOnline)
            {
                IsApiReachable = false;
                return ConnectivityStatus.Offline;
            }

            IsApiReachable = await ApiService.Instance.PingAsync();
            var status = IsApiReachable
                ? ConnectivityStatus.OnlineApiReachable
                : ConnectivityStatus.OnlineApiUnreachable;

            StatusChanged?.Invoke(status);
            return status;
        }

        // ── Đồng bộ Bookings ─────────────────────────────────────────────────

        public async Task SyncPendingBookingsAsync()
        {
            if (!IsOnline) return;

            try
            {
                var pending = await App.Database.GetPendingBookingsAsync();
                if (pending.Count == 0) return;

                System.Diagnostics.Debug.WriteLine(
                    $"[OfflineService] Syncing {pending.Count} pending bookings...");

                foreach (var booking in pending)
                {
                    bool ok = await ApiService.Instance.PostBookingAsync(booking);
                    if (ok)
                    {
                        booking.SyncStatus = "synced";
                        await App.Database.UpdateBookingAsync(booking);
                        System.Diagnostics.Debug.WriteLine(
                            $"[OfflineService] ✅ Synced booking {booking.BookingCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OfflineService] SyncBookings: {ex.Message}");
            }
        }

        // ── Đồng bộ Analytics ────────────────────────────────────────────────

        public async Task SyncPendingAnalyticsAsync()
        {
            if (!IsOnline) return;

            try
            {
                var events = await App.Database.GetAllAnalyticsEventsAsync();
                var unsent = events.Where(e => !e.IsSynced).ToList();
                if (unsent.Count == 0) return;

                System.Diagnostics.Debug.WriteLine(
                    $"[OfflineService] Syncing {unsent.Count} analytics events...");

                foreach (var evt in unsent)
                {
                    bool ok = await ApiService.Instance.PostAnalyticAsync(
                        evt.PoiId, evt.EventType, evt.Lat, evt.Lng);
                    if (ok)
                    {
                        evt.IsSynced = true;
                        await App.Database.UpdateAnalyticsEventAsync(evt);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OfflineService] SyncAnalytics: {ex.Message}");
            }
        }

        // ── Làm mới danh sách nhà hàng ───────────────────────────────────────

        /// <summary>
        /// Lấy danh sách nhà hàng từ API và cập nhật cache SQLite.
        /// Nếu thành công, phát sự kiện RestaurantsRefreshed.
        /// </summary>
        public async Task RefreshRestaurantsAsync()
        {
            if (!IsOnline) return;

            try
            {
                var apiList = await ApiService.Instance.GetRestaurantsAsync();
                if (apiList.Count == 0) return;

                // Cập nhật cache
                var oldList = await App.Database.GetRestaurantsAsync();
                foreach (var old in oldList)
                    await App.Database.DeleteRestaurantAsync(old.Id);
                foreach (var r in apiList)
                    await App.Database.SaveRestaurantAsync(r);

                System.Diagnostics.Debug.WriteLine(
                    $"[OfflineService] Refreshed {apiList.Count} restaurants from API");

                RestaurantsRefreshed?.Invoke(apiList);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OfflineService] RefreshRestaurants: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy nhà hàng từ API (nếu online) hoặc SQLite cache (nếu offline).
        /// Trả về (danh sách, isFromCache).
        /// </summary>
        public async Task<(List<Restaurant> Data, bool FromCache)> GetRestaurantsWithFallbackAsync()
        {
            if (IsOnline)
            {
                try
                {
                    var apiList = await ApiService.Instance.GetRestaurantsAsync();
                    if (apiList.Count > 0)
                    {
                        // Cập nhật cache
                        var oldList = await App.Database.GetRestaurantsAsync();
                        foreach (var old in oldList)
                            await App.Database.DeleteRestaurantAsync(old.Id);
                        foreach (var r in apiList)
                            await App.Database.SaveRestaurantAsync(r);

                        return (apiList, false);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[OfflineService] API failed, using cache: {ex.Message}");
                }
            }

            var cached = await App.Database.GetRestaurantsAsync();
            return (cached, true);
        }

        public void Dispose()
        {
            Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
        }
    }

    public enum ConnectivityStatus
    {
        /// <summary>Không có internet</summary>
        Offline,
        /// <summary>Có internet, API phản hồi bình thường</summary>
        OnlineApiReachable,
        /// <summary>Có internet nhưng server không phản hồi (server down / sai IP)</summary>
        OnlineApiUnreachable
    }
}
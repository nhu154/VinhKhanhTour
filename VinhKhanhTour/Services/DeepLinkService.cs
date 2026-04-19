// ============================================================
//  Services/DeepLinkService.cs
//  Singleton xử lý toàn bộ logic Deep Link / QR Code
//  Hỗ trợ 2 scheme:
//    • vinhkhanhtour://poi/{id}          (App Scheme)
//    • https://vinhkhanhtour.com/poi/{id} (Universal Link)
// ============================================================

using VinhKhanhTour.Views;

namespace VinhKhanhTour.Services
{
    public class DeepLinkService
    {
        // ── Singleton ────────────────────────────────────────────────
        private static DeepLinkService? _instance;
        public static DeepLinkService Instance => _instance ??= new DeepLinkService();
        private DeepLinkService() { }

        // ── Sự kiện thông báo cho UI ─────────────────────────────────
        /// <summary>
        /// Phát sự kiện khi nhận được Deep Link hợp lệ.
        /// Tham số: (poiId, autoplay)
        /// </summary>
        public event Action<int, bool>? OnDeepLinkReceived;

        // ── Lưu tạm link khi app chưa kịp khởi động xong ────────────
        private Uri? _pendingUri;

        /// <summary>
        /// Được gọi từ MainActivity (Android) hoặc AppDelegate (iOS).
        /// Nếu UI chưa sẵn sàng thì lưu lại để xử lý sau.
        /// </summary>
        public void Process(Uri uri)
        {
            System.Diagnostics.Debug.WriteLine($"[DeepLink] Received: {uri}");

            // Handle legacy guest login deep link: vinhkhanhtour://open/guest
            if (uri.Host?.ToLower() == "open" && uri.AbsolutePath?.Trim('/').ToLower() == "guest")
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        UserSession.Instance.LoginAsGuest();
                        if (Application.Current?.MainPage is NavigationPage nav)
                        {
                            var mainPage = new MainTabbedPage();
                            await nav.PushAsync(mainPage);
                        }
                        else if (Application.Current != null)
                        {
                            Application.Current.MainPage = new NavigationPage(new MainTabbedPage());
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DeepLink] Navigation error: {ex.Message}");
                    }
                });
                return;
            }

            if (OnDeepLinkReceived == null)
            {
                // UI chưa đăng ký lắng nghe → lưu tạm
                _pendingUri = uri;
                System.Diagnostics.Debug.WriteLine("[DeepLink] Pending (UI not ready yet)");
                return;
            }

            ParseAndDispatch(uri);
        }

        /// <summary>
        /// Gọi từ MainTabbedPage.OnAppearing() để xử lý link còn tồn đọng
        /// (trường hợp app vừa khởi động lạnh từ QR).
        /// </summary>
        public void FlushPending()
        {
            if (_pendingUri == null) return;
            var uri = _pendingUri;
            _pendingUri = null;
            System.Diagnostics.Debug.WriteLine($"[DeepLink] Flushing pending: {uri}");
            ParseAndDispatch(uri);
        }

        // ── Logic bóc tách URL ────────────────────────────────────────
        private void ParseAndDispatch(Uri uri)
        {
            // Hỗ trợ 2 dạng URL:
            // vinhkhanhtour://poi/5
            // https://vinhkhanhtour.com/poi/5?autoplay=true
            try
            {
                var segments = uri.AbsolutePath
                    .Split('/', StringSplitOptions.RemoveEmptyEntries);

                // Tìm "poi" trong path rồi lấy số ngay sau nó
                int poiIndex = Array.FindIndex(segments,
                    s => s.Equals("poi", StringComparison.OrdinalIgnoreCase));

                // Nếu scheme là custom scheme và host là poi (vinhkhanhtour://poi/5)
                if (uri.Scheme.ToLower() == "vinhkhanhtour" && uri.Host.ToLower() == "poi")
                {
                    if (segments.Length > 0 && int.TryParse(segments[0], out int id))
                    {
                        DispatchPoi(uri, id);
                        return;
                    }
                }

                if (poiIndex < 0 || poiIndex + 1 >= segments.Length)
                {
                    System.Diagnostics.Debug.WriteLine("[DeepLink] ❌ Cannot find /poi/{id} in URL");
                    return;
                }

                if (!int.TryParse(segments[poiIndex + 1], out int poiId) || poiId <= 0)
                {
                    System.Diagnostics.Debug.WriteLine("[DeepLink] ❌ Invalid POI id");
                    return;
                }

                DispatchPoi(uri, poiId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeepLink] Parse error: {ex.Message}");
            }
        }

        private void DispatchPoi(Uri uri, int poiId)
        {
            // Đọc query param ?autoplay=true
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            bool autoplay = string.Equals(query["autoplay"], "true",
                StringComparison.OrdinalIgnoreCase);

            System.Diagnostics.Debug.WriteLine(
                $"[DeepLink] ✅ POI={poiId}  autoplay={autoplay}");

            // Phát sự kiện lên UI thread
            MainThread.BeginInvokeOnMainThread(() =>
                OnDeepLinkReceived?.Invoke(poiId, autoplay));
        }
    }
}

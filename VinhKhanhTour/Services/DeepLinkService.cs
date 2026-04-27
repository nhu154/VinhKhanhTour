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
        /// Xử lý deep link nếu app được mở bởi QR khi đang tắt (cold start).
        /// </summary>
        public void ProcessPendingIfAny() => FlushPending();

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
        /// <summary>
        /// Thử giải mã nội dung QR/DeepLink để lấy POI ID và trạng thái autoplay.
        /// Chấp nhận cả custom scheme và URL redirect từ Web.
        /// </summary>
        public static (int poiId, bool autoplay) TryParsePoiLink(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return (0, false);

            try
            {
                // Nếu là custom scheme trực tiếp: vinhkhanhtour://poi/5
                if (value.StartsWith("vinhkhanhtour://", StringComparison.OrdinalIgnoreCase))
                {
                    if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
                    {
                        var segments = uri.AbsolutePath.Trim('/').Split('/');
                        if (uri.Host.Equals("poi", StringComparison.OrdinalIgnoreCase) && 
                            segments.Length > 0 && int.TryParse(segments[0], out int id))
                        {
                            return (id, IsAutoplay(uri));
                        }
                    }
                }

                // Nếu là URL Web (Redirect hoặc POI page)
                if (value.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
                    {
                        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                        
                        // Case 1: go.html?poi=5
                        if (int.TryParse(query["poi"], out int poiId)) return (poiId, IsAutoplay(uri));
                        
                        // Case 2: poi.html?id=5 (Legacy)
                        if (int.TryParse(query["id"], out int id2)) return (id2, IsAutoplay(uri));

                        // Case 3: vinhkhanhtour.com/poi/5
                        var segments = uri.AbsolutePath.Trim('/').Split('/');
                        int poiIdx = Array.FindIndex(segments, s => s.Equals("poi", StringComparison.OrdinalIgnoreCase));
                        if (poiIdx >= 0 && poiIdx + 1 < segments.Length && int.TryParse(segments[poiIdx + 1], out int id3))
                        {
                            return (id3, IsAutoplay(uri));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeepLink] TryParsePoiLink error: {ex.Message}");
            }

            return (0, false);
        }

        private static bool IsAutoplay(Uri uri)
        {
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            return string.Equals(query["autoplay"], "true", StringComparison.OrdinalIgnoreCase);
        }

        private void ParseAndDispatch(Uri uri)
        {
            var (poiId, autoplay) = TryParsePoiLink(uri.ToString());
            if (poiId > 0)
            {
                DispatchPoi(uri, poiId);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[DeepLink] ❌ Cannot parse POI from: {uri}");
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

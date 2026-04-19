using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Webkit;
using VinhKhanhTour.Platforms.Android;
using VinhKhanhTour.Services;
using VinhKhanhTour.Views;
using System.Runtime.Versioning;

namespace VinhKhanhTour
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation |
        ConfigChanges.UiMode | ConfigChanges.ScreenLayout |
        ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]

    // ── Deep link: vinhkhanhtour://open/guest ──────────────────────────
    [IntentFilter(new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "vinhkhanhtour")]
    // ── Universal link: https://vinhkhanhtour.com/poi/{id} ─────────────
    [IntentFilter(new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "https",
        DataHost = "vinhkhanhtour.com")]
    public class MainActivity : MauiAppCompatActivity
    {
        private Intent? _serviceIntent;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // ── Xử lý deep link khi app được mở lần đầu từ QR ─────────
            HandleDeepLinkIntent(Intent);

            // ── WebView settings ──────────────────────────────────────
            Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping(
                "WebViewGeoPermission", (handler, view) =>
                {
                    var settings = handler.PlatformView.Settings;
                    handler.PlatformView.SetWebChromeClient(new GeoWebChromeClient());
                    settings.JavaScriptEnabled = true;
                    settings.SetGeolocationEnabled(true);
                    var geoPath = handler.PlatformView.Context?.GetExternalFilesDir(null)?.AbsolutePath;
                    if (geoPath != null) settings.SetGeolocationDatabasePath(geoPath);
                    settings.MixedContentMode = MixedContentHandling.AlwaysAllow;
                    settings.AllowFileAccessFromFileURLs = true;
                    settings.AllowUniversalAccessFromFileURLs = true;
                    settings.LoadsImagesAutomatically = true;

                    // ── Offline tile cache: dùng Default thay vì NoCache ─────
                    // NoCache ngăn Android WebView lưu Google Maps tiles vào disk.
                    // Default = cho phép cache → tiles đã xem lúc online sẽ dùng được lúc offline.
                    // OfflineService sẽ switch sang CacheOnly khi phát hiện mất mạng.
                    bool isOnline = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
                    settings.CacheMode = isOnline ? CacheModes.Default : CacheModes.CacheElseNetwork;

                    // Lắng nghe thay đổi kết nối → cập nhật cache mode cho WebView này
                    Connectivity.Current.ConnectivityChanged += (s, e) =>
                    {
                        bool online = e.NetworkAccess == NetworkAccess.Internet;
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            try
                            {
                                handler.PlatformView.Settings.CacheMode =
                                    online ? CacheModes.Default : CacheModes.CacheElseNetwork;
                                System.Diagnostics.Debug.WriteLine(
                                    $"[WebView] Cache mode → {(online ? "Default (online)" : "CacheElseNetwork (offline)")}");
                            }
                            catch { }
                        });
                    };
                });
        }

        // ── Nhận deep link khi app đang chạy nền ─────────────────────
        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            HandleDeepLinkIntent(intent);
        }

        private static void HandleDeepLinkIntent(Intent? intent)
        {
            if (intent?.Action != Intent.ActionView) return;
            var uriString = intent.DataString;
            if (string.IsNullOrEmpty(uriString)) return;
            if (Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
                DeepLinkService.Instance.Process(uri);
        }

        protected override void OnResume()
        {
            base.OnResume();
            StartLocationService();
        }

        protected override void OnDestroy()
        {
            StopLocationService();
            base.OnDestroy();
        }

        // ── Foreground Service control ────────────────────────────────

        [SupportedOSPlatform("android26.0")]
        private void StartLocationService()
        {
            if (_serviceIntent != null) return; // đã start rồi

            // Android 14+ đòi hỏi quyền Location phải được granted trước khi bật Foreground Service.
            if (AndroidX.Core.Content.ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.AccessFineLocation) != Permission.Granted &&
                AndroidX.Core.Content.ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.AccessCoarseLocation) != Permission.Granted)
            {
                return;
            }

            _serviceIntent = new Intent(this, typeof(LocationForegroundService));

            try 
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                    StartForegroundService(_serviceIntent);
                else
                    StartService(_serviceIntent);
            }
            catch (Java.Lang.SecurityException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Security] Lỗi permission FGS: {ex.Message}");
                _serviceIntent = null;
            }
        }

        private void StopLocationService()
        {
            if (_serviceIntent == null) return;
            try { StopService(_serviceIntent); } catch {}
            _serviceIntent = null;
        }
    }

    // ── WebChromeClient cấp quyền geolocation cho WebView ────────────
    public class GeoWebChromeClient : WebChromeClient
    {
        public override void OnGeolocationPermissionsShowPrompt(
            string? origin,
            GeolocationPermissions.ICallback? callback)
        {
            callback?.Invoke(origin, true, false);
        }
    }
}
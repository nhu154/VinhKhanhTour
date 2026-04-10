using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Webkit;
using VinhKhanhTour.Platforms.Android;

namespace VinhKhanhTour
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation |
        ConfigChanges.UiMode | ConfigChanges.ScreenLayout |
        ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private Intent? _serviceIntent;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // ── WebView settings (giữ nguyên cấu hình cũ) ────────────
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
                    settings.CacheMode = CacheModes.NoCache;
                });
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
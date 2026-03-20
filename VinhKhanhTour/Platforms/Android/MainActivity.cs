using Android.App;
using Android.Content.PM;
using Android.Webkit;
using Android.OS;

namespace VinhKhanhTour
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation |
        ConfigChanges.UiMode | ConfigChanges.ScreenLayout |
        ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // ✅ Cấp quyền geolocation cho WebView
            Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping(
                "WebViewGeoPermission", (handler, view) =>
                {
                    handler.PlatformView.SetWebChromeClient(new GeoWebChromeClient());
                    handler.PlatformView.Settings.JavaScriptEnabled = true;
                    handler.PlatformView.Settings.SetGeolocationEnabled(true);
                    handler.PlatformView.Settings.SetGeolocationDatabasePath(
                        handler.PlatformView.Context?.GetExternalFilesDir(null)?.AbsolutePath);
                });
        }
    }

    // Chrome client xử lý permission request từ JS
    public class GeoWebChromeClient : Android.Webkit.WebChromeClient
    {
        public override void OnGeolocationPermissionsShowPrompt(
            string? origin,
            GeolocationPermissions.ICallback? callback)
        {
            // Tự động cấp quyền cho tất cả origin
            callback?.Invoke(origin, true, false);
        }
    }
}
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

            Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping(
                "WebViewGeoPermission", (handler, view) =>
                {
                    var settings = handler.PlatformView.Settings;

                    // Geolocation
                    handler.PlatformView.SetWebChromeClient(new GeoWebChromeClient());
                    settings.JavaScriptEnabled = true;
                    settings.SetGeolocationEnabled(true);
                    var geoPath = handler.PlatformView.Context?.GetExternalFilesDir(null)?.AbsolutePath;
                    if (geoPath != null) settings.SetGeolocationDatabasePath(geoPath);

                    // ✅ Cho phép load ảnh HTTP từ file:// context (fix ảnh bị đen)
                    settings.MixedContentMode = MixedContentHandling.AlwaysAllow;

                    // ✅ Cho phép load resource từ file URL
                    settings.AllowFileAccessFromFileURLs = true;
                    settings.AllowUniversalAccessFromFileURLs = true;

                    // ✅ Cho phép load ảnh
                    settings.LoadsImagesAutomatically = true;

                    // ✅ Tắt cache để ảnh mới nhất luôn được load
                    settings.CacheMode = CacheModes.NoCache;
                });
        }
    }

    public class GeoWebChromeClient : Android.Webkit.WebChromeClient
    {
        public override void OnGeolocationPermissionsShowPrompt(
            string? origin,
            GeolocationPermissions.ICallback? callback)
        {
            callback?.Invoke(origin, true, false);
        }
    }
}
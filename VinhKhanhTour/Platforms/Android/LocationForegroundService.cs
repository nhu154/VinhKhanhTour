using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using VinhKhanhTour.Services;
using System.Runtime.Versioning;
using AndroidLocation = global::Android.Locations.Location;
using AndroidLocationManager = global::Android.Locations.LocationManager;
using IAndroidLocationListener = global::Android.Locations.ILocationListener;
using AndroidAvailability = global::Android.Locations.Availability;

namespace VinhKhanhTour.Platforms.Android
{
    /// <summary>
    /// Android Foreground Service giữ GPS hoạt động khi màn hình tắt.
    /// Gọi GeofencingService để kiểm tra POI gần nhất và phát thuyết minh.
    /// </summary>
    [Service(Name = "vinhkhanhtour.platforms.android.LocationForegroundService", ForegroundServiceType = ForegroundService.TypeLocation)]
    [SupportedOSPlatform("android21.0")]
    public class LocationForegroundService : Service, IAndroidLocationListener
    {
        // ── Constants ────────────────────────────────────────────────
        private const string CHANNEL_ID = "vkt_location_channel";
        private const int NOTIFICATION_ID = 1001;
        private const long MIN_TIME_MS = 3000;   // cập nhật tối thiểu mỗi 3 giây
        private const float MIN_DISTANCE_M = 5f;     // cập nhật khi di chuyển >= 5m

        // ── Fields ───────────────────────────────────────────────────
        private AndroidLocationManager? _locationManager;
        private GeofencingService _geofencing = new GeofencingService();
        private AudioService _audio = new AudioService();
        private bool _isRunning = false;

        // ── Binder (không cần bind từ Activity) ──────────────────────
        public override IBinder? OnBind(Intent? intent) => null;

        // ── Lifecycle ────────────────────────────────────────────────
        public override void OnCreate()
        {
            base.OnCreate();
            CreateNotificationChannel();
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            if (_isRunning) return StartCommandResult.Sticky;

            StartForeground(NOTIFICATION_ID, BuildNotification(), ForegroundService.TypeLocation);
            StartGps();
            _isRunning = true;

            return StartCommandResult.Sticky; // Android tự restart nếu bị kill
        }

        public override void OnDestroy()
        {
            StopGps();
            _isRunning = false;
            base.OnDestroy();
        }

        // ── GPS ──────────────────────────────────────────────────────
        [SupportedOSPlatform("android21.0")]
        private void StartGps()
        {
            _locationManager = (AndroidLocationManager?)GetSystemService(LocationService);
            if (_locationManager == null) return;

            if (_locationManager.IsProviderEnabled(AndroidLocationManager.GpsProvider))
            {
                _locationManager.RequestLocationUpdates(
                    AndroidLocationManager.GpsProvider, MIN_TIME_MS, MIN_DISTANCE_M, this);
            }

            if (_locationManager.IsProviderEnabled(AndroidLocationManager.NetworkProvider))
            {
                _locationManager.RequestLocationUpdates(
                    AndroidLocationManager.NetworkProvider, MIN_TIME_MS, MIN_DISTANCE_M, this);
            }
        }

        private void StopGps()
        {
            _locationManager?.RemoveUpdates(this);
        }

        // ── ILocationListener ────────────────────────────────────────
        public void OnLocationChanged(AndroidLocation location)
        {
            // Chạy bất đồng bộ để không block GPS callback
            Task.Run(async () =>
            {
                try
                {
                    // Chỉ phát audio khi user đã đăng nhập thật và ĐÃ BẮT ĐẦU TOUR (không phát ở trang Welcome)
                    if (!VinhKhanhTour.Services.UserSession.Instance.IsAuthenticatedUser || 
                        !VinhKhanhTour.Services.UserSession.Instance.IsTourActive)
                    {
                        return;
                    }

                    var poi = await _geofencing.CheckNearbyRestaurant(
                        location.Latitude, location.Longitude);

                    if (poi != null)
                    {
                        // Cập nhật notification hiển thị tên POI đang gần
                        UpdateNotification($"Gần: {poi.Name}");

                        // Phát thuyết minh (AudioService tự kiểm tra đang phát không)
                        await _audio.PlayNarrationAsync(poi, location.Latitude, location.Longitude);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LocationService] Lỗi: {ex.Message}");
                }
            });
        }

        public void OnStatusChanged(string? provider, AndroidAvailability status, Bundle? extras) { }
        public void OnProviderEnabled(string provider) { }
        public void OnProviderDisabled(string provider) { }

        // ── Notification ─────────────────────────────────────────────
        [SupportedOSPlatform("android26.0")]
        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;

            var channel = new NotificationChannel(
                CHANNEL_ID,
                "Hướng dẫn tham quan",
                NotificationImportance.Low)   // Low = không phát âm thanh thông báo
            {
                Description = "Thuyết minh tự động khi đến gần điểm tham quan"
            };

            var manager = (NotificationManager?)GetSystemService(NotificationService);
            manager?.CreateNotificationChannel(channel);
        }

        private Notification BuildNotification(string text = "Đang theo dõi vị trí...")
        {
            // Intent mở lại app khi bấm vào notification
            var intent = new Intent(this, typeof(MainActivity));
            var pending = PendingIntent.GetActivity(
                this, 0, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            return new NotificationCompat.Builder(this, CHANNEL_ID)
                .SetContentTitle("VinhKhánh Tour")
                .SetContentText(text)
                .SetSmallIcon(Resource.Mipmap.appicon)
                .SetContentIntent(pending)
                .SetOngoing(true)           // Không thể vuốt tắt
                .SetPriority(NotificationCompat.PriorityLow)
                .Build()!;
        }

        private void UpdateNotification(string text)
        {
            var manager = (NotificationManager?)GetSystemService(NotificationService);
            manager?.Notify(NOTIFICATION_ID, BuildNotification(text));
        }
    }
}
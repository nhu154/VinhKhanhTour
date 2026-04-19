using System;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;
using VinhKhanhTour.Models;
using VinhKhanhTour.Services;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;

namespace VinhKhanhTour.Views
{
    public partial class MapPage : ContentPage
    {
        private readonly WebView _webView;
        private readonly Label _statusLabel;
        private readonly Grid _audioBarContainer;
        private readonly Label _audioBarLabel;
        private Button _btnExitTour = null!;
        private readonly GeofencingService _geofencing = new();
        private Location? _userLocation;
        private Location? _previousLocation;
        private HashSet<int> _listenedPois = new();
        private HashSet<int> _leftPois = new();
        private List<Restaurant> _restaurants = new();
        private List<AppLanguage> _availableLangs = new();
        private readonly Dictionary<int, DateTime> _lastNotified = new();
        private bool _htmlLoaded = false;
        private readonly Dictionary<int, string> _imgCache = new(); // cache base64 anh
        private const int COOLDOWN = 5;

        private const string KEY = "AIzaSyAMX0XgjmNv2O4Twk_CBBmjzDwopqtuexE";

        // ── Offline mode ──────────────────────────────────────────────────────
        private Grid _offlineBanner = null!;
        private bool _isOfflineBannerVisible = false;

        string GetImg(Restaurant r)
        {
            if (string.IsNullOrWhiteSpace(r.ImageUrl)) return "";
            // URL đầy đủ từ API → dùng thẳng
            if (r.ImageUrl.StartsWith("http://") || r.ImageUrl.StartsWith("https://"))
                return r.ImageUrl;
            // File local trong assets
            return "file:///android_asset/" + r.ImageUrl;
        }

        private string? _tourName = null;
        private string _currentLang = Preferences.Default.Get("app_lang", "vi");
        private Restaurant? _pendingDirectionPoi; // POI waiting for map to load/refresh
        private bool _isProcessingPending = false;
        private bool _mapIsMapReadyForCommands = false;
        private bool _skipNextRefresh = false; // Flag to prevent OnAppearing from resetting map during jumps
        private bool _isActiveMap = false;
        private bool _tourRouteDrawn = false;

        public MapPage() : this(null, null) { }

        public MapPage(List<Restaurant>? tourRestaurants, string? tourName)
        {
            if (tourRestaurants != null)
            {
                _restaurants = tourRestaurants;
                _tourName = tourName;
            }

            Title = _tourName != null ? $"Tour: {_tourName}" : "Bản đồ";
            NavigationPage.SetHasNavigationBar(this, false);
            BackgroundColor = Color.FromArgb("#0D1B2A");

            _webView = new WebView { HorizontalOptions = LayoutOptions.Fill, VerticalOptions = LayoutOptions.Fill, BackgroundColor = Colors.Transparent };
            _webView.Navigated += OnNavigated;
            _webView.Navigating += OnNavigating;

            _statusLabel = new Label
            {
                Text = "Đang tải bản đồ...",
                BackgroundColor = Color.FromArgb("#F0F6FF"),
                TextColor = Color.FromArgb("#0D2137"),
                Padding = new Thickness(16, 12, 16, 24),
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center
            };

            _audioBarContainer = new Grid
            {
                BackgroundColor = Color.FromArgb("#1565C0"),
                IsVisible = false,
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            _audioBarLabel = new Label
            {
                Text = string.Empty,
                TextColor = Colors.White,
                Padding = new Thickness(16, 10, 0, 10),
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                VerticalTextAlignment = TextAlignment.Center
            };

            var stopAudioBtn = new Button
            {
                Text = "⏹ Dừng",
                TextColor = Colors.White,
                BackgroundColor = Color.FromArgb("#d32f2f"), // Red stop button
                FontAttributes = FontAttributes.Bold,
                FontSize = 12,
                CornerRadius = 14,
                HeightRequest = 28,
                Padding = new Thickness(12, 0),
                Margin = new Thickness(0, 0, 16, 0),
                VerticalOptions = LayoutOptions.Center
            };
            stopAudioBtn.Clicked += (s, e) => _ = AudioService.Instance.StopAsync();

            _audioBarContainer.Add(_audioBarLabel, 0, 0);
            _audioBarContainer.Add(stopAudioBtn, 1, 0);

            _btnExitTour = new Button
            {
                Text = "← Thoát Tour",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                BackgroundColor = Color.FromArgb("#1565C0"),
                CornerRadius = 20,
                Padding = new Thickness(20, 10),
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 8, 0, 8),
                IsVisible = false,
                Shadow = new Shadow { Brush = Color.FromArgb("#1565C0"), Opacity = 0.4f, Radius = 8, Offset = new Point(0, 2) }
            };
            _btnExitTour.Clicked += (s, e) => ExitTourMode();

            AudioService.Instance.PlaybackStateChanged += isPlaying =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _audioBarContainer.IsVisible = isPlaying;
                    _audioBarLabel.Text = isPlaying
                        ? $"🎧 Đang phát: {AudioService.Instance.CurrentTrack ?? "..."}"
                        : string.Empty;
                });
            };

            // ── Offline Banner ─────────────────────────────────────────
            _offlineBanner = new Grid
            {
                BackgroundColor = Color.FromArgb("#B71C1C"),
                IsVisible = false,
                Padding = new Thickness(16, 6),
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };
            _offlineBanner.Add(new Label
            {
                Text = "📡",
                FontSize = 14,
                VerticalOptions = LayoutOptions.Center
            }, 0, 0);
            _offlineBanner.Add(new Label
            {
                Text = "Đang offline — Bản đồ & audio từ bộ nhớ cache",
                TextColor = Colors.White,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(8, 0)
            }, 1, 0);
            var downloadBtn = new Button
            {
                Text = "Tải về",
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                BackgroundColor = Color.FromArgb("#7f0000"),
                CornerRadius = 12,
                HeightRequest = 26,
                Padding = new Thickness(10, 0),
                VerticalOptions = LayoutOptions.Center
            };
            downloadBtn.Clicked += (s, e) =>
                _ = Navigation.PushAsync(new OfflineDownloadPage());
            _offlineBanner.Add(downloadBtn, 2, 0);

            var grid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },  // offline banner
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto }
                }
            };
            grid.Add(_offlineBanner, 0, 0);
            grid.Add(_webView, 0, 1);
            grid.Add(_btnExitTour, 0, 2);
            grid.Add(_audioBarContainer, 0, 3);
            grid.Add(_statusLabel, 0, 4);
            Content = grid;

            // ── Lắng nghe thay đổi kết nối → show/hide offline banner ─
            OfflineService.Instance.StatusChanged += OnConnectivityChanged;

            _webView.IsEnabled = true;
            _ = InitAsync();
        }

        private void OnConnectivityChanged(ConnectivityStatus status)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                bool isOffline = status == ConnectivityStatus.Offline;
                if (isOffline == _isOfflineBannerVisible) return;

                _isOfflineBannerVisible = isOffline;
                _offlineBanner.IsVisible = isOffline;

                // Cập nhật label banner theo ngôn ngữ hiện tại
                if (isOffline && _offlineBanner.Children.Count > 1 &&
                    _offlineBanner.Children[1] is Label bannerLabel)
                {
                    bannerLabel.Text = _currentLang switch
                    {
                        "en" => "Offline — Map & audio from cache",
                        "zh" => "离线 — 使用缓存地图与音频",
                        "ja" => "オフライン — キャッシュから地図と音声",
                        "ko" => "오프라인 — 캐시에서 지도 및 오디오",
                        _ => "Đang offline — Bản đồ & audio từ bộ nhớ cache"
                    };
                }

                // Nếu vừa online lại: trigger pre-warm tiles và refresh
                if (!isOffline)
                {
                    _ = OfflineModeService.Instance.PreWarmMapTilesAsync();
                }

                System.Diagnostics.Debug.WriteLine(
                    $"[MapPage] Offline banner: {(isOffline ? "SHOW" : "HIDE")}");
            });
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _isActiveMap = true;
            VinhKhanhTour.Services.UserSession.Instance.IsTourActive = true;
            if (_tourName != null) { }
            else if (_htmlLoaded && _restaurants.Count > 0)
            {
                _ = _webView.EvaluateJavaScriptAsync("if(typeof map !== 'undefined' && map) { google.maps.event.trigger(map, 'resize'); }");

                // If we are about to navigate to a specific shop, don't trigger a full data refresh
                // because it might reset the map (setting _htmlLoaded=false) and break the request.
                if (_pendingDirectionPoi != null || _skipNextRefresh)
                {
                    System.Diagnostics.Debug.WriteLine("[MapPage] Skipping OnAppearing refresh because navigation is pending");
                    _skipNextRefresh = false;
                }
                else
                {
                    _ = RefreshPoisFromApiAsync();
                }
            }
            else if (!_htmlLoaded)
            {
                _ = InitAsync();
            }

            _ = RequestLocationPermissionAsync();
            _ = StartLocationTrackingAsync();
            _ = RefreshLangsAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _isActiveMap = false;
        }

        private async Task RefreshLangsAsync()
        {
            try
            {
                var langs = await ApiService.Instance.GetLanguagesAsync();
                if (langs.Count > 0 && langs.Count != _availableLangs.Count)
                {
                    _availableLangs = langs;
                    _mapIsMapReadyForCommands = false; // Reset ready state for fresh init if needed
                    _htmlLoaded = false;
                    await InitAsync();
                }
            }
            catch { }
        }

        private async Task RefreshPoisFromApiAsync()
        {
            try
            {
                var apiList = await ApiService.Instance.GetRestaurantsAsync();
                if (apiList.Count > 0)
                {
                    // So sánh nội dung để phát hiện thay đổi (thêm/xóa/sửa)
                    bool hasChanged = apiList.Count != _restaurants.Count ||
                        apiList.Any(a =>
                        {
                            var old = _restaurants.FirstOrDefault(o => o.Id == a.Id);
                            return old == null ||
                                   old.Name != a.Name ||
                                   old.Description != a.Description ||
                                   old.Address != a.Address ||
                                   old.OpenHours != a.OpenHours ||
                                   Math.Abs(old.Rating - a.Rating) > 0.001 ||
                                   old.ImageUrl != a.ImageUrl;
                        });

                    if (hasChanged)
                    {
                        _restaurants = apiList;
                        // Cập nhật SQLite local
                        var oldList = await App.Database.GetRestaurantsAsync();
                        foreach (var old in oldList)
                            await App.Database.DeleteRestaurantAsync(old.Id);
                        foreach (var r in apiList)
                            await App.Database.SaveRestaurantAsync(r);
                        // Reload map với POI mới
                        _imgCache.Clear();
                        _htmlLoaded = false;
                        await InitAsync();
                        System.Diagnostics.Debug.WriteLine($"[MapPage] ✅ Refreshed {apiList.Count} POIs from API (data changed)");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[MapPage] ℹ️ POI data unchanged, skip reload");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapPage] ⚠️ Refresh failed: {ex.Message}");
            }
        }

        private async Task StartLocationTrackingAsync()
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
                var cts = new CancellationTokenSource();
                var location = await Geolocation.Default.GetLocationAsync(request, cts.Token);
                if (location != null)
                {
                    _userLocation = location;
                    // Dùng setInitPos thay vì sPos để chỉ đặt marker ban đầu,
                    // KHÔNG trigger analytics — tránh ghi GPS 2 lần cùng với JS watchPosition
                    if (_htmlLoaded)
                        await SetInitialMapPosition(location.Latitude, location.Longitude);
                }
            }
            catch { }
        }

        private async Task SetInitialMapPosition(double lat, double lng)
        {
            try
            {
                var ic = System.Globalization.CultureInfo.InvariantCulture;
                // setInitPos chỉ đặt marker vị trí người dùng, KHÔNG gọi maui://locationupdated
                await _webView.EvaluateJavaScriptAsync(
                    $"setInitPos({lat.ToString(ic)},{lng.ToString(ic)});");
            }
            catch { }
        }

        private async Task UpdateMapLocation(double lat, double lng)
        {
            try { await _webView.EvaluateJavaScriptAsync($"sPos({lat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{lng.ToString(System.Globalization.CultureInfo.InvariantCulture)});"); }
            catch { }
        }


        private async Task RequestLocationPermissionAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted)
                    {
                        _statusLabel.Text = _currentLang switch { "en" => "GPS permission required", "zh" => "需要 GPS 权限", "ja" => "GPS権限が必要です", "ko" => "GPS 권한이 필요합니다", _ => "Cần cấp quyền GPS để tiếp tục" };
                        return;
                    }
                }
                _statusLabel.Text = _currentLang switch { "en" => "GPS ready", "zh" => "GPS 已准备好", "ja" => "GPS準備完了", "ko" => "GPS 준비 완료", _ => "GPS sẵn sàng" };
            }
            catch { }
        }

        private async Task InitAsync()
        {
            try
            {
                if (_restaurants.Count == 0 && _tourName == null)
                    _restaurants = await App.Database.GetRestaurantsAsync();

                if (!_htmlLoaded)
                {
                    if (_availableLangs == null || _availableLangs.Count == 0)
                        _availableLangs = await ApiService.Instance.GetLanguagesAsync();

                    await FetchImagesAsync();
                    _htmlLoaded = true;
                    var data = BuildJson();
                    var html = GetHtml(data);
                    _webView.Source = new HtmlWebViewSource { Html = html };
                }
            }
            catch (Exception ex) { _statusLabel.Text = $"Lỗi: {ex.Message}"; }
        }

        private void OnNavigating(object? sender, WebNavigatingEventArgs e)
        {
            if (!e.Url.StartsWith("maui://")) return;
            e.Cancel = true;
            System.Diagnostics.Debug.WriteLine($"[MapPage] OnNavigating: {e.Url.Substring(0, Math.Min(e.Url.Length, 80))}");
            var uri = new Uri(e.Url);
            var q = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var ic = System.Globalization.CultureInfo.InvariantCulture;

            switch (uri.Host.ToLower())
            {
                case "mapready":
                    _htmlLoaded = true;
                    _mapIsMapReadyForCommands = true;
                    System.Diagnostics.Debug.WriteLine("[MapPage] Signal Received: mapready");
                    CheckPendingDirection();
                    break;
                case "locationupdated":
                    if (double.TryParse(q["lat"], System.Globalization.NumberStyles.Float, ic, out double lat) &&
                        double.TryParse(q["lng"], System.Globalization.NumberStyles.Float, ic, out double lng))
                    {
                        var newLoc = new Location(lat, lng);

                        // Lọc GPS: Chỉ gửi lên hệ thống và đổi mốc nếu khách di chuyển quá 6 mét
                        if (_userLocation == null || Microsoft.Maui.Devices.Sensors.Location.CalculateDistance(_userLocation, newLoc, DistanceUnits.Kilometers) * 1000 >= 6)
                        {
                            if (_userLocation != null) _previousLocation = _userLocation;
                            _ = AnalyticsService.RecordGpsPointAsync(lat, lng);
                            _userLocation = newLoc;
                        }

                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            try
                            {
                                if (_tourName != null)
                                {
                                    await _webView.EvaluateJavaScriptAsync($"setTourMode(true,'{_tourName.Replace("'", "\'")}');");
                                    if (!_tourRouteDrawn)
                                    {
                                        _tourRouteDrawn = true;
                                        _ = DrawTourRouteAsync(_restaurants, _userLocation);
                                    }
                                }
                            }
                            catch { }
                            _ = CheckNearbyAsync(_userLocation);
                        });
                    }
                    break;
                case "routerequested":
                    var d = Uri.UnescapeDataString(q["data"] ?? "");
                    try
                    {
                        using var doc = JsonDocument.Parse(d);
                        if (doc.RootElement.TryGetProperty("id", out var idProp))
                            _ = AnalyticsService.RecordPoiVisitAsync(idProp.GetInt32(), "click");
                    }
                    catch { }
                    System.Diagnostics.Debug.WriteLine($"[MapPage] routerequested data={d.Substring(0, Math.Min(d.Length, 60))}");
                    MainThread.BeginInvokeOnMainThread(() => _ = DrawRouteAsync(d));
                    break;
                case "statusupdate":
                    var msg = Uri.UnescapeDataString(q["msg"] ?? "");
                    MainThread.BeginInvokeOnMainThread(() => _statusLabel.Text = msg);
                    break;
                case "stopaudio":
                    MainThread.BeginInvokeOnMainThread(() => _ = AudioService.Instance.StopAsync());
                    break;
                case "exittour":
                    MainThread.BeginInvokeOnMainThread(() => ExitTourMode());
                    break;
                case "changelang":
                    var lang = q["lang"] ?? "vi";
                    _currentLang = lang;
                    Preferences.Default.Set("app_lang", lang);
                    AudioService.Instance.SetLanguage(lang);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (Application.Current?.MainPage is MainTabbedPage tabbed)
                            tabbed.UpdateLanguage(lang);

                        _statusLabel.Text = lang switch { "en" => "Map is ready", "zh" => "地图已准备好", "ja" => "地図の準備ができました", "ko" => "지도 준비 완료", _ => "Bản đồ sẵn sàng" };
                        if (_btnExitTour.IsVisible)
                            _btnExitTour.Text = lang switch { "en" => "← Exit Tour", "zh" => "← 退出行程", "ja" => "← ツアー終了", "ko" => "← 투어 종료", _ => "← Thoát Tour" };
                    });
                    break;
                case "speakpoi":
                    var idStr = q["id"] ?? "";
                    if (int.TryParse(idStr, out int poiId))
                    {
                        var target = _restaurants.FirstOrDefault(r => r.Id == poiId);
                        if (target != null)
                            MainThread.BeginInvokeOnMainThread(() => _ = SpeakAsync(target));
                    }
                    break;
                case "togglefav":
                    var favStr = q["id"] ?? "";
                    if (int.TryParse(favStr, out int favId))
                    {
                        var target = _restaurants.FirstOrDefault(r => r.Id == favId);
                        if (target != null)
                        {
                            target.IsFavorite = !target.IsFavorite;
                            _ = App.Database.UpdateRestaurantAsync(target);
                            var isFavStr = target.IsFavorite.ToString().ToLower();
                            MainThread.BeginInvokeOnMainThread(() => _ = _webView.EvaluateJavaScriptAsync($"if(cur && cur.id==={target.Id}){{ cur.fav={isFavStr}; document.getElementById('txFavBtn').innerHTML=cur.fav?'❤️':'🤍'; }}"));
                        }
                    }
                    break;
            }
        }

        private void OnNavigated(object? sender, WebNavigatedEventArgs e)
        {
            if (e.Result != WebNavigationResult.Success) return;
            // Note: We don't set _htmlLoaded = true here anymore. 
            // We wait for JS to call 'maui://mapready' to ensure initMap is complete.
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(500); // Small grace period
                try { await _webView.EvaluateJavaScriptAsync("gps();"); } catch { }
                _statusLabel.Text = _tourName != null
                    ? $"{_currentLang switch { "en" => "Tour", "zh" => "行程", _ => "Tour" }}: {_tourName} — {_restaurants.Count} {(_currentLang == "vi" ? "điểm" : (_currentLang == "en" ? "spots" : "个景点"))}"
                    : (_currentLang switch { "en" => "Map is ready", "zh" => "地图已准备好", _ => "Bản đồ sẵn sàng" });

                try
                {
                    if (_tourName != null)
                        await _webView.EvaluateJavaScriptAsync($"setTourMode(true,'{_tourName.Replace("'", "\'")}');");
                    else
                        await _webView.EvaluateJavaScriptAsync("setTourMode(false,'');");
                }
                catch { }
            });
        }

        private void CheckPendingDirection()
        {
            if (_pendingDirectionPoi != null && !_isProcessingPending)
            {
                var target = _pendingDirectionPoi;
                _pendingDirectionPoi = null;
                MainThread.BeginInvokeOnMainThread(() => FocusAndDirect(target));
            }
        }


        // Tải ảnh về C# và cache dưới dạng base64 để WebView hiện được
        private async Task FetchImagesAsync()
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            foreach (var r in _restaurants)
            {
                if (_imgCache.ContainsKey(r.Id)) continue;
                if (string.IsNullOrWhiteSpace(r.ImageUrl)) continue;
                try
                {
                    var url = r.ImageUrl.StartsWith("http") ? r.ImageUrl
                        : "http://10.0.2.2:5256/" + r.ImageUrl.TrimStart('/');
                    var bytes = await http.GetByteArrayAsync(url);
                    var ext = url.EndsWith(".png") ? "png" : "jpeg";
                    _imgCache[r.Id] = $"data:image/{ext};base64," + Convert.ToBase64String(bytes);
                }
                catch { _imgCache[r.Id] = ""; }
            }
        }

        private string BuildJson()
        {
            var ic = System.Globalization.CultureInfo.InvariantCulture;
            var sb = new StringBuilder("[");
            for (int i = 0; i < _restaurants.Count; i++)
            {
                var r = _restaurants[i];
                var n = r.Name.Replace("\"", "\\\"").Replace("'", "\\'");
                var de = r.Description.Replace("\"", "\\\"").Replace("'", "\\'");
                var a = r.Address.Replace("\"", "\\\"").Replace("'", "\\'");
                var h = r.OpenHours.Replace("\"", "\\\"").Replace("'", "\\'");
                var img = _imgCache.TryGetValue(r.Id, out var b64) && !string.IsNullOrEmpty(b64) ? b64 : GetImg(r);
                if (i > 0) sb.Append(',');
                sb.Append("{\"id\":" + r.Id);
                sb.Append(",\"lat\":" + r.Latitude.ToString(ic));
                sb.Append(",\"lng\":" + r.Longitude.ToString(ic));
                sb.Append(",\"name\":\"" + n + "\"");
                sb.Append(",\"desc\":\"" + de + "\"");
                sb.Append(",\"addr\":\"" + a + "\"");
                sb.Append(",\"rating\":" + r.Rating.ToString(ic));
                sb.Append(",\"hours\":\"" + h + "\"");
                sb.Append(",\"fav\":" + r.IsFavorite.ToString().ToLower());
                sb.Append(",\"img\":\"" + img + "\"}");
            }
            sb.Append("]");
            return sb.ToString();
        }

        private string GetHtml(string data)
        {
            var lines = new List<string>();

            lines.Add("<!DOCTYPE html>");
            lines.Add("<html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1.0,maximum-scale=1.0,user-scalable=no'>");
            lines.Add("<style>");
            lines.Add("*{box-sizing:border-box;margin:0;padding:0;font-family:'Segoe UI',Roboto,sans-serif;-webkit-tap-highlight-color:transparent}");
            lines.Add("body,html{height:100%;width:100%;overflow:hidden;background-color:#F0F6FF;}"); ;
            lines.Add("#map{position:fixed;top:0;left:0;width:100%;height:100%;z-index:1;}");

            // Premium Search Bar + Language Switcher
            lines.Add("#headerRow{position:fixed;top:16px;left:16px;right:16px;z-index:1000;display:flex;gap:12px;align-items:center;}");
            lines.Add("#searchBar{flex:1;display:flex;align-items:center;background:rgba(255,255,255,0.95);backdrop-filter:blur(12px);-webkit-backdrop-filter:blur(12px);border:1px solid rgba(21,101,192,0.3);border-radius:24px;box-shadow:0 4px 20px rgba(21,101,192,0.15);padding:0 16px;height:48px;}");
            lines.Add("#searchBar input{flex:1;border:none;outline:none;font-size:15px;color:#0D2137;background:transparent;margin-left:8px}");
            lines.Add("#searchBar input::placeholder{color:#5A7A9A}");
            lines.Add("#btnSearch{background:none;border:none;font-size:18px;color:#42A5F5;}");
            lines.Add("#btnClearSearch{background:none;border:none;font-size:18px;cursor:pointer;color:#8ba0b2;display:none}");

            // Language Toggle
            lines.Add("#langBtn{width:48px;height:48px;border-radius:24px;background:rgba(255,255,255,0.95);backdrop-filter:blur(12px);border:1px solid rgba(21,101,192,0.3);display:flex;align-items:center;justify-content:center;font-size:20px;box-shadow:0 4px 20px rgba(21,101,192,0.15);color:#0D2137;cursor:pointer;}");
            lines.Add("#langDropdown{position:fixed;top:76px;right:16px;z-index:2000;background:rgba(255,255,255,0.97);border:1px solid rgba(21,101,192,0.3);border-radius:16px;box-shadow:0 10px 40px rgba(21,101,192,0.2);display:none;flex-direction:column;overflow:hidden;backdrop-filter:blur(10px);min-width:160px;}");
            lines.Add(".ldItem{padding:14px 20px;color:#0D2137;font-size:14px;font-weight:600;display:flex;gap:10px;align-items:center;border-bottom:1px solid rgba(21,101,192,0.1);white-space:nowrap;}");
            lines.Add(".ldItem:active{background:rgba(21,101,192,0.1);}");

            // Search Results
            lines.Add("#searchResults{position:fixed;top:72px;left:16px;right:16px;z-index:1001;background:rgba(255,255,255,0.97);backdrop-filter:blur(16px);border:1px solid rgba(21,101,192,0.2);border-radius:20px;box-shadow:0 8px 32px rgba(21,101,192,0.15);display:none;max-height:280px;overflow-y:auto;}");
            lines.Add(".srItem{display:flex;align-items:center;gap:14px;padding:14px 16px;border-bottom:1px solid rgba(21,101,192,0.08);cursor:pointer;}");
            lines.Add(".srImg{width:48px;height:48px;border-radius:12px;object-fit:cover;background:#152535;flex-shrink:0;display:flex;align-items:center;justify-content:center;font-size:24px}");
            lines.Add(".srImg img{width:100%;height:100%;object-fit:cover;border-radius:12px}");
            lines.Add(".srName{font-size:15px;font-weight:600;color:#0D2137}");
            lines.Add(".srAddr{font-size:12px;color:#5A7A9A;margin-top:4px}");

            // Floating Buttons
            lines.Add(".fBtn{width:52px;height:52px;border-radius:50%;background:rgba(13,27,42,0.9);backdrop-filter:blur(8px);border:1px solid rgba(66,165,245,0.4);color:#64B5F6;box-shadow:0 8px 24px rgba(0,0,0,0.5);display:flex;align-items:center;justify-content:center;font-size:22px;transition:all 0.2s;}");
            lines.Add(".fBtn:active{transform:scale(0.9);background:rgba(30,136,229,0.9);border-color:#bbdefb;color:white;box-shadow:0 4px 12px rgba(33,150,243,0.6);}");
            lines.Add("#btnMenu{position:fixed;right:16px;bottom:326px;z-index:1000;}");
            lines.Add("#btnG{position:fixed;right:16px;bottom:260px;z-index:1000;background:rgba(255,255,255,0.95);border:1px solid rgba(21,101,192,0.3);color:#1565C0;box-shadow:0 4px 20px rgba(21,101,192,0.2);}");
            lines.Add("#btnStop{position:fixed;right:16px;bottom:392px;z-index:1000;background:linear-gradient(135deg,#d32f2f,#E91E63);color:white;border:none;display:none;}");

            // Bottom Sheets
            lines.Add(".botSheet{position:fixed;bottom:0;left:0;right:0;z-index:9500;background:rgba(255,255,255,0.98);backdrop-filter:blur(20px);border-radius:28px 28px 0 0;box-shadow:0 -8px 30px rgba(21,101,192,0.15);transform:translateY(100%);transition:transform 0.4s cubic-bezier(0.2,0.8,0.2,1);display:flex;flex-direction:column;border-top:1px solid rgba(21,101,192,0.15)}");
            lines.Add(".botSheet.open{transform:translateY(0)}");
            lines.Add(".botSheet.open.minim{transform:translateY(calc(100% - 90px));}");
            lines.Add(".dragBar{width:40px;height:5px;background:rgba(21,101,192,0.25);border-radius:4px;margin:12px auto}");

            // Menu Sheet
            lines.Add("#menuSheet{max-height:80vh}");
            lines.Add(".mhd{padding:4px 24px 16px;font-size:18px;font-weight:700;color:#0D2137;border-bottom:1px solid rgba(21,101,192,0.1)}");
            lines.Add(".mlist{overflow-y:auto;flex:1;padding-bottom:24px}");
            lines.Add(".mitem{display:flex;align-items:center;gap:14px;padding:16px 24px;border-bottom:1px solid rgba(21,101,192,0.08)}");
            lines.Add(".mimg{width:64px;height:64px;border-radius:14px;object-fit:cover;background:#1c2f42;flex-shrink:0;position:relative;overflow:hidden;}");
            lines.Add(".mimg img{width:100%;height:100%;object-fit:cover}");
            lines.Add(".minfo{flex:1;min-width:0}");
            lines.Add(".mnm{font-size:16px;font-weight:600;color:#0D2137;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}");
            lines.Add(".mrt{font-size:13px;color:#FFCA28;font-weight:700;margin-top:4px}");
            lines.Add(".mhr{font-size:12px;color:#5A7A9A;margin-top:2px}");
            lines.Add(".mbtns{display:flex;flex-direction:column;gap:8px;flex-shrink:0}");
            lines.Add(".mbtn{width:42px;height:42px;border-radius:14px;border:none;font-size:18px;display:flex;align-items:center;justify-content:center;color:#fff;}");
            lines.Add(".mbtnNav{background:linear-gradient(135deg,#1565C0,#42A5F5);}");
            lines.Add(".mbtnAudio{background:rgba(21,101,192,0.1);color:#1565C0;}");

            // Detail Sheet
            lines.Add("#sheet{z-index:9600;max-height:75vh;overflow-y:auto;padding-bottom:32px}");
            lines.Add(".siw{margin:0;height:240px;border-radius:0;overflow:hidden;position:relative;background:#152535;}");
            lines.Add(".simg{width:100%;height:100%;object-fit:cover}");
            lines.Add(".sph{width:100%;height:100%;display:flex;align-items:center;justify-content:center;font-size:80px;}");
            lines.Add(".sOvl{position:absolute;bottom:0;left:0;right:0;height:120px;background:linear-gradient(0deg,rgba(5,15,30,0.95) 0%,rgba(5,15,30,0) 100%);}");
            lines.Add(".sOvlInfo{position:absolute;bottom:0;left:0;right:0;padding:16px 20px 14px;}");
            lines.Add(".sin{padding:0 20px;margin-top:4px}");
            lines.Add(".sbg{display:inline-block;background:rgba(21,101,192,0.85);color:#fff;font-size:10px;font-weight:700;padding:4px 10px;border-radius:20px;margin-bottom:6px;letter-spacing:0.5px}");
            lines.Add(".snm{font-size:22px;font-weight:800;color:#FFFFFF;margin-bottom:4px}");
            lines.Add(".srt{font-size:15px;color:#FFCA28;font-weight:700;margin-bottom:0}");
            lines.Add(".sdv{height:1px;background:rgba(21,101,192,0.1);margin:16px 24px}");
            lines.Add(".sac{display:flex;gap:12px;padding:0 24px;margin-top:16px}");
            lines.Add(".bcl{width:56px;height:56px;background:rgba(21,101,192,0.1);color:#1565C0;border:none;border-radius:18px;font-size:20px;}");
            lines.Add(".bfav{width:56px;height:56px;background:rgba(21,101,192,0.1);color:#1565C0;border:none;border-radius:18px;font-size:24px;}");
            lines.Add(".bau{width:56px;height:56px;background:rgba(21,101,192,0.1);color:#1565C0;border:none;border-radius:18px;font-size:24px;}");
            lines.Add(".bdr{flex:1;height:56px;background:linear-gradient(90deg,#1565C0,#42A5F5);color:white;border:none;border-radius:18px;font-size:16px;font-weight:700;}");
            lines.Add(".sdt{padding:0 24px}");
            lines.Add(".sro{display:flex;gap:16px;padding:12px 0;font-size:14px;color:#0D2137;line-height:1.5}");
            lines.Add(".sic{font-size:18px;width:24px;text-align:center;color:#1565C0}");

            // Navigation Status Sheet
            lines.Add("#rs{padding:0 24px 32px;z-index:9000;}");
            lines.Add(".rrw{display:flex;align-items:center;gap:16px;margin:12px 0 20px}");
            lines.Add(".ric{width:68px;height:68px;border-radius:18px;overflow:hidden;background:#152535;}");
            lines.Add(".ric img{width:100%;height:100%;object-fit:cover}");
            lines.Add(".rnm{font-size:18px;font-weight:700;color:#0D2137}");
            lines.Add(".rsb{font-size:13px;color:#5A7A9A;margin-top:4px}");
            lines.Add(".rst{display:flex;gap:16px;margin-bottom:24px}");
            lines.Add(".rsc{flex:1;background:rgba(21,101,192,0.06);border-radius:20px;padding:18px;text-align:center;border:1px solid rgba(21,101,192,0.12)}");
            lines.Add(".rvl{font-size:24px;font-weight:800;color:#64B5F6}");
            lines.Add(".rll{font-size:12px;color:#5A7A9A;margin-top:4px;text-transform:uppercase;letter-spacing:1px}");
            lines.Add(".ben{width:100%;height:56px;background:rgba(233,30,99,0.15);color:#F06292;border:1px solid rgba(233,30,99,0.3);border-radius:18px;font-size:16px;font-weight:700;}");
            lines.Add(".bmin{position:absolute;top:12px;right:16px;background:rgba(21,101,192,0.1);color:#1565C0;border:none;border-radius:16px;padding:6px 14px;font-size:13px;font-weight:700;z-index:2;cursor:pointer;}");

            // Custom proximity notification overlay (replaces system alert)
            lines.Add("#proximityAlert{position:fixed;top:0;left:0;right:0;bottom:0;z-index:99999;display:none;align-items:flex-end;justify-content:center;padding:0 0 120px 0;pointer-events:none;}");
            lines.Add("#proximityCard{background:rgba(255,255,255,0.98);backdrop-filter:blur(20px);border-radius:24px;margin:0 16px;box-shadow:0 20px 60px rgba(13,33,55,0.3);overflow:hidden;pointer-events:all;width:calc(100% - 32px);max-width:480px;transform:translateY(120px);opacity:0;transition:all 0.45s cubic-bezier(0.2,0.8,0.2,1);}");
            lines.Add("#proximityCard.show{transform:translateY(0);opacity:1;}");
            lines.Add(".pcBanner{height:5px;background:linear-gradient(90deg,#1565C0,#42A5F5);}");
            lines.Add(".pcBody{padding:20px 22px 22px;display:flex;gap:16px;align-items:flex-start;}");
            lines.Add(".pcIcon{width:56px;height:56px;border-radius:16px;background:linear-gradient(135deg,#E3F2FD,#BBDEFB);display:flex;align-items:center;justify-content:center;font-size:28px;flex-shrink:0;}");
            lines.Add(".pcInfo{flex:1;min-width:0;}");
            lines.Add(".pcBadge{display:inline-block;background:rgba(21,101,192,0.12);color:#1565C0;font-size:10px;font-weight:700;padding:3px 10px;border-radius:20px;letter-spacing:0.5px;margin-bottom:6px;}");
            lines.Add(".pcName{font-size:17px;font-weight:700;color:#0D2137;margin-bottom:4px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;}");
            lines.Add(".pcDesc{font-size:12px;color:#5A7A9A;line-height:1.5;display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;overflow:hidden;}");
            lines.Add(".pcActions{display:flex;gap:10px;padding:0 22px 20px;}");
            lines.Add(".pcDismiss{flex:1;height:44px;background:rgba(21,101,192,0.08);color:#1565C0;border:none;border-radius:14px;font-size:14px;font-weight:600;}");
            lines.Add(".pcNav{flex:1;height:44px;background:linear-gradient(90deg,#1565C0,#42A5F5);color:white;border:none;border-radius:14px;font-size:14px;font-weight:700;}");
            lines.Add("#tst{position:fixed;top:80px;left:50%;transform:translateX(-50%);background:rgba(21,101,192,0.95);color:white;padding:10px 24px;border-radius:30px;font-size:14px;font-weight:600;display:none;z-index:9999;box-shadow:0 8px 30px rgba(21,101,192,0.4);}");
            lines.Add("</style></head><body>");

            lines.Add("<div id='map'></div>");
            lines.Add("<div id='tst'></div>");
            lines.Add("<div id='proximityAlert'><div id='proximityCard'><div class='pcBanner'></div><div class='pcBody'><div class='pcIcon' id='pcIconEl'>📍</div><div class='pcInfo'><div class='pcBadge' id='pcBadgeEl'>GẦN BẠN</div><div class='pcName' id='pcNameEl'></div><div class='pcDesc' id='pcDescEl'></div></div></div><div class='pcActions'><button class='pcDismiss' id='pcBtnClose' onclick='closeProximity()'>Đóng</button><button class='pcNav' id='pcBtnNav' onclick='navFromProximity()'>Chỉ đường</button></div></div></div>");

            lines.Add("<div id='headerRow'>");

            var currentLangObj = _availableLangs.FirstOrDefault(l => l.Code == _currentLang) ?? _availableLangs.FirstOrDefault();
            string initialFlag = currentLangObj?.Flag ?? "🌐";

            lines.Add("  <div id='searchBar'><span id='btnSearch'>🔍</span><input id='searchInput' type='text' placeholder='Tìm quán ăn...' oninput='onSearch(this.value)' onfocus='showResults()'/><button id='btnClearSearch' onclick='clearSearch()'>✕</button></div>");
            lines.Add("  <div id='langBtn' onclick='toggleLang()'><span id='currFlag'>" + initialFlag + "</span>");
            lines.Add("    <div id='langDropdown'>");
            foreach (var l in _availableLangs)
            {
                lines.Add($"      <div class='ldItem' onclick='chgL(\"{l.Code}\", event)'>{l.Flag} {l.Name}</div>");
            }
            lines.Add("    </div>");
            lines.Add("  </div>");
            lines.Add("</div>");
            lines.Add("<div id='searchResults'></div>");

            lines.Add("<button class='fBtn' id='btnMenu' onclick='toggleMenu()'>☰</button>");
            lines.Add("<button class='fBtn' id='btnG' onclick='toU()'>📍</button>");
            lines.Add("<button class='fBtn' id='btnStop' onclick='stopAudio()'>⏹</button>");

            lines.Add("<div class='botSheet' id='menuSheet'><div class='dragBar'></div><div class='mhd' id='txPList'>Danh sách điểm đến</div><div class='mlist' id='menuList'></div></div>");
            lines.Add("<div class='botSheet' id='sheet'>");
            lines.Add("  <div class='dragBar'></div>");
            lines.Add("  <div class='siw'><img id='simg' class='simg' src='' onerror=\"this.style.display='none';document.getElementById('sph').style.display='flex'\"><div id='sph' class='sph' style='display:none'></div><div class='sOvl'></div><div class='sOvlInfo'><div class='sbg' id='txCat'>QUÁN ĂN - VĨNH KHÁNH</div><div class='snm' id='snm'></div><div class='srt' id='srt'></div></div></div>");
            // tên + rating đã chuyển vào overlay trên ảnh
            lines.Add("  <div class='sac'><button class='bcl' onclick='closeS()'>✕</button><button class='bfav' onclick='if(cur) toggleFav(cur.id,event)' id='txFavBtn'>🤍</button><button class='bau' onclick='if(cur) speakPoi(cur.id,event)' id='txAud'><svg width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M3 18v-6a9 9 0 0 1 18 0v6'></path><path d='M21 19a2 2 0 0 1-2 2h-1a2 2 0 0 1-2-2v-3a2 2 0 0 1 2-2h3zM3 19a2 2 0 0 0 2 2h1a2 2 0 0 0 2-2v-3a2 2 0 0 0-2-2H3z'></path></svg></button><button class='bdr' onclick='reqR()' id='txDir'><svg width='18' height='18' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round' style='vertical-align:text-bottom;margin-right:6px;margin-bottom:1px'><polygon points='3 11 22 2 13 21 11 13 3 11'></polygon></svg> <span id='txDirText'>Chỉ đường</span></button></div>");
            lines.Add("  <div class='sdv'></div>");
            lines.Add("  <div class='sdt'><div class='sro'><span class='sic'>💬</span><span id='sde'></span></div><div class='sro'><span class='sic'>📍</span><span id='sad'></span></div><div class='sro'><span class='sic'>⏰</span><span id='shr'></span></div></div>");
            lines.Add("</div>");

            lines.Add("<div class='botSheet' id='rs'>");
            lines.Add("  <button class='bmin' onclick='toggleNavSheet()' id='txMin'>⬇ Ẩn bớt</button>");
            lines.Add("  <div class='dragBar' onclick='toggleNavSheet()'></div>");
            lines.Add("  <div class='rrw'><div class='ric' id='ricImg'>📍</div><div><div class='rnm' id='rnm'></div><div class='rsb' id='txWalk'>🚶 Chỉ đường đi bộ</div></div></div>");
            lines.Add("  <div class='rst'><div class='rsc'><div class='rvl' id='rdst'></div><div class='rll' id='txDist'>Khoảng cách</div></div><div class='rsc'><div class='rvl' id='rtim'></div><div class='rll' id='txDur'>Thời gian</div></div></div>");
            lines.Add("  <button class='ben' onclick='endR()' id='txEnd'>✕ Dừng chỉ đường</button>");
            lines.Add("</div>");

            lines.Add("<script>");
            lines.Add("var map,uMk,uCir,rLn,rLnBg,rtMks=[],rtWin,poi={},uP=null,cur=null,cL='" + _currentLang + "';");
            lines.Add("var ALL=" + data + ";");
            lines.Add("var CTR={lat:10.7615,lng:106.7045};");
            lines.Add("var L={");
            lines.Add("  vi:{search:'Tìm quán ăn...',plist:'Danh sách điểm đến',nodata:'Không tìm thấy quán',cat:'QUÁN ĂN - VĨNH KHÁNH',dir:'Chỉ đường',dist:'KHOẢNG CÁCH',dur:'THỜI GIAN',end:'✕ DỪNG CHỈ ĐƯỜNG',walk:'🚶 Chỉ đường đi bộ',near:'GẦN BẠN',close:'Đóng',min:'Ẩn bớt',exp:'Mở rộng'},");
            lines.Add("  en:{search:'Search spots...',plist:'Destinations list',nodata:'No spots found',cat:'STREET FOOD - VINH KHANH',dir:'Get Directions',dist:'DISTANCE',dur:'DURATION',end:'✕ EXIT NAVIGATION',walk:'🚶 Walking directions',near:'NEAR YOU',close:'Close',min:'Hide',exp:'Expand'},");
            lines.Add("  zh:{search:'搜索餐厅...',plist:'目的地列表',nodata:'未找到餐厅',cat:'美食街 - 永庆',dir:'获取路线',dist:'距离',dur:'持续时间',end:'✕ 退出导航',walk:'🚶 步行路线',near:'就在附近',close:'关闭',min:'隐藏',exp:'展开'},");
            lines.Add("  ja:{search:'スポットを検索...',plist:'目的地リスト',nodata:'スポットが見つかりません',cat:'グルメ街 - ヴィンカン',dir:'ルート案内',dist:'距離',dur:'時間',end:'✕ ナビを終了',walk:'🚶 徒歩ルート',near:'近くにあります',close:'閉じる',min:'隠す',exp:'展開'},");
            lines.Add("  ko:{search:'식당 검색...',plist:'목적지 목록',nodata:'검색 결과가 없습니다',cat:'음식 거리 - 빈칸',dir:'길 찾기',dist:'거리',dur:'시간',end:'✕ 길 안내 종료',walk:'🚶 도보 길 안내',near:'근처에 있음',close:'닫기',min:'숨기기',exp:'확장'}");
            lines.Add("};");
            lines.Add("function toggleNavSheet(){var rs=document.getElementById('rs'); rs.classList.toggle('minim'); document.getElementById('txMin').textContent=(rs.classList.contains('minim')?'⬆ ':'⬇ ')+(rs.classList.contains('minim')?L[cL].exp:L[cL].min);}");
            lines.Add("function toggleLang(){ var d=document.getElementById('langDropdown'); d.style.display=d.style.display==='flex'?'none':'flex'; }");
            lines.Add("function chgL(l,ev){ if(ev)ev.stopPropagation(); cL=l; var flags={'vi':'🇻🇳','en':'🇺🇸','zh':'🇨🇳','ja':'🇯🇵','ko':'🇰🇷'}; document.getElementById('currFlag').textContent=flags[l]||'🌐'; document.getElementById('langDropdown').style.display='none'; applyLang(); window.location.href='maui://changelang?lang='+l; }");
            lines.Add("function applyLang(){ var t=L[cL]; document.getElementById('searchInput').placeholder=t.search; document.getElementById('txPList').textContent=t.plist; document.getElementById('txCat').textContent=t.cat; document.getElementById('txDirText').textContent=t.dir; document.getElementById('txDist').textContent=t.dist; document.getElementById('txDur').textContent=t.dur; document.getElementById('txEnd').textContent=t.end; document.getElementById('txWalk').textContent=t.walk; document.getElementById('pcBadgeEl').textContent=t.near; document.getElementById('pcBtnClose').textContent=t.close; document.getElementById('pcBtnNav').textContent=t.dir; var rs=document.getElementById('rs'); document.getElementById('txMin').textContent=(rs.classList.contains('minim')?'⬆ ':'⬇ ')+(rs.classList.contains('minim')?t.exp:t.min); if(document.getElementById('menuSheet').classList.contains('open')) renderMenuList(); }");

            lines.Add("function st(r){return r.toFixed(1);}");
            lines.Add("function toast(m){var t=document.getElementById('tst');t.textContent=m;t.style.display='block';setTimeout(function(){t.style.display='none';},2500);}");
            lines.Add("function toU(){if(uP)map.panTo(uP);else{var msg={'vi':'Đang tìm GPS...','en':'Waiting for GPS...','zh':'正在搜索 GPS...','ja':'GPS検索中...','ko':'GPS 검색 중...'};toast(msg[cL]||'GPS...');}}");
            lines.Add("function pick(r){cur=r;map.panTo({lat:r.lat,lng:r.lng});opS(r);}");
            lines.Add("function opS(r){var si=document.getElementById('simg'),sp=document.getElementById('sph');var foodEmoji='🍽️';if(r.name.includes('Oc'))foodEmoji='🦪';else if(r.name.includes('Bun')||r.name.includes('Pho'))foodEmoji='🍜';if(r.img){si.src=r.img;si.style.display='block';sp.style.display='none';}else{si.style.display='none';sp.innerHTML=foodEmoji;sp.style.display='flex';}document.getElementById('snm').textContent=r.name;document.getElementById('srt').innerHTML='⭐ '+st(r.rating);document.getElementById('sde').textContent=r.desc;document.getElementById('sad').textContent=r.addr;document.getElementById('shr').textContent=r.hours;var fb=document.getElementById('txFavBtn');if(fb){fb.innerHTML=r.fav?'❤️':'🤍';}document.getElementById('sheet').classList.add('open');}");
            lines.Add("function toggleFav(id,ev){if(ev)ev.stopPropagation(); window.location.href='maui://togglefav?id='+id;}");
            lines.Add("function closeS(){document.getElementById('sheet').classList.remove('open');document.getElementById('menuSheet').classList.remove('open');cur=null;}");
            lines.Add("function reqR(){if(!cur){return;}if(!uP){toast('Đang lấy vị trí trung tâm do không có GPS');}window.location.href='maui://routerequested?data='+encodeURIComponent(JSON.stringify({id:cur.id,lat:cur.lat,lng:cur.lng,name:cur.name}))}");
            lines.Add("function shwR(n,d,t,img){document.getElementById('rnm').textContent=n;document.getElementById('rdst').textContent=d;document.getElementById('rtim').textContent=t;var ric=document.getElementById('ricImg');ric.innerHTML=img?'<img src=\"'+img+'\">':'📍';var rs=document.getElementById('rs');rs.classList.remove('minim');document.getElementById('txMin').textContent='⬇ '+L[cL].min;rs.classList.add('open');document.getElementById('sheet').classList.remove('open');}");
            lines.Add("var _segs=[],_rb=null,_rn='',_rd='',_rt='',_ri='';");
            lines.Add("function endR(){_segs.forEach(function(p){p.setMap(null);});_segs=[];if(rLn){rLn.setMap(null);rLn=null;}if(rLnBg){rLnBg.setMap(null);rLnBg=null;}if(rtMks){rtMks.forEach(function(m){m.setMap(null);});rtMks=[];}if(rtWin){rtWin.close();}document.getElementById('rs').classList.remove('open');}");
            lines.Add("function drawPremiumRoute(segsArray, n, d, t, img) {");
            lines.Add("  try {");
            lines.Add("  endR(); _rb = new google.maps.LatLngBounds(); if(uP)_rb.extend(uP);");
            lines.Add("  segsArray.forEach(function(seg) {");
            lines.Add("    var path = seg.pts;");
            lines.Add("    path.forEach(function(p){ _rb.extend(p); });");
            lines.Add("    var isAlley = seg.alley;");
            lines.Add("    var sh = new google.maps.Polyline({path:path, geodesic:true, strokeColor: 'transparent', strokeWeight: 0, strokeOpacity: 0, zIndex:2});");
            lines.Add("    sh.setMap(map); _segs.push(sh);");
            lines.Add("    var ln = new google.maps.Polyline({path:path, geodesic:true, strokeColor: 'transparent', strokeWeight: 0, strokeOpacity: 0, zIndex:3, icons: [{icon:{path:google.maps.SymbolPath.CIRCLE,fillColor:'#1A73E8',fillOpacity:1,scale:6,strokeColor:'white',strokeWeight:2.5},offset:'0',repeat:'22px'}]});");
            lines.Add("    ln.setMap(map); _segs.push(ln);");
            lines.Add("  });");
            lines.Add("  map.fitBounds(_rb, {padding:80});");
            lines.Add("  var L2 = google.maps.event.addListener(map, 'idle', function(){ if(map.getZoom()>18) map.setZoom(18); google.maps.event.removeListener(L2); });");
            lines.Add("  shwR(n, d, t, img);");
            lines.Add("  } catch(e) { console.log('Route Error: '+e); }");
            lines.Add("}");
            lines.Add("function sPos(lat,lng){uP={lat:lat,lng:lng};if(uMk){uMk.setPosition(uP);uCir.setCenter(uP);}else{uMk=new google.maps.Marker({position:uP,map:map,zIndex:1000,icon:{path:google.maps.SymbolPath.CIRCLE,scale:12,fillColor:'#42A5F5',fillOpacity:1,strokeColor:'white',strokeWeight:3}});uCir=new google.maps.Circle({center:uP,radius:35,map:map,fillColor:'#42A5F5',fillOpacity:0.25,strokeColor:'#42A5F5',strokeOpacity:0,strokeWeight:0});map.panTo(uP);}window.location.href='maui://locationupdated?lat='+lat+'&lng='+lng;}");
            // setInitPos: chỉ đặt marker không ghi analytics (dùng cho lần đầu khởi động C# Geolocation)
            lines.Add("function setInitPos(lat,lng){uP={lat:lat,lng:lng};if(uMk){uMk.setPosition(uP);uCir.setCenter(uP);}else{uMk=new google.maps.Marker({position:uP,map:map,zIndex:1000,icon:{path:google.maps.SymbolPath.CIRCLE,scale:12,fillColor:'#42A5F5',fillOpacity:1,strokeColor:'white',strokeWeight:3}});uCir=new google.maps.Circle({center:uP,radius:35,map:map,fillColor:'#42A5F5',fillOpacity:0.25,strokeColor:'#42A5F5',strokeOpacity:0,strokeWeight:0});map.panTo(uP);}}");
            lines.Add("function gps(){if(navigator.geolocation){navigator.geolocation.watchPosition(function(p){sPos(p.coords.latitude,p.coords.longitude);},function(err){},{enableHighAccuracy:true,timeout:10000,maximumAge:3000});}}");
            lines.Add("function stopAudio(){window.location.href='maui://stopaudio';}");
            lines.Add("function speakPoi(id,ev){ev.stopPropagation(); window.location.href='maui://speakpoi?id='+id;}");
            lines.Add("function setTourMode(on,name){}");
            lines.Add("function setAudioPlaying(on){document.getElementById('btnStop').style.display=on?'flex':'none';}");
            // Proximity notification overlay functions
            lines.Add("var _proxCur=null;");
            lines.Add("function showProximity(id,name,desc,img){_proxCur=ALL.find(function(r){return r.id===id;});document.getElementById('pcNameEl').textContent=name;document.getElementById('pcDescEl').textContent=desc;var ic=document.getElementById('pcIconEl');ic.innerHTML=img?'<img src=\"'+img+'\" style=\"width:100%;height:100%;object-fit:cover;border-radius:12px;\">':'🍽️';document.getElementById('proximityAlert').style.display='flex';setTimeout(function(){document.getElementById('proximityCard').classList.add('show');},10);setTimeout(function(){closeProximity();},8000);}");
            lines.Add("function closeProximity(){document.getElementById('proximityCard').classList.remove('show');setTimeout(function(){document.getElementById('proximityAlert').style.display='none';},450);}");
            lines.Add("function navFromProximity(){if(_proxCur){cur=_proxCur;closeProximity();reqR();}}");
            lines.Add("function textMatch(str, q) { if(!q) return true; if(!str) return false; var normTone = function(s){ return String(s).normalize('NFD').replace(/[\\u0300-\\u036f]/g, '').replace(/đ/g, 'd').replace(/Đ/g, 'D').toLowerCase(); }; var qWords = String(q).trim().split(/\\s+/); var sNorm = ' ' + normTone(str) + ' '; var sRaw = ' ' + String(str).toLowerCase() + ' '; return qWords.every(function(w){ var isPlain = !/[àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ]/i.test(w); if(isPlain) { return sNorm.indexOf(' ' + normTone(w)) !== -1; } else { var hasTones = /[\\u0300\\u0301\\u0303\\u0309\\u0323]/.test(w.normalize('NFD')); if(!hasTones) { var removeTones = function(x){ return String(x).normalize('NFD').replace(/[\\u0300\\u0301\\u0303\\u0309\\u0323]/g, '').normalize('NFC').toLowerCase(); }; return (' ' + removeTones(str) + ' ').indexOf(' ' + removeTones(w)) !== -1; } else { return sRaw.indexOf(' ' + w.toLowerCase()) !== -1; } } }); }");
            lines.Add("function onSearch(v){var inp=document.getElementById('btnClearSearch');inp.style.display=v?'block':'none';if(!v){document.getElementById('searchResults').style.display='none';return;}var res=ALL.filter(function(r){return textMatch(r.name, v);});renderSearchResults(res);}");
            lines.Add("function renderSearchResults(res){var d=document.getElementById('searchResults');if(!res.length){d.innerHTML='<div style=\"padding:16px;text-align:center;color:#8ba0b2\">'+L[cL].nodata+'</div>';d.style.display='block';return;}var h='';res.forEach(function(r){var imgHtml=r.img?'<img src=\"'+r.img+'\">':'<div>🍜</div>';h+='<div class=\"srItem\" onclick=\"selectSearchResult('+r.id+')\"><div class=\"srImg\">'+imgHtml+'</div><div><div class=\"srName\">'+r.name+'</div><div class=\"srAddr\">'+r.addr+'</div></div></div>';});d.innerHTML=h;d.style.display='block';}");
            lines.Add("function selectSearchResult(id){var r=ALL.find(function(x){return x.id===id;});if(!r)return;clearSearch();pick(r);}");
            lines.Add("function showResults(){if(document.getElementById('searchInput').value)document.getElementById('searchResults').style.display='block';}");
            lines.Add("function clearSearch(){document.getElementById('searchInput').value='';document.getElementById('searchResults').style.display='none';document.getElementById('btnClearSearch').style.display='none';}");

            lines.Add("function toggleMenu(){var m=document.getElementById('menuSheet');m.classList.toggle('open');if(m.classList.contains('open'))renderMenuList();}");
            lines.Add("function renderMenuList(){var h='';var sNav='<svg width=\"20\" height=\"20\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><polygon points=\"3 11 22 2 13 21 11 13 3 11\"></polygon></svg>';var sAud='<svg width=\"20\" height=\"20\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M3 18v-6a9 9 0 0 1 18 0v6\"></path><path d=\"M21 19a2 2 0 0 1-2 2h-1a2 2 0 0 1-2-2v-3a2 2 0 0 1 2-2h3zM3 19a2 2 0 0 0 2 2h1a2 2 0 0 0 2-2v-3a2 2 0 0 0-2-2H3z\"></path></svg>';ALL.forEach(function(r){var imgHtml=r.img?'<img src=\"'+r.img+'\">':'<div>🍲</div>';h+='<div class=\"mitem\">'+'<div class=\"mimg\">'+imgHtml+'</div>'+'<div class=\"minfo\"><div class=\"mnm\">'+r.name+'</div><div class=\"mrt\">⭐ '+st(r.rating)+'</div><div class=\"mhr\">🕒 '+r.hours+'</div></div><div class=\"mbtns\"><button class=\"mbtn mbtnNav\" onclick=\"menuNav('+r.id+',event)\">'+sNav+'</button><button class=\"mbtn mbtnAudio\" onclick=\"speakPoi('+r.id+',event)\">'+sAud+'</button></div></div>';});document.getElementById('menuList').innerHTML=h;}");
            lines.Add("function menuNav(id,ev){ev.stopPropagation();var r=ALL.find(function(x){return x.id===id;});if(!r)return;cur=r;document.getElementById('menuSheet').classList.remove('open');reqR();}");

            lines.Add("document.addEventListener('click',function(e){var sr=document.getElementById('searchResults');if(sr&&!sr.contains(e.target)&&e.target.id!=='searchInput')sr.style.display='none';var lb=document.getElementById('langBtn');var ld=document.getElementById('langDropdown');if(ld&&!lb.contains(e.target))ld.style.display='none';});");

            // ── Pre-warm Google Maps tiles khu vực Vĩnh Khánh (chạy sau khi map ready) ──
            lines.Add("var _preCacheDone=false;");
            lines.Add("function preCacheMapArea(){" +
                // Tải tile im lặng bằng Image() — không pan map, không hiện toast
                "if(_preCacheDone||typeof map==='undefined')return;" +
                "_preCacheDone=true;" +
                "var pts=[" +
                "  {lat:10.758,lng:106.699},{lat:10.758,lng:106.705},{lat:10.758,lng:106.710}," +
                "  {lat:10.762,lng:106.699},{lat:10.762,lng:106.705},{lat:10.762,lng:106.710}," +
                "  {lat:10.766,lng:106.699},{lat:10.766,lng:106.705},{lat:10.766,lng:106.710}" +
                "];" +
                "pts.forEach(function(p){" +
                "  [16,17,18].forEach(function(z){" +
                "    var n=Math.pow(2,z);" +
                "    var tx=Math.floor((p.lng+180)/360*n);" +
                "    var rad=p.lat*Math.PI/180;" +
                "    var ty=Math.floor((1-Math.log(Math.tan(rad)+1/Math.cos(rad))/Math.PI)/2*n);" +
                "    var img=new Image();" +
                "    img.src='https://mt1.google.com/vt/lyrs=m&x='+tx+'&y='+ty+'&z='+z;" +
                "  });" +
                "});" +
                "}");

            lines.Add("function initMap(){try{var mapDiv=document.getElementById('map');if(!mapDiv)return;" +
                "var mapOptions={center:CTR,zoom:16,mapTypeControl:false,streetViewControl:false,fullscreenControl:false," +
                "zoomControl:true,zoomControlOptions:{position:google.maps.ControlPosition.LEFT_CENTER}," +
                "scrollwheel:true,gestureHandling:'greedy',clickableIcons:false,mapTypeId:'roadmap',disableDoubleClickZoom:true};" +
                "map=new google.maps.Map(mapDiv,mapOptions);" +
                "if(ALL&&ALL.length>0){ALL.forEach(function(r){" +
                "  var mk=new google.maps.Marker({position:{lat:r.lat,lng:r.lng},map:map,title:r.name});" +
                "  mk.addListener('click',function(){pick(r);});" +
                "});}" +
                "map.addListener('click',function(){closeS();});" +
                "map.addListener('dblclick',function(e){" +
                "  var tp={'vi':'\u0110\u00e3 d\u1ecbch chuy\u1ec3n t\u1edbi \u0111\u00e2y!','en':'Teleported here!','zh':'\u5df2\u4f20\u9001\u5230\u8fd9\u91cc\uff01','ja':'\u3053\u3053\u306b\u79fb\u52d5\u3057\u307e\u3057\u305f!','ko':'\uc5ec\uae30\ub85c \uc774\ub3d9\ud588\uc2b5\ub2c8\ub2e4!'};" +
                "  toast(tp[cL]||tp.vi); sPos(e.latLng.lat(),e.latLng.lng());" +
                "});" +
                "google.maps.event.trigger(map,'resize');" +
                "setTimeout(gps,1000);" +
                // Pre-warm tiles lần đầu (3 giây sau khi map ready, để user map đã render xong)
                "google.maps.event.addListenerOnce(map,'idle',function(){" +
                "  setTimeout(function(){" +
                "    if(navigator.onLine){preCacheMapArea();}" +
                "  },3000);" +
                "});" +
                "applyLang();" +
                "window.location.href='maui://mapready';" +
                "}catch(e){console.error(e);window.location.href='maui://mapready';}}");

            lines.Add("</script>");
            lines.Add("<script src='https://maps.googleapis.com/maps/api/js?key=" + KEY + "'></script>");
            lines.Add("<script>window.addEventListener('load',function(){setTimeout(initMap,200);});</script>");
            lines.Add("</body></html>");


            return string.Join("\n", lines);
        }

        private async Task CheckNearbyAsync(Location location)
        {
            if (_restaurants.Count == 0) return;

            // Lấy danh sách các POI lân cận, cập nhật trạng thái rời khỏi
            var nearbyPois = new List<(Restaurant Poi, double Dist)>();
            foreach (var r in _restaurants)
            {
                double d = GeofencingService.CalculateDistance(location.Latitude, location.Longitude, r.Latitude, r.Longitude);

                // Nếu đã nghe và đi xa hơn 20m, đánh dấu là đã rời khỏi
                if (_listenedPois.Contains(r.Id) && d >= 20 && !_leftPois.Contains(r.Id))
                {
                    _leftPois.Add(r.Id);
                }

                if (d <= 5)
                {
                    nearbyPois.Add((r, d));
                }
            }

            if (nearbyPois.Count == 0) return;

            // Sắp xếp theo khoảng cách
            nearbyPois = nearbyPois.OrderBy(x => x.Dist).ToList();
            var nearest = nearbyPois.First().Poi;
            var minDist = nearbyPois.First().Dist;

            // Xử lý trường hợp 2 điểm có khoảng cách xấp xỉ nhau (chênh lệch < 1 mét)
            if (nearbyPois.Count > 1)
            {
                var first = nearbyPois[0];
                var second = nearbyPois[1];

                if (Math.Abs(first.Dist - second.Dist) < 1.0 && _previousLocation != null)
                {
                    // Xét xem đang đi về hướng nào (khoảng cách từ vị trí cũ đến POI nào giảm nhiều hơn)
                    double distPrevFirst = GeofencingService.CalculateDistance(_previousLocation.Latitude, _previousLocation.Longitude, first.Poi.Latitude, first.Poi.Longitude);
                    double distPrevSecond = GeofencingService.CalculateDistance(_previousLocation.Latitude, _previousLocation.Longitude, second.Poi.Latitude, second.Poi.Longitude);

                    double deltaFirst = distPrevFirst - first.Dist; // > 0 tức là đang tiến lại gần
                    double deltaSecond = distPrevSecond - second.Dist;

                    if (deltaSecond > deltaFirst)
                    {
                        nearest = second.Poi;
                        minDist = second.Dist;
                    }
                }
            }

            string langNear = _currentLang switch { "en" => "Nearest", "zh" => "最近", "ja" => "最近", "ko" => "가장 가까운", _ => "Gần nhất" };
            MainThread.BeginInvokeOnMainThread(() => _statusLabel.Text = $"{langNear}: {nearest.Name} ({minDist:F0}m)");

            if (!VinhKhanhTour.Services.UserSession.Instance.IsAuthenticatedUser || !_isActiveMap) return;

            // Xử lý logic quay lại điểm đã nghe
            if (_listenedPois.Contains(nearest.Id))
            {
                if (_leftPois.Contains(nearest.Id))
                {
                    // Tránh spam prompt liên tục, cooldown 1 phút cho prompt
                    if (_lastNotified.TryGetValue(nearest.Id, out DateTime lastPrompt) && (DateTime.Now - lastPrompt).TotalMinutes < 1)
                        return;

                    _lastNotified[nearest.Id] = DateTime.Now;

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        var app = Application.Current;
                        if (app?.MainPage == null) return;

                        bool replay = await app.MainPage.DisplayAlert(
                            "Phát lại Audio?",
                            $"Bạn đã nghe audio điểm {nearest.Name} này rồi, bạn có muốn nghe lại hay không?",
                            "Có",
                            "Không");

                        if (replay)
                        {
                            _leftPois.Remove(nearest.Id); // Xóa để không hỏi lại cho đến khi đi xa tiếp
                            await SpeakAsync(nearest);
                            await ShowProximityOverlay(nearest);
                        }
                    });
                }
                return; // Đã nghe nhưng chưa rời đi, hoặc đang chờ/mới prompt xong
            }

            if (_lastNotified.TryGetValue(nearest.Id, out DateTime last) &&
                (DateTime.Now - last).TotalMinutes < COOLDOWN) return;

            _lastNotified[nearest.Id] = DateTime.Now;
            _listenedPois.Add(nearest.Id);
            _leftPois.Remove(nearest.Id);

            await SpeakAsync(nearest);
            await ShowProximityOverlay(nearest);

            await App.Database.SaveVisitAsync(new VisitHistory
            {
                RestaurantId = nearest.Id,
                VisitedAt = DateTime.Now,
                Username = VinhKhanhTour.Services.UserSession.Instance.Username ?? string.Empty
            });
            _ = ApiService.Instance.PostAnalyticAsync(nearest.Id, "poi_visit", location.Latitude, location.Longitude);
        }

        private async Task ShowProximityOverlay(Restaurant nearest)
        {
            var imgJs = (_imgCache.TryGetValue(nearest.Id, out var nb64) && !string.IsNullOrEmpty(nb64) ? nb64 : (nearest.ImageUrl ?? "")).Replace("'", "\\'");
            var nameJs = nearest.Name.Replace("'", "\\'");
            var descJs = (!string.IsNullOrWhiteSpace(nearest.TtsScript) ? nearest.TtsScript : nearest.Description).Replace("'", "\\'").Replace("\n", " ").Replace("\r", "");
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await _webView.EvaluateJavaScriptAsync($"showProximity({nearest.Id},'{nameJs}','{descJs}','{imgJs}');"));
        }

        public async void FocusAndDirect(Restaurant r)
        {
            if (r == null) return;

            // 1. If map is not loaded OR not ready for commands yet, queue it
            if (!_htmlLoaded || !_mapIsMapReadyForCommands)
            {
                System.Diagnostics.Debug.WriteLine($"[MapPage] Queueing direction for {r.Name} (Loaded={_htmlLoaded}, Ready={_mapIsMapReadyForCommands})");
                _pendingDirectionPoi = r;
                _skipNextRefresh = true; // Avoid reload during jump
                if (!_htmlLoaded) await InitAsync();
                return;
            }

            if (_isProcessingPending) return;

            try
            {
                _isProcessingPending = true;
                _skipNextRefresh = true;

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // Center and show POI sheet
                    await _webView.EvaluateJavaScriptAsync($"if(typeof pick === 'function') {{ var target = ALL.find(x => x.id === {r.Id}); if(target) pick(target); }} else if(typeof selectSearchResult === 'function') {{ selectSearchResult({r.Id}); }}");

                    // Show status toast
                    await _webView.EvaluateJavaScriptAsync($"if(typeof toast === 'function') toast('{(_currentLang == "vi" ? "Đang định vị và tìm đường..." : (_currentLang == "zh" ? "正在定位..." : (_currentLang == "ja" ? "ルート検索中..." : (_currentLang == "ko" ? "경로 탐색 중..." : "Locating and finding route..."))))}')");

                    // 2. Trigger directions routing
                    var routeData = new { id = r.Id, lat = r.Latitude, lng = r.Longitude, name = r.Name };
                    string json = System.Text.Json.JsonSerializer.Serialize(routeData);

                    // Networking in background
                    bool routeSuccess = await Task.Run(async () => await DrawRouteAsync(json));

                    if (routeSuccess)
                    {
                        // 3. Prompt for commentary AFTER route is shown on map
                        bool answer = await DisplayAlert(
                            _currentLang == "vi" ? "Thuyết minh" : "Commentary",
                            _currentLang == "vi" ? "Bạn có muốn nghe thuyết minh về địa điểm này không?" : "Would you like to hear the commentary for this location?",
                            _currentLang == "vi" ? "Có, phát ngay" : "Yes, play now",
                            _currentLang == "vi" ? "Để sau" : "Maybe later");

                        if (answer)
                        {
                            await SpeakAsync(r);
                        }
                    }
                    else
                    {
                        await _webView.EvaluateJavaScriptAsync($"if(typeof toast === 'function') toast('{(_currentLang == "vi" ? "Lỗi: Không tìm thấy đường đi" : "Error: No route found")}')");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapPage] FocusAndDirect error: {ex.Message}");
            }
            finally
            {
                _isProcessingPending = false;
            }
        }

        public void LoadTourPois(List<Restaurant> restaurants, string tourName)
        {
            _restaurants = restaurants;
            _tourName = tourName;
            _htmlLoaded = false;
            _tourRouteDrawn = false;
            _lastNotified.Clear();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _btnExitTour.IsVisible = true;
                _btnExitTour.Text = _currentLang switch { "en" => $"← Exit {tourName}", "zh" => $"← 退出 {tourName}", _ => $"← Thoát {tourName}" };
                _statusLabel.Text = $"{(_currentLang == "vi" ? "Tour" : "Tour")}: {tourName} — {restaurants.Count} {(_currentLang == "vi" ? "điểm" : "spots")}";
                Title = $"Tour: {tourName}";
            });
            _ = InitAsync();
        }

        public void ExitTourMode()
        {
            _tourName = null;
            _restaurants.Clear();
            _htmlLoaded = false;
            _lastNotified.Clear();
            Title = _currentLang switch { "en" => "Map", "zh" => "地图", _ => "Bản đồ" };
            _ = InitAsync();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _btnExitTour.IsVisible = false;
                _statusLabel.Text = _currentLang switch { "en" => "All locations", "zh" => "所有地点", _ => "Tất cả địa điểm" };
            });
        }

        public void UpdateLanguage(string lang)
        {
            if (_currentLang == lang) return;
            _currentLang = lang;

            if (_htmlLoaded)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _ = _webView.EvaluateJavaScriptAsync($"if(typeof chgL === 'function') chgL('{lang}');");
                });
            }
        }

        private async Task SpeakAsync(Restaurant r)
        {
            await MainThread.InvokeOnMainThreadAsync(async () => await _webView.EvaluateJavaScriptAsync("setAudioPlaying(true);"));
            _ = Task.Run(async () =>
            {
                await AudioService.Instance.PlayCommentaryAsync(r.Id, r.GetTtsScript());
                await MainThread.InvokeOnMainThreadAsync(async () => await _webView.EvaluateJavaScriptAsync("setAudioPlaying(false);"));
            });
        }

        private async Task<bool> DrawRouteAsync(string json)
        {
            var loc = _userLocation ?? new Location(10.7615, 106.7045); // Fallback to Vinh Khanh Center
            bool usingFallback = _userLocation == null;
            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var info = JsonSerializer.Deserialize<RouteRequest>(json, opts);
                if (info == null) return false;

                var ic = System.Globalization.CultureInfo.InvariantCulture;
                var origin = $"{loc.Latitude.ToString(ic)},{loc.Longitude.ToString(ic)}";
                var dest = $"{info.Lat.ToString(ic)},{info.Lng.ToString(ic)}";

                var url = $"https://maps.googleapis.com/maps/api/directions/json?origin={origin}&destination={dest}&mode=walking&avoid=ferries&alternatives=false&language={_currentLang}&key={KEY}";

                // Cảnh báo nếu đang dùng vị trí trung tâm thay GPS thật
                if (usingFallback)
                {
                    var warnMsg = _currentLang switch
                    {
                        "en" => "⚠️ GPS unavailable",
                        "zh" => "⚠️ GPS 不可用",
                        _ => "⚠️ Chưa có GPS"
                    };
                    MainThread.BeginInvokeOnMainThread(() => _statusLabel.Text = warnMsg);
                }

                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var resp = await http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(resp);
                var root = doc.RootElement;
                var status = root.GetProperty("status").GetString();
                System.Diagnostics.Debug.WriteLine($"[MapPage] Directions API status={status}");
                if (status != "OK")
                {
                    var errMsg = _currentLang switch
                    {
                        "en" => "No walking route",
                        "zh" => "无法找到路线",
                        _ => "Lỗi chỉ đường"
                    };
                    MainThread.BeginInvokeOnMainThread(() => _statusLabel.Text = errMsg);
                    return false;
                }

                var route = root.GetProperty("routes")[0];
                var leg = route.GetProperty("legs")[0];
                var dTxt = leg.GetProperty("distance").GetProperty("text").GetString() ?? "";
                var tTxt = leg.GetProperty("duration").GetProperty("text").GetString() ?? "";

                // Parse tung step de phan biet duong lon vs hem
                var steps = leg.GetProperty("steps");
                var segments = new System.Text.StringBuilder("[");
                bool firstSeg = true;

                // --- SEGMENT 1: Từ vị trí User đến điểm bắt đầu của Google Route (Hẻm/Kết nối) ---
                var routeStartPt = DecodePolyline(steps[0].GetProperty("polyline").GetProperty("points").GetString() ?? "").FirstOrDefault();
                if (routeStartPt != null)
                {
                    var connectionPts = new List<object> { new { lat = loc.Latitude, lng = loc.Longitude }, routeStartPt };
                    var connJson = System.Text.Json.JsonSerializer.Serialize(connectionPts);
                    segments.Append($"{{\"pts\":{connJson},\"alley\":true}}");
                    firstSeg = false;
                }

                foreach (var step in steps.EnumerateArray())
                {
                    var stepPoly = step.GetProperty("polyline").GetProperty("points").GetString() ?? "";
                    var stepDist = step.GetProperty("distance").GetProperty("value").GetDouble();
                    var stepDur = step.GetProperty("duration").GetProperty("value").GetDouble();
                    // Hem/duong nho: toc do < 0.85 m/s hoac doan < 20m
                    double speed = stepDur > 0 ? stepDist / stepDur : 1.2;
                    bool isAlley = speed < 0.85 || stepDist < 20;
                    var pts = DecodePolyline(stepPoly);
                    var ptsJson = System.Text.Json.JsonSerializer.Serialize(pts);
                    if (!firstSeg) segments.Append(",");
                    firstSeg = false;
                    segments.Append($"{{\"pts\":{ptsJson},\"alley\":{(isAlley ? "true" : "false")}}}");
                }

                // --- SEGMENT X: Từ điểm kết thúc của Google Route đến tọa độ thực của Quán (Nếu cần) ---
                var lastStep = steps[steps.GetArrayLength() - 1];
                var routeEndPt = DecodePolyline(lastStep.GetProperty("polyline").GetProperty("points").GetString() ?? "").LastOrDefault();
                if (routeEndPt != null)
                {
                    var endConnectionPts = new List<object> { routeEndPt, new { lat = info.Lat, lng = info.Lng } };
                    var endConnJson = System.Text.Json.JsonSerializer.Serialize(endConnectionPts);
                    segments.Append($",{{\"pts\":{endConnJson},\"alley\":true}}");
                }
                segments.Append("]");

                var name = info.Name.Replace("'", "\\'");
                var img = info.Img ?? string.Empty;
                if (string.IsNullOrEmpty(img) && info.Id > 0)
                {
                    if (_imgCache.TryGetValue(info.Id, out var b64) && !string.IsNullOrEmpty(b64))
                        img = b64;
                    else
                    {
                        var r = _restaurants.FirstOrDefault(x => x.Id == info.Id);
                        if (r != null) img = GetImg(r);
                    }
                }
                var imgEsc = img.Replace("'", "\\'");
                var dTxtEsc = dTxt.Replace("'", "\\'");
                var tTxtEsc = tTxt.Replace("'", "\\'");

                // Gửi toàn bộ dữ liệu chỉ đường trong 1 lần gọi JS duy nhất
                var segmentsStr = segments.ToString();
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await _webView.EvaluateJavaScriptAsync($"drawPremiumRoute({segmentsStr}, '{name}', '{dTxtEsc}', '{tTxtEsc}', '{imgEsc}');");
                });
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapPage] ❌ DrawRouteAsync error: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> DrawTourRouteAsync(List<Restaurant> tourPois, Location? userLoc)
        {
            try
            {
                if (tourPois == null || tourPois.Count == 0) return false;

                // Fallback về trung tâm Vĩnh Khánh nếu chưa có GPS
                var effectiveLoc = userLoc ?? new Location(10.7615, 106.7045);

                // 1. Sắp xếp các POI theo Nearest Neighbor
                var routeList = new List<Restaurant>();
                var unvisited = new List<Restaurant>(tourPois);
                var currentLoc = effectiveLoc;

                while (unvisited.Count > 0)
                {
                    var nearest = unvisited.OrderBy(r => GeofencingService.CalculateDistance(currentLoc.Latitude, currentLoc.Longitude, r.Latitude, r.Longitude)).First();
                    routeList.Add(nearest);
                    unvisited.Remove(nearest);
                    currentLoc = new Location(nearest.Latitude, nearest.Longitude);
                }

                var ic = System.Globalization.CultureInfo.InvariantCulture;
                var origin = $"{effectiveLoc.Latitude.ToString(ic)},{effectiveLoc.Longitude.ToString(ic)}";

                var destPoi = routeList.Last();
                var dest = $"{destPoi.Latitude.ToString(ic)},{destPoi.Longitude.ToString(ic)}";

                var waypoints = "";
                if (routeList.Count > 1)
                {
                    var wpList = routeList.Take(routeList.Count - 1).Select(r => $"{r.Latitude.ToString(ic)},{r.Longitude.ToString(ic)}");
                    waypoints = "&waypoints=" + string.Join("|", wpList);
                }

                var url = $"https://maps.googleapis.com/maps/api/directions/json?origin={origin}&destination={dest}{waypoints}&mode=walking&avoid=ferries&alternatives=false&language={_currentLang}&key={KEY}";

                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                var resp = await http.GetStringAsync(url);
                using var doc = System.Text.Json.JsonDocument.Parse(resp);
                var root = doc.RootElement;
                var status = root.GetProperty("status").GetString();
                if (status != "OK")
                {
                    MainThread.BeginInvokeOnMainThread(() => _statusLabel.Text = _currentLang == "vi" ? "Lỗi chỉ đường Tour" : "Tour route error");
                    return false;
                }

                var route = root.GetProperty("routes")[0];
                var legs = route.GetProperty("legs");

                int totalDistVal = 0;
                int totalDurVal = 0;

                var segments = new System.Text.StringBuilder("[");
                bool firstSeg = true;

                // Nối tất cả các chặng lại
                for (int i = 0; i < legs.GetArrayLength(); i++)
                {
                    var leg = legs[i];
                    totalDistVal += leg.GetProperty("distance").GetProperty("value").GetInt32();
                    totalDurVal += leg.GetProperty("duration").GetProperty("value").GetInt32();

                    var steps = leg.GetProperty("steps");

                    if (i == 0) // Nối điểm đầu
                    {
                        var routeStartPt = DecodePolyline(steps[0].GetProperty("polyline").GetProperty("points").GetString() ?? "").FirstOrDefault();
                        if (routeStartPt != null)
                        {
                            var connPts = new List<object> { new { lat = userLoc.Latitude, lng = userLoc.Longitude }, routeStartPt };
                            var connJson = System.Text.Json.JsonSerializer.Serialize(connPts);
                            segments.Append($"{{\"pts\":{connJson},\"alley\":true}}");
                            firstSeg = false;
                        }
                    }

                    foreach (var step in steps.EnumerateArray())
                    {
                        var stepPoly = step.GetProperty("polyline").GetProperty("points").GetString() ?? "";
                        var pts = DecodePolyline(stepPoly);
                        var ptsJson = System.Text.Json.JsonSerializer.Serialize(pts);
                        if (!firstSeg) segments.Append(",");
                        firstSeg = false;
                        segments.Append($"{{\"pts\":{ptsJson},\"alley\":false}}");
                    }

                    if (i == legs.GetArrayLength() - 1) // Nối điểm cuối
                    {
                        var routeEndPt = DecodePolyline(steps[steps.GetArrayLength() - 1].GetProperty("polyline").GetProperty("points").GetString() ?? "").LastOrDefault();
                        if (routeEndPt != null)
                        {
                            var endPts = new List<object> { routeEndPt, new { lat = destPoi.Latitude, lng = destPoi.Longitude } };
                            var endConnJson = System.Text.Json.JsonSerializer.Serialize(endPts);
                            segments.Append($",{{\"pts\":{endConnJson},\"alley\":true}}");
                        }
                    }
                }
                segments.Append("]");

                string dTxt = totalDistVal >= 1000 ? $"{(totalDistVal / 1000.0):F1} km" : $"{totalDistVal} m";
                int mins = totalDurVal / 60;
                string tTxt = mins >= 60 ? $"{mins / 60} h {mins % 60} ph" : $"{mins} ph";

                var name = $"Tour: {_tourName}".Replace("'", "\\'");
                var img = ""; // Không có ảnh chung cho tour, để trống sẽ dùng icon
                var segmentsStr = segments.ToString();

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await _webView.EvaluateJavaScriptAsync($"drawPremiumRoute({segmentsStr}, '{name}', '{dTxt}', '{tTxt}', '{img}');");
                });
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapPage] ❌ DrawTourRouteAsync error: {ex.Message}");
                return false;
            }
        }



        // Decode Google Maps encoded polyline ở C# - tránh JS 32-bit bitwise overflow
        private static List<object> DecodePolyline(string encoded)
        {
            var result = new List<object>();
            int index = 0, lat = 0, lng = 0;
            while (index < encoded.Length)
            {
                int b, shift = 0, result_val = 0;
                do { b = encoded[index++] - 63; result_val |= (b & 0x1f) << shift; shift += 5; } while (b >= 0x20);
                lat += ((result_val & 1) != 0 ? ~(result_val >> 1) : (result_val >> 1));
                shift = 0; result_val = 0;
                do { b = encoded[index++] - 63; result_val |= (b & 0x1f) << shift; shift += 5; } while (b >= 0x20);
                lng += ((result_val & 1) != 0 ? ~(result_val >> 1) : (result_val >> 1));
                result.Add(new { lat = lat / 1e5, lng = lng / 1e5 });
            }
            return result;
        }
    }
    public class RouteRequest
    {
        public int Id { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Img { get; set; } = string.Empty;
    }
}
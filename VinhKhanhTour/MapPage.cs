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

namespace VinhKhanhTour
{
    public class MapPage : ContentPage
    {
        private readonly WebView _webView;
        private readonly Label _statusLabel;
        private readonly Label _audioBar;
        private Button _btnExitTour = null!;
        private readonly GeofencingService _geofencing = new();
        private Location? _userLocation;
        private Restaurant? _nearestRestaurant;
        private List<Restaurant> _restaurants = new();
        private readonly Dictionary<int, DateTime> _lastNotified = new();
        private bool _htmlLoaded = false;
        private const int COOLDOWN = 5;
        private const string KEY = Config.GoogleMapsApiKey;

        private static string GetImg(Restaurant r)
        {
            if (!string.IsNullOrWhiteSpace(r.ImageUrl))
                return "file:///android_asset/" + r.ImageUrl;
            return "";
        }

        private string? _tourName = null;
        private string _currentLang = Preferences.Default.Get("app_lang", "vi");

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

            _audioBar = new Label
            {
                Text = string.Empty,
                BackgroundColor = Color.FromArgb("#1565C0"),
                TextColor = Colors.White,
                Padding = new Thickness(16, 10),
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                IsVisible = false
            };

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
                    _audioBar.IsVisible = isPlaying;
                    _audioBar.Text = isPlaying
                        ? $"🎧 Đang phát: {AudioService.Instance.CurrentTrack ?? "..."}"
                        : string.Empty;
                });
            };

            var grid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto }
                }
            };
            grid.Add(_webView, 0, 0);
            grid.Add(_btnExitTour, 0, 1);
            grid.Add(_audioBar, 0, 2);
            grid.Add(_statusLabel, 0, 3);
            Content = grid;

            _webView.IsEnabled = true;
            _ = InitAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_tourName != null) { }
            else if (_htmlLoaded && _restaurants.Count > 0)
            {
                _ = _webView.EvaluateJavaScriptAsync("if(typeof map !== 'undefined' && map) { google.maps.event.trigger(map, 'resize'); }");
            }
            else if (!_htmlLoaded)
            {
                _ = InitAsync();
            }

            _ = RequestLocationPermissionAsync();
            _ = StartLocationTrackingAsync();
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
                    if (_htmlLoaded)
                        await UpdateMapLocation(location.Latitude, location.Longitude);
                }
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
                        _statusLabel.Text = _currentLang switch { "en" => "GPS permission required", "zh" => "需要 GPS 权限", _ => "Cần cấp quyền GPS để tiếp tục" };
                        return;
                    }
                }
                _statusLabel.Text = _currentLang switch { "en" => "GPS ready", "zh" => "GPS 已准备好", _ => "GPS sẵn sàng" };
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
            var uri = new Uri(e.Url);
            var q = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var ic = System.Globalization.CultureInfo.InvariantCulture;

            switch (uri.Host.ToLower())
            {
                case "locationupdated":
                    if (double.TryParse(q["lat"], System.Globalization.NumberStyles.Float, ic, out double lat) &&
                        double.TryParse(q["lng"], System.Globalization.NumberStyles.Float, ic, out double lng))
                    {
                        _userLocation = new Location(lat, lng);
                        _ = VinhKhanhTour.Services.AnalyticsService.Instance.RecordGpsPointAsync(lat, lng);
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            try
                            {
                                if (_tourName != null)
                                    await _webView.EvaluateJavaScriptAsync($"setTourMode(true,'{_tourName.Replace("'", "\'")}');");
                            }
                            catch { }
                            _ = CheckNearbyAsync(_userLocation);
                        });
                    }
                    break;
                case "routerequested":
                    var d = Uri.UnescapeDataString(q["data"] ?? "");
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
                            
                        _statusLabel.Text = lang switch { "en" => "Map is ready", "zh" => "地图已准备好", _ => "Bản đồ sẵn sàng" };
                        if (_btnExitTour.IsVisible)
                            _btnExitTour.Text = lang switch { "en" => "← Exit Tour", "zh" => "← 退出行程", _ => "← Thoát Tour" };
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
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(2000);
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
                var img = GetImg(r);
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
            lines.Add("body,html{height:100%;width:100%;overflow:hidden;background-color:#F0F6FF;}");;
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
            lines.Add("#langDropdown{position:absolute;top:56px;right:0;background:rgba(255,255,255,0.97);border:1px solid rgba(21,101,192,0.3);border-radius:16px;box-shadow:0 10px 40px rgba(21,101,192,0.2);display:none;flex-direction:column;overflow:hidden;backdrop-filter:blur(10px);}");
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
            lines.Add("#btnMenu{position:fixed;right:16px;bottom:260px;z-index:1000;}");
            lines.Add("#btnG{position:fixed;right:16px;bottom:326px;z-index:1000;color:#fff;background:linear-gradient(135deg,#1565C0,#42A5F5);border:none;}");
            lines.Add("#btnStop{position:fixed;right:16px;bottom:392px;z-index:1000;background:linear-gradient(135deg,#d32f2f,#E91E63);color:white;border:none;display:none;}");
            
            // Bottom Sheets
            lines.Add(".botSheet{position:fixed;bottom:0;left:0;right:0;z-index:9500;background:rgba(255,255,255,0.98);backdrop-filter:blur(20px);border-radius:28px 28px 0 0;box-shadow:0 -8px 30px rgba(21,101,192,0.15);transform:translateY(100%);transition:transform 0.4s cubic-bezier(0.2,0.8,0.2,1);display:flex;flex-direction:column;border-top:1px solid rgba(21,101,192,0.15)}");
            lines.Add(".botSheet.open{transform:translateY(0)}");
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
            lines.Add("#sheet{z-index:9000;max-height:75vh;overflow-y:auto;padding-bottom:32px}");
            lines.Add(".siw{margin:0 20px 16px;height:180px;border-radius:20px;overflow:hidden;position:relative;background:#152535;}");
            lines.Add(".simg{width:100%;height:100%;object-fit:cover}");
            lines.Add(".sph{width:100%;height:100%;display:flex;align-items:center;justify-content:center;font-size:80px;}");
            lines.Add(".sOvl{position:absolute;bottom:0;left:0;right:0;height:80px;background:linear-gradient(0deg,rgba(15,30,46,1) 0%,rgba(15,30,46,0) 100%);}");
            lines.Add(".sin{padding:0 24px}");
            lines.Add(".sbg{display:inline-block;background:rgba(33,150,243,0.12);color:#1565C0;font-size:11px;font-weight:700;padding:6px 12px;border-radius:24px;margin-bottom:8px;letter-spacing:0.5px}");
            lines.Add(".snm{font-size:22px;font-weight:700;color:#0D2137;margin-bottom:6px}");
            lines.Add(".srt{font-size:14px;color:#FFCA28;font-weight:700;margin-bottom:16px}");
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
            
            lines.Add("#tst{position:fixed;top:80px;left:50%;transform:translateX(-50%);background:rgba(21,101,192,0.95);color:white;padding:10px 24px;border-radius:30px;font-size:14px;font-weight:600;display:none;z-index:9999;box-shadow:0 8px 30px rgba(21,101,192,0.4);}");
            lines.Add("</style></head><body>");

            lines.Add("<div id='map'></div>");
            lines.Add("<div id='tst'></div>");
            
            lines.Add("<div id='headerRow'>");
            string initialFlag = _currentLang == "en" ? "🇺🇸" : (_currentLang == "zh" ? "🇨🇳" : "🇻🇳");
            lines.Add("  <div id='searchBar'><span id='btnSearch'>🔍</span><input id='searchInput' type='text' placeholder='Tìm quán ăn...' oninput='onSearch(this.value)' onfocus='showResults()'/><button id='btnClearSearch' onclick='clearSearch()'>✕</button></div>");
            lines.Add("  <div id='langBtn' onclick='toggleLang()'><span id='currFlag'>" + initialFlag + "</span>");
            lines.Add("    <div id='langDropdown'>");
            lines.Add("      <div class='ldItem' onclick='chgL(\"vi\", event)'>🇻🇳 Tiếng Việt</div>");
            lines.Add("      <div class='ldItem' onclick='chgL(\"en\", event)'>🇺🇸 English</div>");
            lines.Add("      <div class='ldItem' onclick='chgL(\"zh\", event)'>🇨🇳 中文</div>");
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
            lines.Add("  <div class='siw'><img id='simg' class='simg' src='' onerror=\"this.style.display='none';document.getElementById('sph').style.display='flex'\"><div id='sph' class='sph' style='display:none'></div><div class='sOvl'></div></div>");
            lines.Add("  <div class='sin'><div class='sbg' id='txCat'>QUÁN ĂN - VĨNH KHÁNH</div><div class='snm' id='snm'></div><div class='srt' id='srt'></div></div>");
            lines.Add("  <div class='sac'><button class='bcl' onclick='closeS()'>✕</button><button class='bfav' onclick='if(cur) toggleFav(cur.id,event)' id='txFavBtn'>🤍</button><button class='bau' onclick='if(cur) speakPoi(cur.id,event)' id='txAud'><svg width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M3 18v-6a9 9 0 0 1 18 0v6'></path><path d='M21 19a2 2 0 0 1-2 2h-1a2 2 0 0 1-2-2v-3a2 2 0 0 1 2-2h3zM3 19a2 2 0 0 0 2 2h1a2 2 0 0 0 2-2v-3a2 2 0 0 0-2-2H3z'></path></svg></button><button class='bdr' onclick='reqR()' id='txDir'><svg width='18' height='18' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round' style='vertical-align:text-bottom;margin-right:6px;margin-bottom:1px'><polygon points='3 11 22 2 13 21 11 13 3 11'></polygon></svg> <span id='txDirText'>Chỉ đường</span></button></div>");
            lines.Add("  <div class='sdv'></div>");
            lines.Add("  <div class='sdt'><div class='sro'><span class='sic'>💬</span><span id='sde'></span></div><div class='sro'><span class='sic'>📍</span><span id='sad'></span></div><div class='sro'><span class='sic'>⏰</span><span id='shr'></span></div></div>");
            lines.Add("</div>");
            
            lines.Add("<div class='botSheet' id='rs'>");
            lines.Add("  <div class='dragBar'></div>");
            lines.Add("  <div class='rrw'><div class='ric' id='ricImg'>📍</div><div><div class='rnm' id='rnm'></div><div class='rsb' id='txWalk'>🚶 Chỉ đường đi bộ</div></div></div>");
            lines.Add("  <div class='rst'><div class='rsc'><div class='rvl' id='rdst'></div><div class='rll' id='txDist'>Khoảng cách</div></div><div class='rsc'><div class='rvl' id='rtim'></div><div class='rll' id='txDur'>Thời gian</div></div></div>");
            lines.Add("  <button class='ben' onclick='endR()' id='txEnd'>✕ Dừng chỉ đường</button>");
            lines.Add("</div>");

            lines.Add("<script>");
            lines.Add("var map,uMk,uCir,rLn,poi={},uP=null,cur=null,cL='" + _currentLang + "';");
            lines.Add("var ALL=" + data + ";");
            lines.Add("var CTR={lat:10.7615,lng:106.7045};");
            lines.Add("var L={");
            lines.Add("  vi:{search:'Tìm quán ăn...',plist:'Danh sách điểm đến',nodata:'Không tìm thấy quán',cat:'QUÁN ĂN - VĨNH KHÁNH',dir:'Mở dẫn đường',dist:'KHOẢNG CÁCH',dur:'THỜI GIAN',end:'✕ KẾT THÚC DẪN ĐƯỜNG',walk:'🚶 Chỉ đường đi bộ'},");
            lines.Add("  en:{search:'Search spots...',plist:'Destinations list',nodata:'No spots found',cat:'STREET FOOD - VINH KHANH',dir:'Get Directions',dist:'DISTANCE',dur:'DURATION',end:'✕ EXIT NAVIGATION',walk:'🚶 Walking directions'},");
            lines.Add("  zh:{search:'搜索餐厅...',plist:'目的地列表',nodata:'未找到餐厅',cat:'美食街 - 永庆',dir:'获取路线',dist:'距离',dur:'持续时间',end:'✕ 退出导航',walk:'🚶 步行路线'}");
            lines.Add("};");

            lines.Add("function toggleLang(){ var d=document.getElementById('langDropdown'); d.style.display=d.style.display==='flex'?'none':'flex'; }");
            lines.Add("function chgL(l,ev){ if(ev)ev.stopPropagation(); cL=l; document.getElementById('currFlag').textContent=l==='vi'?'🇻🇳':(l==='en'?'🇺🇸':'🇨🇳'); document.getElementById('langDropdown').style.display='none'; applyLang(); window.location.href='maui://changelang?lang='+l; }");
            lines.Add("function applyLang(){ var t=L[cL]; document.getElementById('searchInput').placeholder=t.search; document.getElementById('txPList').textContent=t.plist; document.getElementById('txCat').textContent=t.cat; document.getElementById('txDirText').textContent=t.dir; document.getElementById('txDist').textContent=t.dist; document.getElementById('txDur').textContent=t.dur; document.getElementById('txEnd').textContent=t.end; document.getElementById('txWalk').textContent=t.walk; if(document.getElementById('menuSheet').classList.contains('open')) renderMenuList(); }");
            
            lines.Add("function st(r){return r.toFixed(1);}");
            lines.Add("function toast(m){var t=document.getElementById('tst');t.textContent=m;t.style.display='block';setTimeout(function(){t.style.display='none';},2500);}");
            lines.Add("function toU(){if(uP)map.panTo(uP);else toast(cL==='vi'?'Đang tìm GPS...':(cL==='en'?'Waiting for GPS...':'正在搜索 GPS...'));}");
            lines.Add("function pick(r){cur=r;map.panTo({lat:r.lat,lng:r.lng});opS(r);}");
            lines.Add("function opS(r){var si=document.getElementById('simg'),sp=document.getElementById('sph');var foodEmoji='🍽️';if(r.name.includes('Oc'))foodEmoji='🦪';else if(r.name.includes('Bun')||r.name.includes('Pho'))foodEmoji='🍜';if(r.img){si.src=r.img;si.style.display='block';sp.style.display='none';}else{si.style.display='none';sp.innerHTML=foodEmoji;sp.style.display='flex';}document.getElementById('snm').textContent=r.name;document.getElementById('srt').innerHTML='⭐ '+st(r.rating);document.getElementById('sde').textContent=r.desc;document.getElementById('sad').textContent=r.addr;document.getElementById('shr').textContent=r.hours;var fb=document.getElementById('txFavBtn');if(fb){fb.innerHTML=r.fav?'❤️':'🤍';}document.getElementById('sheet').classList.add('open');}");
            lines.Add("function toggleFav(id,ev){if(ev)ev.stopPropagation(); window.location.href='maui://togglefav?id='+id;}");
            lines.Add("function closeS(){document.getElementById('sheet').classList.remove('open');document.getElementById('menuSheet').classList.remove('open');cur=null;}");
            lines.Add("function reqR(){if(!cur){return;}if(!uP){toast('Đang lấy vị trí trung tâm do không có GPS');}window.location.href='maui://routerequested?data='+encodeURIComponent(JSON.stringify({lat:cur.lat,lng:cur.lng,name:cur.name,img:cur.img||''}))}");
            lines.Add("function shwR(n,d,t,img){document.getElementById('rnm').textContent=n;document.getElementById('rdst').textContent=d;document.getElementById('rtim').textContent=t;var ric=document.getElementById('ricImg');ric.innerHTML=img?'<img src=\"'+img+'\">':'📍';document.getElementById('rs').classList.add('open');document.getElementById('sheet').classList.remove('open');}");
            lines.Add("function endR(){if(rLn){rLn.setMap(null);rLn=null;}document.getElementById('rs').classList.remove('open');}");
            lines.Add("function dec(enc){var pts=[],i=0,len=enc.length,lat=0,lng=0;while(i<len){var b,s=0,r=0;do{b=enc.charCodeAt(i++)-63;r|=(b&0x1f)<<s;s+=5;}while(b>=0x20);lat+=((r&1)?~(r>>1):(r>>1));s=0;r=0;do{b=enc.charCodeAt(i++)-63;r|=(b&0x1f)<<s;s+=5;}while(b>=0x20);lng+=((r&1)?~(r>>1):(r>>1));pts.push({lat:lat/1e5,lng:lng/1e5});}return pts;}");
            lines.Add("function drR(enc,n,d,t,img){if(rLn){rLn.setMap(null);rLn=null;}var path=dec(enc);rLn=new google.maps.Polyline({path:path,geodesic:true,strokeColor:'#42A5F5',strokeWeight:6,strokeOpacity:1});rLn.setMap(map);var b=new google.maps.LatLngBounds();path.forEach(function(p){b.extend(p);});map.fitBounds(b,{padding:100});shwR(n,d,t,img);}");
            lines.Add("function sPos(lat,lng){uP={lat:lat,lng:lng};if(uMk){uMk.setPosition(uP);uCir.setCenter(uP);}else{uMk=new google.maps.Marker({position:uP,map:map,zIndex:1000,icon:{path:google.maps.SymbolPath.CIRCLE,scale:12,fillColor:'#42A5F5',fillOpacity:1,strokeColor:'white',strokeWeight:3}});uCir=new google.maps.Circle({center:uP,radius:35,map:map,fillColor:'#42A5F5',fillOpacity:0.25,strokeColor:'#42A5F5',strokeOpacity:0,strokeWeight:0});map.panTo(uP);}window.location.href='maui://locationupdated?lat='+lat+'&lng='+lng;}");
            lines.Add("function gps(){if(navigator.geolocation){navigator.geolocation.watchPosition(function(p){sPos(p.coords.latitude,p.coords.longitude);},function(err){},{enableHighAccuracy:true,timeout:10000,maximumAge:3000});}}");
            lines.Add("function stopAudio(){window.location.href='maui://stopaudio';}");
            lines.Add("function speakPoi(id,ev){ev.stopPropagation(); window.location.href='maui://speakpoi?id='+id;}");
            lines.Add("function setTourMode(on,name){}");
            lines.Add("function setAudioPlaying(on){document.getElementById('btnStop').style.display=on?'flex':'none';}");
            lines.Add("function unAccent(str){str=str.toLowerCase();str=str.replace(/à|á|ạ|ả|ã|â|ầ|ấ|ậ|ẩ|ẫ|ă|ằ|ắ|ặ|ẳ|ẵ/g,'a');str=str.replace(/è|é|ẹ|ẻ|ẽ|ê|ề|ế|ệ|ể|ễ/g,'e');str=str.replace(/ì|í|ị|ỉ|ĩ/g,'i');str=str.replace(/ò|ó|ọ|ỏ|õ|ô|ồ|ố|ộ|ổ|ỗ|ơ|ờ|ớ|ợ|ở|ỡ/g,'o');str=str.replace(/ù|ú|ụ|ủ|ũ|ư|ừ|ứ|ự|ử|ữ/g,'u');str=str.replace(/ỳ|ý|ỵ|ỷ|ỹ/g,'y');str=str.replace(/đ/g,'d');return str;}");
            lines.Add("function onSearch(v){var inp=document.getElementById('btnClearSearch');inp.style.display=v?'block':'none';if(!v){document.getElementById('searchResults').style.display='none';return;}var q=unAccent(v.trim());var res=ALL.filter(function(r){return (' '+unAccent(r.name)).indexOf(' '+q)!==-1;});renderSearchResults(res);}");
            lines.Add("function renderSearchResults(res){var d=document.getElementById('searchResults');if(!res.length){d.innerHTML='<div style=\"padding:16px;text-align:center;color:#8ba0b2\">'+L[cL].nodata+'</div>';d.style.display='block';return;}var h='';res.forEach(function(r){var imgHtml=r.img?'<img src=\"'+r.img+'\">':'<div>🍜</div>';h+='<div class=\"srItem\" onclick=\"selectSearchResult('+r.id+')\"><div class=\"srImg\">'+imgHtml+'</div><div><div class=\"srName\">'+r.name+'</div><div class=\"srAddr\">'+r.addr+'</div></div></div>';});d.innerHTML=h;d.style.display='block';}");
            lines.Add("function selectSearchResult(id){var r=ALL.find(function(x){return x.id===id;});if(!r)return;clearSearch();pick(r);}");
            lines.Add("function showResults(){if(document.getElementById('searchInput').value)document.getElementById('searchResults').style.display='block';}");
            lines.Add("function clearSearch(){document.getElementById('searchInput').value='';document.getElementById('searchResults').style.display='none';document.getElementById('btnClearSearch').style.display='none';}");
            
            lines.Add("function toggleMenu(){var m=document.getElementById('menuSheet');m.classList.toggle('open');if(m.classList.contains('open'))renderMenuList();}");
            lines.Add("function renderMenuList(){var h='';var sNav='<svg width=\"20\" height=\"20\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><polygon points=\"3 11 22 2 13 21 11 13 3 11\"></polygon></svg>';var sAud='<svg width=\"20\" height=\"20\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M3 18v-6a9 9 0 0 1 18 0v6\"></path><path d=\"M21 19a2 2 0 0 1-2 2h-1a2 2 0 0 1-2-2v-3a2 2 0 0 1 2-2h3zM3 19a2 2 0 0 0 2 2h1a2 2 0 0 0 2-2v-3a2 2 0 0 0-2-2H3z\"></path></svg>';ALL.forEach(function(r){var imgHtml=r.img?'<img src=\"'+r.img+'\">':'<div>🍲</div>';h+='<div class=\"mitem\">'+'<div class=\"mimg\">'+imgHtml+'</div>'+'<div class=\"minfo\"><div class=\"mnm\">'+r.name+'</div><div class=\"mrt\">⭐ '+st(r.rating)+'</div><div class=\"mhr\">🕒 '+r.hours+'</div></div><div class=\"mbtns\"><button class=\"mbtn mbtnNav\" onclick=\"menuNav('+r.id+',event)\">'+sNav+'</button><button class=\"mbtn mbtnAudio\" onclick=\"speakPoi('+r.id+',event)\">'+sAud+'</button></div></div>';});document.getElementById('menuList').innerHTML=h;}");
            lines.Add("function menuNav(id,ev){ev.stopPropagation();var r=ALL.find(function(x){return x.id===id;});if(!r)return;cur=r;document.getElementById('menuSheet').classList.remove('open');reqR();}");
            
            lines.Add("document.addEventListener('click',function(e){var sr=document.getElementById('searchResults');if(sr&&!sr.contains(e.target)&&e.target.id!=='searchInput')sr.style.display='none';var lb=document.getElementById('langBtn');var ld=document.getElementById('langDropdown');if(ld&&!lb.contains(e.target))ld.style.display='none';});");
            lines.Add("function initMap(){try{var mapDiv=document.getElementById('map');if(!mapDiv)return;var mapOptions={center:CTR,zoom:16,mapTypeControl:false,streetViewControl:false,fullscreenControl:false,mapTypeId:'roadmap'};map=new google.maps.Map(mapDiv,mapOptions);if(ALL&&ALL.length>0){ALL.forEach(function(r){var mk=new google.maps.Marker({position:{lat:r.lat,lng:r.lng},map:map,title:r.name});mk.addListener('click',function(){pick(r);});});}map.addListener('click',function(){closeS();});google.maps.event.trigger(map, 'resize');setTimeout(gps,1000); applyLang(); }catch(e){console.error(e);}}");
            lines.Add("</script>");
            lines.Add("<script src='https://maps.googleapis.com/maps/api/js?key=" + KEY + "'></script>");
            lines.Add("<script>window.addEventListener('load',function(){setTimeout(initMap,100);});</script>");
            lines.Add("</body></html>");

            return string.Join("\n", lines);
        }

        private async Task CheckNearbyAsync(Location location)
        {
            if (_restaurants.Count == 0) return;
            Restaurant? nearest = null;
            double minDist = double.MaxValue;
            foreach (var r in _restaurants)
            {
                double d = _geofencing.CalculateDistance(location.Latitude, location.Longitude, r.Latitude, r.Longitude);
                if (d < minDist) { minDist = d; nearest = r; }
            }
            if (nearest == null) return;
            _nearestRestaurant = nearest;
            
            string langNear = _currentLang switch { "en" => "Nearest", "zh" => "最近", _ => "Gần nhất" };
            MainThread.BeginInvokeOnMainThread(() => _statusLabel.Text = $"{langNear}: {nearest.Name} ({minDist:F0}m)");
            
            if (minDist > 50) return;
            if (_lastNotified.TryGetValue(nearest.Id, out DateTime last) &&
                (DateTime.Now - last).TotalMinutes < COOLDOWN) return;
            _lastNotified[nearest.Id] = DateTime.Now;
            await SpeakAsync(nearest);
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayAlert("Location Alert!", $"{nearest.Name}\n{nearest.Description}", "OK"));
            await App.Database.SaveVisitAsync(new VisitHistory { RestaurantId = nearest.Id, VisitedAt = DateTime.Now });
        }

        public void LoadTourPois(List<Restaurant> restaurants, string tourName)
        {
            _restaurants = restaurants;
            _tourName = tourName;
            _htmlLoaded = false;
            _lastNotified.Clear();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _btnExitTour.IsVisible = true;
                _btnExitTour.Text = _currentLang switch { "en" => $"← Exit {tourName}", "zh" => $"← 退出 {tourName}", _ => $"← Thoát {tourName}" };
                _statusLabel.Text = $"{(_currentLang=="vi"?"Tour":"Tour")}: {tourName} — {restaurants.Count} {(_currentLang == "vi" ? "điểm" : "spots")}";
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

        private async Task DrawRouteAsync(string json)
        {
            var loc = _userLocation ?? new Location(10.7615, 106.7045); // Fallback to Vinh Khanh Center
            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var info = JsonSerializer.Deserialize<RouteRequest>(json, opts);
                if (info == null) return;
                var ic = System.Globalization.CultureInfo.InvariantCulture;
                var origin = $"{loc.Latitude.ToString(ic)},{loc.Longitude.ToString(ic)}";
                var dest = $"{info.Lat.ToString(ic)},{info.Lng.ToString(ic)}";
                var url = $"https://maps.googleapis.com/maps/api/directions/json?origin={origin}&destination={dest}&mode=walking&language={_currentLang}&key={KEY}";

                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var resp = await http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(resp);
                var root = doc.RootElement;
                if (root.GetProperty("status").GetString() != "OK") return;

                var route = root.GetProperty("routes")[0];
                var leg = route.GetProperty("legs")[0];
                var poly = route.GetProperty("overview_polyline").GetProperty("points").GetString() ?? "";
                var dTxt = leg.GetProperty("distance").GetProperty("text").GetString() ?? "";
                var tTxt = leg.GetProperty("duration").GetProperty("text").GetString() ?? "";

                var esc = poly.Replace("\\", "\\\\").Replace("'", "\\'");
                var name = info.Name.Replace("'", "\\'");
                var img = (info.Img ?? string.Empty).Replace("'", "\\'");
                await MainThread.InvokeOnMainThreadAsync(async () => await _webView.EvaluateJavaScriptAsync($"drR('{esc}','{name}','{dTxt}','{tTxt}','{img}');"));
            }
            catch { }
        }
    }

    public class RouteRequest
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Img { get; set; } = string.Empty;
    }
}
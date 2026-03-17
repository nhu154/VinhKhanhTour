using System.Text;
using System.Text.Json;
using VinhKhanhTour.Models;
using VinhKhanhTour.Services;

namespace VinhKhanhTour
{
    public class MapPage : ContentPage
    {
        private WebView _webView;
        private bool _mapReady = false;
        private Label _statusLabel;

        private bool _isTracking = false;
        private CancellationTokenSource? _cancelToken;
        private GeofencingService _geofencing = new GeofencingService();
        private Location? _userLocation;
        private Restaurant? _nearestRestaurant;
        private List<Restaurant> _restaurants = new();

        private Dictionary<int, DateTime> _lastNotified = new();
        private const int COOLDOWN_MINUTES = 5;

        public MapPage()
        {
            Title = "Bản đồ";

            var grid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = GridLength.Auto }
                }
            };

            _webView = new WebView
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };
            _webView.Navigated += OnMapNavigated;
            _webView.Navigating += OnMapNavigating;

            _statusLabel = new Label
            {
                Text = "Đang tải bản đồ...",
                BackgroundColor = Color.FromArgb("#2C3E50"),
                TextColor = Colors.White,
                Padding = new Thickness(15, 8),
                FontSize = 13,
                HorizontalTextAlignment = TextAlignment.Center
            };

            grid.Add(_webView, 0, 0);
            grid.Add(_statusLabel, 0, 1);
            Content = grid;

            _ = InitAsync();
        }

        private async Task InitAsync()
        {
            _restaurants = await App.Database.GetRestaurantsAsync();
            LoadMapHtml();
        }

        private void OnMapNavigating(object? sender, WebNavigatingEventArgs e)
        {
            if (!e.Url.StartsWith("maui://")) return;
            e.Cancel = true;
            var uri = new Uri(e.Url);
            if (uri.Host.ToLower() != "routerequested") return;
            var raw = System.Web.HttpUtility.ParseQueryString(uri.Query)["data"] ?? "";
            var data = Uri.UnescapeDataString(raw);
            MainThread.BeginInvokeOnMainThread(() => _ = DrawRouteAsync(data));
        }

        private void OnMapNavigated(object? sender, WebNavigatedEventArgs e)
        {
            if (e.Result != WebNavigationResult.Success) return;
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // Chờ 5 giây để CDN load xong trên emulator
                _statusLabel.Text = "⏳ Đang tải Leaflet...";
                await Task.Delay(5000);

                // Kiểm tra Leaflet đã load chưa
                var leafletCheck = await _webView.EvaluateJavaScriptAsync(
                    "typeof L !== 'undefined' ? 'ok' : 'missing'");
                System.Diagnostics.Debug.WriteLine($"Leaflet check: {leafletCheck}");
                _statusLabel.Text = $"Leaflet: {leafletCheck}";

                _mapReady = true;

                // Test tọa độ cứng trước
                var testResult = await _webView.EvaluateJavaScriptAsync(
                    "updateUserLocation(10.7615, 106.7045);");
                System.Diagnostics.Debug.WriteLine($"Test location: {testResult}");

                await StartTrackingAsync();
            });
        }

        private void LoadMapHtml()
        {
            var markersJson = BuildMarkersJson();
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>
    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
    <link rel='stylesheet' href='https://unpkg.com/leaflet-routing-machine@3.2.12/dist/leaflet-routing-machine.css'/>
    <script src='https://unpkg.com/leaflet-routing-machine@3.2.12/dist/leaflet-routing-machine.js'></script>
    <style>
        * {{ box-sizing:border-box; margin:0; padding:0; }}
        body,html {{ height:100%; font-family:sans-serif; }}
        #map {{ width:100vw; height:100vh; }}
        .leaflet-routing-container {{ display:none !important; }}
        .user-dot {{
            width:18px; height:18px;
            background:#4285F4; border:3px solid white;
            border-radius:50%; animation:pulse 2s infinite;
        }}
        @keyframes pulse {{
            0%   {{ box-shadow:0 0 0 0 rgba(66,133,244,.6); }}
            70%  {{ box-shadow:0 0 0 14px rgba(66,133,244,0); }}
            100% {{ box-shadow:0 0 0 0 rgba(66,133,244,0); }}
        }}
        .poi-popup  {{ min-width:185px; font-size:13px; line-height:1.6; }}
        .poi-name   {{ font-size:15px; font-weight:bold; color:#2C3E50; margin-bottom:3px; }}
        .poi-desc   {{ color:#555; margin-bottom:3px; }}
        .poi-meta   {{ color:#888; font-size:12px; margin-bottom:6px; }}
        .poi-rating {{ color:#f39c12; font-weight:bold; margin-bottom:8px; }}
        .btn-route  {{
            display:block; width:100%; padding:8px 0;
            background:#27ae60; color:white;
            border:none; border-radius:6px;
            font-size:14px; font-weight:bold; cursor:pointer;
        }}
        .btn-clear  {{
            display:block; width:100%; margin-top:5px; padding:6px 0;
            background:#e74c3c; color:white;
            border:none; border-radius:6px; font-size:13px; cursor:pointer;
        }}
        #toast {{
            position:fixed; bottom:70px; left:50%; transform:translateX(-50%);
            background:rgba(0,0,0,.75); color:white;
            padding:10px 20px; border-radius:20px;
            font-size:13px; display:none; z-index:9999; white-space:nowrap;
        }}
        #loadingMsg {{
            position:fixed; top:50%; left:50%; transform:translate(-50%,-50%);
            background:rgba(0,0,0,.6); color:white;
            padding:16px 24px; border-radius:12px; font-size:14px; z-index:9999;
        }}
    </style>
</head>
<body>
<div id='map'></div>
<div id='toast'></div>
<div id='loadingMsg'>Đang tải bản đồ...</div>
<script>
    window.addEventListener('load', function() {{
        document.getElementById('loadingMsg').style.display = 'none';
    }});

    var map = L.map('map',{{zoomControl:true,attributionControl:false}})
               .setView([10.761500,106.704500],17);
    L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png',{{maxZoom:20}}).addTo(map);

    var userLatLng   = null;
    var userMarker   = null;
    var accCircle    = null;
    var currentRoute = null;
    var poiMarkers   = {{}};

    var poiIcon = L.divIcon({{
        className:'',
        html:'<div style=""width:22px;height:22px;background:#FF6B6B;border:3px solid white;border-radius:50%;box-shadow:0 2px 6px rgba(0,0,0,.35)""></div>',
        iconSize:[22,22],iconAnchor:[11,11],popupAnchor:[0,-14]
    }});
    var poiIconHL = L.divIcon({{
        className:'',
        html:'<div style=""width:28px;height:28px;background:#e74c3c;border:3px solid white;border-radius:50%;box-shadow:0 0 14px rgba(231,76,60,.9)""></div>',
        iconSize:[28,28],iconAnchor:[14,14],popupAnchor:[0,-16]
    }});
    var userIcon = L.divIcon({{
        className:'',
        html:'<div class=""user-dot""></div>',
        iconSize:[18,18],iconAnchor:[9,9]
    }});

    function showToast(msg){{
        var t=document.getElementById('toast');
        t.textContent=msg; t.style.display='block';
        setTimeout(function(){{t.style.display='none';}},3500);
    }}
    function clearRoute(){{
        if(currentRoute){{ map.removeLayer(currentRoute); currentRoute=null; }}
    }}
    function routeTo(destLat,destLng,name){{
        if(!userLatLng){{ showToast('\u26a0\ufe0f Ch\u01b0a l\u1ea5y \u0111\u01b0\u1ee3c v\u1ecb tr\u00ed GPS'); return; }}
        var data=encodeURIComponent(JSON.stringify({{lat:destLat,lng:destLng,name:name}}));
        window.location.href='maui://routerequested?data='+data;
    }}
    function drawRoute(coordsJson){{
        clearRoute();
        var coords=JSON.parse(coordsJson);
        currentRoute=L.polyline(coords.map(function(c){{return[c.lat,c.lng];}}),
            {{color:'#27ae60',weight:5,opacity:0.85}}).addTo(map);
        map.fitBounds(currentRoute.getBounds(),{{padding:[30,30]}});
    }}
    function makePopup(r){{
        return '<div class=""poi-popup"">'+
            '<div class=""poi-name"">'+r.name+'</div>'+
            '<div class=""poi-desc"">'+r.description+'</div>'+
            '<div class=""poi-meta"">&#128205; '+r.address+'<br>&#128336; '+r.hours+'</div>'+
            '<div class=""poi-rating"">&#11088; '+r.rating+'</div>'+
            '<button class=""btn-route"" onclick=""routeTo('+r.lat+','+r.lng+',\''+r.name+'\')"">&#128506; Ch\u1ec9 \u0111\u01b0\u1eddng</button>'+
            '<button class=""btn-clear"" onclick=""clearRoute()"">\u2716 X\u00f3a \u0111\u01b0\u1eddng</button>'+
            '</div>';
    }}
    var restaurants={markersJson};
    restaurants.forEach(function(r){{
        var m=L.marker([r.lat,r.lng],{{icon:poiIcon}})
            .addTo(map).bindPopup(makePopup(r),{{maxWidth:220}});
        poiMarkers[r.id]={{marker:m,data:r}};
    }});

    function updateUserLocation(lat,lng){{
        userLatLng={{lat:lat,lng:lng}};
        var ll=[lat,lng];
        if(userMarker){{
            userMarker.setLatLng(ll);
            accCircle.setLatLng(ll);
        }} else {{
            userMarker=L.marker(ll,{{icon:userIcon,zIndexOffset:1000}}).addTo(map);
            accCircle=L.circle(ll,{{
                radius:30,color:'#4285F4',fillColor:'#4285F4',
                fillOpacity:0.15,weight:1.5,opacity:0.5
            }}).addTo(map);
            map.setView(ll,17);
        }}
        return 'ok';
    }}
    function highlightPOI(poiId){{
        var entry=poiMarkers[poiId];
        if(!entry) return;
        entry.marker.setIcon(poiIconHL);
        entry.marker.openPopup();
        setTimeout(function(){{entry.marker.setIcon(poiIcon);}},3000);
    }}
    function showDistance(name,dist){{
        showToast('&#128205; '+name+' \u00b7 '+dist+'m');
    }}
</script>
</body>
</html>";
            _webView.Source = new HtmlWebViewSource { Html = html };
        }

        private string BuildMarkersJson()
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < _restaurants.Count; i++)
            {
                var r = _restaurants[i];
                var name = r.Name.Replace("\"", "\\\"").Replace("'", "\\'");
                var desc = r.Description.Replace("\"", "\\\"").Replace("'", "\\'");
                var addr = r.Address.Replace("\"", "\\\"").Replace("'", "\\'");
                var hrs = r.OpenHours.Replace("\"", "\\\"").Replace("'", "\\'");
                var ic = System.Globalization.CultureInfo.InvariantCulture;
                sb.Append($@"{{""id"":{r.Id},""lat"":{r.Latitude.ToString(ic)},""lng"":{r.Longitude.ToString(ic)},""name"":""{name}"",""description"":""{desc}"",""address"":""{addr}"",""rating"":{r.Rating.ToString(ic)},""hours"":""{hrs}""}}");
                if (i < _restaurants.Count - 1) sb.Append(",");
            }
            sb.Append("]");
            return sb.ToString();
        }

        private async Task StartTrackingAsync()
        {
            if (_isTracking) return;
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                    _statusLabel.Text = "❌ Chưa cấp quyền GPS");
                return;
            }
            _isTracking = true;
            _cancelToken = new CancellationTokenSource();
            await TrackLoopAsync(_cancelToken.Token);
        }

        private async Task TrackLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _isTracking)
            {
                try
                {
                    var location = await Geolocation.GetLocationAsync(new GeolocationRequest
                    {
                        DesiredAccuracy = GeolocationAccuracy.Best,
                        Timeout = TimeSpan.FromSeconds(10)
                    });

                    if (location != null)
                    {
                        _userLocation = location;
                        System.Diagnostics.Debug.WriteLine(
                            $"Got location: {location.Latitude}, {location.Longitude}");

                        if (_mapReady)
                        {
                            var ic = System.Globalization.CultureInfo.InvariantCulture;
                            var lat = location.Latitude.ToString(ic);
                            var lng = location.Longitude.ToString(ic);
                            await MainThread.InvokeOnMainThreadAsync(async () =>
                            {
                                var r = await _webView.EvaluateJavaScriptAsync(
                                    $"updateUserLocation({lat},{lng});");
                                System.Diagnostics.Debug.WriteLine($"JS result: {r}");
                            });
                            await CheckNearbyAsync(location);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Location is null");
                        MainThread.BeginInvokeOnMainThread(() =>
                            _statusLabel.Text = "⚠️ GPS null — SET LOCATION trong emulator");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"GPS error: {ex.Message}");
                    MainThread.BeginInvokeOnMainThread(() =>
                        _statusLabel.Text = $"❌ {ex.Message}");
                }
                await Task.Delay(3000, token);
            }
        }

        private async Task CheckNearbyAsync(Location location)
        {
            if (_restaurants.Count == 0) return;
            Restaurant? nearest = null;
            double minDist = double.MaxValue;
            foreach (var r in _restaurants)
            {
                double d = _geofencing.CalculateDistance(
                    location.Latitude, location.Longitude, r.Latitude, r.Longitude);
                if (d < minDist) { minDist = d; nearest = r; }
            }
            if (nearest == null) return;
            _nearestRestaurant = nearest;
            MainThread.BeginInvokeOnMainThread(() =>
                _statusLabel.Text = $"📍 Gần nhất: {nearest.Name} ({minDist:F0}m)");
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await _webView.EvaluateJavaScriptAsync(
                    $"showDistance('{nearest.Name.Replace("'", "\\'")}','{minDist:F0}');");
            });
            if (minDist <= 50)
            {
                if (_lastNotified.TryGetValue(nearest.Id, out DateTime last) &&
                    (DateTime.Now - last).TotalMinutes < COOLDOWN_MINUTES)
                    return;
                _lastNotified[nearest.Id] = DateTime.Now;
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await _webView.EvaluateJavaScriptAsync($"highlightPOI({nearest.Id});");
                });
                await SpeakAsync(nearest);
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("🔔 Đã đến gần!",
                        $"{nearest.Name}\n{nearest.Description}", "OK");
                });
                await App.Database.SaveVisitAsync(new VisitHistory
                {
                    RestaurantId = nearest.Id,
                    VisitedAt = DateTime.Now
                });
            }
        }

        private static async Task SpeakAsync(Restaurant r)
        {
            try
            {
                await TextToSpeech.SpeakAsync(
                    $"Bạn đang đến gần {r.Name}. {r.Description}. Đánh giá {r.Rating} sao.",
                    new SpeechOptions { Volume = 1.0f, Pitch = 1.0f });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TTS error: {ex.Message}");
            }
        }

        private async Task DrawRouteAsync(string json)
        {
            if (_userLocation == null) return;
            try
            {
                var info = JsonSerializer.Deserialize<RouteRequest>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (info == null) return;

                var ic = System.Globalization.CultureInfo.InvariantCulture;
                var url = "https://router.project-osrm.org/route/v1/foot/" +
                          $"{_userLocation.Longitude.ToString(ic)},{_userLocation.Latitude.ToString(ic)};" +
                          $"{info.Lng.ToString(ic)},{info.Lat.ToString(ic)}" +
                          "?overview=full&geometries=geojson";

                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var response = await http.GetStringAsync(url);
                var osrm = JsonSerializer.Deserialize<OsrmResponse>(response,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var route = osrm?.Routes?.FirstOrDefault();
                if (route == null) return;

                var coords = route.Geometry.Coordinates
                    .Select(c => new { lat = c[1], lng = c[0] }).ToList();
                var coordsJson = JsonSerializer.Serialize(coords).Replace("'", "\\'");
                var distM = route.Distance.ToString("F0");
                var durMin = (route.Duration / 60).ToString("F0");

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await _webView.EvaluateJavaScriptAsync($"drawRoute('{coordsJson}');");
                    _statusLabel.Text = $"🗺️ {info.Name} · {distM}m · ~{durMin} phút đi bộ";
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Route error: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                    await DisplayAlert("Lỗi", "Không thể tải lộ trình. Kiểm tra kết nối mạng.", "OK"));
            }
        }
    }
}
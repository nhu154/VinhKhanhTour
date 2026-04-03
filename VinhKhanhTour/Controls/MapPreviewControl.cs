using Microsoft.Maui.Controls;
using System.Text;

namespace VinhKhanhTour.Controls
{
    public class MapPreviewControl : WebView, IDisposable
    {
        private bool _isPageLoaded = false;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _disposed = false;

        public MapPreviewControl()
        {
            HeightRequest = 300;
            HorizontalOptions = LayoutOptions.Fill;
            VerticalOptions = LayoutOptions.Fill;

            Navigated += OnNavigated;

            LoadMap();
            _ = StartLocationTrackingAsync(_cts.Token);
        }

        private void OnNavigated(object? sender, WebNavigatedEventArgs e)
        {
            if (e.Result == WebNavigationResult.Success)
                _isPageLoaded = true;
        }

        private async void LoadMap()
        {
            var restaurants = await App.Database.GetRestaurantsAsync();

            var markersJson = new StringBuilder("[");
            for (int i = 0; i < restaurants.Count; i++)
            {
                var r = restaurants[i];
                var ic = System.Globalization.CultureInfo.InvariantCulture;
                var name = r.Name.Replace("\"", "\\\"").Replace("'", "\\'");
                markersJson.Append($"{{\"lat\":{r.Latitude.ToString(ic)},\"lng\":{r.Longitude.ToString(ic)},\"name\":\"{name}\"}}");
                if (i < restaurants.Count - 1) markersJson.Append(",");
            }
            markersJson.Append("]");

            var html = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />
    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
    <style>
        body, html {{ margin: 0; padding: 0; height: 100%; }}
        #map {{ width: 100%; height: 300px; }}
    </style>
</head>
<body>
    <div id='map'></div>
    <script>
        var map = L.map('map', {{
            dragging: false,
            touchZoom: false,
            scrollWheelZoom: false,
            doubleClickZoom: false,
            boxZoom: false,
            keyboard: false,
            zoomControl: false
        }}).setView([10.761500, 106.704500], 15);

        L.tileLayer('https://{{s}}.basemaps.cartocdn.com/light_all/{{z}}/{{x}}/{{y}}{{r}}.png', {{
            maxZoom: 19,
            attribution: '&copy; OpenStreetMap contributors, &copy; CARTO'
        }}).addTo(map);

        var redIcon = L.divIcon({{
            html: '<svg xmlns=""http://www.w3.org/2000/svg"" width=""24"" height=""24""><circle cx=""12"" cy=""12"" r=""10"" fill=""#FF6B6B"" stroke=""white"" stroke-width=""2""/></svg>',
            iconSize: [24, 24],
            iconAnchor: [12, 12],
            className: ''
        }});

        var userIcon = L.divIcon({{
            html: '<svg xmlns=""http://www.w3.org/2000/svg"" width=""28"" height=""28""><circle cx=""14"" cy=""14"" r=""6"" fill=""#4285F4"" stroke=""white"" stroke-width=""2""/><circle cx=""14"" cy=""14"" r=""12"" fill=""none"" stroke=""#4285F4"" stroke-width=""1.5"" opacity=""0.3""/></svg>',
            iconSize: [28, 28],
            iconAnchor: [14, 14],
            className: ''
        }});

        var markers = {markersJson};
        markers.forEach(function(m) {{
            L.marker([m.lat, m.lng], {{ icon: redIcon }})
                .bindTooltip(m.name, {{ permanent: false, direction: 'top' }})
                .addTo(map);
        }});

        var userMarker = null;
        function updateUserLocation(lat, lng) {{
            if (userMarker) {{
                userMarker.setLatLng([lat, lng]);
            }} else {{
                userMarker = L.marker([lat, lng], {{ icon: userIcon }}).addTo(map);
            }}
        }}
    </script>
</body>
</html>";

            Source = new HtmlWebViewSource { Html = html };
        }

        // ? Dùng CancellationToken — d?ng ngay khi control b? dispose
        private async Task StartLocationTrackingAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var location = await Geolocation.GetLocationAsync(new GeolocationRequest
                    {
                        DesiredAccuracy = GeolocationAccuracy.Medium,
                        Timeout = TimeSpan.FromSeconds(10)
                    }, token);

                    if (location != null && _isPageLoaded && !token.IsCancellationRequested)
                    {
                        var ic = System.Globalization.CultureInfo.InvariantCulture;
                        var lat = location.Latitude.ToString(ic);
                        var lng = location.Longitude.ToString(ic);

                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            if (!token.IsCancellationRequested)
                                await EvaluateJavaScriptAsync($"updateUserLocation({lat}, {lng});");
                        });
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"MapPreview location error: {ex.Message}");
                }

                try { await Task.Delay(5000, token); }
                catch (OperationCanceledException) { break; }
            }
        }

        // ? Dispose d?ng vòng l?p GPS khi WelcomePage unmount
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _cts.Cancel();
                _cts.Dispose();
            }
        }
    }
}

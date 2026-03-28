using VinhKhanhTour.Services;
using VinhKhanhTour.Models;


namespace VinhKhanhTour
{
    public partial class WelcomePage : ContentPage
    {
        private const string MAPS_API_KEY = Config.GoogleMapsApiKey;
        private bool _dropdownOpen = false;
        private string _currentLang = Preferences.Default.Get("app_lang", "vi");
        private List<ApiTour> _apiTours = new();

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = LoadToursFromApiAsync();
        }

        private async Task LoadToursFromApiAsync()
        {
            try
            {
                var tours = await ApiService.Instance.GetToursAsync();
                if (tours.Count >= 4)
                {
                    _apiTours = tours;
                    // Update UI labels with API tour data
                    MainThread.BeginInvokeOnMainThread(() => UpdateTourLabelsFromApi());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WelcomePage] Tours load failed: {ex.Message}");
            }
        }

        private void UpdateTourLabelsFromApi()
        {
            if (_apiTours.Count == 0) return;
            try
            {
                if (_apiTours.Count > 0) { LblTour1.Text = _apiTours[0].GetName(_currentLang); LblDesc1.Text = _apiTours[0].GetDescription(_currentLang); LblDur1.Text = "⏱ " + _apiTours[0].GetDuration(_currentLang); LblPts1.Text = _apiTours[0].GetRestaurantIds().Count + (_currentLang == "en" ? " spots" : _currentLang == "zh" ? "个景点" : " điểm"); }
                if (_apiTours.Count > 1) { LblTour2.Text = _apiTours[1].GetName(_currentLang); LblDesc2.Text = _apiTours[1].GetDescription(_currentLang); LblDur2.Text = "⏱ " + _apiTours[1].GetDuration(_currentLang); LblPts2.Text = _apiTours[1].GetRestaurantIds().Count + (_currentLang == "en" ? " spots" : _currentLang == "zh" ? "个景点" : " điểm"); }
                if (_apiTours.Count > 2) { LblTour3.Text = _apiTours[2].GetName(_currentLang); LblDesc3.Text = _apiTours[2].GetDescription(_currentLang); LblDur3.Text = "⏱ " + _apiTours[2].GetDuration(_currentLang); LblPts3.Text = _apiTours[2].GetRestaurantIds().Count + (_currentLang == "en" ? " spots" : _currentLang == "zh" ? "个景点" : " điểm"); }
                if (_apiTours.Count > 3) { LblTour4.Text = _apiTours[3].GetName(_currentLang); LblDesc4.Text = _apiTours[3].GetDescription(_currentLang); LblDur4.Text = "⏱ " + _apiTours[3].GetDuration(_currentLang); LblPts4.Text = _apiTours[3].GetRestaurantIds().Count + (_currentLang == "en" ? " spots" : _currentLang == "zh" ? "个景点" : " điểm"); }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[WelcomePage] UpdateLabels: {ex.Message}"); }
        }

        public WelcomePage()
        {
            InitializeComponent();
            LoadMiniMap();
        }

        private void LoadMiniMap()
        {
            var html = $@"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<meta name='viewport' content='width=device-width,initial-scale=1.0'>
<style>*{{margin:0;padding:0;box-sizing:border-box}}body,html{{height:100%;overflow:hidden}}#map{{width:100%;height:200px}}</style>
</head>
<body>
<div id='map'></div>
<script>
function initMap(){{
  var c={{lat:10.7615,lng:106.7045}};
  var map=new google.maps.Map(document.getElementById('map'),{{
    center:c,zoom:16,disableDefaultUI:true,gestureHandling:'none',
    styles:[{{featureType:'poi',elementType:'labels',stylers:[{{visibility:'off'}}]}}]
  }});
  var pois=[
    {{lat:10.760825,lng:106.703313}},{{lat:10.761090,lng:106.702899}},
    {{lat:10.761758,lng:106.702358}},{{lat:10.761281,lng:106.705373}},
    {{lat:10.761345,lng:106.705690}},{{lat:10.761060,lng:106.706682}},
    {{lat:10.760840,lng:106.704050}},{{lat:10.764267,lng:106.701181}},
    {{lat:10.760625,lng:106.703716}},{{lat:10.761278,lng:106.705293}},
    {{lat:10.760883,lng:106.706741}}
  ];
  pois.forEach(function(p){{
    new google.maps.Marker({{position:p,map:map,icon:{{
      path:google.maps.SymbolPath.CIRCLE,scale:9,
      fillColor:'#42A5F5',fillOpacity:1,strokeColor:'white',strokeWeight:2
    }}}});
  }});
}}
</script>
<script src='https://maps.googleapis.com/maps/api/js?key={MAPS_API_KEY}&loading=async&callback=initMap'></script>
</body>
</html>";
            MiniMapWebView.Source = new HtmlWebViewSource { Html = html };
        }

        private void OnMapClicked(object sender, EventArgs e)
        {
            if (Application.Current?.MainPage is TabbedPage tabbedPage)
                tabbedPage.CurrentPage = tabbedPage.Children[1];
        }

        private void OnLangBtnClicked(object sender, EventArgs e)
        {
            _dropdownOpen = !_dropdownOpen;
            LangDropdown.IsVisible = _dropdownOpen;
        }

        private void OnLangVI(object sender, EventArgs e) => SetLanguage("vi");
        private void OnLangEN(object sender, EventArgs e) => SetLanguage("en");
        private void OnLangZH(object sender, EventArgs e) => SetLanguage("zh");

        private void SetLanguage(string lang)
        {
            _dropdownOpen = false;
            LangDropdown.IsVisible = false;

            if (_currentLang == lang) return;

            Preferences.Default.Set("app_lang", lang);
            VinhKhanhTour.Services.AudioService.Instance.SetLanguage(lang);

            if (Application.Current?.MainPage is MainTabbedPage tabbedPage)
                tabbedPage.UpdateLanguage(lang);
            else
                UpdateLanguage(lang);
        }

        public void UpdateLanguage(string lang)
        {
            _currentLang = lang;

            LangFlag.Text = lang switch { "en" => "🇺🇸", "zh" => "🇨🇳", _ => "🇻🇳" };

            var activeColor = Color.FromArgb("#1565C0");
            var inactiveColor = Color.FromArgb("#F0F6FF");
            BtnVI.BackgroundColor = lang == "vi" ? activeColor : inactiveColor;
            LblLangVI.TextColor = lang == "vi" ? Colors.White : Color.FromArgb("#5A7A9A");
            BtnEN.BackgroundColor = lang == "en" ? activeColor : inactiveColor;
            LblLangEN.TextColor = lang == "en" ? Colors.White : Color.FromArgb("#5A7A9A");
            BtnZH.BackgroundColor = lang == "zh" ? activeColor : inactiveColor;
            LblLangZH.TextColor = lang == "zh" ? Colors.White : Color.FromArgb("#5A7A9A");

            switch (lang)
            {
                case "en":
                    LblNarration.Text = "Narration language";
                    LblNarrationSub.Text = "Select audio tour language";
                    LblChoTour.Text = "Vinh Khanh area";
                    LblChoTourSub.Text = "Famous food street in District 4";
                    LblTapHint.Text = "👆 Tap to view detailed map";
                    LblTourTheme.Text = "Choose your Tour";
                    LblTourThemeSub.Text = "4 unique food journeys";
                    LblWelcome.Text = "Explore District 4 food street";
                    LblLocation.Text = "District 4 · HCMC";
                    LblMainSubtitle.Text = "  FOOD TOUR GUIDE";
                    LblStats1Value.Text = "11"; LblStats1Text.Text = "Spots";
                    LblStats2Value.Text = "4"; LblStats2Text.Text = "Themes";
                    LblStats3Value.Text = "4.5★"; LblStats3Text.Text = "Rating";
                    LblCTA.Text = "🗺️  Start Exploring";
                    LblTour1.Text = "Shellfish Tour"; LblTag1.Text = "OC";
                    LblTour2.Text = "BBQ Tour"; LblTag2.Text = "BBQ";
                    LblTour3.Text = "Snack Tour"; LblTag3.Text = "SNACK";
                    LblTour4.Text = "Specialty Tour"; LblTag4.Text = "SPECIAL";
                    LblDesc1.Text = "3 famous shellfish restaurants";
                    LblDesc2.Text = "BBQ, grilled beef in pepper leaves";
                    LblDesc3.Text = "Crispy rice, grilled pork noodles";
                    LblDesc4.Text = "Chau Doc fish noodle soup";
                    LblDur1.Text = "⏱ 45 min"; LblPts1.Text = "3 spots";
                    LblDur2.Text = "⏱ 60 min"; LblPts2.Text = "3 spots";
                    LblDur3.Text = "⏱ 40 min"; LblPts3.Text = "2 spots";
                    LblDur4.Text = "⏱ 50 min"; LblPts4.Text = "3 spots";
                    break;
                case "zh":
                    LblNarration.Text = "解说语言";
                    LblNarrationSub.Text = "选择语音导览语言";
                    LblChoTour.Text = "永庆地区";
                    LblChoTourSub.Text = "第四郡著名美食街";
                    LblTapHint.Text = "👆 点击查看详细地图";
                    LblTourTheme.Text = "选择您的路线";
                    LblTourThemeSub.Text = "4条独特的美食之旅";
                    LblWelcome.Text = "探索第四郡美食街";
                    LblLocation.Text = "第四郡 · 胡志明市";
                    LblMainSubtitle.Text = "  美食指南";
                    LblStats1Value.Text = "11"; LblStats1Text.Text = "餐厅";
                    LblStats2Value.Text = "4"; LblStats2Text.Text = "主题游";
                    LblStats3Value.Text = "4.5★"; LblStats3Text.Text = "评分";
                    LblCTA.Text = "🗺️  开始探索";
                    LblTour1.Text = "贝类美食游"; LblTag1.Text = "海鲜";
                    LblTour2.Text = "烧烤美食游"; LblTag2.Text = "烧烤";
                    LblTour3.Text = "小吃游"; LblTag3.Text = "小吃";
                    LblTour4.Text = "特产美食游"; LblTag4.Text = "特产";
                    LblDesc1.Text = "3家知名贝类餐厅";
                    LblDesc2.Text = "烧烤，胡椒叶牛肉";
                    LblDesc3.Text = "锅巴，烤猪肉米线";
                    LblDesc4.Text = "朱笃鱼米线";
                    LblDur1.Text = "⏱ 45分钟"; LblPts1.Text = "3个景点";
                    LblDur2.Text = "⏱ 60分钟"; LblPts2.Text = "3个景点";
                    LblDur3.Text = "⏱ 40分钟"; LblPts3.Text = "2个景点";
                    LblDur4.Text = "⏱ 50分钟"; LblPts4.Text = "3个景点";
                    break;
                default:
                    LblNarration.Text = "Giọng thuyết minh";
                    LblNarrationSub.Text = "Chọn ngôn ngữ audio tour";
                    LblChoTour.Text = "Khu vực Vĩnh Khánh";
                    LblChoTourSub.Text = "Phố ẩm thực nổi tiếng Quận 4";
                    LblTapHint.Text = "👆 Nhấn để xem bản đồ chi tiết";
                    LblTourTheme.Text = "Chọn Tour của bạn";
                    LblTourThemeSub.Text = "4 hành trình ẩm thực độc đáo";
                    LblWelcome.Text = "Khám phá phố ẩm thực nổi tiếng nhất Quận 4";
                    LblLocation.Text = "Quận 4 · TP.HCM";
                    LblMainSubtitle.Text = "  CẨM NANG ẨM THỰC";
                    LblStats1Value.Text = "11"; LblStats1Text.Text = "Quán ăn";
                    LblStats2Value.Text = "4"; LblStats2Text.Text = "Tour chủ đề";
                    LblStats3Value.Text = "4.5★"; LblStats3Text.Text = "Đánh giá";
                    LblCTA.Text = "🗺️  Bắt đầu khám phá";
                    LblTour1.Text = "Tour Ăn Ốc"; LblTag1.Text = "ỐC";
                    LblTour2.Text = "Tour Ăn Nướng"; LblTag2.Text = "NƯỚNG";
                    LblTour3.Text = "Tour Ăn Vặt"; LblTag3.Text = "ĂN VẶT";
                    LblTour4.Text = "Tour Đặc Sản"; LblTag4.Text = "ĐẶC SẢN";
                    LblDesc1.Text = "3 quán ốc ngon nổi tiếng";
                    LblDesc2.Text = "Lẩu nướng, bò lá lốt";
                    LblDesc3.Text = "Cơm cháy, bún nướng";
                    LblDesc4.Text = "Bún cá Châu Đốc";
                    LblDur1.Text = "⏱ 45 phút"; LblPts1.Text = "3 điểm";
                    LblDur2.Text = "⏱ 60 phút"; LblPts2.Text = "3 điểm";
                    LblDur3.Text = "⏱ 40 phút"; LblPts3.Text = "2 điểm";
                    LblDur4.Text = "⏱ 50 phút"; LblPts4.Text = "3 điểm";
                    break;
            }
        }

        private void OnTour1Clicked(object sender, EventArgs e)
        {
            var t = _apiTours.Count > 0 ? _apiTours[0] : null;
            Navigation.PushAsync(new TourDetailPage(new Tour
            {
                Id = t != null ? t.Id.ToString() : "1",
                Name = t != null ? t.GetName(_currentLang) : _currentLang switch { "en" => "Shellfish Tour", "zh" => "贝类美食游", _ => "Tour Ăn Ốc" },
                Description = t != null ? t.GetDescription(_currentLang) : _currentLang switch { "en" => "3 famous shellfish restaurants", "zh" => "3家知名贝类餐厅", _ => "3 quán ốc ngon nổi tiếng" },
                Duration = t != null ? t.GetDuration(_currentLang) : _currentLang switch { "en" => "45 min", "zh" => "45分钟", _ => "45 phút" },
                Rating = t != null ? t.Rating : 4.4,
                RestaurantIds = t != null ? t.GetRestaurantIds() : new List<int> { 1, 2, 3 }
            }));
        }

        private void OnTour2Clicked(object sender, EventArgs e)
        {
            var t = _apiTours.Count > 1 ? _apiTours[1] : null;
            Navigation.PushAsync(new TourDetailPage(new Tour
            {
                Id = t != null ? t.Id.ToString() : "2",
                Name = t != null ? t.GetName(_currentLang) : _currentLang switch { "en" => "BBQ Tour", "zh" => "烧烤美食游", _ => "Tour Ăn Nướng" },
                Description = t != null ? t.GetDescription(_currentLang) : _currentLang switch { "en" => "BBQ, grilled beef in pepper leaves", "zh" => "烧烤，胡椒叶牛肉", _ => "Lẩu nướng, bò lá lốt" },
                Duration = t != null ? t.GetDuration(_currentLang) : _currentLang switch { "en" => "60 min", "zh" => "60分钟", _ => "60 phút" },
                Rating = t != null ? t.Rating : 4.5,
                RestaurantIds = t != null ? t.GetRestaurantIds() : new List<int> { 7, 8, 10 }
            }));
        }

        private void OnTour3Clicked(object sender, EventArgs e)
        {
            var t = _apiTours.Count > 2 ? _apiTours[2] : null;
            Navigation.PushAsync(new TourDetailPage(new Tour
            {
                Id = t != null ? t.Id.ToString() : "3",
                Name = t != null ? t.GetName(_currentLang) : _currentLang switch { "en" => "Snack Tour", "zh" => "小吃游", _ => "Tour Ăn Vặt" },
                Description = t != null ? t.GetDescription(_currentLang) : _currentLang switch { "en" => "Crispy rice, grilled pork noodles", "zh" => "锅巴，烤猪肉米线", _ => "Cơm cháy, bún thịt nướng" },
                Duration = t != null ? t.GetDuration(_currentLang) : _currentLang switch { "en" => "40 min", "zh" => "40分钟", _ => "40 phút" },
                Rating = t != null ? t.Rating : 4.3,
                RestaurantIds = t != null ? t.GetRestaurantIds() : new List<int> { 9, 11 }
            }));
        }

        private void OnTour4Clicked(object sender, EventArgs e)
        {
            var t = _apiTours.Count > 3 ? _apiTours[3] : null;
            Navigation.PushAsync(new TourDetailPage(new Tour
            {
                Id = t != null ? t.Id.ToString() : "4",
                Name = t != null ? t.GetName(_currentLang) : _currentLang switch { "en" => "Specialty Tour", "zh" => "特产美食游", _ => "Tour Đặc Sản" },
                Description = t != null ? t.GetDescription(_currentLang) : _currentLang switch { "en" => "Chau Doc fish noodle soup", "zh" => "朱笃鱼米线", _ => "Bún cá Châu Đốc" },
                Duration = t != null ? t.GetDuration(_currentLang) : _currentLang switch { "en" => "50 min", "zh" => "50分钟", _ => "50 phút" },
                Rating = t != null ? t.Rating : 4.6,
                RestaurantIds = t != null ? t.GetRestaurantIds() : new List<int> { 4, 5, 6 }
            }));
        }
    }
}
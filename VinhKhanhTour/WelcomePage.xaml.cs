using VinhKhanhTour.Models;

namespace VinhKhanhTour
{
    public partial class WelcomePage : ContentPage
    {
        private const string MAPS_API_KEY = "AIzaSyCqEET9xuXB2sGAByb-5zGALGamJ2bwbxc";
        private bool _dropdownOpen = false;

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
      fillColor:'#FF6B35',fillOpacity:1,strokeColor:'white',strokeWeight:2
    }}}});
  }});
}}
</script>
<script src='https://maps.googleapis.com/maps/api/js?key={MAPS_API_KEY}&callback=initMap' async defer></script>
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

            LangFlag.Text = lang switch { "en" => "🇺🇸", "zh" => "🇨🇳", _ => "🇻🇳" };

            var activeColor = Color.FromArgb("#FF6B35");
            var inactiveColor = Color.FromArgb("#F5F5F5");
            BtnVI.BackgroundColor = lang == "vi" ? activeColor : inactiveColor;
            BtnEN.BackgroundColor = lang == "en" ? activeColor : inactiveColor;
            BtnZH.BackgroundColor = lang == "zh" ? activeColor : inactiveColor;

            switch (lang)
            {
                case "en":
                    LblNarration.Text = "Narration language";
                    LblChoTour.Text = "Vinh Khanh area";
                    LblTapHint.Text = "👆 Tap to view detailed map";
                    BtnTourAll.Text = "🗺️ Start Exploring";
                    LblTourTheme.Text = "Choose your Tour";
                    LblTour1.Text = "Shellfish Tour";
                    LblTour2.Text = "BBQ Tour";
                    LblTour3.Text = "Snack Tour";
                    LblTour4.Text = "Specialty Tour";
                    LblWelcome.Text = "Explore Quan 4 food street";
                    break;
                case "zh":
                    LblNarration.Text = "解说语言";
                    LblChoTour.Text = "永庆地区";
                    LblTapHint.Text = "👆 点击查看详细地图";
                    BtnTourAll.Text = "🗺️ 开始探索";
                    LblTourTheme.Text = "选择您的路线";
                    LblTour1.Text = "贝类美食游";
                    LblTour2.Text = "烧烤美食游";
                    LblTour3.Text = "小吃游";
                    LblTour4.Text = "特产美食游";
                    LblWelcome.Text = "探索第四郡美食街";
                    break;
                default:
                    LblNarration.Text = "Giọng thuyết minh";
                    LblChoTour.Text = "Khu vực Vĩnh Khánh";
                    LblTapHint.Text = "👆 Nhấn để xem bản đồ chi tiết";
                    BtnTourAll.Text = "🗺️ Bắt đầu khám phá";
                    LblTourTheme.Text = "Chọn Tour của bạn";
                    LblTour1.Text = "Tour Ăn Ốc";
                    LblTour2.Text = "Tour Ăn Nướng";
                    LblTour3.Text = "Tour Ăn Vặt";
                    LblTour4.Text = "Tour Đặc Sản";
                    LblWelcome.Text = "Khám phá phố ẩm thực Quận 4";
                    break;
            }
        }

        private void OnTour1Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new TourDetailPage(new Tour
            {
                Id = "1",
                Name = "Tour Ăn Ốc",
                Description = "3 quán ốc ngon nổi tiếng",
                Duration = "45 phút",
                Rating = 4.4,
                RestaurantIds = new List<int> { 1, 2, 3 }
            }));
        }

        private void OnTour2Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new TourDetailPage(new Tour
            {
                Id = "2",
                Name = "Tour Ăn Nướng",
                Description = "Lẩu nướng, bò lá lốt",
                Duration = "60 phút",
                Rating = 4.5,
                RestaurantIds = new List<int> { 7, 8, 10 }
            }));
        }

        private void OnTour3Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new TourDetailPage(new Tour
            {
                Id = "3",
                Name = "Tour Ăn Vặt",
                Description = "Cơm cháy, bún thịt nướng",
                Duration = "40 phút",
                Rating = 4.3,
                RestaurantIds = new List<int> { 9, 11 }
            }));
        }

        private void OnTour4Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new TourDetailPage(new Tour
            {
                Id = "4",
                Name = "Tour Đặc Sản",
                Description = "Bún cá Châu Đốc",
                Duration = "50 phút",
                Rating = 4.6,
                RestaurantIds = new List<int> { 4, 5, 6 }
            }));
        }
    }
}
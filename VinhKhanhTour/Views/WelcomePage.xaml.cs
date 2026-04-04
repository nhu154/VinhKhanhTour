using VinhKhanhTour.Services;
using VinhKhanhTour.Models;
using Microsoft.Maui.Controls.Shapes;

namespace VinhKhanhTour.Views
{
    public partial class WelcomePage : ContentPage
    {
        private const string MAPS_API_KEY = Config.GoogleMapsApiKey;
        private bool _dropdownOpen = false;
        private string _currentLang = Preferences.Default.Get("app_lang", "vi");
        private List<ApiTour> _apiTours = new();
        private List<AppLanguage> _availableLanguages = new();

        public WelcomePage()
        {
            InitializeComponent();
            LoadMiniMap();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = LoadToursFromApiAsync();
            _ = LoadLanguagesFromApiAsync();
        }

        // ══ LOAD TOURS TỪ API ══
        private async Task LoadToursFromApiAsync()
        {
            try
            {
                var tours = await ApiService.Instance.GetToursAsync();
                if (tours.Count > 0)
                {
                    _apiTours = tours;
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        RenderTourCards();
                        UpdateTourThemeLabel();
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WelcomePage] Tours load failed: {ex.Message}");
            }
        }

        // ══ RENDER CARDS ĐỘNG ══
        private void RenderTourCards()
        {
            TourCardsContainer.Children.Clear();

            string[] coverImages = { "tour_oc.jpg", "tour_nuong.jpg", "tour_vat.jpg", "tour_dacssan.jpg" };
            string[] accentColors = { "#CC1565C0", "#CC0D47A1", "#CC01579B", "#CC311B92" };
            string[] shadowColors = { "#1565C0", "#1976D2", "#0288D1", "#4A148C" };

            for (int i = 0; i < _apiTours.Count; i++)
            {
                var tour = _apiTours[i];
                int idx = i; // capture for closure
                string coverImg = i < coverImages.Length ? coverImages[i] : "tour_oc.jpg";
                string accent = i < accentColors.Length ? accentColors[i] : "#CC1565C0";
                string shadow = i < shadowColors.Length ? shadowColors[i] : "#1565C0";
                bool isLast = i == _apiTours.Count - 1;

                var card = BuildTourCard(tour, coverImg, accent, shadow, isLast, idx);
                TourCardsContainer.Children.Add(card);
            }
        }

        private Border BuildTourCard(ApiTour tour, string coverImg, string accent, string shadowColor, bool isLast, int idx)
        {
            var ptsCount = tour.GetRestaurantIds().Count;
            var ptsText = _currentLang == "en" ? $"{ptsCount} spots" : _currentLang == "zh" ? $"{ptsCount}个景点" : $"{ptsCount} điểm";
            var durText = "⏱ " + tour.GetDuration(_currentLang);
            var ratingText = $"⭐ {tour.Rating:F1}";

            // Image grid
            var imageGrid = new Grid { HeightRequest = 160 };
            imageGrid.Add(new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(20, 20, 0, 0) },
                StrokeThickness = 0,
                Content = new Image { Source = coverImg, Aspect = Aspect.AspectFill, HorizontalOptions = LayoutOptions.Fill, VerticalOptions = LayoutOptions.Fill }
            });
            imageGrid.Add(new BoxView
            {
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Colors.Transparent, 0.5f),
                        new GradientStop(Color.FromArgb("#CC0D2137"), 1f)
                    }
                }
            });

            // Tag badge
            imageGrid.Add(new Border
            {
                BackgroundColor = Color.FromArgb(accent),
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                StrokeThickness = 0,
                Padding = new Thickness(10, 5),
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(12, 12, 0, 0),
                Content = new HorizontalStackLayout
                {
                    Spacing = 5,
                    Children =
                    {
                        new Label { Text = tour.Emoji, FontSize = 13 },
                        new Label { Text = tour.GetName(_currentLang).ToUpper(), FontSize = 10, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#BBDEFB"), VerticalOptions = LayoutOptions.Center }
                    }
                }
            });

            // Arrow badge
            imageGrid.Add(new Border
            {
                BackgroundColor = Color.FromArgb(accent),
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                StrokeThickness = 0,
                Padding = new Thickness(10, 8),
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.End,
                Margin = new Thickness(0, 0, 12, 10),
                Content = new Label { Text = "›", FontSize = 20, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center }
            });

            // Info section
            var infoStack = new VerticalStackLayout
            {
                Padding = new Thickness(16, 12, 16, 14),
                Spacing = 6,
                Children =
                {
                    new Label { Text = tour.GetName(_currentLang), FontSize = 17, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D2137") },
                    new Label { Text = tour.GetDescription(_currentLang), FontSize = 12, TextColor = Color.FromArgb("#5A7A9A") },
                    new HorizontalStackLayout
                    {
                        Spacing = 8, Margin = new Thickness(0, 2, 0, 0),
                        Children =
                        {
                            new Border { BackgroundColor = Color.FromArgb("#301565C0"), StrokeShape = new RoundRectangle { CornerRadius = 8 }, StrokeThickness = 0, Padding = new Thickness(8, 4), Content = new Label { Text = ratingText, FontSize = 11, TextColor = Color.FromArgb("#64B5F6"), FontAttributes = FontAttributes.Bold } },
                            new Border { BackgroundColor = Color.FromArgb("#EEF4FF"), StrokeShape = new RoundRectangle { CornerRadius = 8 }, StrokeThickness = 0, Padding = new Thickness(8, 4), Content = new Label { Text = durText, FontSize = 11, TextColor = Color.FromArgb("#5A7A9A") } },
                            new Border { BackgroundColor = Color.FromArgb("#EEF4FF"), StrokeShape = new RoundRectangle { CornerRadius = 8 }, StrokeThickness = 0, Padding = new Thickness(8, 4), Content = new Label { Text = ptsText, FontSize = 11, TextColor = Color.FromArgb("#5A7A9A") } }
                        }
                    }
                }
            };

            var card = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Stroke = Color.FromArgb("#CCE0FF"),
                StrokeThickness = 1,
                BackgroundColor = Colors.White,
                Margin = new Thickness(0, 0, 0, isLast ? 0 : 0),
                Shadow = new Shadow { Brush = Color.FromArgb(shadowColor), Opacity = 0.25f, Radius = 14, Offset = new Point(0, 5) },
                Content = new VerticalStackLayout
                {
                    Spacing = 0,
                    Children = { imageGrid, infoStack }
                }
            };

            var tgr = new TapGestureRecognizer();
            tgr.Tapped += (s, e) => OnTourCardClicked(idx);
            card.GestureRecognizers.Add(tgr);

            return card;
        }

        private void OnTourCardClicked(int idx)
        {
            if (idx >= _apiTours.Count) return;
            var t = _apiTours[idx];
            Navigation.PushAsync(new TourDetailPage(new Tour
            {
                Id = t.Id.ToString(),
                Name = t.GetName(_currentLang),
                Description = t.GetDescription(_currentLang),
                Duration = t.GetDuration(_currentLang),
                Rating = t.Rating,
                RestaurantIds = t.GetRestaurantIds()
            }));
        }

        private void UpdateTourThemeLabel()
        {
            var count = _apiTours.Count;
            LblTourThemeSub.Text = _currentLang switch
            {
                "en" => $"{count} unique food journeys",
                "zh" => $"{count}条独特的美食之旅",
                _ => $"{count} hành trình ẩm thực độc đáo"
            };
            LblStats2Value.Text = count.ToString();
        }

        // ══ MINI MAP ══
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

        // ══ LOAD VÀ RENDER NGÔN NGỮ TỪ API ══
        private async Task LoadLanguagesFromApiAsync()
        {
            try
            {
                var langs = await ApiService.Instance.GetLanguagesAsync();
                if (langs.Count > 0)
                {
                    _availableLanguages = langs;
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        RenderLanguageButtons();
                        RenderDropdownItems();
                        UpdateLanguage(_currentLang);
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WelcomePage] Languages load failed: {ex.Message}");
                // Dùng default nếu API thất bại
                _availableLanguages = new List<AppLanguage>
                {
                    new() { Code = "vi", Name = "Tiếng Việt", Flag = "🇻🇳", IsDefault = true },
                    new() { Code = "en", Name = "English",     Flag = "🇺🇸" },
                    new() { Code = "zh", Name = "中文",          Flag = "🇨🇳" },
                };
                MainThread.BeginInvokeOnMainThread(() => { RenderLanguageButtons(); RenderDropdownItems(); });
            }
        }

        private void RenderLanguageButtons()
        {
            LangBtnContainer.Children.Clear();
            var activeColor = Color.FromArgb("#1565C0");
            var inactiveColor = Color.FromArgb("#F0F6FF");

            foreach (var lang in _availableLanguages)
            {
                var code = lang.Code;
                var isActive = code == _currentLang;

                var flagLabel = new Label { Text = lang.Flag, FontSize = 22, HorizontalOptions = LayoutOptions.Center };
                var nameLabel = new Label
                {
                    Text = lang.Name, // Hiện đầy đủ: "Tiếng Việt", "English", "中文"
                    FontSize = 11,
                    FontAttributes = isActive ? FontAttributes.Bold : FontAttributes.None,
                    TextColor = isActive ? Colors.White : Color.FromArgb("#5A7A9A"),
                    HorizontalOptions = LayoutOptions.Center
                };

                var frame = new Frame
                {
                    AutomationId = $"lang-btn-{code}",
                    CornerRadius = 14,
                    HasShadow = false,
                    BackgroundColor = isActive ? activeColor : inactiveColor,
                    BorderColor = isActive ? Colors.Transparent : Color.FromArgb("#CCE0FF"),
                    Padding = new Thickness(0, 12),
                    Margin = new Thickness(4, 0),
                    WidthRequest = 100,
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children = { flagLabel, nameLabel }
                    }
                };

                var tgr = new TapGestureRecognizer();
                tgr.Tapped += (_, _) => SetLanguage(code);
                frame.GestureRecognizers.Add(tgr);

                LangBtnContainer.Children.Add(frame);
            }
        }

        private void RenderDropdownItems()
        {
            LangDropdownList.Children.Clear();
            bool isFirst = true;
            foreach (var lang in _availableLanguages)
            {
                var code = lang.Code;
                if (!isFirst)
                    LangDropdownList.Children.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#CCE0FF"), Margin = new Thickness(12, 0) });
                isFirst = false;

                var row = new Border
                {
                    Padding = new Thickness(16, 12),
                    BackgroundColor = Colors.Transparent
                };
                var tgr = new TapGestureRecognizer();
                tgr.Tapped += (_, _) => SetLanguage(code);
                row.GestureRecognizers.Add(tgr);
                row.Content = new HorizontalStackLayout
                {
                    Spacing = 10,
                    Children =
                    {
                        new Label { Text = lang.Flag, FontSize = 18 },
                        new Label { Text = lang.Name, FontSize = 13, TextColor = Color.FromArgb("#0D2137"), VerticalOptions = LayoutOptions.Center }
                    }
                };
                LangDropdownList.Children.Add(row);
            }
        }

        private void OnLangBtnClicked(object sender, EventArgs e)
        {
            _dropdownOpen = !_dropdownOpen;
            LangDropdown.IsVisible = _dropdownOpen;
        }

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

            // Cập nhật cờ hiển thị trên nút
            var activeLang = _availableLanguages.FirstOrDefault(l => l.Code == lang);
            LangFlag.Text = activeLang?.Flag ?? lang switch { "en" => "🇺🇸", "zh" => "🇨🇳", _ => "🇻🇳" };

            // Cập nhật trạng thái nút ngôn ngữ động
            var activeColor = Color.FromArgb("#1565C0");
            var inactiveColor = Color.FromArgb("#F0F6FF");
            foreach (var child in LangBtnContainer.Children)
            {
                if (child is not Frame btn) continue;
                var isActive = btn.AutomationId == $"lang-btn-{lang}";
                btn.BackgroundColor = isActive ? activeColor : inactiveColor;
                btn.BorderColor = isActive ? Colors.Transparent : Color.FromArgb("#CCE0FF");
                if (btn.Content is VerticalStackLayout vsl && vsl.Children.Count >= 2
                    && vsl.Children[1] is Label nameLabel)
                {
                    nameLabel.TextColor = isActive ? Colors.White : Color.FromArgb("#5A7A9A");
                    nameLabel.FontAttributes = isActive ? FontAttributes.Bold : FontAttributes.None;
                }
            }

            switch (lang)
            {
                case "en":
                    LblNarration.Text = "Narration language"; LblNarrationSub.Text = "Select audio tour language";
                    LblChoTour.Text = "Vinh Khanh area"; LblChoTourSub.Text = "Famous food street in District 4";
                    LblTapHint.Text = "👆 Tap to view detailed map";
                    LblTourTheme.Text = "Choose your Tour";
                    LblWelcome.Text = "Explore District 4 food street"; LblLocation.Text = "District 4 · HCMC";
                    LblMainSubtitle.Text = "  FOOD TOUR GUIDE";
                    LblStats1Value.Text = "11"; LblStats1Text.Text = "Spots";
                    LblStats3Value.Text = "4.5★"; LblStats3Text.Text = "Rating";
                    LblCTA.Text = "🗺️  Start Exploring";
                    break;
                case "zh":
                    LblNarration.Text = "解说语言"; LblNarrationSub.Text = "选择语音导览语言";
                    LblChoTour.Text = "永庆地区"; LblChoTourSub.Text = "第四郡著名美食街";
                    LblTapHint.Text = "👆 点击查看详细地图";
                    LblTourTheme.Text = "选择您的路线";
                    LblWelcome.Text = "探索第四郡美食街"; LblLocation.Text = "第四郡 · 胡志明市";
                    LblMainSubtitle.Text = "  美食指南";
                    LblStats1Value.Text = "11"; LblStats1Text.Text = "餐厅";
                    LblStats3Value.Text = "4.5★"; LblStats3Text.Text = "评分";
                    LblCTA.Text = "🗺️  开始探索";
                    break;
                case "ja":
                    LblNarration.Text = "ガイド言語"; LblNarrationSub.Text = "音声ガイドの言語を選択";
                    LblChoTour.Text = "ヴィンカンエリア"; LblChoTourSub.Text = "第4区の有名な飲食街";
                    LblTapHint.Text = "👆 タップして地図を表示";
                    LblTourTheme.Text = "ツアーを選択";
                    LblWelcome.Text = "第4区の飲食街を探索しよう"; LblLocation.Text = "第4区 · ホーチミン市";
                    LblMainSubtitle.Text = "  美食ガイド";
                    LblStats1Value.Text = "11"; LblStats1Text.Text = "スポット";
                    LblStats3Value.Text = "4.5★"; LblStats3Text.Text = "評価";
                    LblCTA.Text = "🗺️  探索を始める";
                    break;
                default:
                    LblNarration.Text = "Giọng thuyết minh"; LblNarrationSub.Text = "Chọn ngôn ngữ audio tour";
                    LblChoTour.Text = "Khu vực Vĩnh Khánh"; LblChoTourSub.Text = "Phố ẩm thực nổi tiếng Quận 4";
                    LblTapHint.Text = "👆 Nhấn để xem bản đồ chi tiết";
                    LblTourTheme.Text = "Chọn Tour của bạn";
                    LblWelcome.Text = "Khám phá phố ẩm thực nổi tiếng nhất Quận 4"; LblLocation.Text = "Quận 4 · TP.HCM";
                    LblMainSubtitle.Text = "  CẨM NANG ẨM THỰC";
                    LblStats1Value.Text = "11"; LblStats1Text.Text = "Quán ăn";
                    LblStats3Value.Text = "4.5★"; LblStats3Text.Text = "Đánh giá";
                    LblCTA.Text = "🗺️  Bắt đầu khám phá";
                    break;
            }

            // Re-render tour cards với ngôn ngữ mới
            if (_apiTours.Count > 0)
            {
                UpdateTourThemeLabel();
                RenderTourCards();
            }
        }
    }
}
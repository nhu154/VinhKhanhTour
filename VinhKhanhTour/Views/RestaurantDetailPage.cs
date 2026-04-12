using Microsoft.Maui.Controls.Shapes;
using VinhKhanhTour.Models;
using VinhKhanhTour.Services;

namespace VinhKhanhTour.Views
{
    public class RestaurantDetailPage : ContentPage
    {
        private readonly Restaurant _restaurant;
        private readonly string _currentLang = Preferences.Default.Get("app_lang", "vi");

        // Vector Path Data (Material Design inspired)
        private const string NAV_PATH = "M12,2L4.5,20.29L5.21,21L12,18L18.79,21L19.5,20.29L12,2Z";
        private const string CALL_PATH = "M6.62,10.79C8.06,13.62 10.38,15.94 13.21,17.38L15.41,15.18C15.68,14.91 16.08,14.82 16.43,14.94C17.55,15.31 18.76,15.51 20,15.51C20.55,15.51 21,15.96 21,16.51V20C21,20.55 20.55,21 20,21C10.61,21 3,13.39 3,4C3,3.45 3.45,3 4,3H7.5C8.05,3 8.5,3.45 8.5,4C8.5,5.24 8.7,6.45 9.07,7.57C9.19,7.92 9.1,8.32 8.83,8.59L6.62,10.79Z";
        private const string HEART_PATH = "M12.1,20.9L11,19.9C5.1,14.5 1.3,11.1 1.3,6.9C1.3,3.5 4,0.8 7.4,0.8C9.3,0.8 11.1,1.7 12.3,3.1C13.5,1.7 15.3,0.8 17.2,0.8C20.6,0.8 23.3,3.5 23.3,6.9C23.3,11.1 19.5,14.5 13.6,19.9L12.1,20.9Z";
        private const string SHARE_PATH = "M18,16.08C17.24,16.08 16.56,16.38 16.04,16.85L8.91,12.7C8.96,12.47 9,12.24 9,12C9,11.76 8.96,11.53 8.91,11.3L15.96,7.19C16.5,7.69 17.21,8 18,8C19.66,8 21,6.66 21,5C21,3.34 19.66,2 18,2C16.34,2 15,3.34 15,5C15,5.24 15.04,5.47 15.09,5.7L8.04,9.81C7.5,9.31 6.79,9 6,9C4.34,9 3,10.34 3,12C3,13.66 4.34,15 6,15C6.79,15 7.5,14.69 8.04,14.19L15.16,18.34C15.11,18.55 15.08,18.77 15.08,19C15.08,20.61 16.39,21.92 18,21.92C19.61,21.92 20.92,20.61 20.92,19C20.92,17.39 19.61,16.08 18,16.08Z";
        private const string ADDR_PATH = "M12,2C8.13,2 5,5.13 5,9C5,14.25 12,22 12,22C12,22 19,14.25 19,9C19,5.13 15.87,2 12,2ZM12,11.5C10.62,11.5 9.5,10.38 9.5,9C9.5,7.62 10.62,6.5 12,6.5C13.38,6.5 14.5,7.62 14.5,9C14.5,10.38 13.38,11.5 12,11.5Z";
        private const string DESC_PATH = "M21,15H3V17H21V15M21,7H3V9H21V7M21,11H3V13H21V11M3,19H21V21H3V19Z";
        private const string TIME_PATH = "M12,20A8,8 0 0,0 20,12A8,8 0 0,0 12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22C6.47,22 2,17.53 2,12A10,10 0 0,1 12,2M12.5,7V12.25L17,14.92L16.25,16.15L11,13V7H12.5Z";

        public RestaurantDetailPage(Restaurant restaurant)
        {
            _restaurant = restaurant;
            BackgroundColor = Color.FromArgb("#F8FAFC");
            NavigationPage.SetHasNavigationBar(this, false);

            // Record manual click interaction
            _ = AnalyticsService.Instance.RecordPoiVisitAsync(_restaurant.Id, "click");

            CreateUI();
        }

        private void CreateUI()
        {
            var mainLayout = new Grid { RowDefinitions = { new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }, new RowDefinition { Height = GridLength.Auto } } };
            var scrollView = new ScrollView { VerticalScrollBarVisibility = ScrollBarVisibility.Never };
            var contentLayout = new VerticalStackLayout { Spacing = 0 };

            // 1. Immersive Header
            var headerGrid = new Grid { HeightRequest = 350 };
            if (string.IsNullOrEmpty(_restaurant.ImageUrl))
            {
                headerGrid.Add(new BoxView { BackgroundColor = Color.FromArgb("#1565C0"), HeightRequest = 350 });
                headerGrid.Add(new Microsoft.Maui.Controls.Shapes.Path { Data = (Geometry)new PathGeometryConverter().ConvertFromInvariantString("M11,9H13V7H11M12,20C7.59,20 4,16.41 4,12C4,7.59 7.59,4 12,4C16.41,4 20,7.59 20,12C20,16.41 16.41,20 12,20M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M11,17H13V11H11V17Z"), Fill = Colors.White, Opacity = 0.2, WidthRequest = 100, HeightRequest = 100, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center });
            }
            else
            {
                headerGrid.Add(new Image { Source = _restaurant.ImageUrl, Aspect = Aspect.AspectFill });
            }

            headerGrid.Add(new BoxView { Background = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1), GradientStops = new GradientStopCollection { new GradientStop(Colors.Transparent, 0.4f), new GradientStop(Color.FromArgb("#CC0D2137"), 1.0f) } } });

            var backBtn = new Border { BackgroundColor = Color.FromArgb("#40000000"), StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 24 }, Margin = new Thickness(20, 50, 0, 0), HeightRequest = 48, WidthRequest = 48, HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Start };
            backBtn.Content = new Label { Text = "←", TextColor = Colors.White, FontSize = 22, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
            backBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });
            headerGrid.Add(backBtn);
            contentLayout.Add(headerGrid);

            // 2. Info Content
            var infoSection = new VerticalStackLayout { Padding = new Thickness(24, 0, 24, 24), Spacing = 16, TranslationY = -30 };

            var nameCard = new Border { BackgroundColor = Colors.White, StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 24 }, Padding = 24, Shadow = new Shadow { Brush = Color.FromArgb("#000000"), Opacity = 0.1f, Radius = 15, Offset = new Point(0, 4) } };
            var nameStack = new VerticalStackLayout { Spacing = 8 };
            nameStack.Add(new Label { Text = _restaurant.Name, FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D2137") });
            var metaRow = new HorizontalStackLayout { Spacing = 12 };
            metaRow.Add(new Label { Text = $"⭐ {_restaurant.Rating:0.0}", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#FFB300"), VerticalOptions = LayoutOptions.Center });
            var status = GetOpenStatus(_restaurant.OpenHours);
            metaRow.Add(new Border { BackgroundColor = status.IsOpen ? Color.FromArgb("#E8F5E9") : Color.FromArgb("#FFEBEE"), StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 8 }, Padding = new Thickness(10, 4), Content = new Label { Text = status.Text, TextColor = status.IsOpen ? Color.FromArgb("#2E7D32") : Color.FromArgb("#C62828"), FontSize = 11, FontAttributes = FontAttributes.Bold } });
            nameStack.Add(metaRow);
            nameCard.Content = nameStack;
            infoSection.Add(nameCard);

            // Quick Actions (Professional Text-Only Buttons)
            var actionRow = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }, RowDefinitions = { new RowDefinition { Height = GridLength.Auto }, new RowDefinition { Height = GridLength.Auto } }, RowSpacing = 12, ColumnSpacing = 12 };
            
            actionRow.Add(CreateProfessionalButton(_currentLang switch { "en" => "DIRECTIONS", "zh" => "导航", _ => "DẪN ĐƯỜNG" }, async () => await DirectToMap(), Color.FromArgb("#1565C0"), Colors.White), 0, 0);
            actionRow.Add(CreateProfessionalButton(_currentLang switch { "en" => "CALL", "zh" => "电话", _ => "GỌI ĐIỆN" }, () => Launcher.OpenAsync("tel:0937916159"), Color.FromArgb("#F1F5F9"), Color.FromArgb("#1565C0")), 1, 0);
            actionRow.Add(CreateProfessionalButton(_restaurant.IsFavorite ? (_currentLang == "en" ? "SAVED" : (_currentLang == "zh" ? "已收藏" : "ĐÃ LƯU")) : (_currentLang == "en" ? "SAVE" : (_currentLang == "zh" ? "收藏" : "LƯU")), async () => { _restaurant.IsFavorite = !_restaurant.IsFavorite; await App.Database.UpdateRestaurantAsync(_restaurant); CreateUI(); }, _restaurant.IsFavorite ? Color.FromArgb("#E91E63") : Color.FromArgb("#F1F5F9"), _restaurant.IsFavorite ? Colors.White : Color.FromArgb("#64748B")), 0, 1);
            actionRow.Add(CreateProfessionalButton(_currentLang switch { "en" => "SHARE", "zh" => "分享", _ => "CHIA SẺ" }, () => Share.RequestAsync(new ShareTextRequest { Title = _restaurant.Name, Text = $"{_restaurant.Name} - {_restaurant.Address}", Uri = "https://vinhkhanhtour.com" }), Color.FromArgb("#F1F5F9"), Color.FromArgb("#64748B")), 1, 1);
            
            infoSection.Add(actionRow);

            // Details
            var detailsCard = new Border { BackgroundColor = Colors.White, StrokeShape = new RoundRectangle { CornerRadius = 24 }, StrokeThickness = 0, Padding = 24, Shadow = new Shadow { Brush = Color.FromArgb("#000000"), Opacity = 0.05f, Radius = 10, Offset = new Point(0, 2) } };
            var detailsStack = new VerticalStackLayout { Spacing = 20 };
            detailsStack.Add(CreateInfoRow(ADDR_PATH, _restaurant.Address));
            detailsStack.Add(CreateInfoRow(DESC_PATH, _restaurant.Description));
            detailsStack.Add(CreateInfoRow(TIME_PATH, _restaurant.OpenHours));
            detailsCard.Content = detailsStack;
            infoSection.Add(detailsCard);

            // Footer (Original Style Reverted)
            contentLayout.Add(infoSection);
            scrollView.Content = contentLayout;
            mainLayout.Add(scrollView, 0, 0);

            // Footer
            var footer = new Border { BackgroundColor = Colors.White, Padding = new Thickness(24, 16, 24, 32), VerticalOptions = LayoutOptions.End };
            var goBtn = new Border { HeightRequest = 58, Background = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0), GradientStops = new GradientStopCollection { new GradientStop(Color.FromArgb("#1565C0"), 0), new GradientStop(Color.FromArgb("#42A5F5"), 1) } }, StrokeShape = new RoundRectangle { CornerRadius = 20 }, Shadow = new Shadow { Brush = Color.FromArgb("#1565C0"), Opacity = 0.4f, Radius = 12, Offset = new Point(0, 5) } };
            goBtn.Content = new Label { Text = _currentLang switch { "en" => "GET DIRECTIONS ON MAP", "zh" => "在地图上获取路线", _ => "CHỈ ĐƯỜNG" }, TextColor = Colors.White, FontAttributes = FontAttributes.Bold, FontSize = 14, CharacterSpacing = 1, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
            goBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await DirectToMap()) });
            footer.Content = goBtn;
            mainLayout.Add(footer, 0, 1);
            Content = mainLayout;
        }

        private Border CreateProfessionalButton(string label, System.Action action, Color bgColor, Color textColor)
        {
            var btn = new Border
            {
                BackgroundColor = bgColor,
                HeightRequest = 48,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                StrokeThickness = 0,
                Padding = new Thickness(12, 0),
                Content = new Label 
                { 
                    Text = label, 
                    TextColor = textColor, 
                    FontSize = 13, 
                    FontAttributes = FontAttributes.Bold, 
                    HorizontalOptions = LayoutOptions.Center, 
                    VerticalOptions = LayoutOptions.Center,
                    CharacterSpacing = 0.5
                }
            };
            btn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(action) });
            return btn;
        }

        private HorizontalStackLayout CreateInfoRow(string pathData, string text)
        {
            var row = new HorizontalStackLayout { Spacing = 16 };
            row.Add(new Microsoft.Maui.Controls.Shapes.Path { Data = (Geometry)new PathGeometryConverter().ConvertFromInvariantString(pathData), Fill = Color.FromArgb("#1565C0"), WidthRequest = 20, HeightRequest = 20, VerticalOptions = LayoutOptions.Start, Margin = new Thickness(0, 2, 0, 0) });
            row.Add(new Label { Text = text, FontSize = 14, TextColor = Color.FromArgb("#475569"), LineHeight = 1.4, VerticalOptions = LayoutOptions.Center });
            return row;
        }

        private (bool IsOpen, string Text) GetOpenStatus(string hours)
        {
            if (string.IsNullOrWhiteSpace(hours)) return (true, _currentLang switch { "en" => "Open", "zh" => "营业中", _ => "Đang mở" });
            try
            {
                var now = DateTime.Now.TimeOfDay;
                var parts = hours.Split('-');
                if (parts.Length == 2)
                {
                    var start = TimeSpan.Parse(parts[0].Trim());
                    var end = TimeSpan.Parse(parts[1].Trim());
                    bool isOpen = (start < end) ? (now >= start && now <= end) : (now >= start || now <= end);
                    string text = isOpen ? (_currentLang switch { "en" => "OPEN NOW", "zh" => "正在营业", _ => "ĐANG MỞ CỬA" }) : (_currentLang switch { "en" => "CLOSED", "zh" => "已休息", _ => "ĐÃ ĐÓNG CỬA" });
                    if (!isOpen) text += $" • {(_currentLang == "en" ? "Opens" : (_currentLang == "zh" ? "营业于" : "Mở lúc"))} {parts[0].Trim()}";
                    return (isOpen, text);
                }
            } catch { }
            return (true, hours);
        }

        private async Task DirectToMap()
        {
            if (Application.Current?.MainPage is TabbedPage tabbed)
            {
                MapPage? map = null;
                NavigationPage? nav = null;
                foreach (var child in tabbed.Children)
                {
                    if (child is NavigationPage n && (n.RootPage is MapPage || n.CurrentPage is MapPage)) { map = (n.RootPage as MapPage) ?? (n.CurrentPage as MapPage); nav = n; break; }
                    if (child is MapPage m) { map = m; break; }
                }
                if (map != null) { tabbed.CurrentPage = nav ?? (Page)map; map.FocusAndDirect(_restaurant); }
            }
            await Navigation.PopAsync();
        }
    }
}

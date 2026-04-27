using System.Linq;
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

        // ── Fields thay thế x:Name từ XAML ──────────────────────────────────
        private VerticalStackLayout? _tourCardsContainer;
        private VerticalStackLayout? _ticketBannerContainer;
        private HorizontalStackLayout? _langBtnHStack;
        private VerticalStackLayout? _langDropdownList;
        private Border? _langDropdown;
        private WebView? _miniMapWebView;
        private Label? _langFlag;
        private Label? _lblNarration, _lblNarrationSub;
        private Label? _lblChoTour, _lblChoTourSub;
        private Label? _lblTapHint;
        private Label? _lblTourTheme, _lblTourThemeSub;
        private Label? _lblWelcome, _lblLocation;
        private Label? _lblMainSubtitle;
        private Label? _lblStats1Value, _lblStats1Text;
        private Label? _lblStats2Value;
        private Label? _lblStats3Value, _lblStats3Text;
        private Label? _lblCTA;

        public WelcomePage()
        {
            BuildUI();
            LoadMiniMap();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = LoadToursFromApiAsync();
            _ = LoadLanguagesFromApiAsync();
            UpdateTicketBanner();
        }

        // ── BUILD UI HOÀN TOÀN BẰNG CODE (không dùng XAML) ─────────────────
        private void BuildUI()
        {
            BackgroundColor = Color.FromArgb("#F8FAFF");
            NavigationPage.SetHasNavigationBar(this, false);

            var mainScroll = new ScrollView { VerticalScrollBarVisibility = ScrollBarVisibility.Never };
            var root = new VerticalStackLayout { Spacing = 0 };

            // ── Hero ─────────────────────────────────────────────────────────
            var heroGrid = new Grid { HeightRequest = 500 };
            var heroBg = new BoxView();
            heroBg.Background = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1), GradientStops = { new GradientStop(Color.FromArgb("#0D47A1"), 0), new GradientStop(Color.FromArgb("#1565C0"), 0.3f), new GradientStop(Color.FromArgb("#1976D2"), 0.6f), new GradientStop(Color.FromArgb("#2196F3"), 1) } };
            heroGrid.Add(heroBg);
            heroGrid.Add(new Ellipse { Fill = Colors.White, Opacity = 0.05, WidthRequest = 400, HeightRequest = 400, HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Start, Margin = new Thickness(0, -180, -150, 0) });
            heroGrid.Add(new Ellipse { Fill = Colors.White, Opacity = 0.08, WidthRequest = 220, HeightRequest = 220, HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.End, Margin = new Thickness(-110, 0, 0, -80) });

            var heroContent = new Grid { Padding = new Thickness(24, 50, 24, 28) };
            heroContent.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            heroContent.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            heroContent.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Top bar
            _lblLocation = new Label { Text = "Quận 4 · TP.HCM", FontSize = 12, TextColor = Colors.White, FontAttributes = FontAttributes.Bold, Opacity = 0.9, VerticalOptions = LayoutOptions.Center };
            var locationBorder = new Border { BackgroundColor = Color.FromArgb("#15FFFFFF"), StrokeShape = new RoundRectangle { CornerRadius = 12 }, Padding = new Thickness(12, 6), HorizontalOptions = LayoutOptions.Start, Content = new HorizontalStackLayout { Spacing = 6, Children = { new Label { Text = "📍", FontSize = 14 }, _lblLocation } } };
            // Khởi tạo cờ từ ngôn ngữ đã lưu (tránh hiện 🇻🇳 khi app_lang = "en")
            string initFlag = _currentLang switch { "en" => "🇺🇸", "zh" => "🇨🇳", "ja" => "🇯🇵", "ko" => "🇰🇷", _ => "🇻🇳" };
            _langFlag = new Label { Text = initFlag, FontSize = 20, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
            var langBtnGrid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, Padding = new Thickness(10, 0) };
            langBtnGrid.Add(_langFlag, 0, 0);
            langBtnGrid.Add(new Label { Text = "▾", FontSize = 12, TextColor = Color.FromArgb("#1565C0"), FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center, Margin = new Thickness(4, 0, 0, 0) }, 1, 0);
            var langBtn = new Border { BackgroundColor = Colors.White, StrokeShape = new RoundRectangle { CornerRadius = 22 }, Padding = new Thickness(4), HeightRequest = 44, WidthRequest = 84, Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.15f, Radius = 10, Offset = new Point(0, 4) }, Content = langBtnGrid };
            langBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => { _dropdownOpen = !_dropdownOpen; if (_langDropdown != null) _langDropdown.IsVisible = _dropdownOpen; }) });
            var topBar = new Grid();
            topBar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            topBar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            topBar.Add(locationBorder, 0, 0);
            topBar.Add(langBtn, 1, 0);
            Grid.SetRow(topBar, 0);
            heroContent.Add(topBar);

            // Dropdown
            _langDropdownList = new VerticalStackLayout { Spacing = 0 };
            _langDropdown = new Border { IsVisible = false, BackgroundColor = Colors.White, StrokeShape = new RoundRectangle { CornerRadius = 16 }, Stroke = Color.FromArgb("#E0E0E0"), Padding = new Thickness(0), Margin = new Thickness(0, 55, 0, 0), HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Start, WidthRequest = 160, Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.1f, Radius = 20, Offset = new Point(0, 10) }, Content = _langDropdownList };
            Grid.SetRow(_langDropdown, 0); Grid.SetRowSpan(_langDropdown, 3);
            heroContent.Add(_langDropdown);

            // Title
            _lblMainSubtitle = new Label { Text = "  CẨM NANG ẨM THỰC", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#BBDEFB"), CharacterSpacing = 2 };
            _lblWelcome = new Label { Text = "Khám phá tinh hoa ẩm thực đường phố Quận 4", FontSize = 16, TextColor = Colors.White, Opacity = 0.85, LineHeight = 1.5, Margin = new Thickness(2, 16, 0, 0) };
            _lblStats1Value = new Label { Text = "11", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center };
            _lblStats1Text = new Label { Text = "Quán ăn", FontSize = 10, TextColor = Colors.White, Opacity = 0.7, HorizontalOptions = LayoutOptions.Center };
            _lblStats2Value = new Label { Text = "4", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center };
            _lblStats3Value = new Label { Text = "4.8", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center };
            _lblStats3Text = new Label { Text = "Rating", FontSize = 10, TextColor = Colors.White, Opacity = 0.7, HorizontalOptions = LayoutOptions.Center };
            var statsBar = new Border { BackgroundColor = Color.FromArgb("#15FFFFFF"), StrokeShape = new RoundRectangle { CornerRadius = 16 }, Stroke = Color.FromArgb("#20FFFFFF"), Padding = new Thickness(20, 15), Margin = new Thickness(0, 20, 0, 0) };
            statsBar.Content = new HorizontalStackLayout
            {
                Spacing = 25,
                HorizontalOptions = LayoutOptions.Center,
                Children ={
                new VerticalStackLayout{HorizontalOptions=LayoutOptions.Center,Spacing=2,Children={_lblStats1Value,_lblStats1Text}},
                new BoxView{WidthRequest=1,BackgroundColor=Colors.White,Opacity=0.2,VerticalOptions=LayoutOptions.Fill},
                new VerticalStackLayout{HorizontalOptions=LayoutOptions.Center,Spacing=2,Children={_lblStats2Value,new Label{Text="Tour",FontSize=10,TextColor=Colors.White,Opacity=0.7,HorizontalOptions=LayoutOptions.Center}}},
                new BoxView{WidthRequest=1,BackgroundColor=Colors.White,Opacity=0.2,VerticalOptions=LayoutOptions.Fill},
                new VerticalStackLayout{HorizontalOptions=LayoutOptions.Center,Spacing=2,Children={_lblStats3Value,_lblStats3Text}}
            }
            };
            var titleStack = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Spacing = 0, Margin = new Thickness(0, -20, 0, 0) };
            titleStack.Add(new Label { Text = "VĨNH", FontSize = 56, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, CharacterSpacing = 4, LineHeight = 0.9 });
            titleStack.Add(new Label { Text = "KHÁNH", FontSize = 56, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, CharacterSpacing = 4, Margin = new Thickness(0, -5, 0, 0) });
            titleStack.Add(new HorizontalStackLayout { Spacing = 8, Margin = new Thickness(2, 8, 0, 0), Children = { new BoxView { WidthRequest = 24, HeightRequest = 3, BackgroundColor = Color.FromArgb("#64B5F6"), VerticalOptions = LayoutOptions.Center }, _lblMainSubtitle } });
            titleStack.Add(_lblWelcome);
            titleStack.Add(statsBar);
            Grid.SetRow(titleStack, 1);
            heroContent.Add(titleStack);

            // CTA
            _lblCTA = new Label { Text = "Bắt đầu hành trình", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center };
            var ctaContent = new HorizontalStackLayout
            {
                Spacing = 15,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Children = {
                    new Microsoft.Maui.Controls.Shapes.Path {
                        Data = (Geometry)new PathGeometryConverter().ConvertFrom("M2,21L23,12L2,3V10L17,12L2,14V21Z"),
                        Fill = Colors.White,
                        HeightRequest = 20,
                        WidthRequest = 20,
                        VerticalOptions = LayoutOptions.Center,
                        Aspect = Stretch.Uniform
                    },
                    _lblCTA,
                    new Label { Text = "➜", FontSize = 24, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center, Margin = new Thickness(5, 0, 0, 0) }
                }
            };
            var ctaBtn = new Border { HeightRequest = 64, StrokeShape = new RoundRectangle { CornerRadius = 24 }, StrokeThickness = 0, Background = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0), GradientStops = { new GradientStop(Color.FromArgb("#0D47A1"), 0), new GradientStop(Color.FromArgb("#1976D2"), 1) } }, Shadow = new Shadow { Brush = Color.FromArgb("#0D47A1"), Opacity = 0.4f, Radius = 15, Offset = new Point(0, 6) }, Content = ctaContent };
            ctaBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(OnMapClicked_Code) });
            Grid.SetRow(ctaBtn, 2);
            heroContent.Add(ctaBtn);
            heroGrid.Add(heroContent);
            root.Add(heroGrid);

            // ── Narration ────────────────────────────────────────────────────
            _lblNarration = new Label { Text = "Giọng thuyết minh", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1A237E") };
            _lblNarrationSub = new Label { Text = "Chọn ngôn ngữ audio tour", FontSize = 12, TextColor = Color.FromArgb("#78909C") };
            _langBtnHStack = new HorizontalStackLayout { Spacing = 12, Padding = new Thickness(2, 2, 24, 10) };
            var narSection = new VerticalStackLayout { BackgroundColor = Colors.White, Padding = new Thickness(24, 24), Spacing = 16 };
            narSection.Add(new HorizontalStackLayout { Spacing = 12, Children = { new Border { BackgroundColor = Color.FromArgb("#F0F6FF"), StrokeShape = new RoundRectangle { CornerRadius = 10 }, Padding = new Thickness(10), Content = new Label { Text = "🎙️", FontSize = 20 } }, new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Children = { _lblNarration, _lblNarrationSub } } } });
            narSection.Add(new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = _langBtnHStack });
            root.Add(narSection);

            // ── Mini Map ─────────────────────────────────────────────────────
            _lblChoTour = new Label { Text = "Bản đồ con phố", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1A237E") };
            _lblChoTourSub = new Label { Text = "Vĩnh Khánh, Phường 8, Quận 4", FontSize = 12, TextColor = Color.FromArgb("#78909C") };
            _miniMapWebView = new WebView { HorizontalOptions = LayoutOptions.Fill, VerticalOptions = LayoutOptions.Fill, InputTransparent = true };
            _lblTapHint = new Label { Text = "👆 Nhấn để xem bản đồ lớn", FontSize = 12, TextColor = Color.FromArgb("#1565C0"), FontAttributes = FontAttributes.Bold };
            var tapHintBorder = new Border { BackgroundColor = Color.FromArgb("#CCFFFFFF"), StrokeShape = new RoundRectangle { CornerRadius = 12 }, Padding = new Thickness(12, 6), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.End, Margin = new Thickness(0, 0, 0, 16), Content = _lblTapHint };
            var tapOverlay = new BoxView { BackgroundColor = Colors.Transparent };
            tapOverlay.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(OnMapClicked_Code) });
            var mapGrid = new Grid { Children = { _miniMapWebView, tapOverlay, tapHintBorder } };
            var mapBorder = new Border { StrokeShape = new RoundRectangle { CornerRadius = 24 }, Stroke = Color.FromArgb("#E0E0E0"), HeightRequest = 210, Padding = new Thickness(0), BackgroundColor = Color.FromArgb("#F5F5F5"), Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.08f, Radius = 15, Offset = new Point(0, 8) }, Content = mapGrid };
            var mapSection = new VerticalStackLayout { Padding = new Thickness(24, 10, 24, 24), Spacing = 16, BackgroundColor = Colors.White };
            mapSection.Add(new HorizontalStackLayout { Spacing = 12, Children = { new Border { BackgroundColor = Color.FromArgb("#F0F6FF"), StrokeShape = new RoundRectangle { CornerRadius = 10 }, Padding = new Thickness(10), Content = new Label { Text = "🗺️", FontSize = 20 } }, new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Children = { _lblChoTour, _lblChoTourSub } } } });
            mapSection.Add(mapBorder);
            root.Add(mapSection);

            // ── Ticket banner ────────────────────────────────────────────────
            _ticketBannerContainer = new VerticalStackLayout { Padding = new Thickness(24, 16, 24, 0), Spacing = 0 };
            root.Add(_ticketBannerContainer);

            // ── Tour section ─────────────────────────────────────────────────
            _lblTourTheme = new Label { Text = "Hành trình khám phá", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1A237E") };
            _lblTourThemeSub = new Label { Text = "Gợi ý tour dành cho bạn", FontSize = 12, TextColor = Color.FromArgb("#78909C") };
            _tourCardsContainer = new VerticalStackLayout { Spacing = 18, MinimumHeightRequest = 400 };
            var tourSection = new VerticalStackLayout { Padding = new Thickness(24, 20, 24, 40), Spacing = 20 };
            tourSection.Add(new HorizontalStackLayout { Spacing = 12, Children = { new Border { BackgroundColor = Color.FromArgb("#F0F6FF"), StrokeShape = new RoundRectangle { CornerRadius = 10 }, Padding = new Thickness(10), Content = new Label { Text = "🔥", FontSize = 20 } }, new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Children = { _lblTourTheme, _lblTourThemeSub } } } });
            tourSection.Add(_tourCardsContainer);
            root.Add(tourSection);

            mainScroll.Content = root;
            
            var mainGrid = new Grid();
            mainGrid.Add(mainScroll);

            // QR FAB
            var fab = new Border
            {
                BackgroundColor = Color.FromArgb("#2BBFB0"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 28 },
                WidthRequest = 56,
                HeightRequest = 56,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.End,
                Margin = new Thickness(0, 0, 24, 24),
                Shadow = new Shadow { Brush = Color.FromArgb("#2BBFB0"), Opacity = 0.5f, Radius = 15, Offset = new Point(0, 6) },
                Content = new Label { Text = "▣", FontSize = 28, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
            };
            fab.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () => await Navigation.PushAsync(new QRScanPage()))
            });
            mainGrid.Add(fab);

            Content = mainGrid;
        }

        // ══ TOURS ══
        private async Task LoadToursFromApiAsync()
        {
            try
            {
                var tours = await ApiService.Instance.GetToursAsync();
                if (tours.Count > 0) { _apiTours = tours; MainThread.BeginInvokeOnMainThread(() => { RenderTourCards(); UpdateTourThemeLabel(); }); }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[WelcomePage] Tours load failed: {ex.Message}"); }
        }

        private void RenderTourCards()
        {
            if (_tourCardsContainer == null) return;
            _tourCardsContainer.Children.Clear();
            string[] accentColors = { "#CC1565C0", "#CC0D47A1", "#CC01579B", "#CC311B92" };
            string[] shadowColors = { "#1565C0", "#1976D2", "#0288D1", "#4A148C" };
            for (int i = 0; i < _apiTours.Count; i++)
                _tourCardsContainer.Children.Add(BuildTourCard(_apiTours[i], i < accentColors.Length ? accentColors[i] : "#CC1565C0", i < shadowColors.Length ? shadowColors[i] : "#1565C0", i == _apiTours.Count - 1, i));
        }

        private Border BuildTourCard(ApiTour tour, string accent, string shadowColor, bool isLast, int idx)
        {
            var ptsCount = tour.GetRestaurantIds().Count;
            var ptsText = _currentLang switch { "en" => $"{ptsCount} spots", "zh" => $"{ptsCount}个景点", "ja" => $"{ptsCount}箇所", "ko" => $"{ptsCount}개 장소", _ => $"{ptsCount} điểm" };
            string[] coverImages = { "tour_oc.jpg", "tour_nuong.jpg", "tour_vat.jpg", "tour_dacssan.jpg" };
            string finalImg = !string.IsNullOrWhiteSpace(tour.ImageUrl) ? tour.ImageUrl : (idx < coverImages.Length ? coverImages[idx] : "tour_oc.jpg");

            var imageGrid = new Grid { HeightRequest = 160 };
            imageGrid.Add(new Border { StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(20, 20, 0, 0) }, StrokeThickness = 0, Content = new Image { Source = finalImg, Aspect = Aspect.AspectFill, HorizontalOptions = LayoutOptions.Fill, VerticalOptions = LayoutOptions.Fill } });
            imageGrid.Add(new BoxView { Background = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1), GradientStops = { new GradientStop(Colors.Transparent, 0.5f), new GradientStop(Color.FromArgb("#CC0D2137"), 1f) } } });
            imageGrid.Add(new Border { BackgroundColor = Color.FromArgb(accent), StrokeShape = new RoundRectangle { CornerRadius = 8 }, StrokeThickness = 0, Padding = new Thickness(10, 5), HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Start, Margin = new Thickness(12, 12, 0, 0), Content = new HorizontalStackLayout { Spacing = 5, Children = { new Label { Text = tour.Emoji, FontSize = 13 }, new Label { Text = tour.GetName(_currentLang).ToUpper(), FontSize = 10, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#BBDEFB"), VerticalOptions = LayoutOptions.Center } } } });
            imageGrid.Add(new Border { BackgroundColor = Color.FromArgb(accent), StrokeShape = new RoundRectangle { CornerRadius = 20 }, StrokeThickness = 0, Padding = new Thickness(10, 8), HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.End, Margin = new Thickness(0, 0, 12, 10), Content = new Label { Text = "›", FontSize = 20, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center } });

            var infoStack = new VerticalStackLayout
            {
                Padding = new Thickness(16, 12, 16, 14),
                Spacing = 6,
                Children ={
                new Label{Text=tour.GetName(_currentLang),FontSize=17,FontAttributes=FontAttributes.Bold,TextColor=Color.FromArgb("#0D2137")},
                new Label{Text=tour.GetDescription(_currentLang),FontSize=12,TextColor=Color.FromArgb("#5A7A9A")},
                new HorizontalStackLayout{Spacing=8,Margin=new Thickness(0,2,0,0),Children={
                    new Border{BackgroundColor=Color.FromArgb("#301565C0"),StrokeShape=new RoundRectangle{CornerRadius=8},StrokeThickness=0,Padding=new Thickness(8,4),Content=new Label{Text=$"⭐ {tour.Rating:F1}",FontSize=11,TextColor=Color.FromArgb("#64B5F6"),FontAttributes=FontAttributes.Bold}},
                    new Border{BackgroundColor=Color.FromArgb("#EEF4FF"),StrokeShape=new RoundRectangle{CornerRadius=8},StrokeThickness=0,Padding=new Thickness(8,4),Content=new Label{Text="⏱ "+tour.GetDuration(_currentLang),FontSize=11,TextColor=Color.FromArgb("#5A7A9A")}},
                    new Border{BackgroundColor=Color.FromArgb("#EEF4FF"),StrokeShape=new RoundRectangle{CornerRadius=8},StrokeThickness=0,Padding=new Thickness(8,4),Content=new Label{Text=ptsText,FontSize=11,TextColor=Color.FromArgb("#5A7A9A")}}
                }}
            }
            };

            var card = new Border { StrokeShape = new RoundRectangle { CornerRadius = 20 }, Stroke = Color.FromArgb("#CCE0FF"), StrokeThickness = 1, BackgroundColor = Colors.White, Shadow = new Shadow { Brush = Color.FromArgb(shadowColor), Opacity = 0.25f, Radius = 14, Offset = new Point(0, 5) }, Content = new VerticalStackLayout { Spacing = 0, Children = { imageGrid, infoStack } } };
            var tgr = new TapGestureRecognizer(); tgr.Tapped += (s, e) => OnTourCardClicked(idx); card.GestureRecognizers.Add(tgr);
            return card;
        }

        private void OnTourCardClicked(int idx)
        {
            if (idx >= _apiTours.Count) return;
            var t = _apiTours[idx];
            Navigation.PushAsync(new TourDetailPage(new Tour { Id = t.Id.ToString(), Name = t.GetName(_currentLang), Description = t.GetDescription(_currentLang), Duration = t.GetDuration(_currentLang), Rating = t.Rating, ImageUrl = t.ImageUrl ?? "", RestaurantIds = t.GetRestaurantIds() }));
        }

        private void UpdateTourThemeLabel()
        {
            var count = _apiTours.Count;
            if (_lblTourThemeSub != null) _lblTourThemeSub.Text = _currentLang switch { "en" => $"{count} unique food journeys", "zh" => $"{count}条独特的美食之旅", _ => $"{count} hành trình ẩm thực độc đáo" };
            if (_lblStats2Value != null) _lblStats2Value.Text = count.ToString();
        }

        // ══ TICKET BANNER ══
        private void UpdateTicketBanner()
        {
            if (_ticketBannerContainer == null) return;
            _ticketBannerContainer.Children.Clear();
            if (!VinhKhanhTour.Services.TicketService.Instance.HasValidTicket)
            {
                var banner = new Border { Margin = new Thickness(0, 0, 0, 4), StrokeShape = new RoundRectangle { CornerRadius = 20 }, StrokeThickness = 0, Padding = new Thickness(20, 16), Background = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0), GradientStops = { new GradientStop(Color.FromArgb("#FF6F00"), 0), new GradientStop(Color.FromArgb("#FFA000"), 1) } }, Shadow = new Shadow { Brush = Color.FromArgb("#FF6F00"), Opacity = 0.3f, Radius = 12, Offset = new Point(0, 5) } };
                var textStack = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
                textStack.Add(new Label { Text = _currentLang switch { "en" => "🎫 Unlock Premium Features!", "zh" => "🎫 解锁高级功能！", _ => "🎫 Mở khoá tính năng Premium!" }, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Colors.White });
                textStack.Add(new Label { Text = _currentLang switch { "en" => "Audio HD · Offline Map · Photo Frame · from 29,000đ", "zh" => "高清音频·离线地图·相框·从29,000đ起", _ => "Audio HD · Bản đồ offline · Khung ảnh · từ 29.000đ" }, FontSize = 11, TextColor = Color.FromArgb("#FFF8E1") });
                var bannerGrid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
                bannerGrid.Add(textStack, 0, 0);
                bannerGrid.Add(new Label { Text = "›", FontSize = 28, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.End }, 1, 0);
                banner.Content = bannerGrid;
                banner.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PushAsync(new TicketStorePage())) });
                _ticketBannerContainer.Children.Add(banner);
            }
            else
            {
                var ts = VinhKhanhTour.Services.TicketService.Instance;
                var activeBanner = new Border { BackgroundColor = Color.FromArgb("#E8F5E9"), StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = new Thickness(16, 10), Margin = new Thickness(0, 0, 0, 4) };
                activeBanner.Content = new HorizontalStackLayout { Spacing = 10, HorizontalOptions = LayoutOptions.Center, Children = { new Label { Text = "✅", FontSize = 16 }, new Label { Text = _currentLang switch { "en" => $"Premium Active · {ts.Points} pts · {ts.GetUnlockedBadges().Count} badges", _ => $"Premium đang dùng · {ts.Points} điểm · {ts.GetUnlockedBadges().Count} huy hiệu" }, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#2E7D32"), VerticalOptions = LayoutOptions.Center } } };
                activeBanner.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PushAsync(new MyTicketPage())) });
                _ticketBannerContainer.Children.Add(activeBanner);
            }
        }

        // ══ MAP ══
        private void LoadMiniMap()
        {
            if (_miniMapWebView == null) return;
            var html = $@"<!DOCTYPE html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1.0'><style>*{{margin:0;padding:0}}body,html{{height:100%;overflow:hidden}}#map{{width:100%;height:200px}}</style></head><body><div id='map'></div><script>function initMap(){{var c={{lat:10.7615,lng:106.7045}};var map=new google.maps.Map(document.getElementById('map'),{{center:c,zoom:16,disableDefaultUI:true,gestureHandling:'none'}});[{{lat:10.760825,lng:106.703313}},{{lat:10.761090,lng:106.702899}},{{lat:10.761758,lng:106.702358}},{{lat:10.761281,lng:106.705373}},{{lat:10.761060,lng:106.706682}}].forEach(function(p){{new google.maps.Marker({{position:p,map:map,icon:{{path:google.maps.SymbolPath.CIRCLE,scale:9,fillColor:'#42A5F5',fillOpacity:1,strokeColor:'white',strokeWeight:2}}}});}});}}</script><script src='https://maps.googleapis.com/maps/api/js?key={MAPS_API_KEY}&loading=async&callback=initMap'></script></body></html>";
            _miniMapWebView.Source = new HtmlWebViewSource { Html = html };
        }

        private void OnMapClicked_Code()
        {
            if (Application.Current?.MainPage is TabbedPage tabbedPage)
                tabbedPage.CurrentPage = tabbedPage.Children[1];
        }

        // Event handlers cho backward compat (nếu XAML cũ còn gọi)
        private void OnMapClicked(object sender, EventArgs e) => OnMapClicked_Code();
        private void OnLangBtnClicked(object sender, EventArgs e)
        {
            _dropdownOpen = !_dropdownOpen;
            if (_langDropdown != null) _langDropdown.IsVisible = _dropdownOpen;
        }

        // ══ LANGUAGES ══
        private async Task LoadLanguagesFromApiAsync()
        {
            try
            {
                var langs = await ApiService.Instance.GetLanguagesAsync();
                if (langs.Count > 0) { _availableLanguages = langs; MainThread.BeginInvokeOnMainThread(() => { RenderLanguageButtons(); UpdateLanguage(_currentLang); }); }
            }
            catch
            {
                _availableLanguages = new List<AppLanguage> { new() { Code = "vi", Name = "Tiếng Việt", Flag = "🇻🇳", IsDefault = true }, new() { Code = "en", Name = "English", Flag = "🇺🇸" }, new() { Code = "zh", Name = "中文", Flag = "🇨🇳" } };
                // Gọi UpdateLanguage để đảm bảo toàn bộ text và cờ khớp với ngôn ngữ đã lưu
                MainThread.BeginInvokeOnMainThread(() => { RenderLanguageButtons(); UpdateLanguage(_currentLang); });
            }
        }

        private void RenderLanguageButtons()
        {
            if (_langBtnHStack == null) return;
            _langBtnHStack.Children.Clear();
            var activeColor = Color.FromArgb("#1565C0");
            foreach (var lang in _availableLanguages)
            {
                var code = lang.Code;
                var isActive = code == _currentLang;

                var flagLabel = new Label
                {
                    Text = lang.Flag,
                    FontSize = 26,
                    HorizontalOptions = LayoutOptions.Center
                };
                var nameLabel = new Label
                {
                    Text = lang.Name,
                    FontSize = 11,
                    FontAttributes = isActive ? FontAttributes.Bold : FontAttributes.None,
                    TextColor = isActive ? Colors.White : Color.FromArgb("#5A7A9A"),
                    HorizontalOptions = LayoutOptions.Center
                };

                var frame = new Border
                {
                    StrokeShape = new RoundRectangle { CornerRadius = 14 },
                    Stroke = isActive ? activeColor : Color.FromArgb("#CCE0FF"),
                    StrokeThickness = isActive ? 2.5 : 1,
                    BackgroundColor = isActive ? activeColor : Colors.White,
                    Padding = new Thickness(12, 10),
                    WidthRequest = 85,
                    HeightRequest = 78,
                    Shadow = isActive ? new Shadow { Brush = activeColor, Opacity = 0.3f, Radius = 10, Offset = new Point(0, 4) } : null,
                    AutomationId = $"lang-btn-{code}",
                    Content = new VerticalStackLayout
                    {
                        Spacing = 5,
                        VerticalOptions = LayoutOptions.Center,
                        Children = { flagLabel, nameLabel }
                    }
                };

                // Dùng TapGestureRecognizer trực tiếp — đáng tin cậy hơn Button transparent trên Android
                var tgr = new TapGestureRecognizer();
                tgr.Tapped += (_, _) => SetLanguage(code);
                frame.GestureRecognizers.Add(tgr);

                _langBtnHStack.Children.Add(frame);
            }
        }

        private void SetLanguage(string lang)
        {
            _dropdownOpen = false; if (_langDropdown != null) _langDropdown.IsVisible = false;
            if (_currentLang == lang) return;
            Preferences.Default.Set("app_lang", lang);
            VinhKhanhTour.Services.AudioService.Instance.SetLanguage(lang);
            
            var rootPage = Application.Current?.MainPage;
            MainTabbedPage? tabbedPage = rootPage as MainTabbedPage;
            if (tabbedPage == null && rootPage is NavigationPage nav)
                tabbedPage = nav.CurrentPage as MainTabbedPage;

            if (tabbedPage != null) tabbedPage.UpdateLanguage(lang);
            UpdateLanguage(lang);
        }

        public void UpdateLanguage(string lang)
        {
            _currentLang = lang;
            var activeLang = _availableLanguages.FirstOrDefault(l => l.Code == lang);
            if (_langFlag != null) _langFlag.Text = activeLang?.Flag ?? lang switch { "en" => "🇺🇸", "zh" => "🇨🇳", _ => "🇻🇳" };

            if (_langBtnHStack != null)
            {
                var activeColor = Color.FromArgb("#1565C0");
                foreach (var child in _langBtnHStack.Children)
                {
                    // Mỗi child là Border trực tiếp (không còn Grid wrapper)
                    if (child is not Border btnBorder) continue;

                    var isActive = btnBorder.AutomationId == $"lang-btn-{lang}";
                    btnBorder.BackgroundColor = isActive ? activeColor : Colors.White;
                    btnBorder.Stroke = isActive ? activeColor : Color.FromArgb("#CCE0FF");
                    btnBorder.StrokeThickness = isActive ? 2.5 : 1;
                    btnBorder.Shadow = isActive ? new Shadow { Brush = activeColor, Opacity = 0.3f, Radius = 10, Offset = new Point(0, 4) } : null;
                    if (btnBorder.Content is VerticalStackLayout vsl && vsl.Children.Count >= 2 && vsl.Children[1] is Label nl)
                    { nl.TextColor = isActive ? Colors.White : Color.FromArgb("#5A7A9A"); nl.FontAttributes = isActive ? FontAttributes.Bold : FontAttributes.None; }
                }
            }

            switch (lang)
            {
                case "en":
                    if (_lblNarration != null) _lblNarration.Text = "Voice Narration"; if (_lblNarrationSub != null) _lblNarrationSub.Text = "Select your guide language";
                    if (_lblChoTour != null) _lblChoTour.Text = "Food Map"; if (_lblChoTourSub != null) _lblChoTourSub.Text = "Famous street food hub in District 4";
                    if (_lblTapHint != null) _lblTapHint.Text = "👆 Tap to explore full map";
                    if (_lblTourTheme != null) _lblTourTheme.Text = "Curated Experiences"; if (_lblTourThemeSub != null) _lblTourThemeSub.Text = "Recommended tours for you";
                    if (_lblWelcome != null) _lblWelcome.Text = "Discover the hidden gems of District 4 food street"; if (_lblLocation != null) _lblLocation.Text = "District 4 · HCMC";
                    if (_lblMainSubtitle != null) _lblMainSubtitle.Text = "  FOOD TOUR GUIDE";
                    if (_lblStats1Value != null) _lblStats1Value.Text = "11"; if (_lblStats1Text != null) _lblStats1Text.Text = "Spots";
                    if (_lblStats3Value != null) _lblStats3Value.Text = "4.5★"; if (_lblStats3Text != null) _lblStats3Text.Text = "Rating";
                    if (_lblCTA != null) _lblCTA.Text = "Start Exploring"; break;
                case "zh":
                    if (_lblNarration != null) _lblNarration.Text = "语音导览"; if (_lblNarrationSub != null) _lblNarrationSub.Text = "选择导游语言";
                    if (_lblChoTour != null) _lblChoTour.Text = "美食地图"; if (_lblChoTourSub != null) _lblChoTourSub.Text = "第四郡著名美食街";
                    if (_lblTapHint != null) _lblTapHint.Text = "👆 点击查看详细地图";
                    if (_lblTourTheme != null) _lblTourTheme.Text = "为您推荐的行程"; if (_lblTourThemeSub != null) _lblTourThemeSub.Text = "根据您的喜好推荐";
                    if (_lblWelcome != null) _lblWelcome.Text = "探索第四郡最著名的美食街"; if (_lblLocation != null) _lblLocation.Text = "第四郡 · 胡志明市";
                    if (_lblMainSubtitle != null) _lblMainSubtitle.Text = "  美食指南";
                    if (_lblStats1Value != null) _lblStats1Value.Text = "11"; if (_lblStats1Text != null) _lblStats1Text.Text = "餐厅";
                    if (_lblStats3Value != null) _lblStats3Value.Text = "4.5★"; if (_lblStats3Text != null) _lblStats3Text.Text = "评分";
                    if (_lblCTA != null) _lblCTA.Text = "开始探索"; break;
                case "ko":
                    if (_lblNarration != null) _lblNarration.Text = "오디오 가이드"; if (_lblNarrationSub != null) _lblNarrationSub.Text = "가이드 언어를 선택하세요";
                    if (_lblChoTour != null) _lblChoTour.Text = "음식 지도"; if (_lblChoTourSub != null) _lblChoTourSub.Text = "4군에서 가장 유명한 음식 거리";
                    if (_lblTapHint != null) _lblTapHint.Text = "👆 탭하여 전체 지도 보기";
                    if (_lblTourTheme != null) _lblTourTheme.Text = "추천 여행 코스"; if (_lblTourThemeSub != null) _lblTourThemeSub.Text = "당신을 위한 맞춤형 투어";
                    if (_lblWelcome != null) _lblWelcome.Text = "4군의 숨겨진 맛집을 찾아보세요"; if (_lblLocation != null) _lblLocation.Text = "호치민 · 4군";
                    if (_lblMainSubtitle != null) _lblMainSubtitle.Text = "  푸드 투어 가이드";
                    if (_lblStats1Value != null) _lblStats1Value.Text = "11"; if (_lblStats1Text != null) _lblStats1Text.Text = "장소";
                    if (_lblStats3Value != null) _lblStats3Value.Text = "4.5★"; if (_lblStats3Text != null) _lblStats3Text.Text = "평점";
                    if (_lblCTA != null) _lblCTA.Text = "탐험 시작하기"; break;
                case "ja":
                    if (_lblNarration != null) _lblNarration.Text = "音声ガイド"; if (_lblNarrationSub != null) _lblNarrationSub.Text = "ガイドの言語を選択";
                    if (_lblChoTour != null) _lblChoTour.Text = "グルメマップ"; if (_lblChoTourSub != null) _lblChoTourSub.Text = "第4区の有名な飲食街";
                    if (_lblTapHint != null) _lblTapHint.Text = "👆 タップして地図を表示";
                    if (_lblTourTheme != null) _lblTourTheme.Text = "おすすめツアー"; if (_lblTourThemeSub != null) _lblTourThemeSub.Text = "あなたにぴったりのコース";
                    if (_lblWelcome != null) _lblWelcome.Text = "第4区の美食を探索しましょう"; if (_lblLocation != null) _lblLocation.Text = "第4区 · ホーチミン市";
                    if (_lblMainSubtitle != null) _lblMainSubtitle.Text = "  美食ガイド";
                    if (_lblStats1Value != null) _lblStats1Value.Text = "11"; if (_lblStats1Text != null) _lblStats1Text.Text = "スポット";
                    if (_lblStats3Value != null) _lblStats3Value.Text = "4.5★"; if (_lblStats3Text != null) _lblStats3Text.Text = "評価";
                    if (_lblCTA != null) _lblCTA.Text = "探索を始める"; break;
                default:
                    if (_lblNarration != null) _lblNarration.Text = "Giọng thuyết minh"; if (_lblNarrationSub != null) _lblNarrationSub.Text = "Chọn ngôn ngữ audio tour";
                    if (_lblChoTour != null) _lblChoTour.Text = "Bản đồ con phố"; if (_lblChoTourSub != null) _lblChoTourSub.Text = "Vĩnh Khánh, Phường 8, Quận 4";
                    if (_lblTapHint != null) _lblTapHint.Text = "👆 Nhấn để xem bản đồ lớn";
                    if (_lblTourTheme != null) _lblTourTheme.Text = "Hành trình khám phá"; if (_lblTourThemeSub != null) _lblTourThemeSub.Text = "Gợi ý tour dành cho bạn";
                    if (_lblWelcome != null) _lblWelcome.Text = "Khám phá tinh hoa ẩm thực đường phố Quận 4"; if (_lblLocation != null) _lblLocation.Text = "Quận 4 · TP.HCM";
                    if (_lblMainSubtitle != null) _lblMainSubtitle.Text = "  CẨM NANG ẨM THỰC";
                    if (_lblStats1Value != null) _lblStats1Value.Text = "11"; if (_lblStats1Text != null) _lblStats1Text.Text = "Quán ăn";
                    if (_lblStats3Value != null) _lblStats3Value.Text = "4.5★"; if (_lblStats3Text != null) _lblStats3Text.Text = "Đánh giá";
                    if (_lblCTA != null) _lblCTA.Text = "Bắt đầu khám phá"; break;
            }
            if (_apiTours.Count > 0) { UpdateTourThemeLabel(); RenderTourCards(); }
            UpdateTicketBanner();
        }
    }
}
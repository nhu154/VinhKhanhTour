using Microsoft.Maui.Controls.Shapes;
using VinhKhanhTour.Models;

namespace VinhKhanhTour.Views;

public partial class ProfilePage : ContentPage
{
    private Label _visitCountLabel = null!;
    private Label _tourCountLabel = null!;
    private Label _pointsLabel = null!;
    private VerticalStackLayout _visitHistoryLayout = null!;
    private string _currentLang = Preferences.Default.Get("app_lang", "vi");

    // Labels needing language update
    private Label _lblPageTitle = null!;
    private Label _lblStatVisit = null!;
    private Label _lblStatBadge = null!;
    private Label _lblStatPoints = null!;
    private Label _lblMenuSection = null!;
    private Label _lblLangSub = null!;
    private Label _lblHistorySection = null!;
    private Label _lblLoading = null!;
    private Label _lblFav = null!;
    private Label _lblOffers = null!;
    private Label _lblBadgeMoi = null!;
    private Label _lblLang = null!;
    private Label _lblStats = null!;
    private Label _lblSettings = null!;
    private Label _lblUserName = null!;
    private Label _lblUserRole = null!;
    private Label _lblEditBtn = null!;

    public ProfilePage()
    {
        InitializeComponent();
        BuildUI();
    }

    public void UpdateLanguage(string lang)
    {
        _currentLang = lang;
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        var lang = _currentLang;
        if (_lblPageTitle != null)
            _lblPageTitle.Text = lang switch { "en" => "My Profile", "zh" => "\u6211\u7684\u4E3B\u9875", _ => "Hồ sơ của tôi" };
        if (_lblStatVisit != null)
            _lblStatVisit.Text = lang switch { "en" => "Visited", "zh" => "\u53BB\u8FC7", _ => "Quán đã ghé" };
        if (_lblStatBadge != null)
            _lblStatBadge.Text = lang switch { "en" => "Badges", "zh" => "\u5FBD\u7AE0", _ => "Huy hiệu" };
        if (_lblStatPoints != null)
            _lblStatPoints.Text = lang switch { "en" => "Points", "zh" => "\u79EF\u5206", _ => "Điểm thưởng" };
        if (_lblMenuSection != null)
            _lblMenuSection.Text = lang switch { "en" => "Account", "zh" => "\u8D26\u6237", _ => "Tài khoản" };
        if (_lblLangSub != null)
            _lblLangSub.Text = lang switch { "en" => "English", "zh" => "\u4E2D\u6587", _ => "Tiếng Việt" };
        if (_lblHistorySection != null)
            _lblHistorySection.Text = lang switch { "en" => "Recent Activity", "zh" => "\u6700\u8FD1\u6D3B\u52A8", _ => "Hoạt động gần đây" };

        if (_lblFav != null) _lblFav.Text = lang switch { "en" => "Favorite Spots", "zh" => "\u6536\u85CF\u7684\u9910\u5385", _ => "Quán yêu thích" };
        if (_lblOffers != null) _lblOffers.Text = lang switch { "en" => "My Offers", "zh" => "\u6211\u7684\u4F18\u60E0", _ => "Ưu đãi của tôi" };
        if (_lblBadgeMoi != null) _lblBadgeMoi.Text = lang switch { "en" => "New", "zh" => "\u65B0", _ => "Mới" };
        if (_lblLang != null) _lblLang.Text = lang switch { "en" => "System Language", "zh" => "\u7CFB\u7EDF\u8BED\u8A00", _ => "Ngôn ngữ hệ thống" };
        if (_lblStats != null) _lblStats.Text = lang switch { "en" => "Analytics", "zh" => "\u7EDF\u8BA1\u5206\u6790", _ => "Thống kê phân tích" };
        if (_lblSettings != null) _lblSettings.Text = lang switch { "en" => "Settings & Privacy", "zh" => "\u8BBE\u7F6E\u4E0E\u9690\u79C1", _ => "Cài đặt & Quyền riêng tư" };
        // Only update name/role for language if user is NOT authenticated (guest default text)
        var session = VinhKhanhTour.Services.UserSession.Instance;
        if (_lblUserName != null && !session.IsAuthenticatedUser)
            _lblUserName.Text = lang switch { "en" => "Vinh Khanh Tourist", "zh" => "\u6c38\u5e86\u6e38\u5ba2", _ => "Du kh\u00e1ch V\u0129nh Kh\u00e1nh" };
        if (_lblUserRole != null && !session.IsAuthenticatedUser)
            _lblUserRole.Text = lang switch { "en" => "\u2728 Guest Explorer", "zh" => "\u2728 \u6e38\u5ba2", _ => "Kh\u00e1ch tham quan" };
        if (_lblEditBtn != null) _lblEditBtn.Text = lang switch { "en" => "✏\uFE0F Edit", "zh" => "✏\uFE0F \u7F16\u8F91", _ => "Sửa" };
        if (_lblLoading != null && _lblLoading.Text != null && _lblLoading.Text.EndsWith("..."))
            _lblLoading.Text = lang switch { "en" => "Loading data...", "zh" => "\u52A0\u8F7D\u6570\u636E...", _ => "Đang tải dữ liệu..." };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadStatsAsync();
    }

    private void BuildUI()
    {
        BackgroundColor = Color.FromArgb("#F0F6FF");

        var scroll = new ScrollView { VerticalScrollBarVisibility = ScrollBarVisibility.Never };
        var root = new VerticalStackLayout { Spacing = 24, Padding = new Thickness(0, 0, 0, 40) };

        // ── 1. Premium Hero Header ──────────────────────────────────────────
        var headerArea = new Grid { HeightRequest = 300 };

        // Background Glow
        headerArea.Children.Add(new BoxView
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb("#FF1565C0"), 0),
                    new GradientStop(Color.FromArgb("#3342A5F5"), 1)
                }
            }
        });

        var topNav = new Grid { Padding = new Thickness(24, 60, 24, 0), VerticalOptions = LayoutOptions.Start };
        _lblPageTitle = new Label { Text = "Hồ sơ của tôi", FontSize = 28, FontAttributes = FontAttributes.Bold, TextColor = Colors.White };
        topNav.Add(_lblPageTitle, 0, 0);

        var btnEdit = new Border
        {
            BackgroundColor = Color.FromArgb("#15FFFFFF"),
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            StrokeThickness = 0,
            Padding = new Thickness(16, 8),
            HorizontalOptions = LayoutOptions.End
        };
        _lblEditBtn = new Label { Text = "Sửa", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center };
        btnEdit.Content = _lblEditBtn;
        topNav.Add(btnEdit, 0, 0);
        headerArea.Add(topNav);

        var headerStack = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.End,
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 12,
            Margin = new Thickness(0, 0, 0, 10)
        };

        // Avatar with gradient glowing ring
        var avatarRing = new Border
        {
            BackgroundColor = Color.FromArgb("#0D1B2A"),
            StrokeShape = new RoundRectangle { CornerRadius = 55 },
            WidthRequest = 110,
            HeightRequest = 110,
            HorizontalOptions = LayoutOptions.Center,
            StrokeThickness = 4,
            Stroke = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1), GradientStops = { new GradientStop(Color.FromArgb("#42A5F5"), 0), new GradientStop(Color.FromArgb("#E91E63"), 1) } },
            Content = new Label { Text = "\U0001F60E", FontSize = 55, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        };
        headerStack.Add(avatarRing);
        var _session = VinhKhanhTour.Services.UserSession.Instance;
        _lblUserName = new Label
        {
            Text = _session.IsAuthenticatedUser ? _session.FullName : "Du khách Vĩnh Khánh",
            FontSize = 24,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center
        };
        headerStack.Add(_lblUserName);
        _lblUserRole = new Label
        {
            Text = _session.IsAuthenticatedUser ? "Thành viên Vinh Khánh Tour" : "Khách tham quan",
            FontSize = 14,
            TextColor = Color.FromArgb("#64B5F6"),
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 0, 8)
        };
        headerStack.Add(_lblUserRole);

        headerArea.Children.Add(headerStack);
        root.Add(headerArea);

        // Guest notice banner
        if (!VinhKhanhTour.Services.UserSession.Instance.IsAuthenticatedUser)
        {
            var guestBanner = new Border
            {
                BackgroundColor = Color.FromArgb("#0F2030"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
                Stroke = Color.FromArgb("#1565C0"),
                StrokeThickness = 1,
                Padding = new Thickness(16, 12),
                Margin = new Thickness(24, 0)
            };
            var bannerGrid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
            var bannerText = new VerticalStackLayout { Spacing = 4 };
            bannerText.Add(new Label { Text = "Bạn đang tham quan với tư cách Khách", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Colors.White });
            bannerText.Add(new Label { Text = "Đăng nhập để lưu lịch sử & điểm thưởng", FontSize = 12, TextColor = Color.FromArgb("#8ba0b2") });
            bannerGrid.Add(bannerText, 0, 0);
            var loginBtn = new Border { BackgroundColor = Color.FromArgb("#1565C0"), StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 }, StrokeThickness = 0, Padding = new Thickness(12, 8), VerticalOptions = LayoutOptions.Center };
            loginBtn.Content = new Label { Text = "Đăng nhập", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Colors.White };
            loginBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => { Application.Current!.MainPage = new NavigationPage(new LoginPage()); }) });
            bannerGrid.Add(loginBtn, 1, 0);
            guestBanner.Content = bannerGrid;
            root.Add(guestBanner);
        }

        // ── 2. Glassmorphism Stats Row ───────────────────────────────────────
        _visitCountLabel = MakeStatLabel();
        _tourCountLabel = MakeStatLabel();
        _pointsLabel = MakeStatLabel();

        var statsGlass = new Border
        {
            BackgroundColor = Color.FromArgb("#FFFFFF"),
            Stroke = Color.FromArgb("#CCE0FF"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 24 },
            Padding = new Thickness(20),
            Margin = new Thickness(24, 0),
            Shadow = new Shadow { Brush = Color.FromArgb("#1565C0"), Opacity = 0.2f, Radius = 15, Offset = new Point(0, 8) }
        };
        var statsGrid = new Grid { ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition { Width = 1 }, new ColumnDefinition(), new ColumnDefinition { Width = 1 }, new ColumnDefinition() } };

        _lblStatVisit = new Label { Text = "Quán đã ghé", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#5A7A9A"), HorizontalOptions = LayoutOptions.Center };
        _lblStatBadge = new Label { Text = "Huy hiệu", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#5A7A9A"), HorizontalOptions = LayoutOptions.Center };
        _lblStatPoints = new Label { Text = "Điểm thưởng", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#5A7A9A"), HorizontalOptions = LayoutOptions.Center };
        statsGrid.Add(MakeStatNode(_visitCountLabel, _lblStatVisit, "\U0001F37D"), 0, 0);
        statsGrid.Add(new BoxView { BackgroundColor = Color.FromArgb("#1AFFFFFF"), WidthRequest = 1, VerticalOptions = LayoutOptions.Fill, Margin = new Thickness(0, 8) }, 1, 0);
        statsGrid.Add(MakeStatNode(_tourCountLabel, _lblStatBadge, "\U0001F6E1"), 2, 0);
        statsGrid.Add(new BoxView { BackgroundColor = Color.FromArgb("#1AFFFFFF"), WidthRequest = 1, VerticalOptions = LayoutOptions.Fill, Margin = new Thickness(0, 8) }, 3, 0);
        statsGrid.Add(MakeStatNode(_pointsLabel, _lblStatPoints, "\U0001F48E"), 4, 0);

        statsGlass.Content = statsGrid;
        root.Add(statsGlass);

        // ── 3. Menu Options ────────────────────────────────────────────────
        var menuSection = new VerticalStackLayout { Padding = new Thickness(20, 10), Spacing = 16 };
        _lblMenuSection = new Label { Text = "Tài khoản", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D2137"), Margin = new Thickness(4, 0) };
        menuSection.Add(_lblMenuSection);

        var menuItems = new VerticalStackLayout { Spacing = 0 };

        _lblFav = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D2137"), VerticalOptions = LayoutOptions.Center };
        var favItem = MakeMenuItem("\u2B50", _lblFav, null, Color.FromArgb("#FFCA28"));
        favItem.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PushModalAsync(new FavoriteRestaurantsPage())) });
        menuItems.Add(favItem);
        menuItems.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#0FFFFFFF"), Margin = new Thickness(80, 0, 20, 0) });

        _lblOffers = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D2137"), VerticalOptions = LayoutOptions.Center };
        _lblBadgeMoi = new Label { FontSize = 11, TextColor = Color.FromArgb("#FF8A80"), FontAttributes = FontAttributes.Bold };
        menuItems.Add(MakeMenuItem("\U0001F381", _lblOffers, _lblBadgeMoi, Color.FromArgb("#E91E63")));
        menuItems.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#0FFFFFFF"), Margin = new Thickness(80, 0, 20, 0) });

        _lblLangSub = new Label { FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#64B5F6") };
        _lblLang = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D2137") };
        menuItems.Add(MakeMenuItemWithSub("\U0001F310", _lblLang, _lblLangSub, Color.FromArgb("#42A5F5")));
        menuItems.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#0FFFFFFF"), Margin = new Thickness(80, 0, 20, 0) });

        _lblStats = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D2137"), VerticalOptions = LayoutOptions.Center };
        var statsItem = MakeMenuItem("\U0001F4CA", _lblStats, null, Color.FromArgb("#00BFA5"));
        statsItem.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PushModalAsync(new AnalyticsPage())) });
        menuItems.Add(statsItem);
        menuItems.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#0FFFFFFF"), Margin = new Thickness(80, 0, 20, 0) });

        _lblSettings = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D2137"), VerticalOptions = LayoutOptions.Center };
        menuItems.Add(MakeMenuItem("\u2699", _lblSettings, null, Color.FromArgb("#9E9E9E")));

        menuSection.Add(MakeCard(menuItems));
        root.Add(menuSection);

        // ── 4. Recent Visits ────────────────────────────────────────────────
        var historySection = new VerticalStackLayout { Padding = new Thickness(20, 10), Spacing = 16 };
        _lblHistorySection = new Label { Text = "Hoạt động gần đây", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D2137"), Margin = new Thickness(4, 0) };
        historySection.Add(_lblHistorySection);

        _visitHistoryLayout = new VerticalStackLayout { Spacing = 12 };
        _lblLoading = new Label { Text = "Đang tải dữ liệu...", FontSize = 14, TextColor = Color.FromArgb("#8ba0b2"), HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 20) };
        _visitHistoryLayout.Add(_lblLoading);

        historySection.Add(MakeCard(_visitHistoryLayout));
        root.Add(historySection);

        // ── 5. Footer ────────────────────────────────────────────────
        root.Add(new Label
        {
            Text = "Vĩnh Khánh Tour App",
            FontSize = 13,
            TextColor = Color.FromArgb("#8ba0b2"),
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(16, 20, 16, 20)
        });

        scroll.Content = root;
        Content = scroll;
        ApplyLanguage();
    }

    private async Task LoadStatsAsync()
    {
        try
        {
            var currentUser = VinhKhanhTour.Services.UserSession.Instance.Username;
            bool isAuth = VinhKhanhTour.Services.UserSession.Instance.IsAuthenticatedUser;

            // Load visits scoped to current user only
            var visits = isAuth
                ? await App.Database.GetVisitHistoryByUserAsync(currentUser)
                : new System.Collections.Generic.List<VinhKhanhTour.Models.VisitHistory>();

            var restaurants = await App.Database.GetRestaurantsAsync();

            var uniqueIds = visits.Select(v => v.RestaurantId).Distinct().ToList();
            _visitCountLabel.Text = uniqueIds.Count.ToString();
            _pointsLabel.Text = (uniqueIds.Count * 15).ToString();
            _tourCountLabel.Text = (uniqueIds.Count / 2).ToString();

            var recentDistinct = visits.GroupBy(v => v.RestaurantId)
                .Select(g => g.OrderByDescending(v => v.VisitedAt).First())
                .OrderByDescending(v => v.VisitedAt)
                .Take(4).ToList();

            _visitHistoryLayout.Children.Clear();

            if (recentDistinct.Count == 0)
            {
                _visitHistoryLayout.Children.Add(new Label { Text = "Chưa có dấu chân nào.\nHãy khám phá Vĩnh Khánh ngay!", FontSize = 14, LineHeight = 1.4, TextColor = Color.FromArgb("#8ba0b2"), HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(0, 30, 0, 30) });
                return;
            }

            for (int i = 0; i < recentDistinct.Count; i++)
            {
                var visit = recentDistinct[i];
                var r = restaurants.FirstOrDefault(x => x.Id == visit.RestaurantId);
                if (r == null) continue;

                var row = new Grid
                {
                    ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Auto }, new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Auto } },
                    Padding = new Thickness(16, 12),
                    ColumnSpacing = 16
                };

                View thumb;
                if (!string.IsNullOrWhiteSpace(r.ImageUrl))
                    thumb = new Border { StrokeShape = new RoundRectangle { CornerRadius = 12 }, StrokeThickness = 0, WidthRequest = 50, HeightRequest = 50, Content = new Image { Source = r.ImageUrl, Aspect = Aspect.AspectFill } };
                else
                    thumb = new Border { BackgroundColor = Color.FromArgb("#15FFFFFF"), StrokeShape = new RoundRectangle { CornerRadius = 12 }, WidthRequest = 50, HeightRequest = 50, StrokeThickness = 0, Content = new Label { Text = GetCategoryEmoji(r), FontSize = 24, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center } };

                row.Add(thumb, 0, 0);

                var info = new VerticalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center };
                info.Add(new Label { Text = r.Name, FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D2137"), LineBreakMode = LineBreakMode.TailTruncation, MaxLines = 1 });

                var tagRow = new HorizontalStackLayout { Spacing = 8 };
                tagRow.Add(new Border { BackgroundColor = Color.FromArgb("#25FFCA28"), StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 6 }, Padding = new Thickness(6, 3), Content = new Label { Text = $"⭐ {r.Rating}", FontSize = 11, TextColor = Color.FromArgb("#FFCA28"), FontAttributes = FontAttributes.Bold } });
                tagRow.Add(new Label { Text = TimeAgo(visit.VisitedAt), FontSize = 12, TextColor = Color.FromArgb("#8ba0b2"), VerticalOptions = LayoutOptions.Center });

                info.Add(tagRow);
                row.Add(info, 1, 0);

                row.Add(new Label { Text = "\u203A", FontSize = 28, TextColor = Color.FromArgb("#FFCA28"), VerticalOptions = LayoutOptions.Center, Margin = new Thickness(0, 0, 8, 0) }, 2, 0);

                _visitHistoryLayout.Children.Add(row);

                if (i < recentDistinct.Count - 1)
                    _visitHistoryLayout.Children.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#0FFFFFFF"), Margin = new Thickness(20, 0, 20, 0) });
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"ProfilePage stats error: {ex.Message}"); }
    }

    // ── UI Helpers ──────────────────────────────────────────────────────────

    private static Label MakeStatLabel() => new Label { Text = "0", FontSize = 28, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1565C0"), HorizontalOptions = LayoutOptions.Center };

    private static VerticalStackLayout MakeStatNode(Label valueLabel, Label captionLabel, string iconLabel)
    {
        var stack = new VerticalStackLayout { Spacing = 4, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        var top = new HorizontalStackLayout { Spacing = 6, HorizontalOptions = LayoutOptions.Center };
        top.Add(new Label { Text = iconLabel, FontSize = 16, VerticalOptions = LayoutOptions.Center });
        top.Add(valueLabel);
        stack.Add(top);
        stack.Add(captionLabel);
        return stack;
    }

    private static Grid MakeMenuItemWithSub(string icon, Label titleLabel, Label subtitleLabel, Color iconBg)
    {
        var grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Auto }, new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Auto } },
            ColumnSpacing = 16,
            Padding = new Thickness(20, 16)
        };
        var iconWrap = new Border { BackgroundColor = iconBg.WithAlpha(0.15f), StrokeThickness = 0, StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 }, WidthRequest = 44, HeightRequest = 44, Content = new Label { Text = icon, FontSize = 20, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center } };
        grid.Add(iconWrap, 0, 0);
        var s = new VerticalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center };
        s.Add(titleLabel);
        s.Add(subtitleLabel);
        grid.Add(s, 1, 0);
        var rightStack = new HorizontalStackLayout { Spacing = 14, VerticalOptions = LayoutOptions.Center };
        rightStack.Add(new Label { Text = "\u203A", FontSize = 26, TextColor = Color.FromArgb("#FFCA28"), VerticalOptions = LayoutOptions.Center });
        grid.Add(rightStack, 2, 0);
        return grid;
    }

    private static Border MakeCard(View content) => new Border
    {
        BackgroundColor = Color.FromArgb("#FFFFFF"),
        Stroke = Color.FromArgb("#CCE0FF"),
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 24 },
        StrokeThickness = 1,
        Padding = new Thickness(0, 12),
        Shadow = new Shadow { Brush = Color.FromArgb("#1565C0"), Opacity = 0.1f, Radius = 15, Offset = new Point(0, 8) },
        Content = content
    };

    private static Grid MakeMenuItem(string icon, Label titleLabel, Label? badgeLabel, Color iconBg)
    {
        var grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Auto }, new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Auto } },
            ColumnSpacing = 16,
            Padding = new Thickness(20, 16)
        };

        var iconWrap = new Border { BackgroundColor = iconBg.WithAlpha(0.15f), StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 14 }, WidthRequest = 44, HeightRequest = 44, Content = new Label { Text = icon, FontSize = 20, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center } };
        grid.Add(iconWrap, 0, 0);

        grid.Add(titleLabel, 1, 0);

        var rightStack = new HorizontalStackLayout { Spacing = 14, VerticalOptions = LayoutOptions.Center };
        if (badgeLabel != null)
            rightStack.Add(new Border { BackgroundColor = Color.FromArgb("#30E91E63"), StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 8 }, Padding = new Thickness(8, 4), Content = badgeLabel, VerticalOptions = LayoutOptions.Center });

        rightStack.Add(new Label { Text = "\u203A", FontSize = 26, TextColor = Color.FromArgb("#FFCA28"), VerticalOptions = LayoutOptions.Center });
        grid.Add(rightStack, 2, 0);

        return grid;
    }

    private static string GetCategoryEmoji(Restaurant r)
    {
        var n = r.Name.ToLower();
        if (n.Contains("ốc")) return "\U0001F9AA";
        if (n.Contains("bún") || n.Contains("phở")) return "\U0001F35C";
        if (n.Contains("cơm") || n.Contains("xôi")) return "\U0001F35A";
        if (n.Contains("lẩu") || n.Contains("nướng") || n.Contains("bò")) return "\U0001F969";
        return "\U0001F958";
    }

    private static string TimeAgo(DateTime dt)
    {
        var d = DateTime.Now - dt;
        if (d.TotalMinutes < 1) return "Vừa truy cập";
        if (d.TotalHours < 1) return $"{(int)d.TotalMinutes} phút trước";
        if (d.TotalDays < 1) return $"{(int)d.TotalHours} giờ trước";
        if (d.TotalDays < 7) return $"{(int)d.TotalDays} ngày trước";
        return dt.ToString("dd/MM/yyyy");
    }
}
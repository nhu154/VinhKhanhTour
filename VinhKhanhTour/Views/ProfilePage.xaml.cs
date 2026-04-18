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
    private Label _lblTicket = null!;
    private Label _lblJournal = null!;
    private Label _lblBookings = null!;

    public ProfilePage()
    {
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

        // Dynamic labels with fallback logic
        if (_lblPageTitle != null) _lblPageTitle.Text = lang switch { "en" => "My Profile", "zh" => "我的主页", "ja" => "マイプロフィール", "ko" => "내 프로필", _ => "Hồ sơ của tôi" };
        if (_lblStatVisit != null) _lblStatVisit.Text = lang switch { "en" => "Visited", "zh" => "去过", "ja" => "訪問済み", "ko" => "방문함", _ => "Quán đã ghé" };
        if (_lblStatBadge != null) _lblStatBadge.Text = lang switch { "en" => "Badges", "zh" => "徽章", "ja" => "バッジ", "ko" => "배지", _ => "Huy hiệu" };
        if (_lblStatPoints != null) _lblStatPoints.Text = lang switch { "en" => "Points", "zh" => "积分", "ja" => "ポイント", "ko" => "포인트", _ => "Điểm thưởng" };
        if (_lblMenuSection != null) _lblMenuSection.Text = lang switch { "en" => "Account", "zh" => "账户", "ja" => "アカウント", "ko" => "계정", _ => "Tài khoản" };

        if (_lblHistorySection != null) _lblHistorySection.Text = lang switch { "en" => "Recent Activity", "zh" => "最近活动", "ja" => "最近の活動", "ko" => "최근 활동", _ => "Hoạt động gần đây" };

        if (_lblFav != null) _lblFav.Text = lang switch { "en" => "Favorite Spots", "zh" => "收藏的餐厅", "ja" => "お気に入り", "ko" => "즐겨찾기", _ => "Quán yêu thích" };
        if (_lblOffers != null) _lblOffers.Text = lang switch { "en" => "My Offers", "zh" => "我的优惠", "ja" => "オファー", "ko" => "내 제안", _ => "Ưu đãi của tôi" };
        if (_lblBadgeMoi != null) _lblBadgeMoi.Text = lang switch { "en" => "New", "zh" => "新", "ja" => "新着", "ko" => "새로운", _ => "Mới" };
        if (_lblLang != null) _lblLang.Text = lang switch { "en" => "System Language", "zh" => "系统语言", "ja" => "システム言語", "ko" => "시스템 언어", _ => "Ngôn ngữ hệ thống" };
        if (_lblStats != null) _lblStats.Text = lang switch { "en" => "Analytics", "zh" => "统计分析", "ja" => "統計", "ko" => "통계 분석", _ => "Thống kê phân tích" };
        if (_lblSettings != null) _lblSettings.Text = lang switch { "en" => "Logout", "zh" => "登出", "ja" => "ログアウト", "ko" => "로그아웃", _ => "Đăng xuất" };
        if (_lblTicket != null) _lblTicket.Text = lang switch { "en" => "My Ticket", "zh" => "我的票", "ja" => "チケット", "ko" => "내 티켓", _ => "Vé của tôi" };
        if (_lblJournal != null) _lblJournal.Text = lang switch { "en" => "Food Journal", "zh" => "美食日记", "ja" => "食日記", "ko" => "푸드 저널", _ => "Nhật ký ẩm thực" };
        if (_lblBookings != null) _lblBookings.Text = lang switch { "en" => "My Bookings", "zh" => "我的预约", "ja" => "予約履歴", "ko" => "내 예약", _ => "Đặt chỗ của tôi" };

        var _session = VinhKhanhTour.Services.UserSession.Instance;
        if (!_session.IsAuthenticatedUser)
        {
            if (_lblUserName != null) _lblUserName.Text = lang switch { "en" => "Vinh Khanh Tourist", "ja" => "ヴィンカン観光客", "ko" => "빈칸 관광객", _ => "Du khách Vĩnh Khánh" };
            if (_lblUserRole != null) _lblUserRole.Text = lang switch { "en" => "✨ Guest Explorer", "ja" => "✨ ゲストエクスプローラー", "ko" => "✨ 게스트 탐험가", _ => "Khách tham quan" };
        }

        if (_lblEditBtn != null) _lblEditBtn.Text = lang switch { "en" => "✏️ Edit", "ja" => "✏️ 編集", "ko" => "✏️ 편집", _ => "Sửa" };
        if (_lblLoading != null && _lblLoading.Text != null && _lblLoading.Text.EndsWith("..."))
            _lblLoading.Text = lang switch { "en" => "Loading data...", "zh" => "加载数据...", "ja" => "読み込み中...", "ko" => "데이터 로딩 중...", _ => "Đang tải dữ liệu..." };
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

        _lblLangSub = new Label { FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#64B5F6") };
        _lblLang = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D2137") };
        menuItems.Add(MakeMenuItemWithSub("\U0001F310", _lblLang, _lblLangSub, Color.FromArgb("#42A5F5")));
        menuItems.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#0FFFFFFF"), Margin = new Thickness(80, 0, 20, 0) });

        _lblStats = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D2137"), VerticalOptions = LayoutOptions.Center };
        var statsItem = MakeMenuItem("\U0001F4CA", _lblStats, null, Color.FromArgb("#00BFA5"));
        statsItem.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => {
                if (!VinhKhanhTour.Services.UserSession.Instance.IsAuthenticatedUser)
                {
                    ShowAuthRequiredPopup();
                }
                else
                {
                    Navigation.PushModalAsync(new AnalyticsPage());
                }
            })
        });
        menuItems.Add(statsItem);
        menuItems.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#0FFFFFFF"), Margin = new Thickness(80, 0, 20, 0) });

        _lblSettings = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#FF5252"), VerticalOptions = LayoutOptions.Center };
        var logoutItem = MakeMenuItem("\U0001F6AA", _lblSettings, null, Color.FromArgb("#FF5252"));
        logoutItem.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => {
            VinhKhanhTour.Services.UserSession.Instance.Logout();
            Application.Current!.MainPage = new NavigationPage(new LoginPage());
        }) });
        menuItems.Add(logoutItem);

        // ── Vé của tôi ────────────────────────────────────────────────────
        menuItems.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#0FFFFFFF"), Margin = new Thickness(80, 0, 20, 0) });
        _lblTicket = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D2137"), VerticalOptions = LayoutOptions.Center };
        var ticketBadgeLabel = VinhKhanhTour.Services.TicketService.Instance.HasValidTicket
            ? new Label { Text = "✅ Đang dùng", FontSize = 11, TextColor = Color.FromArgb("#2E7D32"), FontAttributes = FontAttributes.Bold }
            : new Label { Text = "HOT 🔥", FontSize = 11, TextColor = Color.FromArgb("#FF6F00"), FontAttributes = FontAttributes.Bold };
        var ticketItem = MakeMenuItem("🎫", _lblTicket, ticketBadgeLabel, Color.FromArgb("#FF6F00"));
        ticketItem.GestureRecognizers.Add(new TapGestureRecognizer
        { Command = new Command(async () => await Navigation.PushAsync(new MyTicketPage())) });
        menuItems.Add(ticketItem);

        // ── Đặt chỗ của tôi ───────────────────────────────────────────────
        menuItems.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#0FFFFFFF"), Margin = new Thickness(80, 0, 20, 0) });
        _lblBookings = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D2137"), VerticalOptions = LayoutOptions.Center };
        var bookingItem = MakeMenuItem("📋", _lblBookings, null, Color.FromArgb("#1565C0"));
        bookingItem.GestureRecognizers.Add(new TapGestureRecognizer
        { Command = new Command(async () => await Navigation.PushAsync(new BookingHistoryPage())) });
        menuItems.Add(bookingItem);

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

        // Overlay Grid for Custom Popups
        var mainGrid = new Grid();
        mainGrid.Children.Add(scroll);

        // Popup Overlay (Hidden initially)
        _popupOverlay = new Grid
        {
            BackgroundColor = Color.FromArgb("#AA000000"),
            IsVisible = false,
            Opacity = 0
        };
        var dismissTap = new TapGestureRecognizer();
        dismissTap.Tapped += (s, e) => HidePopup();
        _popupOverlay.GestureRecognizers.Add(dismissTap);

        mainGrid.Children.Add(_popupOverlay);

        Content = mainGrid;
        ApplyLanguage();
    }

    private Grid _popupOverlay = null!;

    private void ShowAuthRequiredPopup()
    {
        var lang = _currentLang;
        _popupOverlay.Children.Clear();

        var card = new Border
        {
            BackgroundColor = Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 30 },
            WidthRequest = 320,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Padding = new Thickness(24, 32),
            Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.3f, Radius = 20 }
        };

        var stack = new VerticalStackLayout { Spacing = 20, HorizontalOptions = LayoutOptions.Center };

        stack.Add(new Label { Text = "🔐", FontSize = 48, HorizontalOptions = LayoutOptions.Center });

        stack.Add(new Label
        {
            Text = lang switch { "en" => "Do you have an account?", "zh" => "你已经有账号了吗？", "ja" => "アカウントをお持ちですか？", "ko" => "계정이 있으신가요?", _ => "Bạn đã có tài khoản chưa?" },
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#0D2137"),
            HorizontalTextAlignment = TextAlignment.Center
        });

        stack.Add(new Label
        {
            Text = lang switch { "en" => "Journey tracking is reserved for Vinh Khanh Tour members to record your experience history.", "zh" => "行程统计功能仅限永庆旅游会员，用于记录您的体验历史。", "ja" => "ジャーニートラッキングは、体験履歴を記録するためにヴィンカンツアーメンバー専用となっています。", "ko" => "여정 추적은 귀하의 경험 기록을 위해 빈칸 투어 회원 전용입니다.", _ => "Tính năng thống kê hành trình dành riêng cho thành viên Vĩnh Khánh Tour để ghi lại lịch sử trải nghiệm của bạn." },
            FontSize = 14,
            TextColor = Color.FromArgb("#64748B"),
            HorizontalTextAlignment = TextAlignment.Center,
            LineHeight = 1.4
        });

        var btnLogin = new Button
        {
            Text = lang switch { "en" => "Login Now", "zh" => "立即登录", "ja" => "今すぐログイン", "ko" => "지금 로그인", _ => "Đã có (Đăng nhập)" },
            BackgroundColor = Color.FromArgb("#1565C0"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 15,
            HeightRequest = 50
        };
        btnLogin.Clicked += (s, e) => { HidePopup(); Application.Current!.MainPage = new NavigationPage(new LoginPage()); };

        var btnReg = new Button
        {
            Text = lang switch { "en" => "Register", "zh" => "注册", "ja" => "登録", "ko" => "등록", _ => "Chưa có (Đăng ký)" },
            BackgroundColor = Color.FromArgb("#E3F2FD"),
            TextColor = Color.FromArgb("#1565C0"),
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 15,
            HeightRequest = 50
        };
        btnReg.Clicked += async (s, e) => { HidePopup(); await Navigation.PushAsync(new RegisterPage()); };

        var btnCancel = new Label
        {
            Text = lang switch { "en" => "Skip", "zh" => "跳过", "ja" => "スキップ", "ko" => "건너뛰기", _ => "Bỏ qua" },
            FontSize = 13,
            TextColor = Color.FromArgb("#94A3B8"),
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        btnCancel.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => HidePopup()) });

        stack.Add(btnLogin);
        stack.Add(btnReg);
        stack.Add(btnCancel);

        card.Content = stack;
        _popupOverlay.Children.Add(card);

        _popupOverlay.IsVisible = true;
        _popupOverlay.FadeTo(1, 200);
    }

    private async void HidePopup()
    {
        await _popupOverlay.FadeTo(0, 150);
        _popupOverlay.IsVisible = false;
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

            // Lấy điểm thưởng và huy hiệu thật từ TicketService
            var ts = VinhKhanhTour.Services.TicketService.Instance;
            _pointsLabel.Text = ts.Points.ToString();
            _tourCountLabel.Text = ts.GetUnlockedBadges().Count.ToString();

            var recentDistinct = visits.GroupBy(v => v.RestaurantId)
                .Select(g => g.OrderByDescending(v => v.VisitedAt).First())
                .OrderByDescending(v => v.VisitedAt)
                .Take(4).ToList();

            _visitHistoryLayout.Children.Clear();

            if (recentDistinct.Count == 0)
            {
                var emptyMsg = _currentLang switch
                {
                    "en" => "No tracks yet.\nExplore Vinh Khanh now!",
                    "zh" => "还没有足迹。\n现在就开始探索永庆吧！",
                    "ja" => "まだ足跡がありません。\n今すぐヴィンカンを探索しましょう！",
                    "ko" => "아직 흔적이 없습니다.\n지금 바로 빈칸을 탐험해보세요!",
                    _ => "Chưa có dấu chân nào.\nHãy khám phá Vĩnh Khánh ngay!"
                };
                _visitHistoryLayout.Children.Add(new Label { Text = emptyMsg, FontSize = 14, LineHeight = 1.4, TextColor = Color.FromArgb("#8ba0b2"), HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(0, 30, 0, 30) });
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

    private string TimeAgo(DateTime dt)
    {
        var d = DateTime.Now - dt;
        return _currentLang switch
        {
            "en" => d.TotalMinutes < 1 ? "Just now" : (d.TotalHours < 1 ? $"{(int)d.TotalMinutes}m ago" : (d.TotalDays < 1 ? $"{(int)d.TotalHours}h ago" : $"{(int)d.TotalDays}d ago")),
            "zh" => d.TotalMinutes < 1 ? "刚刚" : (d.TotalHours < 1 ? $"{(int)d.TotalMinutes}分钟前" : (d.TotalDays < 1 ? $"{(int)d.TotalHours}小时前" : $"{(int)d.TotalDays}天前")),
            "ja" => d.TotalMinutes < 1 ? "たった今" : (d.TotalHours < 1 ? $"{(int)d.TotalMinutes}分前" : (d.TotalDays < 1 ? $"{(int)d.TotalHours}時間前" : $"{(int)d.TotalDays}日前")),
            "ko" => d.TotalMinutes < 1 ? "방금 전" : (d.TotalHours < 1 ? $"{(int)d.TotalMinutes}분 전" : (d.TotalDays < 1 ? $"{(int)d.TotalHours}시간 전" : $"{(int)d.TotalDays}일 전")),
            _ => d.TotalMinutes < 1 ? "Vừa truy cập" : (d.TotalHours < 1 ? $"{(int)d.TotalMinutes} phút trước" : (d.TotalDays < 1 ? $"{(int)d.TotalHours} giờ trước" : (d.TotalDays < 7 ? $"{(int)d.TotalDays} ngày trước" : dt.ToString("dd/MM/yyyy"))))
        };
    }
}
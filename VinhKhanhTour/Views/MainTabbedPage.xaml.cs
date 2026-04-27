namespace VinhKhanhTour.Views
{
    public partial class MainTabbedPage : TabbedPage
    {
        public MainTabbedPage()
        {
            InitializeComponent();
            Services.DeepLinkService.Instance.OnDeepLinkReceived += HandlePoiDeepLink;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateLanguage(Preferences.Default.Get("app_lang", "vi"));
            Services.DeepLinkService.Instance.FlushPending();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Services.DeepLinkService.Instance.OnDeepLinkReceived -= HandlePoiDeepLink;
        }

        private async void HandlePoiDeepLink(int poiId, bool autoplay)
        {
            try
            {
                var restaurant = await App.Database.GetRestaurantByIdAsync(poiId);
                if (restaurant == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DeepLink] ❌ POI {poiId} not found");
                    return;
                }

                // Chuyển sang tab Bản đồ (thường là index 1)
                SelectedItem = Children[1];

                if (Children[1] is NavigationPage navMap)
                {
                    // Đảm bảo quay về root của tab bản đồ nếu đang ở trang con nào đó
                    if (navMap.Navigation.NavigationStack.Count > 1)
                    {
                        await navMap.PopToRootAsync(false);
                    }

                    if (navMap.CurrentPage is MapPage mapPage)
                    {
                        mapPage.FocusAndDirect(restaurant, autoplay);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[DeepLink] ✅ Navigated to Map Tab for {restaurant.Name}  autoplay={autoplay}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeepLink] Nav error: {ex.Message}");
            }
        }

        public void UpdateLanguage(string lang)
        {
            // ── Cập nhật title tab theo ngôn ngữ ──────────────────────────────
            switch (lang)
            {
                case "en":
                    Children[0].Title = "Home";
                    Children[1].Title = "Map";
                    Children[2].Title = "Profile";
                    break;
                case "zh":
                    Children[0].Title = "首页";
                    Children[1].Title = "地图";
                    Children[2].Title = "我的";
                    break;
                case "ja":
                    Children[0].Title = "ホーム";
                    Children[1].Title = "地図";
                    Children[2].Title = "プロフィール";
                    break;
                case "ko":
                    Children[0].Title = "홈";
                    Children[1].Title = "지도";
                    Children[2].Title = "프로필";
                    break;
                case "fr":
                    Children[0].Title = "Accueil";
                    Children[1].Title = "Carte";
                    Children[2].Title = "Profil";
                    break;
                default:
                    Children[0].Title = "Trang chủ";
                    Children[1].Title = "Bản đồ";
                    Children[2].Title = "Cá nhân";
                    break;
            }

            // ── FIX: Duyệt TẤT CẢ tab, notify cả WelcomePage + MapPage + ProfilePage ──
            foreach (var child in Children)
            {
                if (child is not NavigationPage navPage) continue;

                // Duyệt toàn bộ navigation stack của tab, không chỉ CurrentPage
                foreach (var page in navPage.Navigation.NavigationStack)
                {
                    if (page is WelcomePage welcome)
                        welcome.UpdateLanguage(lang);
                    else if (page is MapPage map)
                        map.UpdateLanguage(lang);
                    else if (page is ProfilePage profile)
                        profile.UpdateLanguage(lang);
                }
            }
        }

        protected override async void OnCurrentPageChanged()
        {
            base.OnCurrentPageChanged();
            // Reset navigation stack for the newly selected tab, resolving MAUI inactive tab bugs
            if (CurrentPage is NavigationPage navPage && navPage.Navigation.NavigationStack.Count > 1)
            {
                await navPage.PopToRootAsync(false);
            }
        }
    }
}

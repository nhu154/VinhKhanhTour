namespace VinhKhanhTour.Views
{
    public partial class MainTabbedPage : TabbedPage
    {
        public MainTabbedPage()
        {
            InitializeComponent();
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

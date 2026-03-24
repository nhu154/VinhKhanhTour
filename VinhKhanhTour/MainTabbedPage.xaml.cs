namespace VinhKhanhTour
{
    public partial class MainTabbedPage : TabbedPage
    {
        public MainTabbedPage()
        {
            InitializeComponent();
        }

        public void UpdateLanguage(string lang)
        {
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
                default:
                    Children[0].Title = "Trang chủ";
                    Children[1].Title = "Bản đồ";
                    Children[2].Title = "Cá nhân";
                    break;
            }
            foreach (var child in Children)
            {
                if (child is NavigationPage navPage)
                {
                    if (navPage.CurrentPage is MapPage map)
                    {
                        map.UpdateLanguage(lang);
                    }
                    if (navPage.CurrentPage is ProfilePage profile)
                    {
                        profile.UpdateLanguage(lang);
                    }
                    if (navPage.CurrentPage is WelcomePage welcome)
                    {
                        welcome.UpdateLanguage(lang);
                    }
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

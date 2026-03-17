using VinhKhanhTour.Models;

namespace VinhKhanhTour
{
    public partial class WelcomePage : ContentPage
    {
        public WelcomePage()
        {
            InitializeComponent();
        }

        private void OnMapClicked(object sender, EventArgs e)
        {
            // ✅ Fix: kiểm tra null trước khi cast
            if (Application.Current?.MainPage is TabbedPage tabbedPage)
            {
                tabbedPage.CurrentPage = tabbedPage.Children[1];
            }
        }

        private void OnTour1Clicked(object sender, EventArgs e)
        {
            var tour = new Tour
            {
                Id = "1",
                Name = "Tour Ăn Ốc",
                Description = "3 quán ốc ngon nổi tiếng",
                Duration = "45 phút",
                Rating = 4.4,
                RestaurantIds = new List<int> { 1, 2, 3 }
            };
            Navigation.PushAsync(new TourDetailPage(tour));
        }

        private void OnTour2Clicked(object sender, EventArgs e)
        {
            var tour = new Tour
            {
                Id = "2",
                Name = "Tour Ăn Nướng",
                Description = "Lẩu nướng, bò lá lốt",
                Duration = "60 phút",
                Rating = 4.5,
                RestaurantIds = new List<int> { 7, 8, 10 }
            };
            Navigation.PushAsync(new TourDetailPage(tour));
        }

        private void OnTour3Clicked(object sender, EventArgs e)
        {
            var tour = new Tour
            {
                Id = "3",
                Name = "Tour Ăn Vặt",
                Description = "Cơm cháy, bún thịt nướng",
                Duration = "40 phút",
                Rating = 4.3,
                RestaurantIds = new List<int> { 9, 11 }
            };
            Navigation.PushAsync(new TourDetailPage(tour));
        }

        private void OnTour4Clicked(object sender, EventArgs e)
        {
            var tour = new Tour
            {
                Id = "4",
                Name = "Tour Đặc Sản",
                Description = "Bún cá Châu Đốc",
                Duration = "50 phút",
                Rating = 4.6,
                RestaurantIds = new List<int> { 4, 5, 6 }
            };
            Navigation.PushAsync(new TourDetailPage(tour));
        }
    }
}
using VinhKhanhTour.Models;
using VinhKhanhTour.Services;

namespace VinhKhanhTour.Views
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            LoadRestaurants();
        }

        private async void LoadRestaurants()
        {
            try
            {
                // Th? load t? API tru?c
                var apiList = await ApiService.Instance.GetRestaurantsAsync();
                if (apiList.Count > 0)
                {
                    RestaurantsCollection.ItemsSource = apiList;
                    return;
                }
            }
            catch { }

            // Fallback: dùng SQLite local
            try
            {
                await Task.Delay(300);
                var restaurants = await App.Database.GetRestaurantsAsync();
                if (restaurants.Count > 0)
                    RestaurantsCollection.ItemsSource = restaurants;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] Load error: {ex.Message}");
            }
        }

        private async void OnRestaurantSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Restaurant restaurant)
            {
                await DisplayAlert(
                    restaurant.Name,
                    $"{restaurant.Description}\n\n" +
                    $"?? {restaurant.Address}\n" +
                    $"? Rating: {restaurant.Rating}\n" +
                    $"?? {restaurant.OpenHours}\n\n" +
                    $"?? T?a d?: {restaurant.Latitude}, {restaurant.Longitude}",
                    "OK"
                );
                ((CollectionView)sender).SelectedItem = null;
            }
        }

        private async void OnMapClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MapPage());
        }
    }
}
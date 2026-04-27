using Microsoft.Maui.Controls.Shapes;
using VinhKhanhTour.Models;
using VinhKhanhTour.Services;

namespace VinhKhanhTour.Views
{
    public class TourDetailPage : ContentPage
    {
        private Tour _tour;
        private string _currentLang = Preferences.Default.Get("app_lang", "vi");
        private Label _lblBack = null!;
        private Label _lblTourName = null!;
        private Label _lblTourDesc = null!;
        private Label _lblDuration = null!;
        private Label _lblSpotsCount = null!;
        private Label _lblSectionTitle = null!;
        private Label _lblStartTour = null!;

        public TourDetailPage(Tour tour)
        {
            _tour = tour;
            Title = tour.Name;
            NavigationPage.SetHasNavigationBar(this, false);
            BackgroundColor = Color.FromArgb("#F0F6FF");
            CreateUI();
            UpdateLanguage(_currentLang);
        }

        public void UpdateLanguage(string lang)
        {
            _currentLang = lang;
            if (_lblBack != null) _lblBack.Text = lang switch { "en" => "← Back", "zh" => "← 返回", "ja" => "← 戻る", "ko" => "← 뒤로", _ => "← Trở về" };
            if (_lblSectionTitle != null) _lblSectionTitle.Text = lang switch { "en" => "Food Journey", "zh" => "美食之旅", "ja" => "美食の旅", "ko" => "음식 여정", _ => "Hành trình ẩm thực" };
            if (_lblStartTour != null) _lblStartTour.Text = lang switch { "en" => "Start GPS Navigation", "zh" => "开始导航", "ja" => "ナビを開始", "ko" => "네비게이션 시작", _ => "Bắt đầu dẫn đường" };

            // Re-render tour-specific info if labels are available
            if (_lblTourName != null) _lblTourName.Text = _tour.Name;
            if (_lblTourDesc != null) _lblTourDesc.Text = _tour.Description;
        }

        private async void CreateUI()
        {
            // Load từ API trước để đảm bảo đúng ID với MySQL
            List<Restaurant> allRestaurants;
            try
            {
                allRestaurants = await ApiService.Instance.GetRestaurantsAsync();
                if (allRestaurants.Count == 0)
                    allRestaurants = await App.Database.GetRestaurantsAsync();
            }
            catch
            {
                allRestaurants = await App.Database.GetRestaurantsAsync();
            }

            var tourRestaurants = allRestaurants
                .Where(r => _tour.RestaurantIds.Contains(r.Id))
                .ToList();

            var mainLayout = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = GridLength.Auto }
                }
            };

            var scrollView = new ScrollView { VerticalScrollBarVisibility = ScrollBarVisibility.Never };
            var contentLayout = new VerticalStackLayout { Spacing = 0 };

            // 1. Premium Header (Hero Image + Gradient + Content)
            string coverImg = _tour.Id switch
            {
                "1" => "tour_oc.jpg",
                "2" => "tour_nuong.jpg",
                "3" => "tour_vat.jpg",
                "4" => "tour_dacssan.jpg",
                _ => "tour_oc.jpg"
            };

            var headerGrid = new Grid { HeightRequest = 320 };
            headerGrid.Add(new Image
            {
                Source = coverImg,
                Aspect = Aspect.AspectFill,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            });

            headerGrid.Add(new BoxView
            {
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb("#001565C0"), 0.2f),
                        new GradientStop(Color.FromArgb("#CC0D2137"), 1.0f)
                    }
                }
            });

            // Back button
            var backBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#40000000"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Padding = new Thickness(15, 8),
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(20, 50, 0, 0)
            };
            _lblBack = new Label { Text = "← Trở về", TextColor = Colors.White, FontAttributes = FontAttributes.Bold };
            backBtn.Content = _lblBack;
            backBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });
            headerGrid.Add(backBtn);

            var headerContent = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.End,
                Padding = new Thickness(24, 0, 24, 24),
                Spacing = 8
            };

            var tagLine = new HorizontalStackLayout { Spacing = 8 };
            tagLine.Add(new Border
            {
                BackgroundColor = Color.FromArgb("#1565C0"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                Padding = new Thickness(8, 4),
                Content = new Label { Text = "TOUR", TextColor = Colors.White, FontSize = 10, FontAttributes = FontAttributes.Bold }
            });
            tagLine.Add(new Label { Text = $"⭐ {_tour.Rating}", TextColor = Color.FromArgb("#64B5F6"), FontSize = 12, FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center });

            headerContent.Add(tagLine);

            _lblTourName = new Label
            {
                Text = _tour.Name,
                FontSize = 28,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                CharacterSpacing = 1
            };
            headerContent.Add(_lblTourName);
            _lblTourDesc = new Label
            {
                Text = _tour.Description,
                FontSize = 14,
                TextColor = Color.FromArgb("#FFFFFF99"),
                LineHeight = 1.4
            };
            headerContent.Add(_lblTourDesc);

            var statsRow = new HorizontalStackLayout { Spacing = 20, Margin = new Thickness(0, 12, 0, 0) };
            _lblDuration = new Label { Text = _tour.Duration, FontSize = 12, TextColor = Color.FromArgb("#5A7A9A"), FontAttributes = FontAttributes.Bold };
            statsRow.Add(CreateStatNode("⏱", _lblDuration));

            _lblSpotsCount = new Label { Text = $"{tourRestaurants.Count} địa điểm", FontSize = 12, TextColor = Color.FromArgb("#5A7A9A"), FontAttributes = FontAttributes.Bold };
            statsRow.Add(CreateStatNode("📍", _lblSpotsCount));
            headerContent.Add(statsRow);

            headerGrid.Add(headerContent);
            contentLayout.Add(headerGrid);

            // 2. Restaurant List Area
            var listLayout = new VerticalStackLayout { Padding = new Thickness(20, 10, 20, 100), Spacing = 16 };
            _lblSectionTitle = new Label
            {
                Text = "Hành trình ẩm thực",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#0D2137"),
                Margin = new Thickness(0, 0, 0, 5)
            };
            listLayout.Add(_lblSectionTitle);

            foreach (var r in tourRestaurants)
                listLayout.Add(CreateRestaurantCard(r));

            contentLayout.Add(listLayout);
            scrollView.Content = contentLayout;
            mainLayout.Add(scrollView, 0, 0);

            // 3. Floating Bottom Bar
            var ctaContainer = new Border
            {
                BackgroundColor = Color.FromArgb("#FFFFFF"),
                StrokeThickness = 0,
                Padding = new Thickness(20, 16, 20, 32),
                VerticalOptions = LayoutOptions.End
            };

            var btnBorder = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                StrokeThickness = 0,
                HeightRequest = 56,
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb("#1565C0"), 0),
                        new GradientStop(Color.FromArgb("#42A5F5"), 1)
                    }
                },
                Shadow = new Shadow { Brush = Color.FromArgb("#1565C0"), Opacity = 0.5f, Radius = 15, Offset = new Point(0, 6) }
            };

            var btnGrid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, Padding = new Thickness(24, 0) };

            var btnLabelStack = new HorizontalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };
            btnLabelStack.Add(new Microsoft.Maui.Controls.Shapes.Path { Data = (Geometry)new PathGeometryConverter().ConvertFromInvariantString("M3 11 L22 2 L13 21 L11 13 Z"), Stroke = Colors.White, StrokeThickness = 2, Fill = new SolidColorBrush(Colors.Transparent), WidthRequest = 18, HeightRequest = 18, VerticalOptions = LayoutOptions.Center });
            _lblStartTour = new Label { Text = "Bắt đầu dẫn đường", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center };
            btnLabelStack.Add(_lblStartTour);

            btnGrid.Add(btnLabelStack, 0, 0);
            btnGrid.Add(new Label { Text = "→", FontSize = 22, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center }, 1, 0);

            btnBorder.Content = btnGrid;
            btnBorder.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await StartTour(tourRestaurants)) });

            ctaContainer.Content = btnBorder;
            mainLayout.Add(ctaContainer, 0, 1);

            Content = mainLayout;
            
            // Apply language after UI is built
            UpdateLanguage(_currentLang);
        }

        private VerticalStackLayout CreateStatNode(string icon, View textEl)
        {
            var stack = new VerticalStackLayout { Spacing = 2 };
            stack.Add(new Label { Text = icon, FontSize = 18 });
            stack.Add(textEl);
            return stack;
        }

        private async Task StartTour(List<Restaurant> restaurants)
        {
            try
            {
                // Hỗ trợ cả 2 trường hợp:
                // 1. MainPage là NavigationPage wrapping MainTabbedPage
                // 2. MainPage là TabbedPage trực tiếp
                TabbedPage? tabbedPage = null;

                if (Application.Current?.MainPage is NavigationPage navWrapper)
                {
                    tabbedPage = navWrapper.CurrentPage as TabbedPage
                              ?? navWrapper.RootPage as TabbedPage;
                }
                else if (Application.Current?.MainPage is TabbedPage direct)
                {
                    tabbedPage = direct;
                }

                if (tabbedPage == null)
                {
                    // Fallback: mở MapPage độc lập với tour
                    await Navigation.PushAsync(new MapPage(restaurants, _tour.Name));
                    return;
                }

                MapPage? mapTab = null;
                NavigationPage? mapNavPage = null;

                foreach (var child in tabbedPage.Children)
                {
                    if (child is NavigationPage nav)
                    {
                        var found = nav.RootPage as MapPage ?? nav.CurrentPage as MapPage;
                        if (found != null) { mapTab = found; mapNavPage = nav; break; }
                    }
                    else if (child is MapPage dm) { mapTab = dm; break; }
                }

                if (mapTab != null)
                {
                    mapTab.LoadTourPois(restaurants, _tour.Name);
                    tabbedPage.CurrentPage = mapNavPage ?? (Page)mapTab;
                    await Navigation.PopAsync();
                }
                else
                {
                    // Không tìm thấy MapPage trong tab → mở mới
                    await Navigation.PushAsync(new MapPage(restaurants, _tour.Name));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TourDetail] StartTour error: {ex.Message}");
                await DisplayAlert("Lỗi", "Không thể bắt đầu dẫn đường. Vui lòng thử lại.", "OK");
            }
        }

        private Border CreateRestaurantCard(Restaurant restaurant)
        {
            var border = new Border
            {
                BackgroundColor = Color.FromArgb("#FFFFFF"),
                Stroke = Color.FromArgb("#CCE0FF"),
                StrokeThickness = 1,
                Padding = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Shadow = new Shadow { Brush = Color.FromArgb("#000000"), Opacity = 0.3f, Radius = 10, Offset = new Point(0, 5) }
            };

            var grid = new Grid
            {
                RowDefinitions = { new RowDefinition { Height = 170 }, new RowDefinition { Height = GridLength.Auto } }
            };

            var imageGrid = new Grid();

            if (!string.IsNullOrWhiteSpace(restaurant.ImageUrl))
            {
                imageGrid.Add(new Image
                {
                    Source = restaurant.ImageUrl,
                    Aspect = Aspect.AspectFill,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Fill
                });
            }
            else
            {
                imageGrid.Add(new Label
                {
                    Text = "🍜",
                    FontSize = 64,
                    Opacity = 0.1,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    TextColor = Colors.White
                });
            }

            // Overlay Fade bottom
            imageGrid.Add(new BoxView
            {
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1),
                    GradientStops = new GradientStopCollection { new GradientStop(Colors.Transparent, 0.5f), new GradientStop(Color.FromArgb("#0D2137CC"), 1f) }
                }
            });

            // Favorite Button on top left
            var favBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#B30F1E2E"),
                StrokeThickness = 0,
                Padding = new Thickness(10, 8),
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start,
                Margin = 12,
                StrokeShape = new RoundRectangle { CornerRadius = 16 }
            };
            var favLabel = new Label
            {
                Text = restaurant.IsFavorite ? "❤️" : "🤍",
                FontSize = 14,
                VerticalOptions = LayoutOptions.Center
            };
            favBtn.Content = favLabel;

            var tgr = new TapGestureRecognizer();
            tgr.Tapped += async (s, e) =>
            {
                restaurant.IsFavorite = !restaurant.IsFavorite;
                favLabel.Text = restaurant.IsFavorite ? "❤️" : "🤍";
                await App.Database.UpdateRestaurantAsync(restaurant);
            };
            favBtn.GestureRecognizers.Add(tgr);
            imageGrid.Add(favBtn);

            // Rating badge on top right
            var ratingBadge = new Border
            {
                BackgroundColor = Color.FromArgb("#CC1565C0"),
                StrokeThickness = 0,
                Padding = new Thickness(10, 6),
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                Margin = 12,
                StrokeShape = new RoundRectangle { CornerRadius = 12 }
            };
            var ratingStack = new HorizontalStackLayout { Spacing = 4 };
            ratingStack.Add(new Label { Text = "★", FontSize = 12, TextColor = Color.FromArgb("#BBDEFB"), VerticalOptions = LayoutOptions.Center });
            ratingStack.Add(new Label { Text = restaurant.Rating.ToString("0.0"), FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center });
            ratingBadge.Content = ratingStack;
            imageGrid.Add(ratingBadge);

            grid.Add(imageGrid, 0, 0);

            // Info text area
            var info = new VerticalStackLayout { Padding = new Thickness(16, 12, 16, 16), Spacing = 8 };
            info.Add(new Label { Text = restaurant.Name, FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D2137") });
            info.Add(new Label { Text = restaurant.Description, FontSize = 13, TextColor = Color.FromArgb("#5A7A9A"), LineBreakMode = LineBreakMode.TailTruncation, MaxLines = 2 });

            var timeStack = new HorizontalStackLayout { Spacing = 6 };
            timeStack.Add(new Border { BackgroundColor = Color.FromArgb("#1565C030"), StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 6 }, Padding = new Thickness(6, 3), Content = new Label { Text = "🕐", FontSize = 10 } });
            timeStack.Add(new Label { Text = restaurant.OpenHours, FontSize = 12, TextColor = Color.FromArgb("#64B5F6"), FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center });
            info.Add(timeStack);

            grid.Add(info, 0, 1);
            border.Content = grid;

            // Navigation to Detail Page
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (s, e) =>
            {
                await Navigation.PushAsync(new RestaurantDetailPage(restaurant));
            };
            border.GestureRecognizers.Add(tapGesture);

            return border;
        }
    }
}
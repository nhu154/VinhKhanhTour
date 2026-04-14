using VinhKhanhTour.Services;
using Microsoft.Maui.Controls.Shapes;

namespace VinhKhanhTour.Views
{
    public class LoginPage : ContentPage
    {
        private Entry _usernameEntry = null!;
        private Entry _passwordEntry = null!;
        private Label _errorLabel = null!;
        private Button _btnLogin = null!;

        public LoginPage()
        {
            NavigationPage.SetHasNavigationBar(this, false);
            BackgroundColor = Color.FromArgb("#121212");
            
            // Xóa session cũ tránh trường hợp audio nền tự động phát từ session trước
            UserSession.Instance.Logout();
            
            CreateUI();
        }

        private void CreateUI()
        {
            var rootGrid = new Grid();
            rootGrid.BackgroundColor = Color.FromArgb("#121212");

            // Subtle background accent blobs
            rootGrid.Add(new Ellipse
            {
                Fill = Color.FromArgb("#1565C0"),
                Opacity = 0.12,
                WidthRequest = 500,
                HeightRequest = 500,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(-200, -200, 0, 0)
            });
            rootGrid.Add(new Ellipse
            {
                Fill = Color.FromArgb("#0D47A1"),
                Opacity = 0.08,
                WidthRequest = 350,
                HeightRequest = 350,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.End,
                Margin = new Thickness(0, 0, -150, -80)
            });

            // Foreground scroll content
            var scroll = new ScrollView { VerticalScrollBarVisibility = ScrollBarVisibility.Never };
            var main = new VerticalStackLayout
            {
                Padding = new Thickness(32, 64, 32, 40),
                Spacing = 0
            };

            // === Brand Header ===
            var brandArea = new VerticalStackLayout { Spacing = 6, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 0, 0, 40) };
            brandArea.Add(new Label
            {
                Text = "VĨNH KHÁNH",
                FontSize = 32,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                CharacterSpacing = 4
            });
            brandArea.Add(new Label
            {
                Text = "Đăng nhập để tiếp tục",
                FontSize = 14,
                TextColor = Color.FromArgb("#B3B3B3"),
                HorizontalOptions = LayoutOptions.Center
            });
            main.Add(brandArea);

            // === Form Card (dark glass style) ===
            var cardBorder = new Border
            {
                BackgroundColor = Color.FromArgb("#1E1E1E"),
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                StrokeThickness = 1,
                Stroke = Color.FromArgb("#2A2A2A"),
                Padding = new Thickness(24, 28, 24, 28),
                Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.5f, Radius = 30, Offset = new Point(0, 16) }
            };

            var cardLayout = new VerticalStackLayout { Spacing = 16 };

            cardLayout.Add(new Label
            {
                Text = "Đăng nhập",
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                Margin = new Thickness(0, 0, 0, 8)
            });

            // Username field
            _usernameEntry = CreateEntry("Tên tài khoản", false);
            cardLayout.Add(CreateDarkField("Tài khoản", _usernameEntry));

            // Password field
            _passwordEntry = CreateEntry("Mật khẩu", true);
            cardLayout.Add(CreateDarkField("Mật khẩu", _passwordEntry));

            // Error label
            _errorLabel = new Label
            {
                TextColor = Color.FromArgb("#FF5252"),
                FontSize = 13,
                IsVisible = false,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 4, 0, 0)
            };
            cardLayout.Add(_errorLabel);

            // Login button
            _btnLogin = new Button
            {
                Text = "Đăng nhập",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HeightRequest = 56,
                CornerRadius = 28,
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0),
                    GradientStops = {
                        new GradientStop(Color.FromArgb("#1565C0"), 0),
                        new GradientStop(Color.FromArgb("#1E88E5"), 1)
                    }
                },
                Shadow = new Shadow { Brush = Color.FromArgb("#1565C0"), Opacity = 0.5f, Radius = 16, Offset = new Point(0, 8) },
                Margin = new Thickness(0, 8, 0, 0)
            };
            _btnLogin.Clicked += OnLoginClicked;
            cardLayout.Add(_btnLogin);

            // Divider
            var div = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
                Margin = new Thickness(0, 16)
            };
            div.Add(new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#2A2A2A"), VerticalOptions = LayoutOptions.Center }, 0, 0);
            div.Add(new Label { Text = " hoặc ", TextColor = Color.FromArgb("#6B6B6B"), FontSize = 12, VerticalOptions = LayoutOptions.Center }, 1, 0);
            div.Add(new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#2A2A2A"), VerticalOptions = LayoutOptions.Center }, 2, 0);
            cardLayout.Add(div);

            // Guest button (outlined dark style)
            var btnGuest = new Button
            {
                Text = "Tham quan với quyền Khách",
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#B3B3B3"),
                HeightRequest = 56,
                CornerRadius = 28,
                BackgroundColor = Colors.Transparent,
                BorderColor = Color.FromArgb("#3A3A3A"),
                BorderWidth = 1.5
            };
            btnGuest.Clicked += (s, e) =>
            {
                UserSession.Instance.LoginAsGuest();
                Application.Current!.MainPage = new MainTabbedPage();
            };
            cardLayout.Add(btnGuest);

            cardBorder.Content = cardLayout;
            main.Add(cardBorder);

            // Register link
            var regRow = new HorizontalStackLayout
            {
                Spacing = 6,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 24, 0, 0)
            };
            regRow.Add(new Label { Text = "Chưa có tài khoản?", FontSize = 14, TextColor = Color.FromArgb("#6B6B6B"), VerticalOptions = LayoutOptions.Center });
            var regLink = new Label
            {
                Text = "Đăng ký",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#42A5F5"),
                VerticalOptions = LayoutOptions.Center
            };
            regLink.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PushAsync(new RegisterPage())) });
            regRow.Add(regLink);
            main.Add(regRow);

            scroll.Content = main;
            rootGrid.Add(scroll);
            Content = rootGrid;
        }

        private static View CreateDarkField(string label, Entry entry)
        {
            var layout = new VerticalStackLayout { Spacing = 8 };
            layout.Add(new Label
            {
                Text = label,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#B3B3B3"),
                Margin = new Thickness(2, 0, 0, 0)
            });
            var border = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                Stroke = Color.FromArgb("#3A3A3A"),
                BackgroundColor = Color.FromArgb("#2A2A2A"),
                HeightRequest = 52,
                Padding = new Thickness(14, 0),
                Content = entry
            };
            layout.Add(border);
            return layout;
        }

        private static Entry CreateEntry(string placeholder, bool isPassword) => new Entry
        {
            Placeholder = placeholder,
            PlaceholderColor = Color.FromArgb("#6B6B6B"),
            TextColor = Colors.White,
            IsPassword = isPassword,
            BackgroundColor = Colors.Transparent,
            VerticalOptions = LayoutOptions.Center,
            FontSize = 15
        };

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string user = _usernameEntry.Text?.Trim() ?? "";
            string pass = _passwordEntry.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                ShowError("Vui lòng nhập tài khoản và mật khẩu");
                return;
            }

            _btnLogin.Text = "Đang xử lý...";
            _btnLogin.IsEnabled = false;

            try
            {
                string? fullName = null;
                bool apiLogin = false;
                try
                {
                    var res = await ApiService.Instance.LoginWithDetailsAsync(user, pass);
                    if (res != null) { apiLogin = true; fullName = res.FullName; }
                }
                catch { }

                bool localLogin = false;
                if (!apiLogin)
                {
                    var dbUser = await App.Database.GetUserAsync(user, pass);
                    if (dbUser != null) { localLogin = true; fullName = dbUser.FullName; }
                    if (!localLogin && user == "admin" && (pass == "admin123" || pass == "admin"))
                    { localLogin = true; fullName = "Administrator"; }
                }

                if (apiLogin || localLogin)
                {
                    UserSession.Instance.LoginAsUser(user, fullName ?? user);
                    Application.Current!.MainPage = new MainTabbedPage();
                }
                else
                {
                    ShowError("Tài khoản hoặc mật khẩu không chính xác");
                }
            }
            finally
            {
                _btnLogin.Text = "Đăng nhập";
                _btnLogin.IsEnabled = true;
            }
        }

        private void ShowError(string message)
        {
            _errorLabel.Text = message;
            _errorLabel.IsVisible = true;
        }
    }
}
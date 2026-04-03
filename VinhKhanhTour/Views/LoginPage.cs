using VinhKhanhTour.Services;
using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using VinhKhanhTour.Models;

namespace VinhKhanhTour.Views
{
    public class LoginPage : ContentPage
    {
        private Entry _usernameEntry;
        private Entry _passwordEntry;
        private Label _errorLabel;
        private Button _btnLogin;

        public LoginPage()
        {
            NavigationPage.SetHasNavigationBar(this, false);
            BackgroundColor = Color.FromArgb("#0D1B2A");
            CreateUI();
        }

        private void CreateUI()
        {
            var mainLayout = new VerticalStackLayout
            {
                Padding = new Thickness(30, 80, 30, 30),
                Spacing = 24,
                VerticalOptions = LayoutOptions.Center
            };

            // Header Section
            var headerGroup = new VerticalStackLayout { Spacing = 8, HorizontalOptions = LayoutOptions.Center };
            headerGroup.Add(new Label
            {
                Text = "Vinh Khanh Tour",
                FontSize = 34,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center
            });
            headerGroup.Add(new Label
            {
                Text = "Khám phá thiên đường ẩm thực Quận 4",
                FontSize = 14,
                TextColor = Color.FromArgb("#8ba0b2"),
                HorizontalOptions = LayoutOptions.Center
            });
            mainLayout.Add(headerGroup);

            // Spacer
            mainLayout.Add(new BoxView { HeightRequest = 20, Color = Colors.Transparent });

            // Input Fields
            _usernameEntry = CreateEntry("Tên đăng nhập", false);
            _passwordEntry = CreateEntry("Mật khẩu", true);
            mainLayout.Add(_usernameEntry);
            mainLayout.Add(_passwordEntry);

            // Error Message
            _errorLabel = new Label
            {
                TextColor = Color.FromArgb("#FF5252"),
                FontSize = 13,
                IsVisible = false,
                HorizontalOptions = LayoutOptions.Center
            };
            mainLayout.Add(_errorLabel);

            // Login Button
            _btnLogin = new Button
            {
                Text = "Đăng nhập",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HeightRequest = 56,
                CornerRadius = 16,
                BackgroundColor = Color.FromArgb("#1565C0"), // Fallback
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
                Shadow = new Shadow { Brush = Color.FromArgb("#1565C0"), Opacity = 0.5f, Radius = 15, Offset = new Point(0, 6) },
                Margin = new Thickness(0, 10, 0, 0)
            };
            _btnLogin.Clicked += OnLoginClicked;
            mainLayout.Add(_btnLogin);

            // Register Link
            var registerLabel = new Label
            {
                Text = "Chưa có tài khoản? Đăng ký ngay",
                FontSize = 14,
                TextColor = Color.FromArgb("#64B5F6"),
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 10, 0, 30),
                TextDecorations = TextDecorations.Underline
            };
            registerLabel.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () => await Navigation.PushAsync(new RegisterPage()))
            });
            mainLayout.Add(registerLabel);

            // Guest Entry Divider
            var dividerLayout = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
                Margin = new Thickness(0, 10)
            };
            dividerLayout.Add(new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#152535"), VerticalOptions = LayoutOptions.Center }, 0, 0);
            dividerLayout.Add(new Label { Text = " HOẶC ", TextColor = Color.FromArgb("#8ba0b2"), FontSize = 12, VerticalOptions = LayoutOptions.Center }, 1, 0);
            dividerLayout.Add(new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#152535"), VerticalOptions = LayoutOptions.Center }, 2, 0);
            mainLayout.Add(dividerLayout);

            // Guest Button
            var btnGuest = new Button
            {
                Text = "Tham quan với tư cách Khách",
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#64B5F6"),
                HeightRequest = 56,
                CornerRadius = 16,
                BackgroundColor = Color.FromArgb("#0D1B2A"), // Match background
                BorderColor = Color.FromArgb("#1565C0"),
                BorderWidth = 1.5
            };
            btnGuest.Clicked += (s, e) => { Application.Current.MainPage = new MainTabbedPage(); };
            mainLayout.Add(btnGuest);

            Content = new ScrollView { Content = mainLayout, VerticalScrollBarVisibility = ScrollBarVisibility.Never };
        }

        private Entry CreateEntry(string placeholder, bool isPassword)
        {
            var entry = new Entry
            {
                Placeholder = placeholder,
                PlaceholderColor = Color.FromArgb("#8ba0b2"),
                TextColor = Colors.White,
                IsPassword = isPassword,
                BackgroundColor = Color.FromArgb("#152535"),
                HeightRequest = 56,
                FontSize = 15
            };
            return entry;
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string user = _usernameEntry.Text?.Trim() ?? string.Empty;
            string pass = _passwordEntry.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                ShowError("Vui lòng nhập tài khoản và mật khẩu");
                return;
            }

            _btnLogin.Text = "Đang xử lý...";
            _btnLogin.IsEnabled = false;

            // Thử đăng nhập qua API trước
            bool apiLogin = false;
            try
            {
                apiLogin = await ApiService.Instance.LoginAsync(user, pass);
            }
            catch { }

            // Fallback: đăng nhập qua SQLite local
            bool localLogin = false;
            if (!apiLogin)
            {
                var dbUser = await App.Database.GetUserAsync(user, pass);
                localLogin = dbUser != null;
                // Tài khoản admin mặc định
                if (!localLogin && user == "admin" && pass == "admin123")
                    localLogin = true;
                if (!localLogin && user == "admin" && pass == "admin")
                    localLogin = true;
            }

            if (apiLogin || localLogin)
            {
                Application.Current.MainPage = new MainTabbedPage();
            }
            else
            {
                ShowError("Tài khoản hoặc mật khẩu không chính xác");
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

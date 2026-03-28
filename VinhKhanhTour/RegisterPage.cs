using VinhKhanhTour.Services;
using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using VinhKhanhTour.Models;

namespace VinhKhanhTour
{
    public class RegisterPage : ContentPage
    {
        private Entry _fullnameEntry;
        private Entry _usernameEntry;
        private Entry _passwordEntry;
        private Label _errorLabel;
        private Button _btnRegister;

        public RegisterPage()
        {
            NavigationPage.SetHasNavigationBar(this, false);
            BackgroundColor = Color.FromArgb("#0D1B2A");
            CreateUI();
        }

        private void CreateUI()
        {
            var mainLayout = new VerticalStackLayout
            {
                Padding = new Thickness(30, 60, 30, 30),
                Spacing = 20,
                VerticalOptions = LayoutOptions.Start
            };

            // Back button
            var backBtn = new Label
            {
                Text = "← Trở về Đăng nhập",
                TextColor = Color.FromArgb("#8ba0b2"),
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 20)
            };
            backBtn.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () => await Navigation.PopAsync())
            });
            mainLayout.Add(backBtn);

            // Header
            mainLayout.Add(new Label
            {
                Text = "Tạo tài khoản",
                FontSize = 30,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                Margin = new Thickness(0, 0, 0, 10)
            });

            // Input Fields
            _fullnameEntry = CreateEntry("Họ và tên", false);
            _usernameEntry = CreateEntry("Tên đăng nhập", false);
            _passwordEntry = CreateEntry("Mật khẩu", true);

            mainLayout.Add(_fullnameEntry);
            mainLayout.Add(_usernameEntry);
            mainLayout.Add(_passwordEntry);

            // Error Message
            _errorLabel = new Label
            {
                TextColor = Color.FromArgb("#FF5252"),
                FontSize = 13,
                IsVisible = false
            };
            mainLayout.Add(_errorLabel);

            // Register Button
            _btnRegister = new Button
            {
                Text = "Đăng ký",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HeightRequest = 56,
                CornerRadius = 16,
                BackgroundColor = Color.FromArgb("#1565C0"),
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
                Margin = new Thickness(0, 20, 0, 0)
            };
            _btnRegister.Clicked += OnRegisterClicked;
            mainLayout.Add(_btnRegister);

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

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            string fname = _fullnameEntry.Text?.Trim() ?? string.Empty;
            string user = _usernameEntry.Text?.Trim() ?? string.Empty;
            string pass = _passwordEntry.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(fname) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                ShowError("Vui lòng điền đầy đủ thông tin");
                return;
            }

            _btnRegister.Text = "Đang xử lý...";
            _btnRegister.IsEnabled = false;

            bool isTaken = await App.Database.IsUsernameTakenAsync(user);
            if (isTaken)
            {
                ShowError("Tên đăng nhập này đã có người sử dụng");
                _btnRegister.Text = "Đăng ký";
                _btnRegister.IsEnabled = true;
                return;
            }

            var newUser = new User
            {
                FullName = fname,
                Username = user,
                Password = pass
            };

            // Lưu vào SQLite local
            await App.Database.SaveUserAsync(newUser);

            // Đồng thời đăng ký lên API server
            await ApiService.Instance.RegisterAsync(user, pass, fname);

            await DisplayAlert("Thành công", "Tạo tài khoản thành công! Khám phá ẩm thực ngay.", "Vào App");
            Application.Current.MainPage = new MainTabbedPage();
        }

        private void ShowError(string message)
        {
            _errorLabel.Text = message;
            _errorLabel.IsVisible = true;
        }
    }
}
using VinhKhanhTour.Services;
using Microsoft.Maui.Controls.Shapes;

namespace VinhKhanhTour.Views
{
    public class RegisterPage : ContentPage
    {
        private Entry _fullnameEntry = null!;
        private Entry _usernameEntry = null!;
        private Entry _passwordEntry = null!;
        private Entry _confirmEntry = null!;
        private Label _errorLabel = null!;
        private Button _btnRegister = null!;

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
                Padding = new Thickness(30, 60, 30, 40),
                Spacing = 16
            };

            // Back button
            var backBtn = new Label { Text = "< Tro ve Dang nhap", TextColor = Color.FromArgb("#8ba0b2"), FontSize = 14 };
            backBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });
            mainLayout.Add(backBtn);

            mainLayout.Add(new Label { Text = "Tao tai khoan", FontSize = 30, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, Margin = new Thickness(0, 10, 0, 4) });
            mainLayout.Add(new Label { Text = "Dang ky de luu hanh trinh kham pha cua ban", FontSize = 13, TextColor = Color.FromArgb("#8ba0b2"), Margin = new Thickness(0, 0, 0, 10) });

            _fullnameEntry = CreateEntry("Ho va ten", false);
            _usernameEntry = CreateEntry("Ten dang nhap", false);
            _passwordEntry = CreateEntry("Mat khau", true);
            _confirmEntry = CreateEntry("Nhap lai mat khau", true);
            mainLayout.Add(_fullnameEntry);
            mainLayout.Add(_usernameEntry);
            mainLayout.Add(_passwordEntry);
            mainLayout.Add(_confirmEntry);

            // Password rules hint
            mainLayout.Add(new Label
            {
                Text = "Mat khau it nhat 6 ky tu",
                FontSize = 11,
                TextColor = Color.FromArgb("#8ba0b2"),
                Margin = new Thickness(4, -8, 0, 0)
            });

            _errorLabel = new Label { TextColor = Color.FromArgb("#FF5252"), FontSize = 13, IsVisible = false };
            mainLayout.Add(_errorLabel);

            _btnRegister = new Button
            {
                Text = "Tao tai khoan",
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
                    GradientStops = { new GradientStop(Color.FromArgb("#1565C0"), 0), new GradientStop(Color.FromArgb("#42A5F5"), 1) }
                },
                Shadow = new Shadow { Brush = Color.FromArgb("#1565C0"), Opacity = 0.5f, Radius = 15, Offset = new Point(0, 6) },
                Margin = new Thickness(0, 12, 0, 0)
            };
            _btnRegister.Clicked += OnRegisterClicked;
            mainLayout.Add(_btnRegister);

            // Terms note
            mainLayout.Add(new Label
            {
                Text = "Bang cach dang ky, ban dong y voi dieu khoan su dung cua Vinh Khanh Tour",
                FontSize = 11,
                TextColor = Color.FromArgb("#8ba0b2"),
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            });

            Content = new ScrollView { Content = mainLayout, VerticalScrollBarVisibility = ScrollBarVisibility.Never };
        }

        private static Entry CreateEntry(string placeholder, bool isPassword) => new Entry
        {
            Placeholder = placeholder,
            PlaceholderColor = Color.FromArgb("#8ba0b2"),
            TextColor = Colors.White,
            IsPassword = isPassword,
            BackgroundColor = Color.FromArgb("#152535"),
            HeightRequest = 56,
            FontSize = 15
        };

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            string fname = _fullnameEntry.Text?.Trim() ?? "";
            string user = _usernameEntry.Text?.Trim() ?? "";
            string pass = _passwordEntry.Text?.Trim() ?? "";
            string conf = _confirmEntry.Text?.Trim() ?? "";

            // Validation
            if (string.IsNullOrEmpty(fname) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            { ShowError("Vui long dien day du thong tin"); return; }
            if (pass.Length < 6)
            { ShowError("Mat khau phai co it nhat 6 ky tu"); return; }
            if (pass != conf)
            { ShowError("Mat khau nhap lai khong khop"); return; }
            if (user.Length < 3)
            { ShowError("Ten dang nhap phai co it nhat 3 ky tu"); return; }

            _btnRegister.Text = "Dang xu ly...";
            _btnRegister.IsEnabled = false;

            try
            {
                bool isTaken = await App.Database.IsUsernameTakenAsync(user);
                if (isTaken)
                {
                    ShowError("Ten dang nhap nay da co nguoi su dung");
                    return;
                }

                // Save local
                await App.Database.SaveUserAsync(new Models.User
                {
                    FullName = fname,
                    Username = user,
                    Password = pass
                });

                // Sync to API
                await ApiService.Instance.RegisterAsync(user, pass, fname);

                // Auto login after register
                UserSession.Instance.LoginAsUser(user, fname);

                await DisplayAlert("Chao mung!", $"Chao mung {fname} den voi Vinh Khanh Tour!\nHay bat dau kham pha am thuc Quan 4.", "Vao App ngay");
                Application.Current!.MainPage = new MainTabbedPage();
            }
            finally
            {
                _btnRegister.Text = "Tao tai khoan";
                _btnRegister.IsEnabled = true;
            }
        }

        private void ShowError(string message)
        {
            _errorLabel.Text = message;
            _errorLabel.IsVisible = true;
        }
    }
}
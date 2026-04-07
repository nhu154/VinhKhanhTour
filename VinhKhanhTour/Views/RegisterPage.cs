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
            BackgroundColor = Color.FromArgb("#121212");
            CreateUI();
        }

        private void CreateUI()
        {
            var rootGrid = new Grid();
            rootGrid.BackgroundColor = Color.FromArgb("#121212");

            // Background accents
            rootGrid.Add(new Ellipse
            {
                Fill = Color.FromArgb("#1565C0"),
                Opacity = 0.10,
                WidthRequest = 400,
                HeightRequest = 400,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, -150, -150, 0)
            });

            var scroll = new ScrollView { VerticalScrollBarVisibility = ScrollBarVisibility.Never };
            var main = new VerticalStackLayout
            {
                Padding = new Thickness(32, 56, 32, 40),
                Spacing = 0
            };

            // Back button
            var backRow = new HorizontalStackLayout { Spacing = 8, Margin = new Thickness(0, 0, 0, 32) };
            var backLabel = new Label
            {
                Text = "← Quay lại",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#42A5F5"),
                VerticalOptions = LayoutOptions.Center
            };
            backLabel.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });
            backRow.Add(backLabel);
            main.Add(backRow);

            // Header
            var header = new VerticalStackLayout { Spacing = 6, Margin = new Thickness(0, 0, 0, 32) };
            header.Add(new Label
            {
                Text = "Tạo tài khoản",
                FontSize = 30,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White
            });
            header.Add(new Label
            {
                Text = "Đăng ký để lưu hành trình khám phá của bạn",
                FontSize = 14,
                TextColor = Color.FromArgb("#B3B3B3")
            });
            main.Add(header);

            // Form card
            var card = new Border
            {
                BackgroundColor = Color.FromArgb("#1E1E1E"),
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                StrokeThickness = 1,
                Stroke = Color.FromArgb("#2A2A2A"),
                Padding = new Thickness(24, 28, 24, 28),
                Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.5f, Radius = 30, Offset = new Point(0, 16) }
            };

            var cardLayout = new VerticalStackLayout { Spacing = 16 };

            _fullnameEntry = CreateEntry("Nguyễn Văn A", false);
            _usernameEntry = CreateEntry("Tên đăng nhập (không dấu)", false);
            _passwordEntry = CreateEntry("Ít nhất 6 ký tự", true);
            _confirmEntry = CreateEntry("Nhập lại mật khẩu", true);

            cardLayout.Add(CreateDarkField("Họ và tên", _fullnameEntry));
            cardLayout.Add(CreateDarkField("Tên đăng nhập", _usernameEntry));
            cardLayout.Add(CreateDarkField("Mật khẩu", _passwordEntry));
            cardLayout.Add(CreateDarkField("Xác nhận mật khẩu", _confirmEntry));

            _errorLabel = new Label
            {
                TextColor = Color.FromArgb("#FF5252"),
                FontSize = 13,
                IsVisible = false,
                Margin = new Thickness(0, 4, 0, 0)
            };
            cardLayout.Add(_errorLabel);

            _btnRegister = new Button
            {
                Text = "Tạo tài khoản",
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
            _btnRegister.Clicked += OnRegisterClicked;
            cardLayout.Add(_btnRegister);

            card.Content = cardLayout;
            main.Add(card);

            // Terms note
            main.Add(new Label
            {
                Text = "Bằng cách đăng ký, bạn đồng ý với điều khoản sử dụng của Vĩnh Khánh Tour",
                FontSize = 11,
                TextColor = Color.FromArgb("#6B6B6B"),
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            });

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

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            string fname = _fullnameEntry.Text?.Trim() ?? "";
            string user = _usernameEntry.Text?.Trim() ?? "";
            string pass = _passwordEntry.Text?.Trim() ?? "";
            string conf = _confirmEntry.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(fname) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            { ShowError("Vui lòng điền đầy đủ thông tin"); return; }
            if (pass.Length < 6)
            { ShowError("Mật khẩu phải có ít nhất 6 ký tự"); return; }
            if (pass != conf)
            { ShowError("Mật khẩu nhập lại không khớp"); return; }
            if (user.Length < 3)
            { ShowError("Tên đăng nhập phải có ít nhất 3 ký tự"); return; }

            _btnRegister.Text = "Đang xử lý...";
            _btnRegister.IsEnabled = false;

            try
            {
                bool isTaken = await App.Database.IsUsernameTakenAsync(user);
                if (isTaken) { ShowError("Tên đăng nhập này đã có người sử dụng"); return; }

                await App.Database.SaveUserAsync(new Models.User
                {
                    FullName = fname,
                    Username = user,
                    Password = pass
                });

                await ApiService.Instance.RegisterAsync(user, pass, fname);
                UserSession.Instance.LoginAsUser(user, fname);

                await DisplayAlert("Chào mừng!", $"Chào mừng {fname} đến với Vĩnh Khánh Tour!\nHãy bắt đầu khám phá ẩm thực Quận 4.", "Vào App ngay");
                Application.Current!.MainPage = new MainTabbedPage();
            }
            finally
            {
                _btnRegister.Text = "Tạo tài khoản";
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
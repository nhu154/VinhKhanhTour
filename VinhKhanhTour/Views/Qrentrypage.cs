// Views/QREntryPage.cs — Màn hình vào app qua QR
// Design: Tối giản sang trọng — dark navy, gold accent, không icon rác

using Microsoft.Maui.Controls.Shapes;
using VinhKhanhTour.Services;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace VinhKhanhTour.Views
{
    // ── Màn hình chính khi mở app ─────────────────────────────
    public class QREntryPage : ContentPage
    {
        public QREntryPage()
        {
            BackgroundColor = Color.FromArgb("#080D14");
            NavigationPage.SetHasNavigationBar(this, false);
            BuildUI();
        }

        private void BuildUI()
        {
            var root = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = GridLength.Auto }
                }
            };

            // ── Phần trên ─────────────────────────────────────
            var top = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Padding = new Thickness(40, 0),
                Spacing = 0
            };

            // Accent line
            top.Add(new BoxView
            {
                Color = Color.FromArgb("#C9A84C"),
                HeightRequest = 2,
                WidthRequest = 40,
                HorizontalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 0, 0, 28)
            });

            top.Add(new Label
            {
                Text = "VĨNH KHÁNH",
                FontSize = 34,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                CharacterSpacing = 6,
                Margin = new Thickness(0, 0, 0, 8)
            });

            top.Add(new Label
            {
                Text = "Audio Guide · Quận 4",
                FontSize = 14,
                TextColor = Color.FromArgb("#C9A84C"),
                CharacterSpacing = 2,
                Margin = new Thickness(0, 0, 0, 52)
            });

            // QR frame minh họa — tối giản, không emoji
            top.Add(new Border
            {
                WidthRequest = 180,
                HeightRequest = 180,
                HorizontalOptions = LayoutOptions.Start,
                StrokeShape = new RoundRectangle { CornerRadius = 4 },
                StrokeThickness = 0,
                BackgroundColor = Colors.Transparent,
                Margin = new Thickness(0, 0, 0, 40),
                Content = BuildQRFrame()
            });

            top.Add(new Label
            {
                Text = "Đưa camera vào mã QR\ntại lối vào để bắt đầu",
                FontSize = 16,
                TextColor = Color.FromArgb("#8899AA"),
                LineHeight = 1.7,
                Margin = new Thickness(0, 0, 0, 0)
            });

            Grid.SetRow(top, 0);
            root.Add(top);

            // ── Phần dưới: nút ────────────────────────────────
            var btns = new VerticalStackLayout
            {
                Padding = new Thickness(40, 0, 40, 52),
                Spacing = 0
            };

            // Nút chính
            var scanBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#C9A84C"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 3 },
                HeightRequest = 56,
                HorizontalOptions = LayoutOptions.Fill,
                Margin = new Thickness(0, 0, 0, 14),
                Content = new Label
                {
                    Text = "QUÉT MÃ QR ĐỂ VÀO",
                    FontSize = 13,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#080D14"),
                    CharacterSpacing = 2,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            };
            scanBtn.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                    await Navigation.PushAsync(new EntryQRScannerPage()))
            });
            btns.Add(scanBtn);

            // Nút phụ — demo
            var demoBtn = new Border
            {
                BackgroundColor = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 3 },
                StrokeThickness = 1,
                Stroke = Color.FromArgb("#1E2D3F"),
                HeightRequest = 52,
                HorizontalOptions = LayoutOptions.Fill,
                Margin = new Thickness(0, 0, 0, 24),
                Content = new Label
                {
                    Text = "Truy cập không cần QR",
                    FontSize = 13,
                    TextColor = Color.FromArgb("#4A6280"),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            };
            demoBtn.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() =>
                {
                    UserSession.Instance.LoginAsGuest();
                    Application.Current!.MainPage = new NavigationPage(new MainTabbedPage());
                })
            });
            btns.Add(demoBtn);

            // Link đăng nhập
            var loginRow = new HorizontalStackLayout
            {
                Spacing = 8,
                HorizontalOptions = LayoutOptions.Center
            };
            loginRow.Add(new Label
            {
                Text = "Đã có tài khoản?",
                FontSize = 13,
                TextColor = Color.FromArgb("#2E3D4D"),
                VerticalOptions = LayoutOptions.Center
            });
            var loginLink = new Label
            {
                Text = "Đăng nhập",
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#C9A84C"),
                VerticalOptions = LayoutOptions.Center
            };
            loginLink.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                    await Navigation.PushAsync(new LoginPage()))
            });
            loginRow.Add(loginLink);
            btns.Add(loginRow);

            Grid.SetRow(btns, 1);
            root.Add(btns);
            Content = root;
        }

        // Khung QR tối giản — chỉ dùng geometry, không emoji
        private static View BuildQRFrame()
        {
            var g = new Grid { WidthRequest = 180, HeightRequest = 180 };

            var gold = Color.FromArgb("#C9A84C");
            var dim = Color.FromArgb("#1A2535");

            // Vùng tối bên trong
            g.Add(new Border
            {
                Margin = new Thickness(24),
                BackgroundColor = dim,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 2 }
            });

            // 4 góc vàng
            int cs = 20; int t = 2;
            foreach (var (h, v, mx, my) in new[]
            {
                (LayoutOptions.Start, LayoutOptions.Start, new Thickness(0,0,0,0), new Thickness(0,0,0,0)),
                (LayoutOptions.End,   LayoutOptions.Start, new Thickness(0,0,0,0), new Thickness(0,0,0,0)),
                (LayoutOptions.Start, LayoutOptions.End,   new Thickness(0,0,0,0), new Thickness(0,0,0,0)),
                (LayoutOptions.End,   LayoutOptions.End,   new Thickness(0,0,0,0), new Thickness(0,0,0,0)),
            })
            {
                g.Add(new BoxView { Color = gold, WidthRequest = cs, HeightRequest = t, HorizontalOptions = h, VerticalOptions = v });
                g.Add(new BoxView { Color = gold, WidthRequest = t, HeightRequest = cs, HorizontalOptions = h, VerticalOptions = v });
            }

            // Đường quét vàng
            var line = new BoxView
            {
                Color = gold,
                HeightRequest = 1.5,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(8, 0),
                Opacity = 0.7
            };
            g.Add(line);
            AnimateLine(line, 180);

            return g;
        }

        private static async void AnimateLine(BoxView line, double maxY)
        {
            while (true)
            {
                try
                {
                    await line.TranslateTo(0, maxY - 16, 2000, Easing.SinInOut);
                    await line.TranslateTo(0, 8, 2000, Easing.SinInOut);
                }
                catch { break; }
            }
        }
    }


    // ── Màn hình camera quét QR vào app ──────────────────────
    public class EntryQRScannerPage : ContentPage
    {
        private const string SEED_LINK = "vinhkhanhtour://open/guest";
        private const string SEED_IMG =
            "https://api.qrserver.com/v1/create-qr-code/?size=120x120&margin=4" +
            "&data=vinhkhanhtour%3A%2F%2Fopen%2Fguest";

        private bool _isProcessing = false;
        private CameraBarcodeReaderView? _cameraView;
        private Grid? _rootGrid;

        public EntryQRScannerPage()
        {
            BackgroundColor = Color.FromArgb("#080D14");
            NavigationPage.SetHasNavigationBar(this, false);
            BuildUI();
        }

        private void BuildUI()
        {
            _rootGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = new GridLength(320) },
                    new RowDefinition { Height = GridLength.Auto }
                }
            };

            // ── Header ────────────────────────────────────────
            var header = new Grid
            {
                BackgroundColor = Color.FromArgb("#080D14"),
                Padding = new Thickness(24, 54, 24, 20),
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star }
                }
            };

            var backBtn = new Label
            {
                Text = "←",
                FontSize = 22,
                TextColor = Colors.White,
                Padding = new Thickness(0, 0, 20, 0),
                VerticalOptions = LayoutOptions.Center
            };
            backBtn.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () => await Navigation.PopAsync())
            });
            header.Add(backBtn, 0, 0);

            var titles = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center };
            titles.Add(new Label
            {
                Text = "QUÉT MÃ VÀO APP",
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                CharacterSpacing = 1.5
            });
            titles.Add(new Label
            {
                Text = "Đặt mã QR tại lối vào vào khung",
                FontSize = 12,
                TextColor = Color.FromArgb("#3D5268")
            });
            header.Add(titles, 1, 0);
            Grid.SetRow(header, 0);
            _rootGrid.Add(header);

            // ── Camera ────────────────────────────────────────
            var camOuter = new Grid { BackgroundColor = Color.FromArgb("#050A10") };

            _cameraView = new CameraBarcodeReaderView
            {
                Options = new BarcodeReaderOptions
                {
                    Formats = BarcodeFormat.QrCode,
                    AutoRotate = true,
                    Multiple = false,
                },
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                IsDetecting = true,
            };
            _cameraView.BarcodesDetected += OnQRDetected;
            camOuter.Add(_cameraView);
            camOuter.Add(BuildCameraOverlay(Color.FromArgb("#C9A84C")));
            Grid.SetRow(camOuter, 1);
            _rootGrid.Add(camOuter);

            // ── Bottom panel ──────────────────────────────────
            var bottom = new VerticalStackLayout
            {
                Padding = new Thickness(24, 20, 24, 40),
                Spacing = 16,
                BackgroundColor = Color.FromArgb("#080D14")
            };

            // Seed card
            var seedCard = new Border
            {
                BackgroundColor = Color.FromArgb("#0D1520"),
                StrokeShape = new RoundRectangle { CornerRadius = 3 },
                StrokeThickness = 1,
                Stroke = Color.FromArgb("#1A2535"),
                Padding = new Thickness(16, 14)
            };
            var seedRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = 80 },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 16
            };
            seedRow.Add(new Border
            {
                BackgroundColor = Colors.White,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 2 },
                Padding = 3,
                Content = new Image
                {
                    Source = SEED_IMG,
                    WidthRequest = 74,
                    HeightRequest = 74,
                    Aspect = Aspect.AspectFit
                }
            }, 0, 0);
            var seedInfo = new VerticalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center };
            seedInfo.Add(new Label
            {
                Text = "Mã QR mẫu",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White
            });
            seedInfo.Add(new Label
            {
                Text = "Quét mã này để vào app\nmà không cần in QR thật",
                FontSize = 12,
                TextColor = Color.FromArgb("#3D5268"),
                LineHeight = 1.5
            });
            seedRow.Add(seedInfo, 1, 0);
            seedCard.Content = seedRow;
            bottom.Add(seedCard);

            // Nút dùng QR mẫu
            var seedBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#C9A84C"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 3 },
                HeightRequest = 52,
                HorizontalOptions = LayoutOptions.Fill,
                Content = new Label
                {
                    Text = "DÙNG MÃ QR MẪU",
                    FontSize = 13,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#080D14"),
                    CharacterSpacing = 1.5,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            };
            seedBtn.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() => EnterApp())
            });
            bottom.Add(seedBtn);

            Grid.SetRow(bottom, 2);
            _rootGrid.Add(bottom);
            Content = _rootGrid;
        }

        private static View BuildCameraOverlay(Color accent)
        {
            var overlay = new Grid { HorizontalOptions = LayoutOptions.Fill, VerticalOptions = LayoutOptions.Fill };
            var frame = new Grid
            {
                WidthRequest = 220,
                HeightRequest = 220,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            foreach (var (h, v) in new[]
            {
                (LayoutOptions.Start, LayoutOptions.Start),
                (LayoutOptions.End,   LayoutOptions.Start),
                (LayoutOptions.Start, LayoutOptions.End),
                (LayoutOptions.End,   LayoutOptions.End)
            })
            {
                frame.Add(new BoxView { Color = accent, WidthRequest = 28, HeightRequest = 2, HorizontalOptions = h, VerticalOptions = v });
                frame.Add(new BoxView { Color = accent, WidthRequest = 2, HeightRequest = 28, HorizontalOptions = h, VerticalOptions = v });
            }

            var line = new BoxView
            {
                Color = accent,
                HeightRequest = 1.5,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(4, 0),
                Opacity = 0.8
            };
            frame.Add(line);
            AnimateLine(line);
            overlay.Add(frame);
            return overlay;
        }

        private static async void AnimateLine(BoxView line)
        {
            while (true)
            {
                try
                {
                    await line.TranslateTo(0, 218, 1800, Easing.SinInOut);
                    await line.TranslateTo(0, 0, 1800, Easing.SinInOut);
                    await Task.Delay(80);
                }
                catch { break; }
            }
        }

        private void OnQRDetected(object? sender, BarcodeDetectionEventArgs e)
        {
            if (_isProcessing) return;
            _isProcessing = true;
            var value = e.Results?.FirstOrDefault()?.Value;
            if (string.IsNullOrWhiteSpace(value)) { _isProcessing = false; return; }

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (_cameraView != null) _cameraView.IsDetecting = false;
                if (value.StartsWith("vinhkhanhtour://", StringComparison.OrdinalIgnoreCase))
                {
                    await FlashAccent();
                    EnterApp();
                }
                else
                {
                    await DisplayAlert("Mã không hợp lệ",
                        "Đây không phải mã QR của VinhKhanhTour.", "Thử lại");
                    _isProcessing = false;
                    if (_cameraView != null) _cameraView.IsDetecting = true;
                }
            });
        }

        private void EnterApp()
        {
            UserSession.Instance.LoginAsGuest();
            Application.Current!.MainPage = new NavigationPage(new MainTabbedPage());
        }

        private async Task FlashAccent()
        {
            if (_rootGrid == null) return;
            var flash = new BoxView { Color = Color.FromArgb("#C9A84C"), Opacity = 0, ZIndex = 99 };
            Grid.SetRowSpan(flash, 3);
            _rootGrid.Add(flash);
            await flash.FadeTo(0.25, 80);
            await flash.FadeTo(0, 300);
            _rootGrid.Remove(flash);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (_cameraView != null) _cameraView.IsDetecting = false;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _isProcessing = false;
            if (_cameraView != null) _cameraView.IsDetecting = true;
        }
    }
}
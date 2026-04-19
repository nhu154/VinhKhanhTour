// Views/QRScanPage.cs — Quét QR tại từng quán trong app
// Design: Dark warm + gold — tối giản, typography sạch, không icon rác

using Microsoft.Maui.Controls.Shapes;
using VinhKhanhTour.Models;
using VinhKhanhTour.Services;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace VinhKhanhTour.Views
{
    public class QRScanPage : ContentPage
    {
        private bool _isProcessing = false;
        private CameraBarcodeReaderView? _cameraView;
        private Grid? _rootGrid;

        private bool _isScanTab = true;
        private Border? _scanTabBtn;
        private Border? _listTabBtn;
        private Grid? _scanPanel;
        private Grid? _listPanel;

        private List<Restaurant> _restaurants = new();
        private VerticalStackLayout? _poiListStack;
        private Label? _loadingLabel;

        public QRScanPage()
        {
            BackgroundColor = Color.FromArgb("#0A0F0D");
            NavigationPage.SetHasNavigationBar(this, false);
            BuildUI();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _isProcessing = false;
            if (_cameraView != null && _isScanTab) _cameraView.IsDetecting = true;
            _ = LoadRestaurantsAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (_cameraView != null) _cameraView.IsDetecting = false;
        }

        // ── Load quán từ DB ───────────────────────────────────────
        private async Task LoadRestaurantsAsync()
        {
            for (int i = 0; i < 5; i++)
            {
                var list = await App.Database.GetRestaurantsAsync();
                if (list.Count > 0)
                {
                    _restaurants = list;
                    MainThread.BeginInvokeOnMainThread(RenderPoiList);
                    return;
                }
                await Task.Delay(700);
            }
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_loadingLabel != null)
                    _loadingLabel.Text = "Không tải được dữ liệu. Vui lòng thử lại.";
            });
        }

        private void RenderPoiList()
        {
            if (_poiListStack == null) return;
            _poiListStack.Children.Clear();

            // Header
            _poiListStack.Add(new Label
            {
                Text = "DANH SÁCH QUÁN",
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#2E4A35"),
                CharacterSpacing = 2,
                Margin = new Thickness(0, 0, 0, 12)
            });

            foreach (var r in _restaurants)
                _poiListStack.Add(BuildPoiRow(r));
        }

        private View BuildPoiRow(Restaurant r)
        {
            var deepLink = $"vinhkhanhtour://poi/{r.Id}?autoplay=true";
            var qrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&margin=6" +
                           $"&data={Uri.EscapeDataString(deepLink)}";

            var row = new Border
            {
                BackgroundColor = Color.FromArgb("#0F1A12"),
                StrokeShape = new RoundRectangle { CornerRadius = 2 },
                StrokeThickness = 1,
                Stroke = Color.FromArgb("#1A2E1E"),
                Padding = new Thickness(20, 16),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var inner = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            var info = new VerticalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center };
            info.Add(new Label
            {
                Text = r.Name,
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White
            });
            info.Add(new Label
            {
                Text = r.OpenHours,
                FontSize = 12,
                TextColor = Color.FromArgb("#2E4A35")
            });
            inner.Add(info, 0, 0);

            var tag = new Border
            {
                BackgroundColor = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 2 },
                StrokeThickness = 1,
                Stroke = Color.FromArgb("#4A8A56"),
                Padding = new Thickness(12, 6),
                VerticalOptions = LayoutOptions.Center,
                Content = new Label
                {
                    Text = "XEM QR",
                    FontSize = 10,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#4A8A56"),
                    CharacterSpacing = 1
                }
            };
            inner.Add(tag, 1, 0);

            row.Content = inner;
            row.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () => await ShowQRPopup(r, qrUrl))
            });
            return row;
        }

        // ── Popup QR ──────────────────────────────────────────────
        private async Task ShowQRPopup(Restaurant r, string qrUrl)
        {
            if (_rootGrid == null) return;

            var overlay = new Grid
            {
                BackgroundColor = Color.FromArgb("#E0080D14"),
                ZIndex = 99
            };

            var popup = new Border
            {
                BackgroundColor = Color.FromArgb("#0F1A12"),
                StrokeShape = new RoundRectangle { CornerRadius = 4 },
                Stroke = Color.FromArgb("#4A8A56"),
                StrokeThickness = 1,
                Padding = new Thickness(32, 28),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                WidthRequest = 320,
                Shadow = new Shadow { Brush = Color.FromArgb("#4A8A56"), Opacity = 0.2f, Radius = 40 }
            };

            var inner = new VerticalStackLayout { Spacing = 20, HorizontalOptions = LayoutOptions.Center };

            inner.Add(new Label
            {
                Text = r.Name.ToUpper(),
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                CharacterSpacing = 2,
                HorizontalOptions = LayoutOptions.Center
            });

            // Đường kẻ vàng
            inner.Add(new BoxView
            {
                Color = Color.FromArgb("#4A8A56"),
                HeightRequest = 1,
                WidthRequest = 40,
                HorizontalOptions = LayoutOptions.Center
            });

            // QR image
            inner.Add(new Border
            {
                BackgroundColor = Colors.White,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 2 },
                Padding = 10,
                HorizontalOptions = LayoutOptions.Center,
                Content = new Image { Source = qrUrl, WidthRequest = 200, HeightRequest = 200 }
            });

            inner.Add(new Label
            {
                Text = "Quét mã để mở trang quán\nvà phát thuyết minh tự động",
                FontSize = 13,
                TextColor = Color.FromArgb("#2E4A35"),
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                LineHeight = 1.6
            });

            // Nút mở trực tiếp
            var openBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#4A8A56"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 2 },
                HeightRequest = 48,
                HorizontalOptions = LayoutOptions.Fill,
                Content = new Label
                {
                    Text = "MỞ TRỰC TIẾP",
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    CharacterSpacing = 1.5,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            };
            openBtn.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                {
                    _rootGrid.Remove(overlay);
                    await Navigation.PushAsync(
                        new RestaurantDetailPage(r, autoplayAudio: true));
                })
            });
            inner.Add(openBtn);

            var closeBtn = new Border
            {
                BackgroundColor = Colors.Transparent,
                StrokeThickness = 1,
                Stroke = Color.FromArgb("#1A2E1E"),
                StrokeShape = new RoundRectangle { CornerRadius = 2 },
                HeightRequest = 44,
                HorizontalOptions = LayoutOptions.Fill,
                Content = new Label
                {
                    Text = "Đóng",
                    FontSize = 13,
                    TextColor = Color.FromArgb("#2E4A35"),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            };
            closeBtn.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() => _rootGrid.Remove(overlay))
            });
            inner.Add(closeBtn);

            popup.Content = inner;
            overlay.Add(popup);
            overlay.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() => _rootGrid.Remove(overlay))
            });
            Grid.SetRowSpan(overlay, 3);
            _rootGrid.Add(overlay);

            overlay.Opacity = 0; popup.Scale = 0.92;
            await Task.WhenAll(overlay.FadeTo(1, 160), popup.ScaleTo(1, 220, Easing.SpringOut));
        }

        // ── ZXing callback ────────────────────────────────────────
        private void OnQRDetected(object? sender, BarcodeDetectionEventArgs e)
        {
            if (_isProcessing) return;
            _isProcessing = true;
            var value = e.Results?.FirstOrDefault()?.Value;
            if (string.IsNullOrWhiteSpace(value)) { _isProcessing = false; return; }

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (_cameraView != null) _cameraView.IsDetecting = false;
                await HandlePOIQR(value);
            });
        }

        private async Task HandlePOIQR(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
                !uri.Scheme.Equals("vinhkhanhtour", StringComparison.OrdinalIgnoreCase) ||
                !uri.Host.Equals("poi", StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlert("Mã không hợp lệ",
                    "Vui lòng quét mã QR trên bảng của từng quán.", "Thử lại");
                _isProcessing = false;
                if (_cameraView != null) _cameraView.IsDetecting = true;
                return;
            }

            if (!int.TryParse(uri.AbsolutePath.Trim('/'), out int poiId))
            {
                _isProcessing = false; return;
            }

            var q = System.Web.HttpUtility.ParseQueryString(uri.Query);
            bool autoplay = string.Equals(q["autoplay"], "true", StringComparison.OrdinalIgnoreCase);

            var restaurant = _restaurants.FirstOrDefault(r => r.Id == poiId)
                          ?? await App.Database.GetRestaurantByIdAsync(poiId);

            if (restaurant == null)
            {
                await DisplayAlert("Không tìm thấy",
                    "Quán này chưa có trong dữ liệu.", "OK");
                _isProcessing = false;
                return;
            }

            await FlashAccent();
            await Navigation.PushAsync(
                new RestaurantDetailPage(restaurant, autoplayAudio: autoplay));
        }

        private async Task FlashAccent()
        {
            if (_rootGrid == null) return;
            var flash = new BoxView
            {
                Color = Color.FromArgb("#4A8A56"),
                Opacity = 0,
                ZIndex = 99
            };
            Grid.SetRowSpan(flash, 3);
            _rootGrid.Add(flash);
            await flash.FadeTo(0.3, 70);
            await flash.FadeTo(0, 250);
            _rootGrid.Remove(flash);
        }

        // ── Build UI ──────────────────────────────────────────────
        private void BuildUI()
        {
            _rootGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
                }
            };

            // Header
            var header = new Grid
            {
                BackgroundColor = Color.FromArgb("#080D0A"),
                Padding = new Thickness(24, 54, 24, 18),
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star }
                }
            };
            var back = new Label
            {
                Text = "←",
                FontSize = 22,
                TextColor = Colors.White,
                Padding = new Thickness(0, 0, 20, 0),
                VerticalOptions = LayoutOptions.Center
            };
            back.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () => await Navigation.PopAsync())
            });
            header.Add(back, 0, 0);

            var hInfo = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center };
            hInfo.Add(new Label
            {
                Text = "AUDIO GUIDE",
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                CharacterSpacing = 2
            });
            hInfo.Add(new Label
            {
                Text = "Quét mã QR tại bảng của mỗi quán",
                FontSize = 11,
                TextColor = Color.FromArgb("#2E4A35")
            });
            header.Add(hInfo, 1, 0);
            Grid.SetRow(header, 0);
            _rootGrid.Add(header);

            // Tab bar
            var tabs = new Grid
            {
                BackgroundColor = Color.FromArgb("#080D0A"),
                Padding = new Thickness(24, 0, 24, 14),
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star)
                },
                ColumnSpacing = 8
            };

            _scanTabBtn = MakeTab("QUÉT QR", true);
            _listTabBtn = MakeTab("DANH SÁCH", false);
            _scanTabBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => SwitchTab(true)) });
            _listTabBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => SwitchTab(false)) });
            tabs.Add(_scanTabBtn, 0, 0);
            tabs.Add(_listTabBtn, 1, 0);
            Grid.SetRow(tabs, 1);
            _rootGrid.Add(tabs);

            // Content
            var content = new Grid();

            _scanPanel = BuildScanPanel();
            _scanPanel.IsVisible = true;
            content.Add(_scanPanel);

            _listPanel = BuildListPanel();
            _listPanel.IsVisible = false;
            content.Add(_listPanel);

            Grid.SetRow(content, 2);
            _rootGrid.Add(content);
            Content = _rootGrid;
        }

        private Grid BuildScanPanel()
        {
            var panel = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = new GridLength(300) },
                    new RowDefinition { Height = GridLength.Auto }
                }
            };

            var cam = new Grid { BackgroundColor = Color.FromArgb("#050A07") };
            _cameraView = new CameraBarcodeReaderView
            {
                Options = new BarcodeReaderOptions { Formats = BarcodeFormat.QrCode, AutoRotate = true, Multiple = false },
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                IsDetecting = true,
            };
            _cameraView.BarcodesDetected += OnQRDetected;
            cam.Add(_cameraView);
            cam.Add(BuildCameraOverlay());
            Grid.SetRow(cam, 0);
            panel.Add(cam);

            var bottom = new VerticalStackLayout
            {
                Padding = new Thickness(24, 16, 24, 28),
                Spacing = 14,
                BackgroundColor = Color.FromArgb("#080D0A")
            };

            bottom.Add(new Label
            {
                Text = "Hướng camera vào mã QR trên bảng của quán.\nThuyết minh sẽ tự động phát khi nhận diện được.",
                FontSize = 13,
                TextColor = Color.FromArgb("#2E4A35"),
                HorizontalTextAlignment = TextAlignment.Center,
                LineHeight = 1.6
            });

            var rescan = new Border
            {
                BackgroundColor = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 2 },
                StrokeThickness = 1,
                Stroke = Color.FromArgb("#1A2E1E"),
                HeightRequest = 46,
                HorizontalOptions = LayoutOptions.Fill,
                Content = new Label
                {
                    Text = "QUÉT LẠI",
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#2E4A35"),
                    CharacterSpacing = 1.5,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            };
            rescan.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() =>
                {
                    _isProcessing = false;
                    if (_cameraView != null) _cameraView.IsDetecting = true;
                })
            });
            bottom.Add(rescan);

            Grid.SetRow(bottom, 1);
            panel.Add(bottom);
            return panel;
        }

        private Grid BuildListPanel()
        {
            var panel = new Grid
            {
                RowDefinitions = { new RowDefinition { Height = new GridLength(1, GridUnitType.Star) } }
            };

            var scroll = new ScrollView { VerticalScrollBarVisibility = ScrollBarVisibility.Never };

            _poiListStack = new VerticalStackLayout
            {
                Padding = new Thickness(24, 20, 24, 40),
                Spacing = 0
            };

            _loadingLabel = new Label
            {
                Text = "Đang tải...",
                FontSize = 13,
                TextColor = Color.FromArgb("#2E4A35"),
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 40, 0, 0)
            };
            _poiListStack.Add(_loadingLabel);

            scroll.Content = _poiListStack;
            panel.Add(scroll);
            return panel;
        }

        private View BuildCameraOverlay()
        {
            var overlay = new Grid { HorizontalOptions = LayoutOptions.Fill, VerticalOptions = LayoutOptions.Fill };
            var frame = new Grid
            {
                WidthRequest = 210,
                HeightRequest = 210,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            var accent = Color.FromArgb("#4A8A56");
            foreach (var (h, v) in new[]
            {
                (LayoutOptions.Start, LayoutOptions.Start),
                (LayoutOptions.End,   LayoutOptions.Start),
                (LayoutOptions.Start, LayoutOptions.End),
                (LayoutOptions.End,   LayoutOptions.End)
            })
            {
                frame.Add(new BoxView { Color = accent, WidthRequest = 24, HeightRequest = 2, HorizontalOptions = h, VerticalOptions = v });
                frame.Add(new BoxView { Color = accent, WidthRequest = 2, HeightRequest = 24, HorizontalOptions = h, VerticalOptions = v });
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
                    await line.TranslateTo(0, 206, 1800, Easing.SinInOut);
                    await line.TranslateTo(0, 0, 1800, Easing.SinInOut);
                    await Task.Delay(80);
                }
                catch { break; }
            }
        }

        private void SwitchTab(bool scan)
        {
            _isScanTab = scan;
            UpdateTabStyle(_scanTabBtn, scan);
            UpdateTabStyle(_listTabBtn, !scan);
            if (_scanPanel != null) _scanPanel.IsVisible = scan;
            if (_listPanel != null) _listPanel.IsVisible = !scan;
            if (_cameraView != null) _cameraView.IsDetecting = scan && !_isProcessing;
        }

        private static void UpdateTabStyle(Border? btn, bool active)
        {
            if (btn == null) return;
            btn.BackgroundColor = active ? Color.FromArgb("#4A8A56") : Colors.Transparent;
            btn.StrokeThickness = active ? 0 : 1;
            if (btn.Content is Label lbl)
            {
                lbl.TextColor = active ? Colors.White : Color.FromArgb("#2E4A35");
                lbl.FontAttributes = active ? FontAttributes.Bold : FontAttributes.None;
            }
        }

        private static Border MakeTab(string text, bool active) => new Border
        {
            BackgroundColor = active ? Color.FromArgb("#4A8A56") : Colors.Transparent,
            StrokeThickness = active ? 0 : 1,
            Stroke = Color.FromArgb("#1A2E1E"),
            StrokeShape = new RoundRectangle { CornerRadius = 2 },
            Padding = new Thickness(0, 11),
            Content = new Label
            {
                Text = text,
                FontSize = 11,
                FontAttributes = active ? FontAttributes.Bold : FontAttributes.None,
                TextColor = active ? Colors.White : Color.FromArgb("#2E4A35"),
                CharacterSpacing = 1.5,
                HorizontalOptions = LayoutOptions.Center
            }
        };
    }
}
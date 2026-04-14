using Microsoft.Maui.Controls.Shapes;
using VinhKhanhTour.Models;
using VinhKhanhTour.Services;

namespace VinhKhanhTour.Views
{
    /// <summary>
    /// Trang thanh toán ví điện tử — hiển thị QR / deeplink, chờ người dùng xác nhận.
    ///
    /// LUỒNG:
    ///   1. Hiển thị thông tin thanh toán + nút "Mở [Ví]" (deeplink)
    ///   2. Người dùng bấm "Tôi đã thanh toán" → gọi ConfirmEWalletPaymentAsync → sang ConfirmationPage
    ///   3. Người dùng bấm "Huỷ" → xoá booking → về trang trước
    /// </summary>
    public class EWalletPaymentPage : ContentPage
    {
        private readonly Booking _booking;
        private readonly EWalletPaymentInfo _info;
        private readonly string _lang;

        private bool _isProcessing;

        public EWalletPaymentPage(Booking booking, EWalletPaymentInfo info)
        {
            _booking = booking;
            _info = info;
            _lang = Preferences.Default.Get("app_lang", "vi");
            BackgroundColor = Color.FromArgb("#F8FAFC");
            NavigationPage.SetHasNavigationBar(this, false);
            BuildUI();
        }

        private void BuildUI()
        {
            var scroll = new ScrollView { VerticalScrollBarVisibility = ScrollBarVisibility.Never };
            var root = new VerticalStackLayout
            {
                Padding = new Thickness(24, 56, 24, 40),
                Spacing = 20
            };

            // ── Back button ──────────────────────────────────────
            var backRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star)
                }
            };
            var backBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#F1F5F9"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                HeightRequest = 40,
                WidthRequest = 40,
                Content = new Label
                {
                    Text = "←",
                    FontSize = 18,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    TextColor = Color.FromArgb("#1565C0")
                }
            };
            backBtn.GestureRecognizers.Add(new TapGestureRecognizer
            { Command = new Command(async () => await OnCancelAsync()) });
            backRow.Add(backBtn, 0, 0);
            root.Add(backRow);

            // ── Wallet Icon + Title ───────────────────────────────
            root.Add(new Label
            {
                Text = _info.Icon,
                FontSize = 56,
                HorizontalOptions = LayoutOptions.Center
            });
            root.Add(new Label
            {
                Text = L($"Thanh toán qua {_info.WalletName}",
                                      $"Pay via {_info.WalletName}",
                                      $"通过{_info.WalletName}付款"),
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#0D2137"),
                HorizontalOptions = LayoutOptions.Center
            });

            // ── Số tiền ───────────────────────────────────────────
            root.Add(new Border
            {
                BackgroundColor = Color.FromArgb(HexToArgb(_info.Color, "20")),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = new Thickness(24, 16),
                Content = new VerticalStackLayout
                {
                    Spacing = 4,
                    Children =
                    {
                        new Label
                        {
                            Text              = L("Số tiền cọc", "Deposit amount", "押金金额"),
                            FontSize          = 13,
                            TextColor         = Color.FromArgb("#64748B"),
                            HorizontalOptions = LayoutOptions.Center
                        },
                        new Label
                        {
                            Text              = $"{_info.Amount:N0}đ",
                            FontSize          = 36,
                            FontAttributes    = FontAttributes.Bold,
                            TextColor         = Color.FromArgb(_info.Color),
                            HorizontalOptions = LayoutOptions.Center
                        },
                        new Label
                        {
                            Text              = L("(số dư sẽ trả tại quán)",
                                                  "(remainder paid at venue)",
                                                  "（余款到店支付）"),
                            FontSize          = 12,
                            TextColor         = Color.FromArgb("#94A3B8"),
                            HorizontalOptions = LayoutOptions.Center
                        }
                    }
                }
            });

            // ── QR giả / hướng dẫn ───────────────────────────────
            root.Add(BuildQrCard());

            // ── Mã đặt chỗ ───────────────────────────────────────
            root.Add(new Border
            {
                BackgroundColor = Colors.White,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Padding = new Thickness(16, 12),
                Shadow = new Shadow
                {
                    Brush = Color.FromArgb("#000"),
                    Opacity = 0.05f,
                    Radius = 6
                },
                Content = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(GridLength.Auto)
                    },
                    Children =
                    {
                        new VerticalStackLayout
                        {
                            Children =
                            {
                                new Label
                                {
                                    Text      = L("Mã đặt chỗ", "Booking code", "预约码"),
                                    FontSize  = 12,
                                    TextColor = Color.FromArgb("#64748B")
                                },
                                new Label
                                {
                                    Text           = _booking.BookingCode,
                                    FontSize       = 18,
                                    FontAttributes = FontAttributes.Bold,
                                    TextColor      = Color.FromArgb("#1565C0")
                                }
                            }
                        },
                        BuildInfoChip($"{_booking.BookingDate} {_booking.BookingTime}")
                    }
                }.Also(g => Grid.SetColumn(g.Children[1] as View ?? new Label(), 1))
            });

            // Hướng dẫn thêm
            root.Add(new Label
            {
                Text = _info.Instruction,
                FontSize = 13,
                TextColor = Color.FromArgb("#64748B"),
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                LineHeight = 1.4
            });

            scroll.Content = root;

            // ── Buttons ───────────────────────────────────────────
            var btnStack = new VerticalStackLayout
            {
                Padding = new Thickness(24, 12, 24, 36),
                Spacing = 10
            };

            // Nút mở ví
            var openWalletBtn = CreateColorButton(
                L($"Mở {_info.WalletName}", $"Open {_info.WalletName}", $"打开{_info.WalletName}"),
                _info.Color,
                "#FFFFFF",
                async () => await OnOpenWalletAsync()
            );
            btnStack.Add(openWalletBtn);

            // Nút đã thanh toán
            var confirmedBtn = CreateColorButton(
                L("✅  Tôi đã thanh toán xong", "✅  I've completed payment", "✅  我已完成付款"),
                "#27AE60",
                "#FFFFFF",
                async () => await OnPaymentConfirmedAsync()
            );
            btnStack.Add(confirmedBtn);

            // Nút huỷ
            var cancelBtn = CreateColorButton(
                L("Huỷ đặt chỗ", "Cancel booking", "取消预约"),
                "#F1F5F9",
                "#E53E3E",
                async () => await OnCancelAsync()
            );
            btnStack.Add(cancelBtn);

            var mainGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Star),
                    new RowDefinition(GridLength.Auto)
                }
            };
            mainGrid.Add(scroll, 0, 0);
            mainGrid.Add(new Border
            {
                BackgroundColor = Colors.White,
                StrokeThickness = 0,
                Content = btnStack
            }, 0, 1);

            Content = mainGrid;
        }

        private View BuildQrCard()
        {
            // QR thật — dùng Google Charts API (miễn phí)
            var qrContent = Uri.EscapeDataString(_info.QrContent ?? $"VinhKhanhTour-{_booking.BookingCode}");
            var qrUrl = $"https://chart.googleapis.com/chart?chs=200x200&cht=qr&chl={qrContent}&choe=UTF-8&chld=M|2";

            var qrImage = new Image
            {
                Source = new UriImageSource
                {
                    Uri = new Uri(qrUrl),
                    CachingEnabled = true,
                    CacheValidity = TimeSpan.FromHours(24)
                },
                HeightRequest = 180,
                WidthRequest = 180,
                HorizontalOptions = LayoutOptions.Center
            };

            return new Border
            {
                BackgroundColor = Colors.White,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = new Thickness(20),
                Shadow = new Shadow
                {
                    Brush = Color.FromArgb("#000"),
                    Opacity = 0.06f,
                    Radius = 10
                },
                Content = new VerticalStackLayout
                {
                    Spacing = 12,
                    HorizontalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label
                        {
                            Text = L("Quét mã để thanh toán", "Scan to pay", "扫码付款"),
                            FontSize = 14,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = Color.FromArgb("#475569"),
                            HorizontalOptions = LayoutOptions.Center
                        },
                        new Border
                        {
                            BackgroundColor = Colors.White,
                            Stroke = Color.FromArgb("#E2E8F0"),
                            StrokeThickness = 1,
                            StrokeShape = new RoundRectangle { CornerRadius = 8 },
                            Padding = new Thickness(8),
                            Content = qrImage
                        },
                        new Label
                        {
                            Text = _booking.BookingCode,
                            FontSize = 12,
                            TextColor = Color.FromArgb("#94A3B8"),
                            FontAttributes = FontAttributes.Bold,
                            HorizontalOptions = LayoutOptions.Center,
                            CharacterSpacing = 1
                        }
                    }
                }
            };
        }

        private View BuildInfoChip(string text)
        {
            return new Border
            {
                BackgroundColor = Color.FromArgb("#F1F5F9"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                Padding = new Thickness(10, 6),
                VerticalOptions = LayoutOptions.Center,
                Content = new Label
                {
                    Text = text,
                    FontSize = 12,
                    TextColor = Color.FromArgb("#475569")
                }
            };
        }

        // ── Actions ───────────────────────────────────────────────────────────

        private async Task OnOpenWalletAsync()
        {
            try
            {
                var canOpen = await Launcher.CanOpenAsync(_info.DeepLink);
                if (canOpen)
                    await Launcher.OpenAsync(_info.DeepLink);
                else
                    await DisplayAlert(
                        L($"Chưa cài {_info.WalletName}",
                          $"{_info.WalletName} not installed",
                          $"未安装{_info.WalletName}"),
                        L($"Vui lòng cài app {_info.WalletName} để thanh toán, hoặc bấm 'Đã thanh toán' sau khi chuyển khoản.",
                          $"Please install {_info.WalletName} to pay, or tap 'Completed' after transferring.",
                          $"请安装{_info.WalletName}进行支付，或转账后点击'已完成'。"),
                        "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EWalletPage] OpenWallet: {ex.Message}");
                await DisplayAlert("Lỗi", ex.Message, "OK");
            }
        }

        private async Task OnPaymentConfirmedAsync()
        {
            if (_isProcessing) return;
            _isProcessing = true;

            var confirmed = await DisplayAlert(
                L("Xác nhận thanh toán", "Confirm payment", "确认付款"),
                L($"Bạn đã chuyển {_info.Amount:N0}đ qua {_info.WalletName}?",
                  $"Have you transferred {_info.Amount:N0}đ via {_info.WalletName}?",
                  $"您已通过{_info.WalletName}转账{_info.Amount:N0}đ？"),
                L("Đã thanh toán", "Yes, paid", "已付款"),
                L("Chưa", "Not yet", "还没"));

            if (!confirmed)
            {
                _isProcessing = false;
                return;
            }

            var result = await PaymentService.Instance.ConfirmEWalletPaymentAsync(_booking);

            await Navigation.PushAsync(new BookingConfirmationPage(_booking));
            // Xoá trang này khỏi stack
            var stack = Navigation.NavigationStack.ToList();
            if (stack.Count >= 2)
                Navigation.RemovePage(stack[^2]);
        }

        private async Task OnCancelAsync()
        {
            if (_isProcessing) return;

            var confirm = await DisplayAlert(
                L("Huỷ đặt chỗ?", "Cancel booking?", "取消预约？"),
                L("Đặt chỗ chưa được xác nhận. Bạn có chắc muốn huỷ?",
                  "Booking is not yet confirmed. Cancel?",
                  "预约尚未确认，确定取消？"),
                L("Huỷ đặt chỗ", "Yes, cancel", "确定取消"),
                L("Tiếp tục thanh toán", "Keep paying", "继续付款"));

            if (!confirm) return;

            await PaymentService.Instance.CancelPendingBookingAsync(_booking.Id);
            await Navigation.PopAsync();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private string L(string vi, string en, string zh) => _lang switch
        {
            "en" => en,
            "zh" => zh,
            _ => vi
        };

        /// <summary>Tạo button màu tùy chỉnh.</summary>
        private Border CreateColorButton(string text, string bgHex, string fgHex, Func<Task> action)
        {
            Color bg, fg;
            try { bg = Color.FromArgb(bgHex); } catch { bg = Colors.Gray; }
            try { fg = Color.FromArgb(fgHex); } catch { fg = Colors.White; }

            var btn = new Border
            {
                BackgroundColor = bg,
                HeightRequest = 52,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Content = new Label
                {
                    Text = text,
                    TextColor = fg,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 14,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            };
            btn.GestureRecognizers.Add(new TapGestureRecognizer
            { Command = new Command(async () => await action()) });
            return btn;
        }

        /// <summary>Tạo màu Argb với alpha từ hex màu + chuỗi alpha hex.</summary>
        private static string HexToArgb(string hex, string alpha)
            => $"#{alpha}{hex.TrimStart('#')}";
    }

    // Re-export extension nếu chưa có file khác define
    // (xoá nếu bị duplicate)
    // public static class ViewExtensions { ... }
}
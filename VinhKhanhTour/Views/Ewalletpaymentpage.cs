using Microsoft.Maui.Controls.Shapes;
using VinhKhanhTour.Models;
using VinhKhanhTour.Services;
using System.Threading.Tasks;
using Microsoft.Maui.Dispatching;

namespace VinhKhanhTour.Views
{
    public class EWalletPaymentPage : ContentPage
    {
        private readonly Booking _booking;
        private readonly EWalletPaymentInfo _info;
        private readonly string _lang;

        private bool _isProcessing;
        private IDispatcherTimer _timer = null!;
        private TimeSpan _timeLeft = TimeSpan.FromMinutes(15); // 15 phút đếm ngược
        private Label _timerLabel = null!;

        // ── Overlay Loading / Kết quả ──
        private Grid _overlayGrid = null!;
        private Label _overlayIcon = null!;
        private Label _overlayTitle = null!;
        private Label _overlayMessage = null!;
        private Border _overlayButton = null!;
        private ActivityIndicator _overlaySpinner = null!;
        private VerticalStackLayout _overlayCenterStack = null!;

        public EWalletPaymentPage(Booking booking, EWalletPaymentInfo info)
        {
            _booking = booking;
            _info = info;
            _lang = Preferences.Default.Get("app_lang", "vi");
            BackgroundColor = Color.FromArgb("#F8FAFC");
            NavigationPage.SetHasNavigationBar(this, false);
            BuildUI();
            StartTimer();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (_timer != null && _timer.IsRunning)
            {
                _timer.Stop();
            }
        }

        private void StartTimer()
        {
            _timer = Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) =>
            {
                if (_timeLeft.TotalSeconds > 0)
                {
                    _timeLeft = _timeLeft.Subtract(TimeSpan.FromSeconds(1));
                    if (_timerLabel != null)
                        _timerLabel.Text = _timeLeft.ToString(@"mm\:ss");
                }
                else
                {
                    _timer.Stop();
                    ShowErrorDisplay("⏳", L("Hết hạn thanh toán", "Payment Expired", "付款超时"),
                                           L("Giao dịch đã quá hạn 15 phút. Vui lòng tạo lại đơn hàng.", "Transaction timed out. Please order again.", "交易已超时，请重新下单。"));
                }
            };
            _timer.Start();
        }

        private void BuildUI()
        {
            var rootGrid = new Grid();

            // ==========================================
            // LỚP 1: GIAO DIỆN CHÍNH (NẰM DƯỚI)
            // ==========================================
            var scroll = new ScrollView { VerticalScrollBarVisibility = ScrollBarVisibility.Never };
            var contentStack = new VerticalStackLayout
            {
                Padding = new Thickness(24, 56, 24, 40),
                Spacing = 20
            };

            // 1. Nút Back
            var backBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#F1F5F9"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                HeightRequest = 40,
                WidthRequest = 40,
                HorizontalOptions = LayoutOptions.Start,
                Content = new Label
                {
                    Text = "←",
                    FontSize = 18,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    TextColor = Color.FromArgb("#1565C0")
                }
            };
            backBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await OnCancelAsync()) });
            contentStack.Add(backBtn);

            // 2. Header Ví điện tử
            contentStack.Add(new Label
            {
                Text = _info.Icon,
                FontSize = 56,
                HorizontalOptions = LayoutOptions.Center
            });
            contentStack.Add(new Label
            {
                Text = L($"Thanh toán qua {_info.WalletName}", $"Pay via {_info.WalletName}", $"通过{_info.WalletName}付款"),
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#0D2137"),
                HorizontalOptions = LayoutOptions.Center
            });

            // 3. Số tiền
            contentStack.Add(new Border
            {
                BackgroundColor = Color.FromArgb(HexToArgb(_info.Color, "15")),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = new Thickness(24, 16),
                Content = new VerticalStackLayout
                {
                    Spacing = 4,
                    Children =
                    {
                        new Label { Text = L("Số tiền cần thanh toán", "Amount to pay", "支付金额"), FontSize = 13, TextColor = Color.FromArgb("#64748B"), HorizontalOptions = LayoutOptions.Center },
                        new Label { Text = $"{_info.Amount:N0}đ", FontSize = 36, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb(_info.Color), HorizontalOptions = LayoutOptions.Center },
                        new Label { Text = _booking.BookingCode, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#475569"), HorizontalOptions = LayoutOptions.Center }
                    }
                }
            });

            // 4. Khung QR Code & Đếm ngược
            var qrBorder = new Border
            {
                BackgroundColor = Colors.White,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Padding = new Thickness(20),
                Shadow = new Shadow { Brush = Color.FromArgb("#000"), Opacity = 0.08f, Radius = 15, Offset = new Point(0, 5) }
            };
            var qrStack = new VerticalStackLayout { Spacing = 16, HorizontalOptions = LayoutOptions.Center };
            
            // Timer
            _timerLabel = new Label
            {
                Text = "15:00",
                FontSize = 28,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#E53E3E"), // Red
                HorizontalOptions = LayoutOptions.Center
            };
            qrStack.Add(new VerticalStackLayout {
                Spacing = 2,
                Children = {
                    new Label { Text = L("Giao dịch hết hạn sau", "Expires in", "交易过期倒计时"), FontSize = 13, TextColor = Color.FromArgb("#64748B"), HorizontalOptions = LayoutOptions.Center },
                    _timerLabel
                }
            });

            // QR Image
            var qrContent = Uri.EscapeDataString(_info.QrContent ?? $"VinhKhanhTour-{_booking.BookingCode}");
            var qrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=220x220&data={qrContent}&margin=10&bgcolor=fff&color=000";
            qrStack.Add(new Border
            {
                BackgroundColor = Colors.White,
                Stroke = Color.FromArgb(_info.Color),
                StrokeThickness = 2,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Padding = new Thickness(10),
                Content = new Image { Source = new UriImageSource { Uri = new Uri(qrUrl) }, HeightRequest = 200, WidthRequest = 200 }
            });

            qrStack.Add(new Label { Text = _info.Instruction, FontSize = 13, TextColor = Color.FromArgb("#64748B"), HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center });
            
            qrBorder.Content = qrStack;
            contentStack.Add(qrBorder);

            // 5. Khu vực Demo (Dành cho Giảng Viên)
            contentStack.Add(BuildDemoSection());

            scroll.Content = contentStack;
            rootGrid.Add(scroll);


            // ==========================================
            // LỚP 2: OVERLAY KẾT QUẢ/LOADING (NẰM TRÊN CÙNG)
            // ==========================================
            _overlayGrid = new Grid { BackgroundColor = Color.FromArgb("#E6FFFFFF"), IsVisible = false };
            var overlayInnerContent = new Border
            {
                BackgroundColor = Colors.White,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 24 },
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                WidthRequest = 320,
                Padding = new Thickness(24, 32),
                Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.15f, Radius = 20, Offset = new Point(0, 10) }
            };

            _overlaySpinner = new ActivityIndicator { IsRunning = true, Color = Color.FromArgb(_info.Color), HorizontalOptions = LayoutOptions.Center, WidthRequest = 50, HeightRequest = 50 };
            _overlayIcon = new Label { FontSize = 64, HorizontalOptions = LayoutOptions.Center, IsVisible = false };
            _overlayTitle = new Label { FontSize = 20, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center, TextColor = Color.FromArgb("#0D2137"), Margin = new Thickness(0, 16, 0, 8), HorizontalTextAlignment = TextAlignment.Center };
            _overlayMessage = new Label { FontSize = 14, TextColor = Color.FromArgb("#64748B"), HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 24) };
            
            _overlayButton = new Border
            {
                BackgroundColor = Color.FromArgb("#1565C0"),
                HeightRequest = 48,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                IsVisible = false
            };
            // Default handler is set dynamically
            
            _overlayCenterStack = new VerticalStackLayout
            {
                Children = { _overlaySpinner, _overlayIcon, _overlayTitle, _overlayMessage, _overlayButton }
            };
            overlayInnerContent.Content = _overlayCenterStack;
            _overlayGrid.Add(overlayInnerContent);

            rootGrid.Add(_overlayGrid);

            Content = rootGrid;
        }

        // ── Kịch Bản Demo (Giả lập IPN Webhook) ──
        private View BuildDemoSection()
        {
            var container = new Border
            {
                BackgroundColor = Color.FromArgb("#FFF8E1"),
                Stroke = Color.FromArgb("#FFC107"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = new Thickness(16),
                Margin = new Thickness(0, 20, 0, 0)
            };

            var stack = new VerticalStackLayout { Spacing = 12 };
            stack.Add(new Label
            {
                Text = "🛠️ THANH TOÁN MÔ PHỎNG",
                FontAttributes = FontAttributes.Bold,
                FontSize = 12,
                TextColor = Color.FromArgb("#E65100"),
                HorizontalOptions = LayoutOptions.Center
            });

            // Btn 1: Success
            stack.Add(CreateDemoBtn("✅ Khách quét QR & Nhập OTP Thành Công", "#E8F5E9", "#2E7D32", async () => await SimulateScenario("success")));

            // Btn 2: Insufficient Funds
            stack.Add(CreateDemoBtn("❌ Lỗi: Tài khoản không đủ số dư", "#FFEBEE", "#C62828", async () => await SimulateScenario("insufficient_funds")));

            // Btn 3: User Cancelled
            stack.Add(CreateDemoBtn("🛑 Khách huỷ giao dịch ngang chừng", "#F3E5F5", "#6A1B9A", async () => await SimulateScenario("user_cancelled")));

            container.Content = stack;
            return container;
        }

        private Border CreateDemoBtn(string text, string bgHex, string textHex, Func<Task> action)
        {
            var btn = new Border
            {
                BackgroundColor = Color.FromArgb(bgHex),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                Padding = new Thickness(0, 12),
                Content = new Label { Text = text, TextColor = Color.FromArgb(textHex), FontSize = 13, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center }
            };
            btn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await action()) });
            return btn;
        }

        private async Task SimulateScenario(string scenario)
        {
            if (_isProcessing) return;
            _isProcessing = true;
            _timer?.Stop();

            // Hiện màn hình loading "Đang kết nối tới ngân hàng..."
            _overlayGrid.IsVisible = true;
            _overlaySpinner.IsVisible = true;
            _overlayIcon.IsVisible = false;
            _overlayButton.IsVisible = false;
            _overlayTitle.Text = L("Đang kiểm tra...", "Processing...", "处理中...");
            _overlayMessage.Text = L("Đang chờ phản hồi từ hệ thống ngân hàng", "Waiting for bank response", "等待银行响应");

            // Delay giả vờ gọi API
            await Task.Delay(2500);

            _overlaySpinner.IsVisible = false;
            _overlayIcon.IsVisible = true;
            _overlayButton.IsVisible = true;

            switch (scenario)
            {
                case "success":
                    _overlayIcon.Text = "🎉";
                    _overlayTitle.Text = L("Thanh toán thành công!", "Payment Successful!", "支付成功！");
                    _overlayTitle.TextColor = Color.FromArgb("#2E7D32");
                    _overlayMessage.Text = L($"Bạn đã thanh toán cọc {_info.Amount:N0}đ.", $"You have paid the {_info.Amount:N0}đ deposit.", $"您已支付{_info.Amount:N0}đ押金。");
                    SetupOverlayBtn(L("Xem Biên Lai", "View Receipt", "看收据"), Color.FromArgb("#2E7D32"), async () => {
                        await PaymentService.Instance.ConfirmEWalletPaymentAsync(_booking);
                        await Navigation.PushAsync(new BookingConfirmationPage(_booking));
                        RemovePageFromStack();
                    });
                    break;

                case "insufficient_funds":
                    _overlayIcon.Text = "💸";
                    _overlayTitle.Text = L("Giao dịch Thất bại", "Transaction Failed", "交易失败");
                    _overlayTitle.TextColor = Color.FromArgb("#C62828");
                    _overlayMessage.Text = L("Tài khoản của quý khách không đủ số dư để thực hiện giao dịch này.", "Insufficient funds to complete this transaction.", "您的账户余额不足以完成此交易。");
                    SetupOverlayBtn(L("Thử ví khác", "Try another wallet", "重试"), Color.FromArgb("#C62828"), () => {
                        ResetForm();
                        return Task.CompletedTask;
                    });
                    break;

                case "user_cancelled":
                    _overlayIcon.Text = "🛑";
                    _overlayTitle.Text = L("Đã huỷ thanh toán", "Payment Cancelled", "支付已取消");
                    _overlayTitle.TextColor = Color.FromArgb("#6A1B9A");
                    _overlayMessage.Text = L("Khách hàng đã từ chối xác thực vân tay/OTP.", "User cancelled the payment process.", "用户已取消支付过程。");
                    SetupOverlayBtn(L("Quay lại", "Go Back", "返回"), Color.FromArgb("#6A1B9A"), async () => await OnCancelAsync());
                    break;
            }
        }

        private void ShowErrorDisplay(string icon, string title, string msg)
        {
            _overlayGrid.IsVisible = true;
            _overlaySpinner.IsVisible = false;
            _overlayIcon.IsVisible = true;
            _overlayButton.IsVisible = true;

            _overlayIcon.Text = icon;
            _overlayTitle.Text = title;
            _overlayTitle.TextColor = Color.FromArgb("#E65100");
            _overlayMessage.Text = msg;

            SetupOverlayBtn(L("Huỷ và Quay Về", "Cancel & Go Back", "取消并返回"), Color.FromArgb("#E65100"), async () => await OnCancelAsync());
        }

        private void SetupOverlayBtn(string text, Color bg, Func<Task> action)
        {
            _overlayButton.BackgroundColor = bg;
            _overlayButton.Content = new Label 
            { 
                Text = text, TextColor = Colors.White, FontAttributes = FontAttributes.Bold, 
                HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center 
            };
            
            _overlayButton.GestureRecognizers.Clear();
            _overlayButton.GestureRecognizers.Add(new TapGestureRecognizer 
            { 
                Command = new Command(async () => await action()) 
            });
        }

        private void ResetForm()
        {
            _isProcessing = false;
            _overlayGrid.IsVisible = false;
            if (_timeLeft.TotalSeconds > 0)
                _timer.Start();
        }

        private async Task OnCancelAsync()
        {
            _timer?.Stop();
            var confirm = await DisplayAlert(
                L("Huỷ đặt chỗ?", "Cancel booking?", "取消预约？"),
                L("Tiến trình thanh toán sẽ bị chấm dứt. Bạn có chắc muốn huỷ?",
                  "The payment process will be terminated. Cancel?",
                  "支付将被终止，确定取消？"),
                L("Đồng ý Huỷ", "Yes, cancel", "确定取消"),
                L("Không", "No", "否"));

            if (!confirm)
            {
                if (_timeLeft.TotalSeconds > 0) _timer.Start();
                return;
            }

            await PaymentService.Instance.CancelPendingBookingAsync(_booking.Id);
            await Navigation.PopAsync();
        }

        private void RemovePageFromStack()
        {
            var stack = Navigation.NavigationStack.ToList();
            if (stack.Count >= 2)
                Navigation.RemovePage(stack[^2]); // Remove EWalletPaymentPage itself
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private string L(string vi, string en, string zh) => _lang switch { "en" => en, "zh" => zh, _ => vi };
        private static string HexToArgb(string hex, string alpha) => $"#{alpha}{hex.TrimStart('#')}";
    }
}
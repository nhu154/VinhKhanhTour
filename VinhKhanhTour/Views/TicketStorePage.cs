using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;
using VinhKhanhTour.Services;

namespace VinhKhanhTour.Views
{
    /// <summary>
    /// Trang mua vé — chọn loại vé + phương thức thanh toán QR.
    /// Sau khi xác nhận → chuyển sang TicketSuccessPage.
    /// </summary>
    public class TicketStorePage : ContentPage
    {
        private readonly string _lang;
        private string _selectedTicket = "day"; // day | full
        private string _selectedPayment = "momo";
        private Border _cardDay = null!;
        private Border _cardFull = null!;
        private readonly Dictionary<string, Border> _payCards = new();
        private Label _summaryLabel = null!;

        // ── Payment method config ─────────────────────────────────────────────
        private static readonly Dictionary<string, (string emoji, string labelVi, string labelEn, string color, string accent, string descVi, string descEn)> PayMethods = new()
        {
            ["momo"]    = ("🟣", "MoMo",          "MoMo",          "#A21094", "#F8E8F9", "Quét QR qua app MoMo",         "Scan QR via MoMo app"),
            ["vnpay"]   = ("💳", "VNPay",          "VNPay",         "#C0392B", "#FFF0F0", "Quét QR qua app VNPay",        "Scan QR via VNPay app"),
            ["zalopay"] = ("🔵", "ZaloPay",        "ZaloPay",       "#0068FF", "#E8F2FF", "Quét QR qua app ZaloPay",      "Scan QR via ZaloPay app"),
            ["bank"]    = ("🏦", "Chuyển khoản",   "Bank Transfer", "#1B7A3E", "#E8F5E9", "Chuyển khoản ngân hàng nội địa", "Direct bank transfer"),
        };

        // Demo bank info (thay bằng thông tin thật khi deploy)
        private const string BANK_ID      = "MB";
        private const string BANK_ACCOUNT = "0347123456";
        private const string BANK_NAME    = "VINH KHANH TOUR";

        public TicketStorePage()
        {
            _lang = Preferences.Default.Get("app_lang", "vi");
            BackgroundColor = Color.FromArgb("#0D1B2A");
            NavigationPage.SetHasNavigationBar(this, false);
            BuildUI();
        }

        // ══ BUILD UI ══════════════════════════════════════════════════════════
        private void BuildUI()
        {
            var scroll = new ScrollView { VerticalScrollBarVisibility = ScrollBarVisibility.Never };
            var root   = new VerticalStackLayout { Spacing = 0 };

            // ── Hero Header ────────────────────────────────────────────────────
            var hero = new Grid { HeightRequest = 220 };
            hero.Add(new BoxView
            {
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0), EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb("#1A237E"), 0),
                        new GradientStop(Color.FromArgb("#0D47A1"), 0.5f),
                        new GradientStop(Color.FromArgb("#1565C0"), 1)
                    }
                }
            });
            hero.Add(new Ellipse { Fill = Color.FromArgb("#15FFFFFF"), WidthRequest = 200, HeightRequest = 200, HorizontalOptions = LayoutOptions.End,   VerticalOptions = LayoutOptions.Start, TranslationX = 40,  TranslationY = -60 });
            hero.Add(new Ellipse { Fill = Color.FromArgb("#10FFFFFF"), WidthRequest = 120, HeightRequest = 120, HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.End,   TranslationX = -30, TranslationY = 30  });

            var backBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#25FFFFFF"), StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 22 },
                HeightRequest = 44, WidthRequest = 44,
                HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(20, 54, 0, 0),
                Content = new Label { Text = "←", TextColor = Colors.White, FontSize = 22, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
            };
            backBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });
            hero.Add(backBtn);

            var heroContent = new VerticalStackLayout { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, Spacing = 8 };
            heroContent.Add(new Label { Text = "🎫", FontSize = 48, HorizontalOptions = LayoutOptions.Center });
            heroContent.Add(new Label { Text = L("MUA VÉ TRẢI NGHIỆM", "BUY EXPERIENCE TICKET", "购买体验票"), FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center, CharacterSpacing = 1 });
            heroContent.Add(new Label { Text = L("Mở khoá tất cả tính năng premium", "Unlock all premium features", "解锁所有高级功能"), FontSize = 13, TextColor = Color.FromArgb("#90CAF9"), HorizontalOptions = LayoutOptions.Center });
            hero.Add(heroContent);
            root.Add(hero);

            // ── Content ────────────────────────────────────────────────────────
            var content = new VerticalStackLayout
            {
                BackgroundColor = Color.FromArgb("#F8FAFC"),
                Padding = new Thickness(20, 28, 20, 100),
                Spacing = 24
            };

            content.Add(BuildFeaturesSection());

            // Ticket cards
            content.Add(SectionTitle(L("🎟️  Chọn loại vé", "🎟️  Choose Ticket", "🎟️  选择票种")));
            var ticketRow = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
                ColumnSpacing = 14
            };
            _cardDay  = BuildTicketCard("day",  L("Vé 1 ngày",    "Day Pass",  "单日票"), "29.000đ", L("Tất cả tính năng\ntrong 24 giờ", "All features\nfor 24 hours", "所有功能\n24小时"),   "⏱️", false);
            _cardFull = BuildTicketCard("full", L("Vé trọn gói",  "Full Pass", "全票"),   "79.000đ", L("Tất cả tính năng\nvĩnh viễn + PDF", "All features\nforever + PDF", "所有功能\n永久+PDF"), "🏆", true);
            ticketRow.Add(_cardDay,  0, 0);
            ticketRow.Add(_cardFull, 1, 0);
            content.Add(ticketRow);

            // Payment method (radio list)
            content.Add(SectionTitle(L("💳  Phương thức thanh toán", "💳  Payment Method", "💳  支付方式")));
            content.Add(BuildPaymentSection());

            _summaryLabel = new Label
            {
                FontSize = 13, TextColor = Color.FromArgb("#475569"),
                HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center
            };
            content.Add(_summaryLabel);

            scroll.Content = root;
            root.Add(content);

            // ── Footer button ─────────────────────────────────────────────────
            var footer = new Border { BackgroundColor = Colors.White, StrokeThickness = 0, Padding = new Thickness(20, 14, 20, 40) };
            var buyBtn = new Border
            {
                HeightRequest = 58,
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0), EndPoint = new Point(1, 0),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb("#FF6F00"), 0),
                        new GradientStop(Color.FromArgb("#FFA000"), 1)
                    }
                },
                StrokeShape = new RoundRectangle { CornerRadius = 18 }, StrokeThickness = 0,
                Shadow = new Shadow { Brush = Color.FromArgb("#FF6F00"), Opacity = 0.4f, Radius = 16, Offset = new Point(0, 6) },
                Content = new Label
                {
                    Text = L("🛒  THANH TOÁN QR NGAY", "🛒  PAY WITH QR NOW", "🛒  立即扫码支付"),
                    TextColor = Colors.White, FontAttributes = FontAttributes.Bold, FontSize = 16, CharacterSpacing = 0.5,
                    HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
                }
            };
            buyBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await OnBuyAsync()) });
            footer.Content = buyBtn;

            var mainGrid = new Grid
            {
                RowDefinitions = { new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) },
                Children = { scroll, footer }
            };
            Grid.SetRow(footer, 1);
            Content = mainGrid;

            SelectTicket("day");
        }

        // ══ FEATURES SECTION ══════════════════════════════════════════════════
        private View BuildFeaturesSection()
        {
            var card = new Border
            {
                BackgroundColor = Color.FromArgb("#EFF6FF"), StrokeThickness = 1.5,
                Stroke = Color.FromArgb("#BFDBFE"), StrokeShape = new RoundRectangle { CornerRadius = 18 },
                Padding = new Thickness(20, 18)
            };
            var col = new VerticalStackLayout { Spacing = 12 };
            col.Add(new Label { Text = L("✨ Bạn sẽ nhận được:", "✨ What you unlock:", "✨ 您将获得："), FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1565C0") });

            var features = new (string icon, string vi, string en)[]
            {
                ("🎧", "Audio guide do người thật đọc",            "Expert audio guide by real narrator"),
                ("🗺️", "Tải bản đồ offline toàn bộ phố Vĩnh Khánh", "Full offline map of Vinh Khanh street"),
                ("📖", "Cẩm nang: menu, giá, tip ăn uống",          "Full guide: menu, prices, eating tips"),
                ("📸", "Khung ảnh AR kỷ niệm độc quyền",             "Exclusive AR photo frames"),
                ("🎯", "Tour cá nhân hóa theo khẩu vị",              "Personalized tour by taste"),
                ("🏅", "Huy hiệu & điểm thưởng tích luỹ",            "Badges & reward points"),
                ("📓", "Nhật ký ẩm thực cá nhân + xuất PDF",         "Personal food journal + PDF export"),
            };
            foreach (var (icon, vi, en) in features)
                col.Add(new HorizontalStackLayout
                {
                    Spacing = 10,
                    Children =
                    {
                        new Label { Text = icon, FontSize = 18, VerticalOptions = LayoutOptions.Center },
                        new Label { Text = _lang == "en" ? en : vi, FontSize = 13, TextColor = Color.FromArgb("#374151"), VerticalOptions = LayoutOptions.Center }
                    }
                });

            card.Content = col;
            return card;
        }

        // ══ TICKET CARD ════════════════════════════════════════════════════════
        private Border BuildTicketCard(string type, string name, string price, string desc, string emoji, bool isRecommended)
        {
            var card = new Border { StrokeShape = new RoundRectangle { CornerRadius = 18 }, StrokeThickness = 2, Padding = new Thickness(16, 18) };
            var inner = new VerticalStackLayout { Spacing = 8, HorizontalOptions = LayoutOptions.Fill };
            if (isRecommended)
                inner.Add(new Border
                {
                    BackgroundColor = Color.FromArgb("#FF6F00"), StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = 6 }, Padding = new Thickness(8, 3), HorizontalOptions = LayoutOptions.Center,
                    Content = new Label { Text = L("HOT 🔥", "HOT 🔥", "热卖🔥"), FontSize = 10, FontAttributes = FontAttributes.Bold, TextColor = Colors.White }
                });
            inner.Add(new Label { Text = emoji, FontSize = 32, HorizontalOptions = LayoutOptions.Center });
            inner.Add(new Label { Text = name,  FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D2137"), HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center });
            inner.Add(new Label { Text = price, FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#E65100"), HorizontalOptions = LayoutOptions.Center });
            inner.Add(new Label { Text = desc,  FontSize = 11, TextColor = Color.FromArgb("#64748B"), HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center });
            card.Content = inner;
            card.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => SelectTicket(type)) });
            return card;
        }

        private void SelectTicket(string type)
        {
            _selectedTicket = type;
            _cardDay.BackgroundColor  = type == "day"  ? Color.FromArgb("#EFF6FF") : Colors.White;
            _cardDay.Stroke           = type == "day"  ? Color.FromArgb("#1565C0") : Color.FromArgb("#E2E8F0");
            _cardFull.BackgroundColor = type == "full" ? Color.FromArgb("#FFF8E1") : Colors.White;
            _cardFull.Stroke          = type == "full" ? Color.FromArgb("#FF6F00") : Color.FromArgb("#E2E8F0");
            UpdateSummary();
        }

        // ══ PAYMENT SECTION (Radio List) ══════════════════════════════════════
        private View BuildPaymentSection()
        {
            var col = new VerticalStackLayout { Spacing = 10 };
            foreach (var kv in PayMethods)
            {
                var (emoji, labelVi, labelEn, color, accent, descVi, descEn) = kv.Value;
                var isSelected = kv.Key == _selectedPayment;
                var label = _lang == "en" ? labelEn : labelVi;
                var desc  = _lang == "en" ? descEn  : descVi;

                var card = new Border
                {
                    BackgroundColor = isSelected ? Color.FromArgb(accent) : Colors.White,
                    Stroke          = isSelected ? Color.FromArgb(color)  : Color.FromArgb("#E2E8F0"),
                    StrokeThickness = isSelected ? 2 : 1,
                    StrokeShape     = new RoundRectangle { CornerRadius = 14 },
                    Padding         = new Thickness(16, 14),
                    Shadow          = isSelected ? new Shadow { Brush = Color.FromArgb(color), Opacity = 0.12f, Radius = 10, Offset = new Point(0, 4) } : null
                };

                var outerCircle = new Ellipse
                {
                    WidthRequest = 22, HeightRequest = 22,
                    Stroke = isSelected ? Color.FromArgb(color) : Color.FromArgb("#CBD5E1"),
                    StrokeThickness = 2,
                    Fill = isSelected ? Color.FromArgb(color) : Colors.White
                };
                var innerDot = new Ellipse
                {
                    WidthRequest = 10, HeightRequest = 10,
                    Fill = Colors.White, IsVisible = isSelected,
                    HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
                };
                var radioGrid = new Grid { WidthRequest = 22, HeightRequest = 22, VerticalOptions = LayoutOptions.Center };
                radioGrid.Add(outerCircle);
                radioGrid.Add(innerDot);

                var inner = new Grid
                {
                    ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }
                };
                inner.Add(new Label { Text = emoji, FontSize = 28, VerticalOptions = LayoutOptions.Center, Margin = new Thickness(0, 0, 14, 0) }, 0, 0);
                inner.Add(new VerticalStackLayout
                {
                    VerticalOptions = LayoutOptions.Center, Spacing = 2,
                    Children =
                    {
                        new Label { Text = label, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D2137") },
                        new Label { Text = desc,  FontSize = 11, TextColor = Color.FromArgb("#64748B") }
                    }
                }, 1, 0);
                inner.Add(radioGrid, 2, 0);
                card.Content = inner;

                var capturedKey = kv.Key;
                card.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => SelectPayment(capturedKey)) });
                _payCards[kv.Key] = card;
                col.Add(card);
            }
            return col;
        }

        private void SelectPayment(string key)
        {
            _selectedPayment = key;
            foreach (var kv in _payCards)
            {
                var (_, _, _, color, accent, _, _) = PayMethods[kv.Key];
                var isSelected = kv.Key == key;
                kv.Value.BackgroundColor = isSelected ? Color.FromArgb(accent) : Colors.White;
                kv.Value.Stroke          = isSelected ? Color.FromArgb(color)  : Color.FromArgb("#E2E8F0");
                kv.Value.StrokeThickness = isSelected ? 2 : 1;
                kv.Value.Shadow          = isSelected ? new Shadow { Brush = Color.FromArgb(color), Opacity = 0.12f, Radius = 10, Offset = new Point(0, 4) } : null;

                // Update radio button visuals
                if (kv.Value.Content is Grid innerGrid && innerGrid.Children.Count > 2 && innerGrid.Children[2] is Grid rg)
                {
                    if (rg.Children[0] is Ellipse outer) { outer.Stroke = isSelected ? Color.FromArgb(color) : Color.FromArgb("#CBD5E1"); outer.Fill = isSelected ? Color.FromArgb(color) : Colors.White; }
                    if (rg.Children[1] is Ellipse inner2) inner2.IsVisible = isSelected;
                }
            }
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            var price      = _selectedTicket == "full" ? "79.000đ" : "29.000đ";
            var ticketName = _selectedTicket == "full" ? L("Vé trọn gói", "Full pass", "全票") : L("Vé 1 ngày", "Day pass", "单日票");
            var (_, labelVi, labelEn, _, _, _, _) = PayMethods[_selectedPayment];
            var payName = _lang == "en" ? labelEn : labelVi;
            _summaryLabel.Text = $"{ticketName} — {price}  •  QR {payName}";
        }

        // ══ BUY FLOW → QR SHEET ═══════════════════════════════════════════════
        private async Task OnBuyAsync()
        {
            if (!VinhKhanhTour.Services.UserSession.Instance.IsAuthenticatedUser)
            {
                await DisplayAlert(
                    L("Yêu cầu đăng nhập", "Login Required", "需要登录"),
                    L("Vui lòng đăng nhập để mua vé!", "Please login to buy tickets!", "请先登录购票！"), "OK");
                return;
            }
            await ShowQRPaymentSheetAsync();
        }

        private async Task ShowQRPaymentSheetAsync()
        {
            double amount = _selectedTicket == "full" ? 79000 : 29000;
            var (emoji, labelVi, labelEn, color, accent, _, _) = PayMethods[_selectedPayment];
            var label = _lang == "en" ? labelEn : labelVi;

            // Build order code & QR image URL
            var orderCode = $"VK{DateTime.Now:MMddHHmmss}";
            var addInfo   = Uri.EscapeDataString($"VinhKhanh {orderCode}");
            string qrImageUrl;

            if (_selectedPayment is "bank" or "vnpay")
            {
                // VietQR chuẩn ngân hàng (MB Bank demo)
                qrImageUrl = $"https://img.vietqr.io/image/{BANK_ID}-{BANK_ACCOUNT}-qr_only.jpg?amount={(int)amount}&addInfo={addInfo}&accountName={Uri.EscapeDataString(BANK_NAME)}";
            }
            else
            {
                // QR text đặc thù cho MoMo / ZaloPay
                var payData = $"{label}|{(amount == 79000 ? "Ve Tron Goi" : "Ve 1 Ngay")}|{(int)amount}|{orderCode}|VinhKhanhTour";
                qrImageUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=320x320&data={Uri.EscapeDataString(payData)}&margin=10&bgcolor=fff&color=000";
            }

            // ── Build overlay ──────────────────────────────────────────────────
            var overlay = new AbsoluteLayout { BackgroundColor = Color.FromArgb("#CC000000") };

            // ── Bottom Sheet container ─────────────────────────────────────────
            var sheetStack = new VerticalStackLayout { Spacing = 0 };

            // Header stripe
            var headerStripe = new VerticalStackLayout
            {
                BackgroundColor = Color.FromArgb(color),
                Padding = new Thickness(24, 22, 24, 26),
                Spacing = 4
            };
            headerStripe.Add(new Label { Text = $"{emoji}  {label}", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center });
            headerStripe.Add(new Label { Text = L("Quét mã QR để thanh toán", "Scan QR Code to Pay", "扫码支付"), FontSize = 13, TextColor = Color.FromArgb("#DDEEFF"), HorizontalOptions = LayoutOptions.Center });
            sheetStack.Add(headerStripe);

            // Body
            var body = new VerticalStackLayout { Padding = new Thickness(24, 20, 24, 8), Spacing = 16 };

            // Amount
            body.Add(new Label
            {
                Text = amount.ToString("N0") + " đ",
                FontSize = 38, FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb(color), HorizontalOptions = LayoutOptions.Center
            });

            // QR Image
            var qrBorder = new Border
            {
                StrokeShape     = new RoundRectangle { CornerRadius = 18 },
                Stroke          = Color.FromArgb("#E2E8F0"),
                StrokeThickness = 1.5,
                Padding         = new Thickness(12),
                BackgroundColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.08f, Radius = 14, Offset = new Point(0, 5) },
            };

            if (_selectedPayment is "bank" or "vnpay")
            {
                qrBorder.Content = new WebView
                {
                    Source = new UrlWebViewSource { Url = qrImageUrl },
                    HeightRequest = 290, WidthRequest = 290,
                    BackgroundColor = Colors.White
                };
            }
            else
            {
                var loadingStack = new VerticalStackLayout { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, Spacing = 8, HeightRequest = 290, WidthRequest = 290 };
                loadingStack.Add(new ActivityIndicator { IsRunning = true, Color = Color.FromArgb(color), HeightRequest = 36, WidthRequest = 36, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, Margin = new Thickness(0, 100, 0, 0) });
                loadingStack.Add(new Label { Text = L("Đang tạo mã QR...", "Generating QR...", "生成中..."), FontSize = 13, TextColor = Color.FromArgb("#94A3B8"), HorizontalOptions = LayoutOptions.Center });
                qrBorder.Content = loadingStack;

                // Load image async
                _ = Task.Run(async () =>
                {
                    await Task.Delay(200); // small delay so sheet animates first
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        qrBorder.Content = new Image
                        {
                            Source = ImageSource.FromUri(new Uri(qrImageUrl)),
                            HeightRequest = 290, WidthRequest = 290,
                            Aspect = Aspect.AspectFit,
                            BackgroundColor = Colors.White
                        };
                    });
                });
            }
            body.Add(qrBorder);

            // Bank info row (for bank / vnpay)
            if (_selectedPayment is "bank" or "vnpay")
            {
                body.Add(new Border
                {
                    BackgroundColor = Color.FromArgb("#F8FAFC"),
                    StrokeShape = new RoundRectangle { CornerRadius = 14 },
                    StrokeThickness = 1, Stroke = Color.FromArgb("#E2E8F0"),
                    Padding = new Thickness(16, 12),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 7,
                        Children =
                        {
                            InfoRow("🏦", $"{BANK_ID} Bank",    "#0D2137"),
                            InfoRow("👤",  BANK_NAME,           "#0D2137"),
                            InfoRow("💰",  $"{(int)amount:N0}đ",  color),
                            InfoRow("📝",  orderCode,            "#1565C0"),
                        }
                    }
                });
            }

            // Countdown
            var countdownLbl = new Label
            {
                Text = "⏱  05:00", FontSize = 16, FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#E65100"), HorizontalOptions = LayoutOptions.Center
            };
            body.Add(countdownLbl);

            // Instruction
            body.Add(new Label
            {
                Text = L(
                    $"Mở app {label} → Quét mã QR → Xác nhận thanh toán\nSau đó nhấn nút \"Đã thanh toán\" bên dưới.",
                    $"Open {label} app → Scan QR → Confirm payment\nThen tap \"Payment Done\" below.",
                    $"打开{label} → 扫描QR → 确认付款 → 点击\"已付款\""),
                FontSize = 12, TextColor = Color.FromArgb("#64748B"),
                HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center
            });

            sheetStack.Add(body);

            // ── Footer buttons ─────────────────────────────────────────────────
            var footerBtns = new VerticalStackLayout { Padding = new Thickness(24, 4, 24, 44), Spacing = 10 };

            var confirmBtn = new Border
            {
                HeightRequest = 56,
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0), EndPoint = new Point(1, 0),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb(color), 0),
                        new GradientStop(Color.FromArgb(color), 1)
                    }
                },
                StrokeShape = new RoundRectangle { CornerRadius = 18 }, StrokeThickness = 0,
                Shadow = new Shadow { Brush = Color.FromArgb(color), Opacity = 0.35f, Radius = 14, Offset = new Point(0, 5) },
                Content = new Label
                {
                    Text = L("✅  ĐÃ THANH TOÁN XONG", "✅  PAYMENT DONE", "✅  已完成付款"),
                    TextColor = Colors.White, FontAttributes = FontAttributes.Bold, FontSize = 15,
                    HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
                }
            };

            var cancelBtn = new Border
            {
                HeightRequest = 48, BackgroundColor = Color.FromArgb("#F1F5F9"),
                StrokeShape = new RoundRectangle { CornerRadius = 16 }, StrokeThickness = 0,
                Content = new Label
                {
                    Text = L("Huỷ bỏ", "Cancel", "取消"),
                    TextColor = Color.FromArgb("#64748B"), FontAttributes = FontAttributes.Bold, FontSize = 14,
                    HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
                }
            };

            footerBtns.Add(confirmBtn);
            footerBtns.Add(cancelBtn);
            sheetStack.Add(footerBtns);

            // ── Place sheet on overlay ─────────────────────────────────────────
            var sheet = new Border
            {
                BackgroundColor = Colors.White, StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(28, 28, 0, 0) },
                Content = sheetStack
            };
            AbsoluteLayout.SetLayoutBounds(sheet, new Rect(0, 1, 1, AbsoluteLayout.AutoSize));
            AbsoluteLayout.SetLayoutFlags(sheet, AbsoluteLayoutFlags.PositionProportional | AbsoluteLayoutFlags.WidthProportional);
            overlay.Add(sheet);

            // Close when tapping dimmed backdrop (above sheet)
            var tcs = new TaskCompletionSource<bool>();
            var backdropTap = new TapGestureRecognizer();
            backdropTap.Tapped += (s, e) =>
            {
                if (e.GetPosition(overlay) is Point pt && pt.Y < sheet.Y) tcs.TrySetResult(false);
            };
            overlay.GestureRecognizers.Add(backdropTap);

            var mainGrid = (Grid)Content;
            mainGrid.Add(overlay);
            Grid.SetRowSpan(overlay, 2);

            // Slide up
            sheet.TranslationY = 700;
            await sheet.TranslateTo(0, 0, 380, Easing.CubicOut);

            // ── Countdown timer ────────────────────────────────────────────────
            int secondsLeft = 300;
            var timer = Dispatcher.CreateTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (_, _) =>
            {
                secondsLeft--;
                var m = secondsLeft / 60;
                var s = secondsLeft % 60;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    countdownLbl.Text = $"⏱  {m:D2}:{s:D2}";
                    if (secondsLeft <= 60) countdownLbl.TextColor = Color.FromArgb("#C0392B");
                    if (secondsLeft <= 0) { timer.Stop(); tcs.TrySetResult(false); }
                });
            };
            timer.Start();

            // Button handlers
            confirmBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => { timer.Stop(); tcs.TrySetResult(true); }) });
            cancelBtn.GestureRecognizers.Add(new TapGestureRecognizer  { Command = new Command(() => { timer.Stop(); tcs.TrySetResult(false); }) });

            bool result = await tcs.Task;

            // Slide down
            await sheet.TranslateTo(0, 700, 300, Easing.CubicIn);
            mainGrid.Remove(overlay);

            if (result)
            {
                var ticket = TicketService.Instance.ActivateTicket(_selectedTicket);
                await Navigation.PushAsync(new TicketSuccessPage(ticket));
                var stack = Navigation.NavigationStack.ToList();
                if (stack.Count >= 2) Navigation.RemovePage(stack[^2]);
            }
        }

        // ══ HELPERS ═══════════════════════════════════════════════════════════
        private static HorizontalStackLayout InfoRow(string icon, string text, string hexColor) => new()
        {
            Spacing = 10,
            Children =
            {
                new Label { Text = icon, FontSize = 16, VerticalOptions = LayoutOptions.Center },
                new Label { Text = text, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb(hexColor), VerticalOptions = LayoutOptions.Center }
            }
        };

        private static Label SectionTitle(string text) => new()
        {
            Text = text, FontSize = 16, FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#0D2137"), Margin = new Thickness(0, 4, 0, 0)
        };

        private string L(string vi, string en, string zh) => _lang switch { "en" => en, "zh" => zh, _ => vi };
    }
}
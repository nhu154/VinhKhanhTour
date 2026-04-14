using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;
using VinhKhanhTour.Services;

namespace VinhKhanhTour.Views
{
    /// <summary>
    /// Trang mua vé — chọn loại vé + phương thức thanh toán.
    /// Sau khi mua xong → chuyển sang TicketSuccessPage.
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

        public TicketStorePage()
        {
            _lang = Preferences.Default.Get("app_lang", "vi");
            BackgroundColor = Color.FromArgb("#0D1B2A");
            NavigationPage.SetHasNavigationBar(this, false);
            BuildUI();
        }

        private void BuildUI()
        {
            var scroll = new ScrollView { VerticalScrollBarVisibility = ScrollBarVisibility.Never };
            var root = new VerticalStackLayout { Spacing = 0 };

            // ── Hero Header ────────────────────────────────────────
            var hero = new Grid { HeightRequest = 220 };
            hero.Add(new BoxView
            {
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb("#1A237E"), 0),
                        new GradientStop(Color.FromArgb("#0D47A1"), 0.5f),
                        new GradientStop(Color.FromArgb("#1565C0"), 1)
                    }
                }
            });

            // Decorative circles
            hero.Add(new Ellipse
            {
                Fill = Color.FromArgb("#15FFFFFF"),
                WidthRequest = 200,
                HeightRequest = 200,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                TranslationX = 40,
                TranslationY = -60
            });
            hero.Add(new Ellipse
            {
                Fill = Color.FromArgb("#10FFFFFF"),
                WidthRequest = 120,
                HeightRequest = 120,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.End,
                TranslationX = -30,
                TranslationY = 30
            });

            var backBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#25FFFFFF"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 22 },
                HeightRequest = 44,
                WidthRequest = 44,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(20, 54, 0, 0)
            };
            backBtn.Content = new Label
            {
                Text = "←",
                TextColor = Colors.White,
                FontSize = 22,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            backBtn.GestureRecognizers.Add(new TapGestureRecognizer
            { Command = new Command(async () => await Navigation.PopAsync()) });
            hero.Add(backBtn);

            var heroContent = new VerticalStackLayout
            {
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Spacing = 8
            };
            heroContent.Add(new Label { Text = "🎫", FontSize = 48, HorizontalOptions = LayoutOptions.Center });
            heroContent.Add(new Label
            {
                Text = L("MUA VÉ TRẢI NGHIỆM", "BUY EXPERIENCE TICKET", "购买体验票"),
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                CharacterSpacing = 1
            });
            heroContent.Add(new Label
            {
                Text = L("Mở khoá tất cả tính năng premium", "Unlock all premium features", "解锁所有高级功能"),
                FontSize = 13,
                TextColor = Color.FromArgb("#90CAF9"),
                HorizontalOptions = LayoutOptions.Center
            });
            hero.Add(heroContent);
            root.Add(hero);

            // ── Content Area ───────────────────────────────────────
            var content = new VerticalStackLayout
            {
                BackgroundColor = Color.FromArgb("#F8FAFC"),
                Padding = new Thickness(20, 28, 20, 100),
                Spacing = 24
            };

            // What you get
            content.Add(BuildFeaturesSection());

            // Ticket selection
            content.Add(SectionTitle(L("🎟️  Chọn loại vé", "🎟️  Choose Ticket", "🎟️  选择票种")));
            var ticketRow = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
                ColumnSpacing = 14
            };
            _cardDay = BuildTicketCard("day",
                L("Vé 1 ngày", "Day Pass", "单日票"),
                "29.000đ",
                L("Tất cả tính năng\ntrong 24 giờ", "All features\nfor 24 hours", "所有功能\n24小时"),
                "⏱️", false);
            _cardFull = BuildTicketCard("full",
                L("Vé trọn gói", "Full Pass", "全票"),
                "79.000đ",
                L("Tất cả tính năng\nvĩnh viễn + PDF", "All features\nforever + PDF export", "所有功能\n永久+PDF导出"),
                "🏆", true);
            ticketRow.Add(_cardDay, 0, 0);
            ticketRow.Add(_cardFull, 1, 0);
            content.Add(ticketRow);

            // Payment method
            content.Add(SectionTitle(L("💳  Thanh toán", "💳  Payment", "💳  支付方式")));
            content.Add(BuildPaymentSection());

            // Summary
            _summaryLabel = new Label
            {
                Text = L("Vé 1 ngày — 29.000đ qua MoMo", "Day pass — 29,000đ via MoMo", "单日票 — 29,000đ通过MoMo"),
                FontSize = 14,
                TextColor = Color.FromArgb("#475569"),
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center
            };
            content.Add(_summaryLabel);

            scroll.Content = root;
            root.Add(content);

            // ── Footer ─────────────────────────────────────────────
            var footer = new Border
            {
                BackgroundColor = Colors.White,
                StrokeThickness = 0,
                Padding = new Thickness(20, 14, 20, 40)
            };
            var buyBtn = new Border
            {
                HeightRequest = 58,
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb("#FF6F00"), 0),
                        new GradientStop(Color.FromArgb("#FFA000"), 1)
                    }
                },
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                StrokeThickness = 0,
                Shadow = new Shadow { Brush = Color.FromArgb("#FF6F00"), Opacity = 0.4f, Radius = 16, Offset = new Point(0, 6) }
            };
            buyBtn.Content = new Label
            {
                Text = L("🛒  THANH TOÁN NGAY", "🛒  PAY NOW", "🛒  立即支付"),
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                FontSize = 16,
                CharacterSpacing = 0.5,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            buyBtn.GestureRecognizers.Add(new TapGestureRecognizer
            { Command = new Command(async () => await OnBuyAsync()) });
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

        // ── Feature List ───────────────────────────────────────────
        private View BuildFeaturesSection()
        {
            var card = new Border
            {
                BackgroundColor = Color.FromArgb("#EFF6FF"),
                StrokeThickness = 1.5,
                Stroke = Color.FromArgb("#BFDBFE"),
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                Padding = new Thickness(20, 18)
            };
            var col = new VerticalStackLayout { Spacing = 12 };
            col.Add(new Label
            {
                Text = L("✨ Bạn sẽ nhận được:", "✨ What you unlock:", "✨ 您将获得："),
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1565C0")
            });

            var features = new (string icon, string vi, string en)[]
            {
                ("🎧", "Audio guide chuyên sâu do người thật đọc", "Expert audio guide by real narrator"),
                ("🗺️", "Tải bản đồ offline toàn bộ phố Vĩnh Khánh", "Full offline map of Vinh Khanh street"),
                ("📖", "Cẩm nang đầy đủ: menu, giá, tip ăn uống", "Full guide: menu, prices, eating tips"),
                ("📸", "Khung ảnh AR kỷ niệm độc quyền", "Exclusive AR photo frames"),
                ("🎯", "Tour cá nhân hóa theo khẩu vị", "Personalized tour by taste"),
                ("🏅", "Huy hiệu & điểm thưởng tích luỹ", "Badges & reward points"),
                ("📓", "Nhật ký ẩm thực cá nhân + xuất PDF", "Personal food journal + PDF export"),
            };

            foreach (var (icon, vi, en) in features)
                col.Add(new HorizontalStackLayout
                {
                    Spacing = 10,
                    Children =
                    {
                        new Label { Text = icon, FontSize = 18, VerticalOptions = LayoutOptions.Center },
                        new Label { Text = _lang == "en" ? en : vi, FontSize = 13,
                            TextColor = Color.FromArgb("#374151"), VerticalOptions = LayoutOptions.Center }
                    }
                });

            card.Content = col;
            return card;
        }

        // ── Ticket Card ────────────────────────────────────────────
        private Border BuildTicketCard(string type, string name, string price, string desc, string emoji, bool isRecommended)
        {
            var card = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                StrokeThickness = 2,
                Padding = new Thickness(16, 18)
            };
            var inner = new VerticalStackLayout { Spacing = 8, HorizontalOptions = LayoutOptions.Fill };

            if (isRecommended)
                inner.Add(new Border
                {
                    BackgroundColor = Color.FromArgb("#FF6F00"),
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = 6 },
                    Padding = new Thickness(8, 3),
                    HorizontalOptions = LayoutOptions.Center,
                    Content = new Label
                    {
                        Text = L("HOT 🔥", "HOT 🔥", "热卖🔥"),
                        FontSize = 10,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Colors.White
                    }
                });

            inner.Add(new Label { Text = emoji, FontSize = 32, HorizontalOptions = LayoutOptions.Center });
            inner.Add(new Label
            {
                Text = name,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#0D2137"),
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center
            });
            inner.Add(new Label
            {
                Text = price,
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#E65100"),
                HorizontalOptions = LayoutOptions.Center
            });
            inner.Add(new Label
            {
                Text = desc,
                FontSize = 11,
                TextColor = Color.FromArgb("#64748B"),
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center
            });

            card.Content = inner;
            card.GestureRecognizers.Add(new TapGestureRecognizer
            { Command = new Command(() => SelectTicket(type)) });
            return card;
        }

        private void SelectTicket(string type)
        {
            _selectedTicket = type;
            _cardDay.BackgroundColor = type == "day" ? Color.FromArgb("#EFF6FF") : Colors.White;
            _cardDay.Stroke = type == "day" ? Color.FromArgb("#1565C0") : Color.FromArgb("#E2E8F0");
            _cardFull.BackgroundColor = type == "full" ? Color.FromArgb("#FFF8E1") : Colors.White;
            _cardFull.Stroke = type == "full" ? Color.FromArgb("#FF6F00") : Color.FromArgb("#E2E8F0");
            UpdateSummary();
        }

        // ── Payment Section ────────────────────────────────────────
        private View BuildPaymentSection()
        {
            var grid = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
                ColumnSpacing = 10
            };

            var methods = new (string key, string icon, string label)[]
            {
                ("momo",    "🟣", "MoMo"),
                ("vnpay",   "💳", "VNPay"),
                ("zalopay", "🔵", "ZaloPay"),
                ("gpay",    "G", "GPay"),
            };

            for (int i = 0; i < methods.Length; i++)
            {
                var (key, icon, label) = methods[i];
                var card = new Border
                {
                    BackgroundColor = key == "momo" ? Color.FromArgb("#F3E5F5") : Colors.White,
                    Stroke = key == "momo" ? Color.FromArgb("#9C27B0") : Color.FromArgb("#E2E8F0"),
                    StrokeThickness = 1.5,
                    StrokeShape = new RoundRectangle { CornerRadius = 12 },
                    Padding = new Thickness(4, 12)
                };
                card.Content = new VerticalStackLayout
                {
                    Spacing = 4,
                    HorizontalOptions = LayoutOptions.Fill,
                    Children =
                    {
                        new Label { Text = icon, FontSize = 22, HorizontalOptions = LayoutOptions.Center },
                        new Label { Text = label, FontSize = 12, FontAttributes = FontAttributes.Bold,
                            TextColor = Color.FromArgb("#0D2137"), HorizontalOptions = LayoutOptions.Center }
                    }
                };
                var capturedKey = key;
                card.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(() =>
                    {
                        _selectedPayment = capturedKey;
                        foreach (var kv in _payCards)
                        {
                            kv.Value.BackgroundColor = kv.Key == capturedKey ? Color.FromArgb("#F3E5F5") : Colors.White;
                            kv.Value.Stroke = kv.Key == capturedKey ? Color.FromArgb("#9C27B0") : Color.FromArgb("#E2E8F0");
                        }
                        UpdateSummary();
                    })
                });
                _payCards[key] = card;
                grid.Add(card, i, 0);
            }
            return grid;
        }

        private void UpdateSummary()
        {
            var price = _selectedTicket == "full" ? "79.000đ" : "29.000đ";
            var ticketName = _selectedTicket == "full" ? L("Vé trọn gói", "Full pass", "全票") : L("Vé 1 ngày", "Day pass", "单日票");
            var payName = _selectedPayment switch { "vnpay" => "VNPay", "zalopay" => "ZaloPay", "gpay" => "Google Pay", _ => "MoMo" };
            _summaryLabel.Text = $"{ticketName} — {price} {L("qua", "via", "通过")} {payName}";
        }

        // ── Buy flow (demo) ────────────────────────────────────────
        private async Task OnBuyAsync()
        {
            if (!VinhKhanhTour.Services.UserSession.Instance.IsAuthenticatedUser)
            {
                await DisplayAlert(
                    L("Yêu cầu đăng nhập", "Login Required", "需要登录"), 
                    L("Vui lòng đăng nhập để nâng cấp tính năng!", "Please login to upgrade features!", "请登录以升级功能！"), 
                    "OK");
                return;
            }

            if (_selectedPayment == "gpay")
            {
                await ShowGPayBottomSheetMockAsync();
                return;
            }

            // Demo: show processing animation cho các phương thức khác
            var overlay = new AbsoluteLayout
            {
                BackgroundColor = Color.FromArgb("#80000000"),
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };
            var processing = new VerticalStackLayout
            {
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Spacing = 16
            };
            processing.Add(new ActivityIndicator { IsRunning = true, Color = Colors.White, HeightRequest = 50, WidthRequest = 50 });
            processing.Add(new Label
            {
                Text = L("Đang xử lý thanh toán...", "Processing payment...", "正在处理付款..."),
                TextColor = Colors.White,
                FontSize = 16,
                HorizontalOptions = LayoutOptions.Center
            });
            AbsoluteLayout.SetLayoutBounds(processing, new Rect(0, 0, 1, 1));
            AbsoluteLayout.SetLayoutFlags(processing, AbsoluteLayoutFlags.All);
            overlay.Add(processing);

            // Add overlay on top of existing content
            var mainGrid = (Grid)Content;
            mainGrid.Add(overlay);
            Grid.SetRowSpan(overlay, 2);

            // Mô phỏng thanh toán qua API Polling
            var premiumService = new PremiumPaymentService();
            double price = _selectedTicket == "full" ? 79000 : 29000;
            
            bool isSuccess = await premiumService.ProcessPremiumPurchaseAsync(_selectedTicket, price, this);

            mainGrid.Remove(overlay);

            if (isSuccess)
            {
                // Kích hoạt vé do TicketService xử lý
                var ticket = TicketService.Instance.ActivateTicket(_selectedTicket);

                // Chuyển sang trang thành công
                await Navigation.PushAsync(new TicketSuccessPage(ticket));
                
                // Xoá trang mua vé khỏi stack
                var stack = Navigation.NavigationStack.ToList();
                if (stack.Count >= 2) Navigation.RemovePage(stack[^2]);
            }
        }

        // ── Google Pay Mock UI ──────────────────────────────────────
        private async Task ShowGPayBottomSheetMockAsync()
        {
            double price = _selectedTicket == "full" ? 79000 : 29000;
            string priceStr = price.ToString("N0") + " đ";

            var overlay = new AbsoluteLayout
            {
                BackgroundColor = Color.FromArgb("#AA000000"),
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };

            var bottomSheet = new Border
            {
                BackgroundColor = Colors.White,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(24, 24, 0, 0) },
                Padding = new Thickness(24, 30, 24, 50),
                WidthRequest = App.Current.MainPage.Width
            };

            var sheetContent = new VerticalStackLayout { Spacing = 20 };

            // Header GPay
            sheetContent.Add(new HorizontalStackLayout
            {
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 6,
                Children = {
                    new Label { Text = "G", FontSize = 28, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#4285F4"), VerticalOptions = LayoutOptions.Center },
                    new Label { Text = "Pay", FontSize = 28, TextColor = Color.FromArgb("#5F6368"), VerticalOptions = LayoutOptions.Center }
                }
            });

            sheetContent.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#E0E0E0") });

            // Amount
            sheetContent.Add(new Label { Text = priceStr, FontSize = 36, FontAttributes = FontAttributes.Bold, TextColor = Colors.Black, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 10) });

            // Card details
            var usr = VinhKhanhTour.Services.UserSession.Instance.Username;
            var emailMask = string.IsNullOrWhiteSpace(usr) ? "guest@vinhkhanh.tour" : $"{usr}@vinhkhanh.tour";
            var cardInfo = new Border
            {
                Stroke = Color.FromArgb("#E0E0E0"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Padding = new Thickness(16, 12),
                BackgroundColor = Color.FromArgb("#F8FAFC")
            };
            cardInfo.Content = new HorizontalStackLayout
            {
                Spacing = 12,
                Children = {
                    new Label { Text = "💳", FontSize = 24, VerticalOptions = LayoutOptions.Center },
                    new VerticalStackLayout {
                        VerticalOptions = LayoutOptions.Center,
                        Children = {
                            new Label { Text = "Visa •••• 1234", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Colors.Black },
                            new Label { Text = emailMask, FontSize = 12, TextColor = Color.FromArgb("#5F6368") }
                        }
                    }
                }
            };
            sheetContent.Add(cardInfo);

            // GPay Button State
            var gpayBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#1A73E8"),
                HeightRequest = 56,
                StrokeShape = new RoundRectangle { CornerRadius = 28 },
                StrokeThickness = 0,
                Margin = new Thickness(0, 20, 0, 0),
                Shadow = new Shadow { Brush = Color.FromArgb("#1A73E8"), Opacity = 0.4f, Radius = 15, Offset = new Point(0, 6) }
            };
            
            var btnContent = new Label { Text = "Pay with GPay", TextColor = Colors.White, FontSize = 18, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
            gpayBtn.Content = btnContent;

            sheetContent.Add(gpayBtn);
            bottomSheet.Content = sheetContent;

            AbsoluteLayout.SetLayoutBounds(bottomSheet, new Rect(0, 1, 1, AbsoluteLayout.AutoSize));
            AbsoluteLayout.SetLayoutFlags(bottomSheet, AbsoluteLayoutFlags.PositionProportional | AbsoluteLayoutFlags.WidthProportional);
            overlay.Add(bottomSheet);

            var mainGrid = (Grid)Content;
            mainGrid.Add(overlay);
            Grid.SetRowSpan(overlay, 2);

            // Xử lý khi nhấn nút Pay
            var tcs = new TaskCompletionSource<bool>();
            gpayBtn.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                {
                    gpayBtn.IsEnabled = false;
                    gpayBtn.BackgroundColor = Color.FromArgb("#E0E0E0"); // Disabled look
                    gpayBtn.Content = new ActivityIndicator { IsRunning = true, Color = Color.FromArgb("#1A73E8"), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
                    
                    await Task.Delay(1500); // Giả lập processing

                    // Đổi thành dấu check xanh
                    gpayBtn.BackgroundColor = Color.FromArgb("#34A853");
                    gpayBtn.Content = new Label { Text = "✓", TextColor = Colors.White, FontSize = 28, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
                    
                    await Task.Delay(800); // Hiện dấu check 1 chút rồi đóng
                    tcs.SetResult(true);
                })
            });

            // Close when tap outside
            overlay.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() => { if (gpayBtn.IsEnabled) tcs.TrySetResult(false); })
            });

            bool result = await tcs.Task;
            mainGrid.Remove(overlay);

            if (result)
            {
                var ticket = TicketService.Instance.ActivateTicket(_selectedTicket);
                await Navigation.PushAsync(new TicketSuccessPage(ticket));
                var stack = Navigation.NavigationStack.ToList();
                if (stack.Count >= 2) Navigation.RemovePage(stack[^2]);
            }
        }

        private Label SectionTitle(string text) => new Label
        {
            Text = text,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#0D2137"),
            Margin = new Thickness(0, 4, 0, 0)
        };

        private string L(string vi, string en, string zh) => _lang switch { "en" => en, "zh" => zh, _ => vi };
    }
}
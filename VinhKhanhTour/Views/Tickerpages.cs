using Microsoft.Maui.Controls.Shapes;
using VinhKhanhTour.Services;

namespace VinhKhanhTour.Views
{
    // ══════════════════════════════════════════════════════════════
    // TicketSuccessPage — Màn hình sau khi mua vé thành công
    // ══════════════════════════════════════════════════════════════
    public class TicketSuccessPage : ContentPage
    {
        private readonly TicketInfo _ticket;
        private readonly string _lang;

        public TicketSuccessPage(TicketInfo ticket)
        {
            _ticket = ticket;
            _lang = Preferences.Default.Get("app_lang", "vi");
            BackgroundColor = Color.FromArgb("#F8FAFC");
            NavigationPage.SetHasNavigationBar(this, false);
            BuildUI();
        }

        private void BuildUI()
        {
            var scroll = new ScrollView { VerticalScrollBarVisibility = ScrollBarVisibility.Never };
            var root = new VerticalStackLayout { Padding = new Thickness(24, 70, 24, 40), Spacing = 24 };

            // Confetti icon area
            root.Add(new Label { Text = "🎉", FontSize = 72, HorizontalOptions = LayoutOptions.Center });

            root.Add(new Label
            {
                Text = L("Thanh toán thành công!", "Payment Successful!", "支付成功！"),
                FontSize = 26,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#0D2137"),
                HorizontalOptions = LayoutOptions.Center
            });
            root.Add(new Label
            {
                Text = L("Tất cả tính năng đã được mở khoá 🔓", "All features are now unlocked 🔓", "所有功能已解锁 🔓"),
                FontSize = 14,
                TextColor = Color.FromArgb("#64748B"),
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center
            });

            // ── Ticket Card ────────────────────────────────────────
            var ticketCard = new Border
            {
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb("#1A237E"), 0),
                        new GradientStop(Color.FromArgb("#1565C0"), 1)
                    }
                },
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 24 },
                Padding = new Thickness(24, 28),
                Shadow = new Shadow { Brush = Color.FromArgb("#1565C0"), Opacity = 0.4f, Radius = 20, Offset = new Point(0, 8) }
            };
            var ticketInner = new VerticalStackLayout { Spacing = 16 };

            ticketInner.Add(new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                Children =
                {
                    new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children =
                        {
                            new Label { Text = "VINH KHÁNH TOUR", FontSize = 11, TextColor = Color.FromArgb("#90CAF9"),
                                FontAttributes = FontAttributes.Bold, CharacterSpacing = 2 },
                            new Label { Text = _ticket.TypeDisplay, FontSize = 20, FontAttributes = FontAttributes.Bold,
                                TextColor = Colors.White }
                        }
                    },
                    new Label { Text = _ticket.Type == "full" ? "🏆" : "🎫", FontSize = 40,
                        VerticalOptions = LayoutOptions.Center }
                }
            });

            // Dashed divider
            ticketInner.Add(new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#40FFFFFF") });

            ticketInner.Add(new Label
            {
                Text = _ticket.Code,
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                CharacterSpacing = 2,
                HorizontalOptions = LayoutOptions.Center
            });

            var expiryText = _ticket.Type == "full"
                ? L("Vĩnh viễn", "Lifetime", "终身有效")
                : $"{L("Hết hạn", "Expires", "有效期至")}: {_ticket.Expiry:dd/MM/yyyy HH:mm}";
            ticketInner.Add(new Label
            {
                Text = expiryText,
                FontSize = 12,
                TextColor = Color.FromArgb("#90CAF9"),
                HorizontalOptions = LayoutOptions.Center
            });

            ticketCard.Content = ticketInner;
            root.Add(ticketCard);

            // ── Badge nhận được ────────────────────────────────────
            var badges = TicketService.Instance.GetUnlockedBadges();
            if (badges.Any())
            {
                root.Add(new Border
                {
                    BackgroundColor = Color.FromArgb("#FFF8E1"),
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = 16 },
                    Padding = new Thickness(18, 14),
                    Content = new HorizontalStackLayout
                    {
                        Spacing = 12,
                        HorizontalOptions = LayoutOptions.Center,
                        Children =
                        {
                            new Label { Text = "🏅", FontSize = 28 },
                            new VerticalStackLayout
                            {
                                Spacing = 2,
                                Children =
                                {
                                    new Label { Text = L("Huy hiệu mới!", "New Badge!", "新徽章！"),
                                        FontSize = 14, FontAttributes = FontAttributes.Bold,
                                        TextColor = Color.FromArgb("#E65100") },
                                    new Label { Text = $"{badges[0].Emoji} {badges[0].Name} — +{badges[0].Points} điểm",
                                        FontSize = 13, TextColor = Color.FromArgb("#92400E") }
                                }
                            }
                        }
                    }
                });
            }

            // ── Unlocked features quick preview ───────────────────
            var unlockedGrid = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
                RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) },
                ColumnSpacing = 10,
                RowSpacing = 10
            };

            var features = new (string emoji, string label)[]
            {
                ("🎧", L("Audio HD", "HD Audio", "高清音频")),
                ("🗺️", L("Bản đồ offline", "Offline Map", "离线地图")),
                ("📖", L("Cẩm nang", "Full Guide", "完整指南")),
                ("📸", L("Khung ảnh AR", "AR Frame", "AR相框")),
                ("🎯", L("Tour cá nhân", "Custom Tour", "个性化")),
                ("📓", L("Nhật ký", "Journal", "日记")),
            };

            for (int i = 0; i < features.Length; i++)
            {
                var (emoji, label) = features[i];
                unlockedGrid.Add(new Border
                {
                    BackgroundColor = Color.FromArgb("#E8F5E9"),
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = 12 },
                    Padding = new Thickness(8, 12),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        HorizontalOptions = LayoutOptions.Fill,
                        Children =
                        {
                            new Label { Text = emoji, FontSize = 22, HorizontalOptions = LayoutOptions.Center },
                            new Label { Text = label, FontSize = 11, TextColor = Color.FromArgb("#1B5E20"),
                                FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center,
                                HorizontalTextAlignment = TextAlignment.Center }
                        }
                    }
                }, i % 3, i / 3);
            }
            root.Add(unlockedGrid);

            // ── Buttons ────────────────────────────────────────────
            root.Add(MakeBtn(L("🏠  VỀ TRANG CHỦ", "🏠  GO HOME", "🏠  返回首页"),
                Color.FromArgb("#1565C0"), Colors.White,
                async () => await Navigation.PopToRootAsync()));

            root.Add(MakeBtn(L("🎫  XEM VÉ CỦA TÔI", "🎫  MY TICKET", "🎫  我的票"),
                Color.FromArgb("#F1F5F9"), Color.FromArgb("#1565C0"),
                async () => await Navigation.PushAsync(new MyTicketPage())));

            scroll.Content = root;
            Content = scroll;
        }

        private Border MakeBtn(string text, Color bg, Color fg, Func<Task> action)
        {
            var btn = new Border
            {
                BackgroundColor = bg,
                HeightRequest = 52,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 16 }
            };
            btn.Content = new Label
            {
                Text = text,
                TextColor = fg,
                FontAttributes = FontAttributes.Bold,
                FontSize = 14,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            btn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await action()) });
            return btn;
        }

        private string L(string vi, string en, string zh) => _lang switch { "en" => en, "zh" => zh, _ => vi };
    }

    // ══════════════════════════════════════════════════════════════
    // MyTicketPage — Ví vé / xem chi tiết vé đang có
    // ══════════════════════════════════════════════════════════════
    public class MyTicketPage : ContentPage
    {
        private readonly string _lang;
        private readonly TicketService _ts = TicketService.Instance;

        public MyTicketPage()
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

            // Header
            var header = new Grid
            {
                HeightRequest = 100,
                BackgroundColor = Color.FromArgb("#0D1B2A")
            };
            var backBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#25FFFFFF"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 22 },
                HeightRequest = 44,
                WidthRequest = 44,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(20, 28, 0, 0)
            };
            backBtn.Content = new Label
            {
                Text = "←",
                TextColor = Colors.White,
                FontSize = 22,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            backBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });
            header.Add(backBtn);
            header.Add(new Label
            {
                Text = L("🎫 Vé của tôi", "🎫 My Ticket", "🎫 我的票"),
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 28, 0, 0)
            });
            root.Add(header);

            var content = new VerticalStackLayout
            {
                BackgroundColor = Color.FromArgb("#F8FAFC"),
                Padding = new Thickness(20, 24, 20, 40),
                Spacing = 20
            };

            if (_ts.HasValidTicket)
            {
                // ── Active Ticket ──────────────────────────────────
                var ticketCard = new Border
                {
                    Background = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 1),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromArgb("#1A237E"), 0),
                            new GradientStop(Color.FromArgb("#0288D1"), 1)
                        }
                    },
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = 24 },
                    Padding = new Thickness(24, 24),
                    Shadow = new Shadow { Brush = Color.FromArgb("#1565C0"), Opacity = 0.4f, Radius = 20, Offset = new Point(0, 8) }
                };
                var tInner = new VerticalStackLayout { Spacing = 16 };
                tInner.Add(new Label
                {
                    Text = L("✅ VÉ ĐANG HOẠT ĐỘNG", "✅ ACTIVE TICKET", "✅ 有效票"),
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#A5F3FC"),
                    CharacterSpacing = 1.5
                });
                tInner.Add(new Label { Text = GetTicketTypeDisplay(), FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White });
                tInner.Add(new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#40FFFFFF") });
                tInner.Add(new Label
                {
                    Text = _ts.TicketCode,
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    CharacterSpacing = 2,
                    HorizontalOptions = LayoutOptions.Center
                });

                var expiryText = _ts.IsFullTicket
                    ? L("♾️  Vĩnh viễn không hết hạn", "♾️  Never expires", "♾️  永不过期")
                    : $"⏳ {L("Hết hạn", "Expires", "有效期至")}: {_ts.TicketExpiry:dd/MM/yyyy HH:mm}";
                tInner.Add(new Label
                {
                    Text = expiryText,
                    FontSize = 13,
                    TextColor = Color.FromArgb("#90CAF9"),
                    HorizontalOptions = LayoutOptions.Center
                });

                ticketCard.Content = tInner;
                content.Add(ticketCard);

                // Điểm thưởng
                content.Add(new Border
                {
                    BackgroundColor = Color.FromArgb("#FFF8E1"),
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = 16 },
                    Padding = new Thickness(20, 16),
                    Content = new Grid
                    {
                        ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                        Children =
                        {
                            new VerticalStackLayout
                            {
                                Spacing = 2,
                                Children =
                                {
                                    new Label { Text = L("🏆 Điểm thưởng", "🏆 Reward Points", "🏆 积分"), FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#92400E") },
                                    new Label { Text = L("Tích điểm mỗi lần ghé quán", "Earn points each visit", "每次光顾获得积分"), FontSize = 12, TextColor = Color.FromArgb("#78350F") }
                                }
                            },
                            new Label { Text = $"{_ts.Points}", FontSize = 32, FontAttributes = FontAttributes.Bold,
                                TextColor = Color.FromArgb("#D97706"), VerticalOptions = LayoutOptions.Center }
                        }
                    }.Also(g => { Grid.SetColumn((View)g.Children[1], 1); })
                });

                // Huy hiệu đã có
                content.Add(BuildBadgesSection());

                // Tính năng đã unlock
                content.Add(BuildUnlockedFeaturesSection());
            }
            else
            {
                // ── No ticket — upsell ──────────────────────────────
                content.Add(new VerticalStackLayout
                {
                    Spacing = 16,
                    Padding = new Thickness(0, 40),
                    Children =
                    {
                        new Label { Text = "🎫", FontSize = 60, HorizontalOptions = LayoutOptions.Center },
                        new Label
                        {
                            Text = L("Bạn chưa có vé", "No ticket yet", "您还没有票"),
                            FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D2137"),
                            HorizontalOptions = LayoutOptions.Center
                        },
                        new Label
                        {
                            Text = L("Mua vé để trải nghiệm đầy đủ\ntất cả tính năng premium!",
                                     "Buy a ticket to unlock\nall premium features!",
                                     "购买门票解锁\n所有高级功能！"),
                            FontSize = 14, TextColor = Color.FromArgb("#64748B"),
                            HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center
                        }
                    }
                });

                var buyBtn = new Border
                {
                    HeightRequest = 56,
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
                    StrokeThickness = 0
                };
                buyBtn.Content = new Label
                {
                    Text = L("🛒  MUA VÉ NGAY", "🛒  BUY TICKET", "🛒  立即购票"),
                    TextColor = Colors.White,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 15,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };
                buyBtn.GestureRecognizers.Add(new TapGestureRecognizer
                { Command = new Command(async () => await Navigation.PushAsync(new TicketStorePage())) });
                content.Add(buyBtn);
            }

            scroll.Content = root;
            root.Add(content);
            Content = scroll;
        }

        private View BuildBadgesSection()
        {
            var badges = _ts.GetUnlockedBadges();
            var allBadges = BadgeDefinitions.All;

            var card = new Border
            {
                BackgroundColor = Colors.White,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                Padding = new Thickness(18, 16),
                Shadow = new Shadow { Brush = Color.FromArgb("#000000"), Opacity = 0.05f, Radius = 8, Offset = new Point(0, 2) }
            };
            var col = new VerticalStackLayout { Spacing = 14 };
            col.Add(new Label
            {
                Text = L($"🏅 Huy hiệu ({badges.Count}/{allBadges.Count})",
                $"🏅 Badges ({badges.Count}/{allBadges.Count})", $"🏅 徽章 ({badges.Count}/{allBadges.Count})"),
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#0D2137")
            });

            var badgeGrid = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
                ColumnSpacing = 10,
                RowSpacing = 10
            };

            for (int i = 0; i < allBadges.Count; i++)
            {
                var def = allBadges[i];
                var unlocked = badges.Any(b => b.Id == def.Id);

                if (i / 3 >= badgeGrid.RowDefinitions.Count)
                    badgeGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

                badgeGrid.Add(new Border
                {
                    BackgroundColor = unlocked ? Color.FromArgb("#20" + def.Color.TrimStart('#')) : Color.FromArgb("#F1F5F9"),
                    StrokeThickness = unlocked ? 1.5 : 0,
                    Stroke = unlocked ? Color.FromArgb(def.Color) : Colors.Transparent,
                    StrokeShape = new RoundRectangle { CornerRadius = 12 },
                    Padding = new Thickness(8, 10),
                    Opacity = unlocked ? 1.0 : 0.4,
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        HorizontalOptions = LayoutOptions.Fill,
                        Children =
                        {
                            new Label { Text = unlocked ? def.Emoji : "🔒", FontSize = 22, HorizontalOptions = LayoutOptions.Center },
                            new Label { Text = def.Name, FontSize = 9, FontAttributes = FontAttributes.Bold,
                                TextColor = Color.FromArgb("#374151"), HorizontalOptions = LayoutOptions.Center,
                                HorizontalTextAlignment = TextAlignment.Center, MaxLines = 2 }
                        }
                    }
                }, i % 3, i / 3);
            }
            col.Add(badgeGrid);
            card.Content = col;
            return card;
        }

        private View BuildUnlockedFeaturesSection()
        {
            var features = new (string emoji, string name, string desc)[]
            {
                ("🎧", L("Audio HD", "HD Audio", "高清音频"), L("Phát trong RestaurantDetail", "Play in detail page", "在详情页播放")),
                ("🗺️", L("Bản đồ offline", "Offline Map", "离线地图"), _ts.IsOfflineMapDownloaded ? L("✅ Đã tải", "✅ Downloaded", "✅ 已下载") : L("Chưa tải", "Not downloaded", "未下载")),
                ("📸", L("Khung ảnh AR", "AR Frame", "AR相框"), L("Mở trong quán bất kỳ", "Open at any restaurant", "在任意餐厅开启")),
                ("📓", L("Nhật ký ẩm thực", "Food Journal", "美食日记"), L("Ghi chú sau mỗi bữa", "Log each meal", "记录每餐")),
            };

            var card = new Border
            {
                BackgroundColor = Colors.White,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                Padding = new Thickness(18, 16),
                Shadow = new Shadow { Brush = Color.FromArgb("#000000"), Opacity = 0.05f, Radius = 8, Offset = new Point(0, 2) }
            };
            var col = new VerticalStackLayout { Spacing = 14 };
            col.Add(new Label
            {
                Text = L("✨ Tính năng của bạn", "✨ Your Features", "✨ 您的功能"),
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#0D2137")
            });

            foreach (var (emoji, name, desc) in features)
                col.Add(new Grid
                {
                    ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                    Margin = new Thickness(0, 2),
                    Children =
                    {
                        new Label { Text = emoji, FontSize = 22, VerticalOptions = LayoutOptions.Center, Margin = new Thickness(0, 0, 12, 0) },
                        new VerticalStackLayout
                        {
                            Spacing = 1, VerticalOptions = LayoutOptions.Center,
                            Children =
                            {
                                new Label { Text = name, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D2137") },
                                new Label { Text = desc, FontSize = 11, TextColor = Color.FromArgb("#94A3B8") }
                            }
                        },
                        new Label { Text = "›", FontSize = 20, TextColor = Color.FromArgb("#CBD5E1"), VerticalOptions = LayoutOptions.Center }
                    }
                }.Also(g => { Grid.SetColumn((View)g.Children[1], 1); Grid.SetColumn((View)g.Children[2], 2); }));

            card.Content = col;
            return card;
        }

        private string GetTicketTypeDisplay() => _ts.TicketType switch
        {
            "full" => L("🏆 Vé Trọn Gói", "🏆 Full Pass", "🏆 全票"),
            "day" => L("🎫 Vé 1 Ngày", "🎫 Day Pass", "🎫 单日票"),
            _ => L("Miễn phí", "Free", "免费")
        };

        private string L(string vi, string en, string zh) => _lang switch { "en" => en, "zh" => zh, _ => vi };
    }
}
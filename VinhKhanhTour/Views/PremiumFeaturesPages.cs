using Microsoft.Maui.Controls.Shapes;
using VinhKhanhTour.Models;
using VinhKhanhTour.Services;

namespace VinhKhanhTour.Views
{
    // ══════════════════════════════════════════════════════════════
    // PremiumGatePage — Hiển thị khi user chưa có vé cố truy cập
    // tính năng premium. Có nút mua vé.
    // ══════════════════════════════════════════════════════════════
    public class PremiumGatePage : ContentPage
    {
        public PremiumGatePage(string featureName)
        {
            var lang = Preferences.Default.Get("app_lang", "vi");
            BackgroundColor = Color.FromArgb("#F8FAFC");
            NavigationPage.SetHasNavigationBar(this, false);

            var root = new VerticalStackLayout
            {
                Padding = new Thickness(32, 80),
                Spacing = 20,
                HorizontalOptions = LayoutOptions.Fill
            };
            root.Add(new Label { Text = "🔒", FontSize = 64, HorizontalOptions = LayoutOptions.Center });
            root.Add(new Label
            {
                Text = lang == "en" ? "Premium Feature" : "Tính năng Premium",
                FontSize = 24,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#0D2137"),
                HorizontalOptions = LayoutOptions.Center
            });
            root.Add(new Label
            {
                Text = lang == "en"
                    ? $"\"{featureName}\" requires a ticket.\nBuy one to unlock all premium features!"
                    : $"\"{featureName}\" yêu cầu có vé.\nMua vé để mở khoá tất cả tính năng!",
                FontSize = 15,
                TextColor = Color.FromArgb("#64748B"),
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center
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
                StrokeThickness = 0,
                Margin = new Thickness(0, 16, 0, 0)
            };
            buyBtn.Content = new Label
            {
                Text = lang == "en" ? "🛒  BUY TICKET" : "🛒  MUA VÉ NGAY",
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                FontSize = 15,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            buyBtn.GestureRecognizers.Add(new TapGestureRecognizer
            { Command = new Command(async () => await Navigation.PushAsync(new TicketStorePage())) });
            root.Add(buyBtn);

            var backBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#F1F5F9"),
                HeightRequest = 48,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 14 }
            };
            backBtn.Content = new Label
            {
                Text = lang == "en" ? "← Back" : "← Quay lại",
                TextColor = Color.FromArgb("#64748B"),
                FontSize = 14,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            backBtn.GestureRecognizers.Add(new TapGestureRecognizer
            { Command = new Command(async () => await Navigation.PopAsync()) });
            root.Add(backBtn);

            Content = root;
        }
    }

    // ══════════════════════════════════════════════════════════════
    // AudioGuidePage — Audio guide chuyên sâu (premium)
    // ══════════════════════════════════════════════════════════════
    public class AudioGuidePage : ContentPage
    {
        private readonly Restaurant _restaurant;
        private readonly string _lang;
        private Label _playLabel = null!;
        private bool _isPlaying = false;

        public AudioGuidePage(Restaurant restaurant)
        {
            _restaurant = restaurant;
            _lang = Preferences.Default.Get("app_lang", "vi");
            BackgroundColor = Color.FromArgb("#0D1B2A");
            NavigationPage.SetHasNavigationBar(this, false);
            BuildUI();
        }

        private void BuildUI()
        {
            var root = new VerticalStackLayout { Padding = new Thickness(24, 60, 24, 40), Spacing = 24 };

            // Back
            var backRow = new HorizontalStackLayout { Spacing = 8 };
            var backBtn = new Label { Text = "←", FontSize = 22, TextColor = Colors.White };
            backBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });
            backRow.Add(backBtn);
            root.Add(backRow);

            // Vinyl/album art area
            var albumArt = new Border
            {
                BackgroundColor = Color.FromArgb("#1A237E"),
                StrokeThickness = 0,
                StrokeShape = new Ellipse(),
                HeightRequest = 220,
                WidthRequest = 220,
                HorizontalOptions = LayoutOptions.Center,
                Shadow = new Shadow { Brush = Color.FromArgb("#1565C0"), Opacity = 0.5f, Radius = 30, Offset = new Point(0, 10) }
            };
            var albumInner = new VerticalStackLayout { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, Spacing = 8 };
            albumInner.Add(new Label { Text = "🎧", FontSize = 64, HorizontalOptions = LayoutOptions.Center });
            albumArt.Content = albumInner;
            root.Add(albumArt);

            root.Add(new Label
            {
                Text = _restaurant.Name,
                FontSize = 24,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center
            });
            root.Add(new Label
            {
                Text = L("🎙️ Audio Guide HD — Người thật đọc", "🎙️ HD Audio Guide — Real narrator", "🎙️ 高清音频导览"),
                FontSize = 13,
                TextColor = Color.FromArgb("#90CAF9"),
                HorizontalOptions = LayoutOptions.Center
            });

            // "Script" preview (premium content)
            var scriptCard = new Border
            {
                BackgroundColor = Color.FromArgb("#1E2D40"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                Padding = new Thickness(20, 18)
            };
            scriptCard.Content = new Label
            {
                Text = GetPremiumScript(),
                FontSize = 14,
                TextColor = Color.FromArgb("#CBD5E1"),
                LineHeight = 1.6,
                HorizontalTextAlignment = TextAlignment.Center
            };
            root.Add(scriptCard);

            // Progress bar (fake)
            var progressBar = new ProgressBar
            {
                Progress = 0,
                ProgressColor = Color.FromArgb("#1565C0"),
                BackgroundColor = Color.FromArgb("#1E2D40")
            };

            root.Add(progressBar);

            // Time labels — estimate duration from script word count (avg 3 words/sec)
            var script = GetPremiumScript();
            int wordCount = script.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            int estSec = Math.Max(15, wordCount * 100 / 300); // ~3 words/sec
            string durStr = estSec >= 60 ? $"{estSec / 60}:{estSec % 60:D2}" : $"0:{estSec:D2}";
            root.Add(new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
                Children =
                {
                    new Label { Text = "0:00", FontSize = 12, TextColor = Color.FromArgb("#64748B") },
                    new Label { Text = durStr, FontSize = 12, TextColor = Color.FromArgb("#64748B"),
                        HorizontalOptions = LayoutOptions.End }
                }
            }.Also(g => Grid.SetColumn((View)g.Children[1], 1)));

            // Controls
            var controls = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
                HorizontalOptions = LayoutOptions.Fill
            };

            controls.Add(new Label
            {
                Text = "⏮",
                FontSize = 28,
                TextColor = Color.FromArgb("#94A3B8"),
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 0, 24, 0)
            }, 0, 0);

            var playBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#1565C0"),
                StrokeThickness = 0,
                StrokeShape = new Ellipse(),
                HeightRequest = 72,
                WidthRequest = 72,
                Shadow = new Shadow { Brush = Color.FromArgb("#1565C0"), Opacity = 0.5f, Radius = 16, Offset = new Point(0, 6) }
            };
            _playLabel = new Label
            {
                Text = "▶",
                FontSize = 28,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            playBtn.Content = _playLabel;
            playBtn.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                {
                    _isPlaying = !_isPlaying;
                    _playLabel.Text = _isPlaying ? "⏸" : "▶";

                    if (_isPlaying)
                    {
                        // Mô phỏng progress
                        _ = Task.Run(async () =>
                        {
                            for (double p = 0; p <= 1.0 && _isPlaying; p += 0.005)
                            {
                                await Task.Delay(100);
                                MainThread.BeginInvokeOnMainThread(() => progressBar.Progress = p);
                            }
                        });

                        // Thực ra gọi AudioService với premium script
                        await AudioService.Instance.PlayNarrationAsync(_restaurant);

                        // Record journal entry nếu có vé
                        if (TicketService.Instance.HasValidTicket)
                            TicketService.Instance.AddPoints(15);
                    }
                    else
                    {
                        await AudioService.Instance.StopAsync();
                    }
                })
            });
            controls.Add(playBtn, 1, 0);

            controls.Add(new Label
            {
                Text = "⏭",
                FontSize = 28,
                TextColor = Color.FromArgb("#94A3B8"),
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(24, 0, 0, 0)
            }, 2, 0);

            root.Add(controls);

            // Premium badge
            root.Add(new Border
            {
                BackgroundColor = Color.FromArgb("#25FFD700"),
                StrokeThickness = 1,
                Stroke = Color.FromArgb("#FFD700"),
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Padding = new Thickness(16, 8),
                HorizontalOptions = LayoutOptions.Center,
                Content = new Label
                {
                    Text = "💎 PREMIUM AUDIO",
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#FFD700"),
                    CharacterSpacing = 1
                }
            });

            Content = new ScrollView { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Never };
        }

        private string GetPremiumScript() => _lang switch
        {
            "en" => $"Welcome to {_restaurant.Name}! This exclusive audio guide reveals the hidden stories behind this legendary Vinh Khanh institution — from the secret spice recipes passed down through generations to the best tables and insider ordering tips only locals know...",
            "zh" => $"欢迎来到{_restaurant.Name}！这份独家音频导览将揭示这家传奇永庆街餐厅背后的隐藏故事——从世代相传的秘方到只有当地人才知道的最佳位置和点餐技巧...",
            _ => $"Chào mừng đến với {_restaurant.Name}! Audio guide độc quyền này sẽ tiết lộ những câu chuyện ẩn sau tên tuổi huyền thoại của phố Vĩnh Khánh — từ công thức gia vị bí truyền qua nhiều thế hệ đến những bàn ngon nhất và bí kíp gọi món chỉ người địa phương mới biết..."
        };

        private string L(string vi, string en, string zh) => _lang switch { "en" => en, "zh" => zh, _ => vi };
    }

    // ══════════════════════════════════════════════════════════════
    // FoodJournalPage — Nhật ký ẩm thực cá nhân
    // ══════════════════════════════════════════════════════════════
    public class FoodJournalPage : ContentPage
    {
        private readonly string _lang;
        private VerticalStackLayout _listLayout = null!;
        private readonly TicketService _ts = TicketService.Instance;

        public FoodJournalPage()
        {
            _lang = Preferences.Default.Get("app_lang", "vi");
            BackgroundColor = Color.FromArgb("#F8FAFC");
            NavigationPage.SetHasNavigationBar(this, false);
            BuildUI();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadEntries();
        }

        private void BuildUI()
        {
            var root = new Grid
            {
                RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) }
            };

            // Header
            var header = new Grid { HeightRequest = 100 };
            header.Add(new BoxView { BackgroundColor = Color.FromArgb("#1565C0") });
            var backBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#30FFFFFF"),
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
                Text = L("📓 Nhật ký ẩm thực", "📓 Food Journal", "📓 美食日记"),
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 28, 0, 0)
            });
            root.Add(header, 0, 0);

            var scroll = new ScrollView { VerticalScrollBarVisibility = ScrollBarVisibility.Never };
            _listLayout = new VerticalStackLayout { Padding = new Thickness(20, 16), Spacing = 14 };
            scroll.Content = _listLayout;
            root.Add(scroll, 0, 1);

            // Footer buttons
            var footer = new Grid
            {
                BackgroundColor = Colors.White,
                Padding = new Thickness(20, 12, 20, 36),
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }
            };

            var addBtn = new Border
            {
                HeightRequest = 50,
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
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                StrokeThickness = 0
            };
            addBtn.Content = new Label
            {
                Text = L("✍️  Ghi chú mới", "✍️  New Entry", "✍️  新记录"),
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                FontSize = 14,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            addBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await AddEntryAsync()) });
            footer.Add(addBtn, 0, 0);

            if (_ts.CanExportJournal)
            {
                var exportBtn = new Border
                {
                    BackgroundColor = Color.FromArgb("#E8F5E9"),
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = 14 },
                    Padding = new Thickness(14, 0),
                    HeightRequest = 50,
                    Margin = new Thickness(10, 0, 0, 0)
                };
                exportBtn.Content = new Label
                {
                    Text = "📄 PDF",
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#2E7D32"),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };
                exportBtn.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(async () =>
                    await DisplayAlert(L("Xuất PDF", "Export PDF", "导出PDF"),
                        L("Tính năng xuất PDF đang được phát triển!", "PDF export coming soon!", "PDF导出功能开发中！"), "OK"))
                });
                footer.Add(exportBtn, 1, 0);
            }

            root.Add(footer, 0, 2);
            Content = root;
        }

        private void LoadEntries()
        {
            _listLayout.Clear();
            var entries = _ts.GetJournalEntries();

            if (entries.Count == 0)
            {
                _listLayout.Add(new Label
                {
                    Text = L("Chưa có ghi chú nào.\nBắt đầu ghi lại trải nghiệm ẩm thực của bạn!",
                             "No entries yet.\nStart recording your food experiences!",
                             "暂无记录。\n开始记录您的美食体验吧！"),
                    FontSize = 15,
                    TextColor = Color.FromArgb("#94A3B8"),
                    HorizontalOptions = LayoutOptions.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 60)
                });
                return;
            }

            foreach (var e in entries)
            {
                var card = new Border
                {
                    BackgroundColor = Colors.White,
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = 16 },
                    Padding = new Thickness(18, 14),
                    Shadow = new Shadow { Brush = Color.FromArgb("#000000"), Opacity = 0.05f, Radius = 8, Offset = new Point(0, 2) }
                };
                var stars = string.Concat(Enumerable.Repeat("⭐", e.Rating));
                card.Content = new VerticalStackLayout
                {
                    Spacing = 6,
                    Children =
                    {
                        new Label { Text = $"{e.Emoji}  {e.RestaurantName}", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D2137") },
                        new Label { Text = stars, FontSize = 14 },
                        new Label { Text = e.Note, FontSize = 13, TextColor = Color.FromArgb("#475569") },
                        new Label { Text = e.VisitedAt.ToString("dd/MM/yyyy HH:mm"), FontSize = 11, TextColor = Color.FromArgb("#94A3B8") }
                    }
                };
                _listLayout.Add(card);
            }
        }

        private async Task AddEntryAsync()
        {
            var name = await DisplayPromptAsync(
                L("Nhật ký mới", "New Entry", "新记录"),
                L("Tên quán bạn vừa ghé?", "Restaurant you visited?", "您刚去的餐厅名？"),
                accept: L("Tiếp", "Next", "下一步"),
                cancel: L("Hủy", "Cancel", "取消"));
            if (name == null) return;

            var note = await DisplayPromptAsync(
                L("Cảm nhận", "Your thoughts", "您的感受"),
                L("Món ngon? Không gian? Ghi gì cũng được!", "Good food? Atmosphere? Anything!", "美食？氛围？随便写！"),
                accept: L("Lưu", "Save", "保存"),
                cancel: L("Hủy", "Cancel", "取消"));
            if (note == null) return;

            _ts.AddJournalEntry(new JournalEntry
            {
                RestaurantName = name,
                Note = note,
                Rating = 5,
                Emoji = "🍽️",
                VisitedAt = DateTime.Now
            });

            // Badge check
            var entries = _ts.GetJournalEntries();
            if (entries.Count >= 5 && !_ts.HasBadge("reviewer"))
            {
                var badge = _ts.UnlockBadge(BadgeDefinitions.Reviewer);
                if (badge != null)
                    await DisplayAlert(L("🏅 Huy hiệu mới!", "🏅 New Badge!", "🏅 新徽章！"),
                        $"{badge.Emoji} {badge.Name}\n+{badge.Points} điểm", "OK");
            }

            LoadEntries();
        }

        private string L(string vi, string en, string zh) => _lang switch { "en" => en, "zh" => zh, _ => vi };
    }

    // ══════════════════════════════════════════════════════════════
    // PhotoFramePage — Khung ảnh AR kỷ niệm
    // ══════════════════════════════════════════════════════════════
    public class PhotoFramePage : ContentPage
    {
        private readonly Restaurant _restaurant;
        private readonly string _lang;

        public PhotoFramePage(Restaurant restaurant)
        {
            _restaurant = restaurant;
            _lang = Preferences.Default.Get("app_lang", "vi");
            BackgroundColor = Color.FromArgb("#0D1B2A");
            NavigationPage.SetHasNavigationBar(this, false);
            BuildUI();
        }

        private void BuildUI()
        {
            var root = new VerticalStackLayout { Padding = new Thickness(20, 60, 20, 40), Spacing = 20 };

            var backBtn = new Label { Text = "←  " + L("Quay lại", "Back", "返回"), FontSize = 16, TextColor = Colors.White };
            backBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });
            root.Add(backBtn);

            root.Add(new Label
            {
                Text = L("📸 Khung ảnh kỷ niệm", "📸 Memory Frame", "📸 纪念相框"),
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center
            });

            // Preview frame
            var framePreview = new Border
            {
                HeightRequest = 340,
                WidthRequest = 320,
                HorizontalOptions = LayoutOptions.Center,
                StrokeThickness = 4,
                Stroke = Color.FromArgb("#FFD700"),
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                BackgroundColor = Color.FromArgb("#1E2D40"),
                Padding = 0
            };

            var frameContent = new Grid();
            // Background gradient
            frameContent.Add(new BoxView
            {
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb("#1A237E"), 0),
                        new GradientStop(Color.FromArgb("#0D47A1"), 1)
                    }
                }
            });

            // Frame overlay content
            var frameInner = new VerticalStackLayout
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                Spacing = 0
            };

            // Top banner
            frameInner.Add(new Border
            {
                BackgroundColor = Color.FromArgb("#CC1565C0"),
                StrokeThickness = 0,
                Padding = new Thickness(16, 10),
                Content = new Label
                {
                    Text = "🐚 VINH KHÁNH FOOD TOUR 2026",
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    CharacterSpacing = 1,
                    HorizontalOptions = LayoutOptions.Center
                }
            });

            // Photo placeholder
            frameInner.Add(new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Fill,
                HorizontalOptions = LayoutOptions.Fill,
                Spacing = 12,
                Padding = new Thickness(20),
                Children =
                {
                    new Label { Text = "📷", FontSize = 60, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center },
                    new Label { Text = L("Chạm để chụp ảnh", "Tap to take photo", "点击拍照"),
                        FontSize = 14, TextColor = Color.FromArgb("#90CAF9"), HorizontalOptions = LayoutOptions.Center }
                }
            });

            // Bottom info
            frameInner.Add(new Border
            {
                BackgroundColor = Color.FromArgb("#CC000000"),
                StrokeThickness = 0,
                Padding = new Thickness(16, 12),
                Content = new VerticalStackLayout
                {
                    Spacing = 2,
                    Children =
                    {
                        new Label { Text = _restaurant.Name, FontSize = 15, FontAttributes = FontAttributes.Bold,
                            TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center },
                        new Label { Text = $"Vĩnh Khánh, Q.4  •  {DateTime.Now:dd/MM/yyyy}",
                            FontSize = 11, TextColor = Color.FromArgb("#90CAF9"), HorizontalOptions = LayoutOptions.Center }
                    }
                }
            });

            frameContent.Add(frameInner);
            framePreview.Content = frameContent;
            root.Add(framePreview);

            // Frames selection
            root.Add(new Label
            {
                Text = L("Chọn khung:", "Choose frame:", "选择相框:"),
                FontSize = 14,
                TextColor = Color.FromArgb("#94A3B8")
            });

            var framesRow = new HorizontalStackLayout { Spacing = 12 };
            var frames = new (string emoji, string name)[] { ("🐚", "Ốc"), ("🍜", "Phở"), ("🏙️", "Sài Gòn"), ("🌟", "VIP") };
            foreach (var (emoji, name) in frames)
                framesRow.Add(new Border
                {
                    BackgroundColor = Color.FromArgb("#1E2D40"),
                    StrokeThickness = 1.5,
                    Stroke = Color.FromArgb("#334155"),
                    StrokeShape = new RoundRectangle { CornerRadius = 12 },
                    Padding = new Thickness(14, 10),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        HorizontalOptions = LayoutOptions.Center,
                        Children =
                        {
                            new Label { Text = emoji, FontSize = 24, HorizontalOptions = LayoutOptions.Center },
                            new Label { Text = name, FontSize = 10, TextColor = Color.FromArgb("#94A3B8"), HorizontalOptions = LayoutOptions.Center }
                        }
                    }
                });
            root.Add(new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = framesRow });

            // Take photo button
            var captureBtn = new Border
            {
                HeightRequest = 56,
                BackgroundColor = Color.FromArgb("#E91E63"),
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                StrokeThickness = 0,
                Shadow = new Shadow { Brush = Color.FromArgb("#E91E63"), Opacity = 0.4f, Radius = 14, Offset = new Point(0, 6) }
            };
            captureBtn.Content = new Label
            {
                Text = L("📸  CHỤP ẢNH", "📸  TAKE PHOTO", "📸  拍照"),
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                FontSize = 15,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            captureBtn.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                {
                    TicketService.Instance.AddPoints(30);
                    var badge = TicketService.Instance.UnlockBadge(BadgeDefinitions.Photographer);
                    var msg = badge != null
                        ? L($"Ảnh đã lưu! 🎉\n+30 điểm thưởng\n🏅 Huy hiệu mới: {badge.Name}!",
                             $"Photo saved! 🎉\n+30 points\n🏅 New badge: {badge.Name}!", "")
                        : L("Ảnh đã lưu vào thư viện! 📁\n+30 điểm thưởng", "Photo saved! 📁\n+30 points", "");
                    await DisplayAlert("✅", msg, "OK");
                })
            });
            root.Add(captureBtn);

            Content = new ScrollView { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Never };
        }

        private string L(string vi, string en, string zh) => _lang switch { "en" => en, "zh" => zh, _ => vi };
    }
}
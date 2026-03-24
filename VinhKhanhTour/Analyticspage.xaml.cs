using Microsoft.Maui.Controls.Shapes;
using VinhKhanhTour.Models;
using VinhKhanhTour.Services;

namespace VinhKhanhTour
{
    public class AnalyticsPage : ContentPage
    {
        private VerticalStackLayout _content = null!;

        // ── Palette ───────────────────────────────────────────────
        static readonly Color BgPage       = Color.FromArgb("#0F0F1A");
        static readonly Color BgCard       = Color.FromArgb("#1C1C2E");
        static readonly Color BgCardAlt    = Color.FromArgb("#16213E");
        static readonly Color AccentRed    = Color.FromArgb("#FF4757");
        static readonly Color AccentOrange = Color.FromArgb("#FF6B35");
        static readonly Color AccentBlue   = Color.FromArgb("#4ECDC4");
        static readonly Color AccentGold   = Color.FromArgb("#FFD93D");
        static readonly Color TextPrimary  = Color.FromArgb("#FFFFFF");
        static readonly Color TextSecond   = Color.FromArgb("#8892A4");
        static readonly Color Divider      = Color.FromArgb("#252538");

        public AnalyticsPage()
        {
            Title = "Thống kê hành trình";
            BackgroundColor = BgPage;
            NavigationPage.SetHasNavigationBar(this, false); // Tránh navigation bar kép do đẩy Modal
            BuildUI();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadAsync();
        }

        private void BuildUI()
        {
            _content = new VerticalStackLayout { Spacing = 0, Padding = new Thickness(0, 0, 0, 40) };
            Content = new ScrollView { Content = _content, BackgroundColor = BgPage };
        }

        private async Task LoadAsync()
        {
            _content.Children.Clear();

            // ── Hero Header ───────────────────────────────────────
            _content.Add(BuildHeroHeader(Navigation));

            try
            {
                var summary  = await AnalyticsService.Instance.GetSummaryAsync();
                var topPois  = await AnalyticsService.Instance.GetTopPoisAsync(6);

                // ── Stat cards row ────────────────────────────────
                _content.Add(BuildStatCardsRow(summary.TotalListens, summary.UniquePois, summary.AvgListenSec, summary.TourCompletes));

                // ── Insight strip ─────────────────────────────────
                _content.Add(BuildInsightStrip(summary.TotalListens, topPois));

                // ── Top POI ranking ───────────────────────────────
                _content.Add(BuildSectionHeader("🏆  Top địa điểm phổ biến"));
                if (topPois.Count > 0)
                    _content.Add(BuildRankingList(topPois));
                else
                    _content.Add(BuildEmptyState("Chưa có lượt nghe thuyết minh nào.\nHãy ghé thăm Vĩnh Khánh và bắt đầu khám phá! 🍜"));

                // ── Avg listen time chart ─────────────────────────
                var withTime = topPois.Where(p => p.AvgSeconds > 0).ToList();
                if (withTime.Count > 0)
                {
                    _content.Add(BuildSectionHeader("⏱  Thời gian nghe trung bình"));
                    _content.Add(BuildAvgTimeChart(withTime));
                }

                // ── Clear button ──────────────────────────────────
                _content.Add(BuildClearButton());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AnalyticsPage] {ex.Message}");
                _content.Add(BuildEmptyState("Lỗi tải dữ liệu: " + ex.Message));
            }
        }

        // ════════════════════════════════════════════════════════
        //  HERO HEADER
        // ════════════════════════════════════════════════════════
        private static Grid BuildHeroHeader(INavigation nav)
        {
            var grid = new Grid { HeightRequest = 180, BackgroundColor = Color.FromArgb("#0F0F1A") };

            // gradient overlay via two overlapping boxes
            grid.Add(new BoxView
            {
                BackgroundColor = Color.FromArgb("#1A0A2E"),
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            });

            // back button top left
            var backBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#1AFFFFFF"),
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                StrokeThickness = 0,
                Padding = new Thickness(14, 8),
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(20, 20, 0, 0)
            };
            backBtn.Content = new Label { Text = "←  Trở về", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Colors.White };
            var tb = new TapGestureRecognizer();
            tb.Tapped += async (s, e) => { if (nav.ModalStack.Count > 0) await nav.PopModalAsync(); else await nav.PopAsync(); };
            backBtn.GestureRecognizers.Add(tb);
            grid.Add(backBtn);

            // red accent line left
            grid.Add(new BoxView
            {
                WidthRequest = 4, HeightRequest = 60,
                CornerRadius = 2,
                BackgroundColor = Color.FromArgb("#FF4757"),
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.End,
                Margin = new Thickness(20, 0, 0, 24)
            });

            var textStack = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.End,
                Margin = new Thickness(36, 0, 20, 28),
                Spacing = 4
            };
            textStack.Add(new Label
            {
                Text = "Thống kê hành trình",
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
            });
            textStack.Add(new Label
            {
                Text = "Khám phá hành trình ẩm thực của bạn",
                FontSize = 12,
                TextColor = Color.FromArgb("#8892A4"),
            });
            grid.Add(textStack);

            // chart icon right
            grid.Add(new Label
            {
                Text = "📊",
                FontSize = 48,
                Opacity = 0.15,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 0, 16, 0)
            });

            // bottom accent line
            grid.Add(new BoxView
            {
                HeightRequest = 2,
                BackgroundColor = Color.FromArgb("#FF4757"),
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.End,
                Opacity = 0.5
            });

            return grid;
        }

        // ════════════════════════════════════════════════════════
        //  STAT CARDS (2x2 grid)
        // ════════════════════════════════════════════════════════
        private static Grid BuildStatCardsRow(int listens, int pois, double avgSec, int tours)
        {
            var grid = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition() },
                RowDefinitions    = { new RowDefinition { Height = GridLength.Auto }, new RowDefinition { Height = GridLength.Auto } },
                ColumnSpacing = 12,
                RowSpacing = 12,
                Margin = new Thickness(16, 20, 16, 0)
            };

            grid.Add(MakeStatCard("🎧", listens.ToString(),    "Lần nghe thuyết minh", AccentRed),    0, 0);
            grid.Add(MakeStatCard("📍", pois.ToString(),       "Địa điểm khác nhau",   AccentOrange),  1, 0);
            grid.Add(MakeStatCard("⏱", $"{avgSec:F0}s",       "Thời gian nghe TB",    AccentBlue),    0, 1);
            grid.Add(MakeStatCard("🗺️", tours.ToString(),      "Tour hoàn thành",      AccentGold),    1, 1);

            return grid;
        }

        private static Border MakeStatCard(string icon, string value, string label, Color accent)
        {
            // accent dot top-right
            var dot = new BoxView
            {
                WidthRequest = 6, HeightRequest = 6,
                CornerRadius = 3,
                BackgroundColor = accent,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 2, 2, 0)
            };
            var iconLbl = new Label { Text = icon,  FontSize = 28, HorizontalOptions = LayoutOptions.Start };
            var valLbl  = new Label
            {
                Text = value,
                FontSize = 26,
                FontAttributes = FontAttributes.Bold,
                TextColor = accent,
                HorizontalOptions = LayoutOptions.Start
            };
            var subLbl = new Label
            {
                Text = label,
                FontSize = 11,
                TextColor = TextSecond,
                HorizontalOptions = LayoutOptions.Start
            };

            var stack = new VerticalStackLayout { Spacing = 2 };
            stack.Add(iconLbl);
            stack.Add(valLbl);
            stack.Add(subLbl);

            var overlay = new Grid();
            overlay.Add(stack);
            overlay.Add(dot);

            return new Border
            {
                BackgroundColor = BgCard,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                StrokeThickness = 1,
                Stroke = new SolidColorBrush(Color.FromArgb("#252538")),
                Padding = new Thickness(16, 14),
                Content = overlay
            };
        }

        // ════════════════════════════════════════════════════════
        //  INSIGHT STRIP
        // ════════════════════════════════════════════════════════
        private static Border BuildInsightStrip(int listens, List<PoiStats> topPois)
        {
            string msg;
            if (listens == 0)
                msg = "✨  Bắt đầu hành trình của bạn — ghé Vĩnh Khánh và nghe thuyết minh!";
            else if (listens < 5)
                msg = $"🌟  Bạn đã nghe {listens} lần. Còn nhiều quán chờ bạn khám phá đấy!";
            else
            {
                var top = topPois.FirstOrDefault();
                msg = top != null
                    ? $"🏆  Địa điểm yêu thích của bạn: {top.PoiName} ({top.ListenCount} lần nghe)"
                    : $"🎉  Bạn đã nghe thuyết minh {listens} lần – thám hiểm Vĩnh Khánh thật tích cực!";
            }

            return new Border
            {
                BackgroundColor = Color.FromArgb("#1A0A2E"),
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                StrokeThickness = 1,
                Stroke = new SolidColorBrush(Color.FromArgb("#FF475730")),
                Padding = new Thickness(16, 12),
                Margin = new Thickness(16, 16, 16, 0),
                Content = new Label
                {
                    Text = msg,
                    FontSize = 13,
                    TextColor = Color.FromArgb("#E0E0E0"),
                    LineBreakMode = LineBreakMode.WordWrap
                }
            };
        }

        // ════════════════════════════════════════════════════════
        //  SECTION HEADER
        // ════════════════════════════════════════════════════════
        private static Grid BuildSectionHeader(string title)
        {
            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 10,
                Margin = new Thickness(16, 24, 16, 12)
            };

            grid.Add(new BoxView
            {
                WidthRequest = 3, HeightRequest = 18,
                CornerRadius = 2,
                BackgroundColor = AccentRed,
                VerticalOptions = LayoutOptions.Center
            }, 0, 0);

            grid.Add(new Label
            {
                Text = title,
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = TextPrimary,
                VerticalOptions = LayoutOptions.Center
            }, 1, 0);

            return grid;
        }

        // ════════════════════════════════════════════════════════
        //  RANKING LIST (Top POIs)
        // ════════════════════════════════════════════════════════
        private static Border BuildRankingList(List<PoiStats> pois)
        {
            int max = Math.Max(pois.Max(p => p.ListenCount), 1);
            double barMax = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density
                            - 16 * 2   // card margin
                            - 16 * 2   // card padding
                            - 32       // rank badge
                            - 12 * 2   // column spacing
                            - 44;      // count label
            if (barMax < 40) barMax = 40;

            var rows = new VerticalStackLayout { Spacing = 0 };
            string[] medals = { "🥇", "🥈", "🥉" };
            Color[]  barColors = { AccentRed, AccentOrange, AccentBlue, AccentGold,
                                   Color.FromArgb("#A29BFE"), Color.FromArgb("#55EFC4") };

            for (int i = 0; i < pois.Count; i++)
            {
                var p = pois[i];
                double pct = (double)p.ListenCount / max;
                Color barColor = barColors[i % barColors.Length];

                // rank
                string rankText = i < medals.Length ? medals[i] : $"#{i + 1}";

                var row = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(32) },
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = new GridLength(44) }
                    },
                    ColumnSpacing = 10,
                    Padding = new Thickness(0, 10)
                };

                row.Add(new Label
                {
                    Text = rankText,
                    FontSize = i < 3 ? 18 : 13,
                    FontAttributes = i >= 3 ? FontAttributes.Bold : FontAttributes.None,
                    TextColor = i >= 3 ? TextSecond : TextPrimary,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalTextAlignment = TextAlignment.Center
                }, 0, 0);

                var nameBar = new VerticalStackLayout { Spacing = 5, VerticalOptions = LayoutOptions.Center };
                nameBar.Add(new Label
                {
                    Text = p.PoiName,
                    FontSize = 13,
                    TextColor = TextPrimary,
                    LineBreakMode = LineBreakMode.TailTruncation
                });
                // bar
                var barTrack = new Grid();
                barTrack.Add(new BoxView
                {
                    BackgroundColor = Color.FromArgb("#252538"),
                    CornerRadius = 4,
                    HeightRequest = 6,
                    HorizontalOptions = LayoutOptions.Fill
                });
                barTrack.Add(new BoxView
                {
                    BackgroundColor = barColor,
                    CornerRadius = 4,
                    HeightRequest = 6,
                    HorizontalOptions = LayoutOptions.Start,
                    WidthRequest = Math.Max(pct * barMax, 6)
                });
                nameBar.Add(barTrack);
                row.Add(nameBar, 1, 0);

                row.Add(new Label
                {
                    Text = $"{p.ListenCount}×",
                    FontSize = 13,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = barColor,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalTextAlignment = TextAlignment.End
                }, 2, 0);

                rows.Add(row);

                if (i < pois.Count - 1)
                    rows.Add(new BoxView { HeightRequest = 1, BackgroundColor = Divider });
            }

            return new Border
            {
                BackgroundColor = BgCard,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                StrokeThickness = 1,
                Stroke = new SolidColorBrush(Divider),
                Padding = new Thickness(16, 6),
                Margin = new Thickness(16, 0),
                Content = rows
            };
        }

        // ════════════════════════════════════════════════════════
        //  AVG TIME CHART
        // ════════════════════════════════════════════════════════
        private static Border BuildAvgTimeChart(List<PoiStats> pois)
        {
            double max = Math.Max(pois.Max(p => p.AvgSeconds), 1);
            double barMax = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density
                            - 16 * 2 - 16 * 2 - 110 - 48 - 8 * 2;
            if (barMax < 40) barMax = 40;

            var rows = new VerticalStackLayout { Spacing = 0 };

            for (int i = 0; i < pois.Count; i++)
            {
                var p = pois[i];
                double pct = p.AvgSeconds / max;

                var row = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(110) },
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = new GridLength(48) }
                    },
                    ColumnSpacing = 10,
                    Padding = new Thickness(0, 10)
                };

                row.Add(new Label
                {
                    Text = p.PoiName,
                    FontSize = 12,
                    TextColor = TextSecond,
                    VerticalOptions = LayoutOptions.Center,
                    LineBreakMode = LineBreakMode.TailTruncation
                }, 0, 0);

                var barTrack = new Grid { VerticalOptions = LayoutOptions.Center };
                barTrack.Add(new BoxView { BackgroundColor = Color.FromArgb("#252538"), CornerRadius = 4, HeightRequest = 8, HorizontalOptions = LayoutOptions.Fill });
                barTrack.Add(new BoxView { BackgroundColor = AccentBlue,               CornerRadius = 4, HeightRequest = 8, HorizontalOptions = LayoutOptions.Start, WidthRequest = Math.Max(pct * barMax, 6) });
                row.Add(barTrack, 1, 0);

                row.Add(new Label
                {
                    Text = $"{p.AvgSeconds:F0}s",
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = AccentBlue,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalTextAlignment = TextAlignment.End
                }, 2, 0);

                rows.Add(row);
                if (i < pois.Count - 1)
                    rows.Add(new BoxView { HeightRequest = 1, BackgroundColor = Divider });
            }

            return new Border
            {
                BackgroundColor = BgCard,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                StrokeThickness = 1,
                Stroke = new SolidColorBrush(Divider),
                Padding = new Thickness(16, 6),
                Margin = new Thickness(16, 0),
                Content = rows
            };
        }

        // ════════════════════════════════════════════════════════
        //  CLEAR BUTTON
        // ════════════════════════════════════════════════════════
        private Button BuildClearButton()
        {
            var btn = new Button
            {
                Text = "🗑   Xóa toàn bộ dữ liệu",
                BackgroundColor = Color.FromArgb("#2A1520"),
                TextColor = Color.FromArgb("#FF4757"),
                CornerRadius = 14,
                FontSize = 13,
                HeightRequest = 48,
                Margin = new Thickness(16, 28, 16, 0),
                BorderColor = Color.FromArgb("#FF475750"),
                BorderWidth = 1
            };
            btn.Clicked += async (s, e) =>
            {
                bool ok = await DisplayAlert("Xác nhận", "Xóa toàn bộ dữ liệu thống kê?", "Xóa", "Hủy");
                if (ok) { await App.Database.ClearAnalyticsAsync(); await LoadAsync(); }
            };
            return btn;
        }

        // ════════════════════════════════════════════════════════
        //  EMPTY STATE
        // ════════════════════════════════════════════════════════
        private static Border BuildEmptyState(string msg) => new Border
        {
            BackgroundColor = BgCard,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            StrokeThickness = 1,
            Stroke = new SolidColorBrush(Divider),
            Padding = new Thickness(24, 28),
            Margin = new Thickness(16, 0),
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label { Text = "📭", FontSize = 36, HorizontalOptions = LayoutOptions.Center },
                    new Label
                    {
                        Text = msg,
                        FontSize = 13,
                        TextColor = TextSecond,
                        HorizontalOptions = LayoutOptions.Center,
                        HorizontalTextAlignment = TextAlignment.Center,
                        LineBreakMode = LineBreakMode.WordWrap
                    }
                }
            }
        };
    }
}
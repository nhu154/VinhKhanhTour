using Microsoft.Maui.Controls.Shapes;
using VinhKhanhTour.Models;
using VinhKhanhTour.Services;

namespace VinhKhanhTour.Views
{
    public class AnalyticsPage : ContentPage
    {
        private VerticalStackLayout _content = null!;

        // ── Palette (Clean BLUE & WHITE Tone) ───────────────────
        static readonly Color BgPage = Color.FromArgb("#F8FAFC"); // Light Background
        static readonly Color BgCard = Colors.White;
        static readonly Color PrimaryBlue = Color.FromArgb("#1565C0"); // Core App Blue
        static readonly Color AccentBlue = Color.FromArgb("#42A5F5"); // Light Blue Accent
        static readonly Color TextPrimary = Color.FromArgb("#0D2137"); // Deep Dark Navy for text
        static readonly Color TextSecond = Color.FromArgb("#475569"); // Darker Slate for High Contrast
        static readonly Color Divider = Color.FromArgb("#E2E8F0");
        static readonly Color AccentGold = Color.FromArgb("#F59E0B");

        public AnalyticsPage()
        {
            Title = "Thống kê hành trình";
            BackgroundColor = BgPage;
            NavigationPage.SetHasNavigationBar(this, false);
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
                var summary = await AnalyticsService.Instance.GetSummaryAsync();
                var topPois = await AnalyticsService.Instance.GetTopPoisAsync(6);

                // ── Stat cards row ────────────────────────────────
                _content.Add(BuildStatCardsRow(summary));

                // ── Insight strip ─────────────────────────────────
                _content.Add(BuildInsightStrip(summary.TotalListens, topPois));

                // ── Top POI ranking ───────────────────────────────
                _content.Add(BuildSectionHeader("🏆  Địa điểm nổi bật"));
                if (topPois.Count > 0)
                    _content.Add(BuildRankingList(topPois));
                else
                    _content.Add(BuildEmptyState("Chưa có lượt nghe thuyết minh nào.\nHãy bắt đầu khám phá Vĩnh Khánh! 🍜"));

                // ── Avg listen time chart ─────────────────────────
                var withTime = topPois.Where(p => p.AvgSeconds > 1).ToList();
                if (withTime.Count > 0)
                {
                    _content.Add(BuildSectionHeader("⏱  Thời gian nghe trung bình (giây)"));
                    _content.Add(BuildAvgTimeChart(withTime));
                }

                _content.Add(BuildClearButton());
            }
            catch (Exception ex)
            {
                _content.Add(BuildEmptyState("Lỗi: " + ex.Message));
            }
        }

        private Grid BuildHeroHeader(INavigation nav)
        {
            var grid = new Grid { HeightRequest = 180, BackgroundColor = PrimaryBlue };
            
            // Subtle blue gradient
            grid.Add(new BoxView
            {
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0), EndPoint = new Point(1, 0),
                    GradientStops = new GradientStopCollection {
                        new GradientStop(Color.FromArgb("#1565C0"), 0),
                        new GradientStop(Color.FromArgb("#1E88E5"), 1)
                    }
                }
            });

            // Back button (Left)
            var backBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#20FFFFFF"),
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                StrokeThickness = 0,
                Padding = new Thickness(12, 4),
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(20, 20, 0, 0)
            };
            backBtn.Content = new Label { Text = "←", FontSize = 22, TextColor = Colors.White, FontAttributes = FontAttributes.Bold };
            var tb = new TapGestureRecognizer();
            tb.Tapped += async (s, e) => { if (nav.ModalStack.Count > 0) await nav.PopModalAsync(); else await nav.PopAsync(); };
            backBtn.GestureRecognizers.Add(tb);
            grid.Add(backBtn);

            // Refresh button (Right)
            var refreshBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#20FFFFFF"),
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                StrokeThickness = 0,
                Padding = new Thickness(10, 8),
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 20, 20, 0)
            };
            refreshBtn.Content = new Label { Text = "🔄", FontSize = 16, TextColor = Colors.White };
            var tr = new TapGestureRecognizer();
            tr.Tapped += async (s, e) => {
                refreshBtn.Opacity = 0.5;
                await LoadAsync();
                refreshBtn.Opacity = 1.0;
            };
            refreshBtn.GestureRecognizers.Add(tr);
            grid.Add(refreshBtn);

            var textStack = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(24, 40, 24, 0),
                Spacing = 2
            };
            textStack.Add(new Label { Text = "Thống kê hành trình", FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = Colors.White });
            textStack.Add(new Label { Text = "Hành trình khám phá Quận 4 của bạn", FontSize = 13, TextColor = Color.FromArgb("#BBDEFB") });
            grid.Add(textStack);

            return grid;
        }

        private static Grid BuildStatCardsRow(AnalyticsSummary s)
        {
            var grid = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition() },
                RowDefinitions = { new RowDefinition(), new RowDefinition() },
                ColumnSpacing = 12, RowSpacing = 12,
                Margin = new Thickness(16, 16, 16, 0)
            };

            grid.Add(MakeStatCard("🎧", s.TotalListens.ToString(), "Tổng lượt nghe", PrimaryBlue), 0, 0);
            grid.Add(MakeStatCard("📍", s.UniquePois.ToString(), "Địa điểm ghé qua", AccentBlue), 1, 0);
            grid.Add(MakeStatCard("⏱", $"{s.AvgListenSec:F0}s", "Thời gian nghe TB", Color.FromArgb("#2E7D32")), 0, 1);
            grid.Add(MakeStatCard("🗺️", s.TourCompletes.ToString(), "Tour hoàn thành", AccentGold), 1, 1);

            return grid;
        }

        private static Border MakeStatCard(string icon, string value, string label, Color accent)
        {
            var valLbl = new Label { Text = value, FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = TextPrimary };
            var subLbl = new Label { Text = label, FontSize = 11, TextColor = TextSecond };
            var iconLbl = new Label { Text = icon, FontSize = 22, HorizontalOptions = LayoutOptions.End, Opacity = 0.8 };

            var stack = new VerticalStackLayout { Spacing = 2 };
            stack.Add(iconLbl);
            stack.Add(valLbl);
            stack.Add(subLbl);

            return new Border
            {
                BackgroundColor = BgCard,
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                StrokeThickness = 1,
                Stroke = new SolidColorBrush(Color.FromArgb("#F1F5F9")),
                Padding = new Thickness(16),
                Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.05f, Radius = 8, Offset = new Point(0, 4) },
                Content = stack
            };
        }

        private static Border BuildInsightStrip(int listens, List<PoiStats> topPois)
        {
            string msg = listens == 0 ? "✨ Bắt đầu khám phá ngay để xem thống kê của riêng bạn!" :
                        $"🏆 Bạn đã nghe thuyết minh {listens} lần. " + (topPois.Count > 0 ? $"Thích nhất là {topPois[0].PoiName}!" : "");

            return new Border
            {
                BackgroundColor = Color.FromArgb("#1565C01A"),
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                StrokeThickness = 0,
                Padding = new Thickness(16, 12),
                Margin = new Thickness(16, 16, 16, 0),
                Content = new Label { Text = msg, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = PrimaryBlue, HorizontalTextAlignment = TextAlignment.Center }
            };
        }

        private static Grid BuildSectionHeader(string title)
        {
            var grid = new Grid { Margin = new Thickness(20, 24, 20, 12) };
            grid.Add(new Label { Text = title, FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = TextPrimary });
            return grid;
        }

        private static Border BuildRankingList(List<PoiStats> pois)
        {
            var stack = new VerticalStackLayout { Spacing = 12, Padding = new Thickness(16) };
            int max = Math.Max(pois.Max(x => x.ListenCount), 1);

            for (int i = 0; i < pois.Count; i++)
            {
                var p = pois[i];
                double ratio = (double)p.ListenCount / max;
                
                var row = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Auto } } };
                
                var nameStack = new VerticalStackLayout { Spacing = 4 };
                nameStack.Add(new Label { Text = $"{(i < 3 ? "⭐ " : "")}{p.PoiName}", FontSize = 14, TextColor = TextPrimary });
                
                var barCont = new Grid { HeightRequest = 6 };
                barCont.Add(new BoxView { BackgroundColor = Color.FromArgb("#2F3E4F"), CornerRadius = 3 });
                barCont.Add(new BoxView { BackgroundColor = PrimaryBlue, CornerRadius = 3, HorizontalOptions = LayoutOptions.Start, WidthRequest = 200 * ratio });
                nameStack.Add(barCont);
                
                row.Add(nameStack, 0, 0);
                row.Add(new Label { Text = $"{p.ListenCount}x", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = AccentBlue, VerticalOptions = LayoutOptions.Center }, 1, 0);
                
                stack.Add(row);
                if (i < pois.Count - 1) stack.Add(new BoxView { HeightRequest = 1, BackgroundColor = Divider });
            }

            return new Border { BackgroundColor = BgCard, StrokeShape = new RoundRectangle { CornerRadius = 20 }, Padding = 0, Margin = new Thickness(16, 0), Content = stack };
        }

        private static Border BuildAvgTimeChart(List<PoiStats> pois)
        {
            var stack = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(16) };
            double max = Math.Max(pois.Max(x => x.AvgSeconds), 1);

            foreach (var p in pois)
            {
                // Increase label column width and add more spacing
                var row = new Grid { ColumnDefinitions = { 
                    new ColumnDefinition { Width = 110 }, 
                    new ColumnDefinition { Width = GridLength.Star }, 
                    new ColumnDefinition { Width = 45 } 
                } };
                
                row.Add(new Label { 
                    Text = p.PoiName, FontSize = 12, TextColor = TextSecond, 
                    VerticalOptions = LayoutOptions.Center, MaxLines = 1, 
                    LineBreakMode = LineBreakMode.TailTruncation 
                }, 0, 0);
                
                var barCont = new Grid { HeightRequest = 6, VerticalOptions = LayoutOptions.Center };
                barCont.Add(new BoxView { BackgroundColor = Color.FromArgb("#F1F5F9"), CornerRadius = 3 }); // Lighter track background
                
                double barWidth = 130 * (p.AvgSeconds / max);
                barCont.Add(new BoxView { 
                    BackgroundColor = Color.FromArgb("#10B981"), // More vibrant Green
                    CornerRadius = 3, HorizontalOptions = LayoutOptions.Start, 
                    WidthRequest = Math.Max(barWidth, 2) 
                });
                row.Add(barCont, 1, 0);
                
                row.Add(new Label { 
                    Text = $"{(int)p.AvgSeconds}s", FontSize = 12, FontAttributes = FontAttributes.Bold, 
                    TextColor = PrimaryBlue, HorizontalOptions = LayoutOptions.End, 
                    VerticalOptions = LayoutOptions.Center 
                }, 2, 0);
                
                stack.Add(row);
            }
            return new Border { BackgroundColor = BgCard, StrokeShape = new RoundRectangle { CornerRadius = 20 }, Margin = new Thickness(16, 0), Content = stack };
        }

        private Button BuildClearButton()
        {
            var btn = new Button
            {
                Text = "Xóa thống kê", BackgroundColor = Colors.Transparent, BorderColor = Color.FromArgb("#FF5252"), TextColor = Color.FromArgb("#FF5252"),
                BorderWidth = 1, CornerRadius = 12, Margin = new Thickness(24, 32, 24, 0), FontSize = 13, HeightRequest = 44
            };
            btn.Clicked += async (s, e) => {
                if (await DisplayAlert("Xác nhận", "Bạn có muốn xóa toàn bộ lịch sử thống kê trên thiết bị và server?", "Xóa sạch", "Hủy")) {
                    await AnalyticsService.Instance.ClearAllAsync();
                    await LoadAsync();
                }
            };
            return btn;
        }

        private static Border BuildEmptyState(string msg) => new Border
        {
            BackgroundColor = BgCard, StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = 30, Margin = 16,
            Content = new Label { Text = msg, TextColor = TextSecond, FontSize = 14, HorizontalTextAlignment = TextAlignment.Center }
        };
    }
}

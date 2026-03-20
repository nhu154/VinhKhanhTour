using Microsoft.Maui.Controls.Shapes;
using VinhKhanhTour.Models;

namespace VinhKhanhTour;

public partial class ProfilePage : ContentPage
{
    // ── References (built in code, not XAML) ──────────────────────
    private Label _visitCountLabel = null!;
    private Label _tourCountLabel = null!;
    private Label _pointsLabel = null!;
    private VerticalStackLayout _visitHistoryLayout = null!;

    public ProfilePage()
    {
        InitializeComponent();
        BuildUI();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadStatsAsync();
    }

    // ── Build entire UI in code-behind ────────────────────────────
    private void BuildUI()
    {
        BackgroundColor = Color.FromArgb("#F5F5F5");

        var scroll = new ScrollView();
        var root = new VerticalStackLayout { Spacing = 0 };

        // ── Hero Header ──────────────────────────────────────────
        var header = new Grid { HeightRequest = 180 };
        header.Children.Add(new BoxView
        {
            BackgroundColor = Color.FromArgb("#1A1A2E"),
            VerticalOptions = LayoutOptions.Fill,
            HorizontalOptions = LayoutOptions.Fill
        });
        header.Children.Add(new BoxView
        {
            BackgroundColor = Color.FromArgb("#E53935"),
            HeightRequest = 4,
            VerticalOptions = LayoutOptions.End,
            HorizontalOptions = LayoutOptions.Fill
        });

        var headerStack = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 8
        };
        headerStack.Add(new Border
        {
            BackgroundColor = Color.FromArgb("#263238"),
            StrokeShape = new RoundRectangle { CornerRadius = 50 },
            WidthRequest = 82,
            HeightRequest = 82,
            HorizontalOptions = LayoutOptions.Center,
            StrokeThickness = 2,
            Stroke = new SolidColorBrush(Color.FromArgb("#E53935")),
            Content = new Label
            {
                Text = "😊",
                FontSize = 44,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        });
        headerStack.Add(new Label
        {
            Text = "Du khách Vĩnh Khánh",
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center
        });
        headerStack.Add(new Label
        {
            Text = "🍜 Khám phá Vĩnh Khánh",
            FontSize = 12,
            TextColor = Color.FromArgb("#90A4AE"),
            HorizontalOptions = LayoutOptions.Center
        });
        header.Children.Add(headerStack);
        root.Add(header);

        // ── Stats row ────────────────────────────────────────────
        _visitCountLabel = MakeStatLabel();
        _tourCountLabel = MakeStatLabel();
        _pointsLabel = MakeStatLabel();

        var statsGrid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition(), new ColumnDefinition() },
            ColumnSpacing = 10,
            Margin = new Thickness(16, 16, 16, 0)
        };
        statsGrid.Add(MakeStatCard(_visitCountLabel, "Quán ghé"), 0, 0);
        statsGrid.Add(MakeStatCard(_tourCountLabel, "Tour xong"), 1, 0);
        statsGrid.Add(MakeStatCard(_pointsLabel, "Điểm thưởng"), 2, 0);
        root.Add(statsGrid);

        // ── Visit history ────────────────────────────────────────
        _visitHistoryLayout = new VerticalStackLayout { Spacing = 12 };
        _visitHistoryLayout.Add(new Label
        {
            Text = "Đang tải...",
            FontSize = 13,
            TextColor = Color.FromArgb("#9E9E9E"),
            HorizontalOptions = LayoutOptions.Center
        });

        var historySection = new VerticalStackLayout { Padding = new Thickness(16, 18, 16, 0), Spacing = 10 };
        historySection.Add(new Label
        {
            Text = "Lịch sử ghé thăm gần đây",
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1A1A2E")
        });
        historySection.Add(MakeCard(_visitHistoryLayout));
        root.Add(historySection);

        // ── Settings menu ────────────────────────────────────────
        var menuSection = new VerticalStackLayout { Padding = new Thickness(16, 18, 16, 0), Spacing = 10 };
        menuSection.Add(new Label
        {
            Text = "Cài đặt & tiện ích",
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1A1A2E")
        });

        var menuItems = new VerticalStackLayout { Spacing = 0 };
        menuItems.Add(MakeMenuItem("⭐", "Quán yêu thích", null));
        menuItems.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#F5F5F5"), Margin = new Thickness(14, 0) });
        menuItems.Add(MakeMenuItem("🎁", "Ưu đãi của tôi", "Mới"));
        menuItems.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#F5F5F5"), Margin = new Thickness(14, 0) });
        menuItems.Add(MakeMenuItem("🌐", "Ngôn ngữ thuyết minh", null, "Tiếng Việt"));
        menuItems.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#F5F5F5"), Margin = new Thickness(14, 0) });
        menuItems.Add(MakeMenuItem("⚙️", "Cài đặt", null));
        menuSection.Add(MakeCard(menuItems));
        root.Add(menuSection);

        // ── Footer ───────────────────────────────────────────────
        root.Add(new Label
        {
            Text = "🍜 Vĩnh Khánh — Thiên đường ẩm thực Quận 4",
            FontSize = 12,
            TextColor = Color.FromArgb("#BDBDBD"),
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(16, 24, 16, 32)
        });

        scroll.Content = root;
        Content = scroll;
    }

    // ── Helpers: UI factories ─────────────────────────────────────
    private static Label MakeStatLabel() => new Label
    {
        Text = "0",
        FontSize = 26,
        FontAttributes = FontAttributes.Bold,
        TextColor = Color.FromArgb("#E53935"),
        HorizontalOptions = LayoutOptions.Center
    };

    private static Border MakeStatCard(Label valueLabel, string caption)
    {
        var card = new Border
        {
            BackgroundColor = Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            StrokeThickness = 0,
            Padding = 12
        };
        var stack = new VerticalStackLayout { Spacing = 4, HorizontalOptions = LayoutOptions.Center };
        stack.Add(valueLabel);
        stack.Add(new Label
        {
            Text = caption,
            FontSize = 11,
            TextColor = Color.FromArgb("#9E9E9E"),
            HorizontalOptions = LayoutOptions.Center
        });
        card.Content = stack;
        return card;
    }

    private static Border MakeCard(View content)
    {
        return new Border
        {
            BackgroundColor = Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            StrokeThickness = 0,
            Padding = 16,
            Content = content
        };
    }

    private static Grid MakeMenuItem(string icon, string title, string? badge, string? subtitle = null)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 12,
            Padding = new Thickness(2, 14)
        };

        grid.Add(new Label { Text = icon, FontSize = 20, VerticalOptions = LayoutOptions.Center }, 0, 0);

        if (subtitle != null)
        {
            var stack = new VerticalStackLayout { Spacing = 1, VerticalOptions = LayoutOptions.Center };
            stack.Add(new Label { Text = title, FontSize = 15, TextColor = Color.FromArgb("#1A1A2E") });
            stack.Add(new Label { Text = subtitle, FontSize = 12, TextColor = Color.FromArgb("#9E9E9E") });
            grid.Add(stack, 1, 0);
        }
        else
        {
            grid.Add(new Label
            {
                Text = title,
                FontSize = 15,
                TextColor = Color.FromArgb("#1A1A2E"),
                VerticalOptions = LayoutOptions.Center
            }, 1, 0);
        }

        if (badge != null)
        {
            grid.Add(new Border
            {
                BackgroundColor = Color.FromArgb("#FFEBEE"),
                StrokeShape = new RoundRectangle { CornerRadius = 10 },
                Padding = new Thickness(6, 3),
                Content = new Label
                {
                    Text = badge,
                    FontSize = 11,
                    TextColor = Color.FromArgb("#E53935"),
                    FontAttributes = FontAttributes.Bold
                }
            }, 2, 0);
        }
        else
        {
            grid.Add(new Label
            {
                Text = "›",
                FontSize = 20,
                TextColor = Color.FromArgb("#BDBDBD"),
                VerticalOptions = LayoutOptions.Center
            }, 2, 0);
        }

        return grid;
    }

    // ── Load data ─────────────────────────────────────────────────
    private async Task LoadStatsAsync()
    {
        try
        {
            var visits = await App.Database.GetVisitHistoryAsync();
            var restaurants = await App.Database.GetRestaurantsAsync();

            var uniqueCount = visits.Select(v => v.RestaurantId).Distinct().Count();
            _visitCountLabel.Text = uniqueCount.ToString();
            _pointsLabel.Text = (uniqueCount * 10).ToString();

            var recent = visits.OrderByDescending(v => v.VisitedAt).Take(5).ToList();

            _visitHistoryLayout.Children.Clear();

            if (recent.Count == 0)
            {
                _visitHistoryLayout.Children.Add(new Label
                {
                    Text = "Chưa có lịch sử. Hãy khám phá Vĩnh Khánh nhé! 🍜",
                    FontSize = 13,
                    TextColor = Color.FromArgb("#9E9E9E"),
                    HorizontalOptions = LayoutOptions.Center,
                    HorizontalTextAlignment = TextAlignment.Center
                });
                return;
            }

            foreach (var visit in recent)
            {
                var r = restaurants.FirstOrDefault(x => x.Id == visit.RestaurantId);
                if (r == null) continue;

                var row = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Auto },
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                    ColumnSpacing = 12
                };

                row.Add(new Border
                {
                    BackgroundColor = Color.FromArgb("#FFF3E0"),
                    StrokeShape = new RoundRectangle { CornerRadius = 10 },
                    WidthRequest = 42,
                    HeightRequest = 42,
                    StrokeThickness = 0,
                    Content = new Label
                    {
                        Text = GetCategoryEmoji(r),
                        FontSize = 22,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    }
                }, 0, 0);

                var info = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
                info.Add(new Label
                {
                    Text = r.Name,
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#1A1A2E")
                });
                info.Add(new Label
                {
                    Text = r.Address,
                    FontSize = 11,
                    TextColor = Color.FromArgb("#9E9E9E"),
                    LineBreakMode = LineBreakMode.TailTruncation
                });
                row.Add(info, 1, 0);

                row.Add(new Label
                {
                    Text = TimeAgo(visit.VisitedAt),
                    FontSize = 11,
                    TextColor = Color.FromArgb("#BDBDBD"),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalTextAlignment = TextAlignment.End
                }, 2, 0);

                _visitHistoryLayout.Children.Add(row);

                if (visit != recent.Last())
                    _visitHistoryLayout.Children.Add(new BoxView
                    { HeightRequest = 1, Color = Color.FromArgb("#F5F5F5") });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ProfilePage stats error: {ex.Message}");
        }
    }

    // ── Static helpers ────────────────────────────────────────────
    private static string GetCategoryEmoji(Restaurant r)
    {
        var n = r.Name.ToLower();
        if (n.Contains("ốc")) return "🦪";
        if (n.Contains("bún") || n.Contains("phở")) return "🍜";
        if (n.Contains("cơm")) return "🍚";
        if (n.Contains("lẩu") || n.Contains("nướng") || n.Contains("bò")) return "🔥";
        return "🍽️";
    }

    private static string TimeAgo(DateTime dt)
    {
        var d = DateTime.Now - dt;
        if (d.TotalMinutes < 1) return "Vừa xong";
        if (d.TotalHours < 1) return $"{(int)d.TotalMinutes} phút trước";
        if (d.TotalDays < 1) return $"{(int)d.TotalHours} giờ trước";
        if (d.TotalDays < 7) return $"{(int)d.TotalDays} ngày trước";
        return dt.ToString("dd/MM/yyyy");
    }
}
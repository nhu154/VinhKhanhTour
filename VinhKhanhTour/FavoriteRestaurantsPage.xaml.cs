using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.Shapes;
using VinhKhanhTour.Models;
using VinhKhanhTour.Services;

namespace VinhKhanhTour;

public partial class FavoriteRestaurantsPage : ContentPage
{
    private VerticalStackLayout _content = null!;

    static readonly Color BgPage = Color.FromArgb("#F0F6FF");
    static readonly Color BgCard = Color.FromArgb("#FFFFFF");
    static readonly Color TextPrimary = Color.FromArgb("#0D2137");
    static readonly Color TextSecond = Color.FromArgb("#5A7A9A");
    static readonly Color Divider = Color.FromArgb("#CCE0FF");
    static readonly Color AccentYellow = Color.FromArgb("#FFCA28");

    public FavoriteRestaurantsPage()
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        BuildUI();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadFavoritesAsync();
    }

    private void BuildUI()
    {
        _content = new VerticalStackLayout { Spacing = 16, Padding = new Thickness(0, 0, 0, 40) };
        Content = new ScrollView { Content = _content, BackgroundColor = BgPage };
    }

    private async Task LoadFavoritesAsync()
    {
        _content.Children.Clear();
        _content.Add(BuildHeroHeader(Navigation));

        try
        {
            var favorites = await App.Database.GetFavoriteRestaurantsAsync();

            if (favorites == null || favorites.Count == 0)
            {
                _content.Add(BuildEmptyState("Chưa có quán yêu thích nào.\nHãy thả tim các quán ngon khi bạn trải nghiệm nhé! ❤️"));
                return;
            }

            var listContainer = new VerticalStackLayout { Spacing = 16, Padding = new Thickness(16, 0) };
            foreach (var r in favorites)
            {
                listContainer.Add(BuildRestaurantCard(r));
            }
            _content.Add(listContainer);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FavoriteRestaurantsPage] {ex.Message}");
            _content.Add(BuildEmptyState("Lỗi tải dữ liệu. Vui lòng thử lại."));
        }
    }

    private static Grid BuildHeroHeader(INavigation nav)
    {
        var grid = new Grid { HeightRequest = 140, BackgroundColor = Color.FromArgb("#1565C0") };

        grid.Add(new BoxView
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0), EndPoint = new Point(0, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb("#2A211565"), 0),
                    new GradientStop(Colors.Transparent, 1)
                }
            }
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

        var textStack = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.End,
            Margin = new Thickness(24, 0, 20, 20),
            Spacing = 4
        };
        textStack.Add(new Label { Text = "Quán yêu thích", FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = Colors.White });
        textStack.Add(new Label { Text = "Những điểm đến tuyệt vời của bạn", FontSize = 13, TextColor = TextSecond });
        grid.Add(textStack);

        grid.Add(new Label
        {
            Text = "⭐", FontSize = 48, Opacity = 0.15,
            HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.End, Margin = new Thickness(0, 0, 20, 10)
        });

        return grid;
    }

    private Border BuildRestaurantCard(Restaurant r)
    {
        var card = new Border
        {
            BackgroundColor = BgCard,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            StrokeThickness = 1,
            Stroke = new SolidColorBrush(Divider),
            Margin = new Thickness(0, 0, 0, 0)
        };

        var row = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Auto }, new ColumnDefinition { Width = GridLength.Star } },
            ColumnSpacing = 16,
            Padding = new Thickness(12)
        };

        View thumb;
        if (!string.IsNullOrWhiteSpace(r.ImageUrl))
        {
            thumb = new Border { StrokeShape = new RoundRectangle { CornerRadius = 12 }, StrokeThickness = 0, WidthRequest = 80, HeightRequest = 80, Content = new Image { Source = r.ImageUrl, Aspect = Aspect.AspectFill } };
        }
        else
        {
            thumb = new Border { BackgroundColor = Color.FromArgb("#15FFFFFF"), StrokeShape = new RoundRectangle { CornerRadius = 12 }, WidthRequest = 80, HeightRequest = 80, StrokeThickness = 0, Content = new Label { Text = "🍽️", FontSize = 36, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center } };
        }
        row.Add(thumb, 0, 0);

        var info = new VerticalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
        info.Add(new Label { Text = r.Name, FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = TextPrimary, LineBreakMode = LineBreakMode.TailTruncation, MaxLines = 1 });
        info.Add(new Label { Text = r.Description, FontSize = 12, TextColor = TextSecond, LineBreakMode = LineBreakMode.TailTruncation, MaxLines = 2 });

        var tagRow = new HorizontalStackLayout { Spacing = 12, Margin = new Thickness(0, 4, 0, 0) };
        tagRow.Add(new HorizontalStackLayout { Spacing = 4, Children = { new Label { Text = "⭐", FontSize = 12, VerticalOptions = LayoutOptions.Center }, new Label { Text = r.Rating.ToString("F1"), FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = AccentYellow, VerticalOptions = LayoutOptions.Center } } });
        tagRow.Add(new Label { Text = "•", TextColor = TextSecond, VerticalOptions = LayoutOptions.Center });
        tagRow.Add(new Label { Text = "❤️ Đã lưu", FontSize = 12, TextColor = Color.FromArgb("#E91E63"), FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center });

        info.Add(tagRow);
        row.Add(info, 1, 0);

        card.Content = row;
        return card;
    }

    private static Border BuildEmptyState(string msg) => new Border
    {
        BackgroundColor = BgCard,
        StrokeShape = new RoundRectangle { CornerRadius = 16 },
        StrokeThickness = 1,
        Stroke = new SolidColorBrush(Divider),
        Padding = new Thickness(24, 40),
        Margin = new Thickness(16, 20, 16, 0),
        Content = new VerticalStackLayout
        {
            Spacing = 12,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = "💔", FontSize = 48, HorizontalOptions = LayoutOptions.Center },
                new Label
                {
                    Text = msg, FontSize = 14, TextColor = TextSecond,
                    HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center, LineBreakMode = LineBreakMode.WordWrap
                }
            }
        }
    };
}

using Microsoft.Maui.Controls.Shapes;
using VinhKhanhTour.Services;

namespace VinhKhanhTour.Views
{
    /// <summary>
    /// Trang quản lý dữ liệu offline: tải audio + pre-warm map tiles khu vực Vĩnh Khánh.
    /// Mở từ offline banner trên MapPage hoặc từ ProfilePage.
    /// </summary>
    public class OfflineDownloadPage : ContentPage
    {
        // ── UI refs ───────────────────────────────────────────────────────────
        private Label _audioStatusLabel = null!;
        private Label _mapStatusLabel = null!;
        private Label _storageSizeLabel = null!;
        private Label _lastSyncLabel = null!;
        private ProgressBar _audioProgressBar = null!;
        private Label _audioProgressLabel = null!;
        private Button _btnDownload = null!;
        private Button _btnClear = null!;
        private Grid _progressSection = null!;

        public OfflineDownloadPage()
        {
            Title = "Tải về Offline";
            NavigationPage.SetHasNavigationBar(this, false);
            BackgroundColor = Color.FromArgb("#0D1B2A");
            BuildUI();
            RefreshStats();

            // ── Premium Gate Check ──
            if (!TicketService.Instance.HasValidTicket)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Navigation.PushAsync(new PremiumGatePage("Offline Downloads"));
                    Navigation.RemovePage(this);
                });
            }

            // Lắng nghe progress khi đang tải
            OfflineModeService.Instance.AudioCacheProgressChanged += OnAudioProgress;
            OfflineModeService.Instance.AudioCacheCompleted       += OnAudioCompleted;
            OfflineModeService.Instance.MapTilesWarmed            += OnMapWarmed;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            OfflineModeService.Instance.AudioCacheProgressChanged -= OnAudioProgress;
            OfflineModeService.Instance.AudioCacheCompleted       -= OnAudioCompleted;
            OfflineModeService.Instance.MapTilesWarmed            -= OnMapWarmed;
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  UI BUILD
        // ═══════════════════════════════════════════════════════════════════════
        private void BuildUI()
        {
            var scroll = new ScrollView();
            var root   = new VerticalStackLayout { Padding = new Thickness(0, 0, 0, 40), Spacing = 0 };

            // ── Header ───────────────────────────────────────────────────────
            var header = new Grid
            {
                BackgroundColor = Color.FromArgb("#0D1B2A"),
                Padding = new Thickness(20, 52, 20, 20),
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star)
                }
            };
            var backBtn = new Button
            {
                Text = "←",
                FontSize = 22,
                TextColor = Colors.White,
                BackgroundColor = Colors.Transparent,
                Padding = new Thickness(0),
                WidthRequest = 40,
                HeightRequest = 40
            };
            backBtn.Clicked += async (s, e) => await Navigation.PopAsync();
            header.Add(backBtn, 0, 0);
            header.Add(new VerticalStackLayout
            {
                Margin = new Thickness(12, 0, 0, 0),
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label { Text = "Chế độ Offline", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                    new Label { Text = "Tải dữ liệu để tham quan không cần mạng", FontSize = 13, TextColor = Color.FromArgb("#8BAABF") }
                }
            }, 1, 0);
            root.Add(header);

            // ── Network status card ───────────────────────────────────────────
            root.Add(BuildNetworkCard());

            // ── Stats cards row ───────────────────────────────────────────────
            root.Add(BuildStatsRow());

            // ── Download section ──────────────────────────────────────────────
            root.Add(BuildDownloadSection());

            // ── Progress section (ẩn khi không tải) ──────────────────────────
            root.Add(BuildProgressSection());

            // ── Danger zone (xóa cache) ───────────────────────────────────────
            root.Add(BuildDangerZone());

            // ── Info box ──────────────────────────────────────────────────────
            root.Add(BuildInfoBox());

            scroll.Content = root;
            Content = scroll;
        }

        // ── Network Status Card ───────────────────────────────────────────────
        private View BuildNetworkCard()
        {
            bool isOnline = OfflineService.Instance.IsOnline;
            bool apiOk    = OfflineService.Instance.IsApiReachable;

            string icon  = isOnline ? (apiOk ? "✅" : "⚠️") : "📵";
            string title = isOnline ? (apiOk ? "Đang kết nối" : "Mạng yếu / Server không phản hồi") : "Không có mạng";
            string sub   = isOnline ? (apiOk ? "Có thể tải dữ liệu offline ngay bây giờ" : "Không thể tải — thử lại sau")
                                    : "Cần có mạng để tải lần đầu";

            var bgColor = isOnline && apiOk ? "#1B2F1B" : (isOnline ? "#2A2010" : "#2A1010");
            var fgColor = isOnline && apiOk ? "#66BB6A" : (isOnline ? "#FFA726" : "#EF5350");

            return new Border
            {
                BackgroundColor = Color.FromArgb(bgColor),
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Stroke = Color.FromArgb(fgColor + "44"),
                StrokeThickness = 1,
                Margin = new Thickness(20, 16, 20, 0),
                Padding = new Thickness(18, 14),
                Content = new HorizontalStackLayout
                {
                    Spacing = 14,
                    VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label { Text = icon, FontSize = 28, VerticalOptions = LayoutOptions.Center },
                        new VerticalStackLayout
                        {
                            Spacing = 2,
                            VerticalOptions = LayoutOptions.Center,
                            Children =
                            {
                                new Label { Text = title, FontSize = 15, FontAttributes = FontAttributes.Bold,
                                            TextColor = Color.FromArgb(fgColor) },
                                new Label { Text = sub, FontSize = 12, TextColor = Color.FromArgb("#8BAABF") }
                            }
                        }
                    }
                }
            };
        }

        // ── Stats Row ─────────────────────────────────────────────────────────
        private View BuildStatsRow()
        {
            var row = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star)
                },
                Margin = new Thickness(20, 16, 20, 0),
                ColumnSpacing = 10
            };

            var stats = OfflineModeService.Instance.GetCacheStats();

            // Audio card
            _audioStatusLabel = new Label
            {
                Text = stats.IsAudioReady ? "✅ Đã tải" : "⬜ Chưa tải",
                FontSize = 11, FontAttributes = FontAttributes.Bold,
                TextColor = stats.IsAudioReady ? Color.FromArgb("#66BB6A") : Color.FromArgb("#8BAABF"),
                HorizontalOptions = LayoutOptions.Center
            };
            row.Add(BuildStatCard("🎧", "Audio", _audioStatusLabel), 0, 0);

            // Map card
            _mapStatusLabel = new Label
            {
                Text = stats.IsMapReady ? "✅ Đã warm" : "⬜ Chưa warm",
                FontSize = 11, FontAttributes = FontAttributes.Bold,
                TextColor = stats.IsMapReady ? Color.FromArgb("#66BB6A") : Color.FromArgb("#8BAABF"),
                HorizontalOptions = LayoutOptions.Center
            };
            row.Add(BuildStatCard("🗺️", "Bản đồ", _mapStatusLabel), 1, 0);

            // Storage card
            _storageSizeLabel = new Label
            {
                Text = stats.TotalSizeDisplay,
                FontSize = 18, FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#42A5F5"),
                HorizontalOptions = LayoutOptions.Center
            };
            row.Add(BuildStatCard("💾", "Dung lượng", _storageSizeLabel), 2, 0);

            return row;
        }

        private static Border BuildStatCard(string emoji, string label, Label valueLabel) => new Border
        {
            BackgroundColor = Color.FromArgb("#112233"),
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Stroke = Color.FromArgb("#1E3A5A"),
            StrokeThickness = 1,
            Padding = new Thickness(12, 14),
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label { Text = emoji, FontSize = 24, HorizontalOptions = LayoutOptions.Center },
                    valueLabel,
                    new Label { Text = label, FontSize = 10, TextColor = Color.FromArgb("#5A7A9A"),
                                HorizontalOptions = LayoutOptions.Center }
                }
            }
        };

        // ── Download Section ──────────────────────────────────────────────────
        private View BuildDownloadSection()
        {
            var card = new Border
            {
                BackgroundColor = Color.FromArgb("#112233"),
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Stroke = Color.FromArgb("#1E3A5A"),
                StrokeThickness = 1,
                Margin = new Thickness(20, 16, 20, 0),
                Padding = new Thickness(20, 20)
            };

            var content = new VerticalStackLayout { Spacing = 16 };

            content.Add(new Label
            {
                Text = "📦 Gói dữ liệu Offline",
                FontSize = 17, FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White
            });

            // Checklist items
            content.Add(BuildCheckItem("🎵", "Audio thuyết minh tất cả điểm (vi/en/zh)", "~3–5 MB"));
            content.Add(BuildCheckItem("🗺️", "Tiles bản đồ khu vực Vĩnh Khánh zoom 14–18", "~8–15 MB"));
            content.Add(BuildCheckItem("📍", "Dữ liệu điểm tham quan (đã có)", "✅ Cache SQLite"));

            // Last sync
            var stats = OfflineModeService.Instance.GetCacheStats();
            _lastSyncLabel = new Label
            {
                Text = $"Lần tải gần nhất: {stats.LastCacheDateDisplay}",
                FontSize = 12,
                TextColor = Color.FromArgb("#5A7A9A"),
                Margin = new Thickness(0, 4, 0, 0)
            };
            content.Add(_lastSyncLabel);

            // Download button
            bool isCaching = OfflineModeService.Instance.IsAudioCaching;
            _btnDownload = new Button
            {
                Text = isCaching ? "⏳ Đang tải..." : "⬇ Tải về để dùng Offline",
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HeightRequest = 56,
                CornerRadius = 16,
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint   = new Point(1, 0),
                    GradientStops =
                    {
                        new GradientStop(Color.FromArgb("#1565C0"), 0),
                        new GradientStop(Color.FromArgb("#1E88E5"), 1)
                    }
                },
                Shadow = new Shadow
                {
                    Brush = Color.FromArgb("#1565C0"), Opacity = 0.4f,
                    Radius = 12, Offset = new Point(0, 4)
                },
                IsEnabled = !isCaching
            };
            _btnDownload.Clicked += OnDownloadClicked;
            content.Add(_btnDownload);

            card.Content = content;
            return card;
        }

        private static View BuildCheckItem(string icon, string text, string sizeHint) =>
            new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 12
            }.Also(g =>
            {
                g.Add(new Label { Text = icon, FontSize = 18, VerticalOptions = LayoutOptions.Center }, 0, 0);
                g.Add(new Label
                {
                    Text = text, FontSize = 13, TextColor = Color.FromArgb("#BBCFDF"),
                    VerticalOptions = LayoutOptions.Center
                }, 1, 0);
                g.Add(new Label
                {
                    Text = sizeHint, FontSize = 11,
                    TextColor = Color.FromArgb("#5A7A9A"),
                    VerticalOptions = LayoutOptions.Center
                }, 2, 0);
            });

        // ── Progress Section ──────────────────────────────────────────────────
        private View BuildProgressSection()
        {
            _progressSection = new Grid
            {
                IsVisible = OfflineModeService.Instance.IsAudioCaching,
                Margin = new Thickness(20, 12, 20, 0)
            };

            var card = new Border
            {
                BackgroundColor = Color.FromArgb("#0A1A2A"),
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Stroke = Color.FromArgb("#1565C0"),
                StrokeThickness = 1,
                Padding = new Thickness(18, 14)
            };

            var inner = new VerticalStackLayout { Spacing = 10 };

            _audioProgressLabel = new Label
            {
                Text = "Đang tải audio...",
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#64B5F6")
            };

            _audioProgressBar = new ProgressBar
            {
                Progress = 0,
                ProgressColor = Color.FromArgb("#1565C0"),
                BackgroundColor = Color.FromArgb("#1E3A5A"),
                HeightRequest = 8
            };

            var cancelBtn = new Button
            {
                Text = "✕ Hủy tải",
                FontSize = 12,
                TextColor = Color.FromArgb("#EF5350"),
                BackgroundColor = Colors.Transparent,
                Padding = new Thickness(0),
                HorizontalOptions = LayoutOptions.End
            };
            cancelBtn.Clicked += (s, e) =>
            {
                OfflineModeService.Instance.CancelPreCache();
                _progressSection.IsVisible = false;
                _btnDownload.Text = "⬇ Tải về để dùng Offline";
                _btnDownload.IsEnabled = true;
            };

            inner.Add(_audioProgressLabel);
            inner.Add(_audioProgressBar);
            inner.Add(cancelBtn);
            card.Content = inner;
            _progressSection.Add(card);
            return _progressSection;
        }

        // ── Danger Zone ───────────────────────────────────────────────────────
        private View BuildDangerZone()
        {
            var card = new Border
            {
                BackgroundColor = Color.FromArgb("#1A0A0A"),
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Stroke = Color.FromArgb("#5A1A1A"),
                StrokeThickness = 1,
                Margin = new Thickness(20, 16, 20, 0),
                Padding = new Thickness(18, 14)
            };

            var inner = new VerticalStackLayout { Spacing = 12 };
            inner.Add(new Label
            {
                Text = "⚠️ Vùng nguy hiểm",
                FontSize = 14, FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#EF5350")
            });
            inner.Add(new Label
            {
                Text = "Xóa cache sẽ yêu cầu tải lại toàn bộ khi online. Bản đồ và audio sẽ không khả dụng offline cho đến khi tải lại.",
                FontSize = 12,
                TextColor = Color.FromArgb("#8BAABF"),
                LineBreakMode = LineBreakMode.WordWrap
            });

            _btnClear = new Button
            {
                Text = "🗑 Xóa cache Offline",
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#EF5350"),
                BackgroundColor = Color.FromArgb("#2A1010"),
                BorderColor = Color.FromArgb("#5A1A1A"),
                BorderWidth = 1,
                CornerRadius = 12,
                HeightRequest = 44
            };
            _btnClear.Clicked += OnClearClicked;
            inner.Add(_btnClear);

            card.Content = inner;
            return card;
        }

        // ── Info Box ──────────────────────────────────────────────────────────
        private static View BuildInfoBox() => new Border
        {
            BackgroundColor = Color.FromArgb("#0A1F0A"),
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Stroke = Color.FromArgb("#1A3A1A"),
            StrokeThickness = 1,
            Margin = new Thickness(20, 16, 20, 0),
            Padding = new Thickness(16, 12),
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label { Text = "💡 Cách hoạt động", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#66BB6A") },
                    new Label
                    {
                        Text = "1. Tải về một lần khi có WiFi\n2. Audio Google TTS chất lượng cao được lưu vào bộ nhớ\n3. Google Maps tự lưu tile bản đồ khu vực Vĩnh Khánh\n4. Khi offline: GPS vẫn hoạt động, đến gần POI → audio tự phát từ cache",
                        FontSize = 12,
                        TextColor = Color.FromArgb("#8BAABF"),
                        LineBreakMode = LineBreakMode.WordWrap,
                        LineHeight = 1.5
                    }
                }
            }
        };

        // ═══════════════════════════════════════════════════════════════════════
        //  Event Handlers
        // ═══════════════════════════════════════════════════════════════════════

        private async void OnDownloadClicked(object? sender, EventArgs e)
        {
            if (!OfflineService.Instance.IsOnline)
            {
                await DisplayAlert("Không có mạng",
                    "Cần có kết nối internet để tải dữ liệu offline. Vui lòng kết nối WiFi và thử lại.", "OK");
                return;
            }

            _btnDownload.Text    = "⏳ Đang tải...";
            _btnDownload.IsEnabled = false;
            _progressSection.IsVisible = true;
            _audioProgressBar.Progress = 0;
            _audioProgressLabel.Text   = "Đang khởi tạo...";

            // Chạy song song: audio + map tiles
            var audioTask = OfflineModeService.Instance.PreCacheAllAudioAsync();
            var mapTask   = OfflineModeService.Instance.PreWarmMapTilesAsync();
            await Task.WhenAll(audioTask, mapTask);
        }

        private async void OnClearClicked(object? sender, EventArgs e)
        {
            bool confirm = await DisplayAlert(
                "Xác nhận xóa",
                "Bạn có chắc muốn xóa toàn bộ dữ liệu offline? Cần tải lại để dùng offline.",
                "Xóa", "Hủy");
            if (!confirm) return;

            OfflineModeService.Instance.ClearAudioCache();
            RefreshStats();
            await DisplayAlert("Đã xóa", "Cache offline đã được xóa.", "OK");
        }

        private void OnAudioProgress(double progress)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _audioProgressBar.Progress = progress;
                int total    = OfflineModeService.Instance.TotalAudioFiles;
                int cached   = OfflineModeService.Instance.CachedAudioFiles;
                _audioProgressLabel.Text = $"Đang tải audio... {cached}/{total} files ({progress:P0})";
            });
        }

        private void OnAudioCompleted()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _progressSection.IsVisible  = false;
                _btnDownload.Text           = "✅ Đã tải — Tải lại";
                _btnDownload.IsEnabled      = true;
                RefreshStats();
            });
        }

        private void OnMapWarmed()
        {
            MainThread.BeginInvokeOnMainThread(RefreshStats);
        }

        // ── Refresh stats display ─────────────────────────────────────────────
        private void RefreshStats()
        {
            var stats = OfflineModeService.Instance.GetCacheStats();

            if (_audioStatusLabel != null)
            {
                _audioStatusLabel.Text      = stats.IsAudioReady ? "✅ Đã tải" : "⬜ Chưa tải";
                _audioStatusLabel.TextColor = stats.IsAudioReady
                    ? Color.FromArgb("#66BB6A") : Color.FromArgb("#8BAABF");
            }

            if (_mapStatusLabel != null)
            {
                _mapStatusLabel.Text      = stats.IsMapReady ? "✅ Đã warm" : "⬜ Chưa warm";
                _mapStatusLabel.TextColor = stats.IsMapReady
                    ? Color.FromArgb("#66BB6A") : Color.FromArgb("#8BAABF");
            }

            if (_storageSizeLabel != null)
                _storageSizeLabel.Text = stats.TotalSizeDisplay;

            if (_lastSyncLabel != null)
                _lastSyncLabel.Text = $"Lần tải gần nhất: {stats.LastCacheDateDisplay}";

            if (_btnDownload != null && !OfflineModeService.Instance.IsAudioCaching)
                _btnDownload.Text = stats.IsAudioReady
                    ? "✅ Đã tải — Tải lại" : "⬇ Tải về để dùng Offline";
        }
    }

}

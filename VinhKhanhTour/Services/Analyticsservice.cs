using VinhKhanhTour.Models;

namespace VinhKhanhTour.Services
{
    public class AnalyticsService
    {
        private static AnalyticsService? _instance;
        public static AnalyticsService Instance => _instance ??= new AnalyticsService();

        private AnalyticsService() { }

        public async Task RecordGpsPointAsync(double lat, double lng)
        {
            try
            {
                var evt = new AnalyticsEvent
                {
                    EventType = "gps_point",
                    Lat = lat,
                    Lng = lng,
                    TimestampTicks = DateTime.Now.Ticks
                };
                await App.Database.InsertAnalyticsEventAsync(evt);

                // Gửi lên server để CMS vẽ heatmap (fire-and-forget, ẩn danh)
                _ = ApiService.Instance.PostGpsPointAsync(lat, lng);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AnalyticsService] RecordGps: {ex.Message}");
            }
        }

        public async Task RecordPoiVisitAsync(int poiId, string eventType = "poi_visit", double lat = 0, double lng = 0)
        {
            try
            {
                var evt = new AnalyticsEvent
                {
                    EventType = eventType,
                    PoiId = poiId,
                    Lat = lat,
                    Lng = lng,
                    TimestampTicks = DateTime.Now.Ticks
                };
                await App.Database.InsertAnalyticsEventAsync(evt);
                bool success = await ApiService.Instance.PostAnalyticAsync(poiId, eventType, lat, lng);
                if (!success) System.Diagnostics.Debug.WriteLine($"[AnalyticsService] Warning: Could not sync {eventType} to server (POI: {poiId})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AnalyticsService] RecordVisit: {ex.Message}");
            }
        }

        public async Task RecordAudioPlayedAsync(int poiId, string lang, double durationSeconds)
        {
            try
            {
                var evt = new AnalyticsEvent
                {
                    EventType = $"audio_{lang}",
                    PoiId = poiId,
                    Value = durationSeconds,
                    TimestampTicks = DateTime.Now.Ticks
                };
                await App.Database.InsertAnalyticsEventAsync(evt);
                bool success = await ApiService.Instance.PostAnalyticAsync(poiId, $"audio_{lang}", 0, 0, durationSeconds);
                if (!success) System.Diagnostics.Debug.WriteLine($"[AnalyticsService] Warning: Could not sync Audio event to server (POI: {poiId})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AnalyticsService] RecordAudio: {ex.Message}");
            }
        }

        public async Task<List<AnalyticsEvent>> GetAllEventsAsync()
        {
            try { return await App.Database.GetAllAnalyticsEventsAsync(); }
            catch { return new List<AnalyticsEvent>(); }
        }

        public async Task<int> GetTotalVisitsAsync()
        {
            try
            {
                var events = await App.Database.GetAnalyticsEventsAsync("poi_visit");
                return events.Count;
            }
            catch { return 0; }
        }

        /// <summary>
        /// Trả về top N POI dưới dạng List PoiStats — dùng cho AnalyticsPage.
        /// Cải thiện: Tự động map tên từ DB/API để tránh hiện POI #ID.
        /// </summary>
        public async Task<List<PoiStats>> GetTopPoisAsync(int topN = 5)
        {
            try
            {
                // 1. Lấy tất cả sự kiện và lọc các ID hợp lệ (>0)
                var allEvents = await App.Database.GetAllAnalyticsEventsAsync();
                var visitEvents = allEvents.Where(e => e.PoiId > 0 && 
                    (e.EventType == "poi_visit" || e.EventType.StartsWith("audio_"))).ToList();

                if (visitEvents.Count == 0) return new List<PoiStats>();

                // 2. Lấy thông tin quán ăn để map tên
                var restaurants = await App.Database.GetRestaurantsAsync();
                
                // Nếu local DB thiếu thông tin (ví dụ cache bị xóa), sync nhanh từ API
                var uniqueIds = visitEvents.Select(e => e.PoiId).Distinct().ToList();
                bool needsSync = uniqueIds.Any(id => !restaurants.Any(r => r.Id == id)) || restaurants.Count == 0;

                if (needsSync)
                {
                    try
                    {
                        var apiList = await ApiService.Instance.GetRestaurantsAsync();
                        if (apiList != null && apiList.Count > 0)
                        {
                            foreach (var r in apiList) await App.Database.SaveRestaurantAsync(r);
                            restaurants = apiList;
                        }
                    }
                    catch { }
                }

                // 3. Phân tích top POI
                var grouped = visitEvents
                    .GroupBy(e => e.PoiId)
                    .OrderByDescending(g => g.Count())
                    .Take(topN);

                var result = new List<PoiStats>();
                foreach (var g in grouped)
                {
                    var r = restaurants.FirstOrDefault(x => x.Id == g.Key);
                    
                    // Nếu tuyệt đối không tìm được tên (r==null), bỏ qua entry này thay vì hiện ID
                    if (r == null) continue;

                    var audioEvents = g.Where(e => e.EventType.StartsWith("audio_")).ToList();
                    
                    result.Add(new PoiStats
                    {
                        PoiId = g.Key,
                        PoiName = r.Name,
                        ListenCount = audioEvents.Count,
                        TotalSeconds = audioEvents.Sum(e => e.Value)
                    });
                }
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AnalyticsService] GetTopPois: {ex.Message}");
                return new List<PoiStats>();
            }
        }

        /// <summary>
        /// Tổng hợp thống kê — trả về object với đúng tên property mà AnalyticsPage dùng
        /// </summary>
        public async Task<AnalyticsSummary> GetSummaryAsync()
        {
            try
            {
                var allEvents = await App.Database.GetAllAnalyticsEventsAsync();

                int totalListens = allEvents.Count(e => e.PoiId > 0 && e.EventType.StartsWith("audio_"));
                int uniquePois = allEvents.Where(e => e.PoiId > 0 && e.EventType.StartsWith("audio_"))
                                            .Select(e => e.PoiId).Distinct().Count();
                double totalSec = allEvents.Where(e => e.PoiId > 0 && e.EventType.StartsWith("audio_"))
                                            .Sum(e => e.Value);
                double avgSec = totalListens > 0 ? totalSec / totalListens : 0;
                int tourCompletes = allEvents.Count(e => e.EventType == "tour_complete");

                return new AnalyticsSummary
                {
                    TotalListens = totalListens,
                    UniquePois = uniquePois,
                    AvgListenSec = avgSec,
                    TourCompletes = tourCompletes
                };
            }
            catch
            {
                return new AnalyticsSummary();
            }
        }

        public async Task ClearAllAsync()
        {
            try
            {
                await App.Database.ClearAnalyticsAsync();
                await ApiService.Instance.ClearAnalyticsOnServerAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AnalyticsService] Clear: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Model tổng hợp thống kê — dùng để tránh lỗi tên tuple
    /// </summary>
    public class AnalyticsSummary
    {
        public int TotalListens { get; set; }
        public int UniquePois { get; set; }
        public double AvgListenSec { get; set; }
        public int TourCompletes { get; set; }
    }
}
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AnalyticsService] RecordGps: {ex.Message}");
            }
        }

        public async Task RecordPoiVisitAsync(int poiId)
        {
            try
            {
                var evt = new AnalyticsEvent
                {
                    EventType = "poi_visit",
                    PoiId = poiId,
                    TimestampTicks = DateTime.Now.Ticks
                };
                await App.Database.InsertAnalyticsEventAsync(evt);
                await ApiService.Instance.PostAnalyticAsync(poiId, "poi_visit");
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
        /// Trả về top N POI dưới dạng List PoiStats — dùng cho AnalyticsPage
        /// </summary>
        public async Task<List<PoiStats>> GetTopPoisAsync(int topN = 5)
        {
            try
            {
                var allEvents = await App.Database.GetAllAnalyticsEventsAsync();
                var restaurants = await App.Database.GetRestaurantsAsync();

                var visitEvents = allEvents.Where(e => e.EventType == "poi_visit" || e.EventType.StartsWith("audio_")).ToList();

                var grouped = visitEvents
                    .GroupBy(e => e.PoiId)
                    .OrderByDescending(g => g.Count())
                    .Take(topN);

                var result = new List<PoiStats>();
                foreach (var g in grouped)
                {
                    var r = restaurants.FirstOrDefault(x => x.Id == g.Key);
                    var audioEvents = g.Where(e => e.EventType.StartsWith("audio_")).ToList();
                    result.Add(new PoiStats
                    {
                        PoiId = g.Key,
                        PoiName = r?.Name ?? $"POI #{g.Key}",
                        ListenCount = audioEvents.Count,
                        TotalSeconds = audioEvents.Sum(e => e.Value)
                    });
                }
                return result;
            }
            catch { return new List<PoiStats>(); }
        }

        /// <summary>
        /// Tổng hợp thống kê — trả về object với đúng tên property mà AnalyticsPage dùng
        /// </summary>
        public async Task<AnalyticsSummary> GetSummaryAsync()
        {
            try
            {
                var allEvents = await App.Database.GetAllAnalyticsEventsAsync();

                int totalListens = allEvents.Count(e => e.EventType.StartsWith("audio_"));
                int uniquePois = allEvents.Where(e => e.EventType.StartsWith("audio_"))
                                            .Select(e => e.PoiId).Distinct().Count();
                double totalSec = allEvents.Where(e => e.EventType.StartsWith("audio_"))
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
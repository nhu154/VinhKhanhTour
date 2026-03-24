using VinhKhanhTour.Models;
using System.Linq;

namespace VinhKhanhTour.Services
{
    public class AnalyticsService
    {
        private static AnalyticsService? _instance;
        public static AnalyticsService Instance => _instance ??= new AnalyticsService();

        private readonly Dictionary<int, DateTime> _listenStartTimes = new();

        private readonly List<AnalyticsEvent> _gpsBuffer = new();
        private const int GPS_FLUSH_SIZE = 10;

        // ── Ghi events ───────────────────────

        public async Task RecordPoiEnterAsync(int poiId, double lat, double lng)
        {
            await WriteEventAsync(new AnalyticsEvent
            {
                EventType = "poi_enter",
                PoiId = poiId,
                Timestamp = DateTime.Now,
                Lat = lat,
                Lng = lng,
            });
        }

        public void RecordListenStart(int poiId)
        {
            _listenStartTimes[poiId] = DateTime.Now;

            _ = WriteEventAsync(new AnalyticsEvent
            {
                EventType = "listen_start",
                PoiId = poiId,
                Timestamp = DateTime.Now,
            });
        }

        public async Task RecordListenEndAsync(int poiId)
        {
            double seconds = 0;

            if (_listenStartTimes.TryGetValue(poiId, out var start))
            {
                seconds = (DateTime.Now - start).TotalSeconds;
                _listenStartTimes.Remove(poiId);
            }

            await WriteEventAsync(new AnalyticsEvent
            {
                EventType = "listen_end",
                PoiId = poiId,
                Timestamp = DateTime.Now,
                Value = seconds,
            });
        }

        public async Task RecordGpsPointAsync(double lat, double lng)
        {
            _gpsBuffer.Add(new AnalyticsEvent
            {
                EventType = "gps_path",
                Timestamp = DateTime.Now,
                Lat = lat,
                Lng = lng,
            });

            if (_gpsBuffer.Count >= GPS_FLUSH_SIZE)
                await FlushGpsBufferAsync();
        }

        public async Task RecordTourStartAsync(string tourName)
        {
            await WriteEventAsync(new AnalyticsEvent
            {
                EventType = "tour_start",
                Timestamp = DateTime.Now,
            });
        }

        public async Task RecordTourCompleteAsync(string tourName)
        {
            await WriteEventAsync(new AnalyticsEvent
            {
                EventType = "tour_complete",
                Timestamp = DateTime.Now,
            });
        }

        // ── Flush ───────────────────────────

        public async Task FlushGpsBufferAsync()
        {
            if (_gpsBuffer.Count == 0) return;

            var toFlush = _gpsBuffer.ToList();
            _gpsBuffer.Clear();

            foreach (var e in toFlush)
                await WriteEventAsync(e);
        }

        // ── Thống kê ────────────────────────

        public async Task<List<PoiStats>> GetTopPoisAsync(int topN = 5)
        {
            var events = await App.Database.GetAnalyticsEventsAsync("listen_start");
            var restaurants = await App.Database.GetRestaurantsAsync();

            var stats = events
                .GroupBy(e => e.PoiId)
                .Select(g =>
                {
                    var poi = restaurants.FirstOrDefault(r => r.Id == g.Key);

                    return new PoiStats
                    {
                        PoiId = g.Key,
                        PoiName = poi?.Name ?? $"POI #{g.Key}",
                        ListenCount = g.Count(),
                    };
                })
                .OrderByDescending(s => s.ListenCount)
                .Take(topN)
                .ToList();

            var endEvents = await App.Database.GetAnalyticsEventsAsync("listen_end");

            foreach (var stat in stats)
            {
                var durations = endEvents
                    .Where(e => e.PoiId == stat.PoiId && e.Value > 0);

                stat.TotalSeconds = durations.Sum(e => e.Value);
            }

            return stats;
        }

        public async Task<List<(double Lat, double Lng)>> GetGpsPathAsync(int maxPoints = 500)
        {
            await FlushGpsBufferAsync();

            var events = await App.Database.GetAnalyticsEventsAsync("gps_path");

            return events
                .Skip(Math.Max(0, events.Count - maxPoints)) // FIX TakeLast
                .Select(e => (e.Lat, e.Lng))
                .ToList();
        }

        public async Task<(int TotalListens, int UniquePois, double AvgListenSec, int TourCompletes)> GetSummaryAsync()
        {
            var allEvents = await App.Database.GetAllAnalyticsEventsAsync();

            var listens = allEvents.Where(e => e.EventType == "listen_start").ToList();
            var ends = allEvents.Where(e => e.EventType == "listen_end" && e.Value > 0).ToList();
            var tours = allEvents.Count(e => e.EventType == "tour_complete");

            int total = listens.Count;
            int unique = listens.Select(e => e.PoiId).Distinct().Count();
            double avgSec = ends.Count > 0 ? ends.Average(e => e.Value) : 0;

            return (total, unique, avgSec, tours);
        }

        // ── Helper ──────────────────────────

        private static async Task WriteEventAsync(AnalyticsEvent e)
        {
            try
            {
                await App.Database.InsertAnalyticsEventAsync(e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Analytics] {ex.Message}");
            }
        }
    }
}
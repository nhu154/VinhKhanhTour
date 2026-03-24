using SQLite;

namespace VinhKhanhTour.Models
{
    
    [Table("AnalyticsEvent")]
    public class AnalyticsEvent
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }

       
        public string EventType { get; set; } = string.Empty;

        
        public int PoiId { get; set; }

      
        public long TimestampTicks { get; set; }

        
        public double Value { get; set; }

      
        public double Lat { get; set; }

        
        public double Lng { get; set; }

        [Ignore]
        public DateTime Timestamp
        {
            get => new DateTime(TimestampTicks, DateTimeKind.Utc).ToLocalTime();
            set => TimestampTicks = value.ToUniversalTime().Ticks;
        }
    }

   
    public class PoiStats
    {
        public int PoiId { get; set; }
        public string PoiName { get; set; } = string.Empty;
        public int ListenCount { get; set; }
        public double TotalSeconds { get; set; }
        public double AvgSeconds => ListenCount > 0 ? TotalSeconds / ListenCount : 0;
    }
}
using SQLite;

namespace VinhKhanhTour.Models
{
    [Table("visit_history")]
    public class VisitHistory
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int RestaurantId { get; set; }
        public DateTime VisitedAt { get; set; }

        // User-scoped visits
        public string Username { get; set; } = string.Empty;
    }
}
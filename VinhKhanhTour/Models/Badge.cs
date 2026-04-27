namespace VinhKhanhTour.Models
{
    public class Badge
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Emoji { get; set; } = "🏅";
        public string Description { get; set; } = "";
        public int Points { get; set; } = 0;
        public string Color { get; set; } = "#1565C0";
    }

    public static class BadgeDefinitions
    {
        public static Badge Reviewer = new Badge 
        { 
            Id = "reviewer", 
            Name = "Người Phê Bình", 
            Emoji = "✍️", 
            Points = 50, 
            Color = "#E65100" 
        };
        
        public static Badge Photographer = new Badge 
        { 
            Id = "photographer", 
            Name = "Nhiếp Ảnh Gia", 
            Emoji = "📸", 
            Points = 100, 
            Color = "#E91E63" 
        };

        public static Badge Gourmet = new Badge
        {
            Id = "gourmet",
            Name = "Người Sành Ăn",
            Emoji = "😋",
            Points = 200,
            Color = "#4CAF50"
        };

        public static List<Badge> All = new List<Badge> { Reviewer, Photographer, Gourmet };
    }
}

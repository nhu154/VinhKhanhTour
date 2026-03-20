using SQLite;
using VinhKhanhTour.Models;

namespace VinhKhanhTour.Services
{
    public class DatabaseService
    {
        private readonly SQLiteAsyncConnection _database;

        public DatabaseService()
        {
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "vinhkhanh.db");
            _database = new SQLiteAsyncConnection(dbPath);
            _database.CreateTableAsync<Restaurant>().Wait();
            _database.CreateTableAsync<VisitHistory>().Wait();
        }

        public Task<List<Restaurant>> GetRestaurantsAsync()
        {
            return _database.Table<Restaurant>().ToListAsync();
        }

        public Task<int> SaveRestaurantAsync(Restaurant restaurant)
        {
            return _database.InsertAsync(restaurant);
        }

        public Task<int> SaveVisitAsync(VisitHistory visit)
        {
            return _database.InsertAsync(visit);
        }

        // ✅ Thêm: lấy toàn bộ lịch sử ghé thăm (mới nhất trước)
        public Task<List<VisitHistory>> GetVisitHistoryAsync()
        {
            return _database.Table<VisitHistory>()
                            .OrderByDescending(v => v.VisitedAt)
                            .ToListAsync();
        }
    }
}
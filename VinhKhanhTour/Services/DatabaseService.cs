using SQLite;
using VinhKhanhTour.Models;

namespace VinhKhanhTour.Services
{
    public class DatabaseService
    {
        private readonly SQLiteAsyncConnection _database;

        private const int DB_VERSION = 7;
        private const string VERSION_KEY = "db_version";

        public DatabaseService()
        {
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "vinhkhanh.db");
            _database = new SQLiteAsyncConnection(dbPath);
            Task.Run(async () => await InitAsync()).GetAwaiter().GetResult();
        }

        private async Task InitAsync()
        {
            await _database.CreateTableAsync<DbMeta>();

            var meta = await _database.Table<DbMeta>()
                                      .FirstOrDefaultAsync(m => m.Key == VERSION_KEY);
            int currentVersion = meta == null ? 0 : int.Parse(meta.Value);

            if (currentVersion < DB_VERSION)
            {
                try { await _database.DropTableAsync<Restaurant>(); } catch { }
                try { await _database.DropTableAsync<VisitHistory>(); } catch { }

                if (meta == null)
                    await _database.InsertAsync(new DbMeta { Key = VERSION_KEY, Value = DB_VERSION.ToString() });
                else
                {
                    meta.Value = DB_VERSION.ToString();
                    await _database.UpdateAsync(meta);
                }
            }

            await _database.CreateTableAsync<Restaurant>();
            await _database.CreateTableAsync<VisitHistory>();
            await _database.CreateTableAsync<AnalyticsEvent>();
            await _database.CreateTableAsync<User>();
            await _database.CreateTableAsync<Booking>();
        }

        // ── Auth ───────────────────────────────────────────────────

        public async Task<User?> GetUserAsync(string username, string password)
            => await _database.Table<User>()
                              .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);

        public async Task<bool> IsUsernameTakenAsync(string username)
            => await _database.Table<User>()
                              .FirstOrDefaultAsync(u => u.Username == username) != null;

        public Task<int> SaveUserAsync(User user)
            => _database.InsertAsync(user);

        // ── Restaurant ─────────────────────────────────────────────

        public Task<List<Restaurant>> GetRestaurantsAsync()
            => _database.Table<Restaurant>().ToListAsync();

        public Task<int> SaveRestaurantAsync(Restaurant restaurant)
            => _database.InsertAsync(restaurant);

        public Task<int> UpdateRestaurantAsync(Restaurant restaurant)
            => _database.UpdateAsync(restaurant);

        public Task<List<Restaurant>> GetFavoriteRestaurantsAsync()
            => _database.Table<Restaurant>().Where(r => r.IsFavorite).ToListAsync();

        public async Task DeleteRestaurantAsync(int id)
            => await _database.DeleteAsync<Restaurant>(id);

        // ── Visit ──────────────────────────────────────────────────

        public Task<int> SaveVisitAsync(VisitHistory visit)
            => _database.InsertAsync(visit);

        public Task<List<VisitHistory>> GetVisitHistoryAsync()
            => _database.Table<VisitHistory>()
                        .OrderByDescending(v => v.VisitedAt)
                        .ToListAsync();

        public Task<List<VisitHistory>> GetVisitHistoryByUserAsync(string username)
            => _database.Table<VisitHistory>()
                        .Where(v => v.Username == username)
                        .OrderByDescending(v => v.VisitedAt)
                        .ToListAsync();

        public Task<int> ClearVisitHistoryAsync()
            => _database.DeleteAllAsync<VisitHistory>();

        // ── Booking ────────────────────────────────────────────────

        public Task<int> SaveBookingAsync(Booking booking)
            => _database.InsertAsync(booking);

        public Task<int> UpdateBookingAsync(Booking booking)
            => _database.UpdateAsync(booking);

        public Task<List<Booking>> GetAllBookingsAsync()
            => _database.Table<Booking>()
                        .OrderByDescending(b => b.CreatedAtTicks)
                        .ToListAsync();

        public Task<List<Booking>> GetBookingsByRestaurantAsync(int restaurantId)
            => _database.Table<Booking>()
                        .Where(b => b.RestaurantId == restaurantId)
                        .OrderByDescending(b => b.CreatedAtTicks)
                        .ToListAsync();

        public Task<List<Booking>> GetPendingBookingsAsync()
            => _database.Table<Booking>()
                        .Where(b => b.SyncStatus == "pending")
                        .ToListAsync();

        public async Task<Booking?> GetBookingByIdAsync(int id)
            => await _database.Table<Booking>()
                               .FirstOrDefaultAsync(b => b.Id == id);

        public Task<int> DeleteBookingAsync(int id)
            => _database.DeleteAsync<Booking>(id);

        // ── Analytics ──────────────────────────────────────────────

        public Task<int> InsertAnalyticsEventAsync(AnalyticsEvent e)
            => _database.InsertAsync(e);

        public Task<int> UpdateAnalyticsEventAsync(AnalyticsEvent e)
            => _database.UpdateAsync(e);

        public Task<List<AnalyticsEvent>> GetAnalyticsEventsAsync(string eventType)
            => _database.Table<AnalyticsEvent>()
                        .Where(e => e.EventType == eventType)
                        .OrderBy(e => e.TimestampTicks)
                        .ToListAsync();

        public Task<List<AnalyticsEvent>> GetAllAnalyticsEventsAsync()
            => _database.Table<AnalyticsEvent>()
                        .OrderBy(e => e.TimestampTicks)
                        .ToListAsync();

        public Task<int> ClearAnalyticsAsync()
            => _database.DeleteAllAsync<AnalyticsEvent>();
    }

    [Table("db_meta")]
    public class DbMeta
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
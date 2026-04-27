using SQLite;
using VinhKhanhTour.Models;

namespace VinhKhanhTour.Services
{
    public class DatabaseService
    {
        private readonly SQLiteAsyncConnection _database;

        private const int DB_VERSION = 8;
        private const string VERSION_KEY = "db_version";

        private Task? _initTask;
        private async Task EnsureInitializedAsync()
        {
            if (_initTask == null)
            {
                _initTask = InitAsync();
            }
            await _initTask;
        }

        public DatabaseService()
        {
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "vinhkhanh.db");
            _database = new SQLiteAsyncConnection(dbPath);
            // Không block UI thread bằng GetResult(), dùng pattern EnsureInitializedAsync trong mỗi phương thức
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

        public async Task<int> SaveUserAsync(User user)
        {
            await EnsureInitializedAsync();
            return await _database.InsertAsync(user);
        }

        // ── Restaurant ─────────────────────────────────────────────

        public async Task<List<Restaurant>> GetRestaurantsAsync()
        {
            await EnsureInitializedAsync();
            return await _database.Table<Restaurant>().ToListAsync();
        }

        public async Task<Restaurant?> GetRestaurantByIdAsync(int id)
        {
            await EnsureInitializedAsync();
            return await _database.Table<Restaurant>().FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<int> SaveRestaurantAsync(Restaurant restaurant)
        {
            await EnsureInitializedAsync();
            return await _database.InsertAsync(restaurant);
        }

        public async Task<int> UpdateRestaurantAsync(Restaurant restaurant)
        {
            await EnsureInitializedAsync();
            return await _database.UpdateAsync(restaurant);
        }

        public async Task<List<Restaurant>> GetFavoriteRestaurantsAsync()
        {
            await EnsureInitializedAsync();
            return await _database.Table<Restaurant>().Where(r => r.IsFavorite).ToListAsync();
        }

        public async Task DeleteRestaurantAsync(int id)
        {
            await EnsureInitializedAsync();
            await _database.DeleteAsync<Restaurant>(id);
        }

        // ── Visit ──────────────────────────────────────────────────

        public async Task<int> SaveVisitAsync(VisitHistory visit)
        {
            await EnsureInitializedAsync();
            return await _database.InsertAsync(visit);
        }

        public async Task<List<VisitHistory>> GetVisitHistoryAsync()
        {
            await EnsureInitializedAsync();
            return await _database.Table<VisitHistory>()
                        .OrderByDescending(v => v.VisitedAt)
                        .ToListAsync();
        }

        public async Task<List<VisitHistory>> GetVisitHistoryByUserAsync(string username)
        {
            await EnsureInitializedAsync();
            return await _database.Table<VisitHistory>()
                        .Where(v => v.Username == username)
                        .OrderByDescending(v => v.VisitedAt)
                        .ToListAsync();
        }

        public async Task<int> ClearVisitHistoryAsync()
        {
            await EnsureInitializedAsync();
            return await _database.DeleteAllAsync<VisitHistory>();
        }

        // ── Booking ────────────────────────────────────────────────

        public async Task<int> SaveBookingAsync(Booking booking)
        {
            await EnsureInitializedAsync();
            return await _database.InsertAsync(booking);
        }

        public async Task<int> UpdateBookingAsync(Booking booking)
        {
            await EnsureInitializedAsync();
            return await _database.UpdateAsync(booking);
        }

        public async Task<List<Booking>> GetAllBookingsAsync()
        {
            await EnsureInitializedAsync();
            return await _database.Table<Booking>()
                        .OrderByDescending(b => b.CreatedAtTicks)
                        .ToListAsync();
        }

        public async Task<List<Booking>> GetBookingsByRestaurantAsync(int restaurantId)
        {
            await EnsureInitializedAsync();
            return await _database.Table<Booking>()
                        .Where(b => b.RestaurantId == restaurantId)
                        .OrderByDescending(b => b.CreatedAtTicks)
                        .ToListAsync();
        }

        public async Task<List<Booking>> GetPendingBookingsAsync()
        {
            await EnsureInitializedAsync();
            return await _database.Table<Booking>()
                        .Where(b => b.SyncStatus == "pending")
                        .ToListAsync();
        }

        public async Task<Booking?> GetBookingByIdAsync(int id)
        {
            await EnsureInitializedAsync();
            return await _database.Table<Booking>()
                               .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<int> DeleteBookingAsync(int id)
        {
            await EnsureInitializedAsync();
            return await _database.DeleteAsync<Booking>(id);
        }

        // ── Analytics ──────────────────────────────────────────────

        public async Task<int> InsertAnalyticsEventAsync(AnalyticsEvent e)
        {
            await EnsureInitializedAsync();
            return await _database.InsertAsync(e);
        }

        public async Task<int> UpdateAnalyticsEventAsync(AnalyticsEvent e)
        {
            await EnsureInitializedAsync();
            return await _database.UpdateAsync(e);
        }

        public async Task<List<AnalyticsEvent>> GetAnalyticsEventsAsync(string eventType)
        {
            await EnsureInitializedAsync();
            return await _database.Table<AnalyticsEvent>()
                        .Where(e => e.EventType == eventType)
                        .OrderBy(e => e.TimestampTicks)
                        .ToListAsync();
        }

        public async Task<List<AnalyticsEvent>> GetAllAnalyticsEventsAsync()
        {
            await EnsureInitializedAsync();
            return await _database.Table<AnalyticsEvent>()
                        .OrderBy(e => e.TimestampTicks)
                        .ToListAsync();
        }

        public async Task<int> ClearAnalyticsAsync()
        {
            await EnsureInitializedAsync();
            return await _database.DeleteAllAsync<AnalyticsEvent>();
        }
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
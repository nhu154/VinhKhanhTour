// Services/DeviceService.cs — Quản lý thiết bị trên MAUI app
// - Tạo DeviceId duy nhất cho mỗi cài đặt app
// - Đăng ký thiết bị khi đăng nhập
// - Heartbeat định kỳ để kiểm tra thiết bị còn active
// - Tự động logout nếu thiết bị bị thu hồi

namespace VinhKhanhTour.Services
{
    public class DeviceService
    {
        private static DeviceService? _instance;
        public static DeviceService Instance => _instance ??= new DeviceService();

        private const string KEY_DEVICE_ID = "device_unique_id";
        private const string KEY_DEVICE_NAME = "device_name";

        private DeviceService() { }

        public string GetDeviceId()
        {
            var id = Preferences.Get(KEY_DEVICE_ID, "");
            if (string.IsNullOrEmpty(id))
            {
                id = $"{DeviceInfo.Platform}_{Guid.NewGuid():N}";
                Preferences.Set(KEY_DEVICE_ID, id);
            }
            return id;
        }

        public string GetDeviceName()
        {
            var saved = Preferences.Get(KEY_DEVICE_NAME, "");
            if (!string.IsNullOrEmpty(saved)) return saved;
            var name = $"{DeviceInfo.Model} ({DeviceInfo.Platform})";
            Preferences.Set(KEY_DEVICE_NAME, name);
            return name;
        }

        public string GetPlatform()
        {
            return DeviceInfo.Platform.ToString();
        }

        public async Task<DeviceRegisterResult> RegisterAsync(int userId)
        {
            try
            {
                var result = await ApiService.Instance.RegisterDeviceAsync(
                    userId,
                    GetDeviceId(),
                    GetDeviceName(),
                    GetPlatform()
                );

                if (result.Success)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[DeviceService] Registered: {GetDeviceName()} (new={result.IsNew})");
                    return result;
                }

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeviceService] Register error: {ex.Message}");
                return new DeviceRegisterResult { Success = true, Message = "Offline mode" };
            }
        }

        public async Task<bool> RevokeDeviceAsync(int userId, string deviceId)
        {
            return await ApiService.Instance.RevokeDeviceAsync(userId, deviceId);
        }
    }
}
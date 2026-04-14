using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using VinhKhanhTour.Models;

namespace VinhKhanhTour.Services
{
    public class PremiumPaymentService
    {
        private readonly HttpClient _httpClient;
        
        // Cấu hình URL của Backend 
        // Lấy từ Preferences tương tự ApiService.Instance hoặc Fix cứng IP
        private string BaseUrl
        {
            get
            {
                var saved = Preferences.Default.Get("api_base_url", "");
                if (!string.IsNullOrWhiteSpace(saved)) return saved.TrimEnd('/') + "/payments";
                return "http://192.168.1.29:5256/api/payments";
            }
        }

        public PremiumPaymentService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
        }

        // 1. Gọi API POST để khởi tạo giao dịch thanh toán
        public async Task<PaymentResponse?> CreatePaymentAsync(string ticketType, double amount)
        {
            try
            {
                var requestBody = new PaymentRequest
                {
                    TicketType = ticketType,
                    Amount = amount
                };

                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/create", requestBody);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<PaymentResponse>();
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PremiumPayment] Create Error: {ex.Message}");
                return null;
            }
        }

        // 2. Polling API GET status mỗi 2 giây
        public async Task<bool> PollPaymentStatusAsync(string transactionId, int maxAttempts = 30) // chờ tối đa 60s
        {
            int attempts = 0;

            while (attempts < maxAttempts)
            {
                try
                {
                    var response = await _httpClient.GetAsync($"{BaseUrl}/status/{transactionId}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var statusResponse = await response.Content.ReadFromJsonAsync<PaymentStatusResponse>();
                        
                        if (statusResponse != null && statusResponse.Status == "success")
                        {
                            return true; // Thanh toán thành công
                        }
                        if (statusResponse != null && statusResponse.Status == "failed")
                        {
                            return false; // Giao dịch bị từ chối/thất bại
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PremiumPayment] Poll Error: {ex.Message}");
                }

                attempts++;
                await Task.Delay(2000); 
            }

            return false; // Hết thời gian chờ (Timeout)
        }

        // 3. Xử lý toàn bộ luồng mua Premium
        public async Task<bool> ProcessPremiumPurchaseAsync(string ticketType, double amount, Page contextPage)
        {
            // Bước 1: Tạo Payment
            var paymentData = await CreatePaymentAsync(ticketType, amount);
            
            if (paymentData == null || string.IsNullOrEmpty(paymentData.TransactionId))
            {
                await contextPage.DisplayAlert("Lỗi", "Không thể khởi tạo thanh toán. Vui lòng kiểm tra lại kết nối.", "Đóng");
                return false;
            }

            // (Demo App) Tùy chọn: Mở link app MoMo ảo nếu cần
            // if (!string.IsNullOrEmpty(paymentData.PaymentUrl))
            // {
            //     // await Browser.OpenAsync(paymentData.PaymentUrl, BrowserLaunchMode.SystemPreferred);
            // }

            // Bước 2: Polling chờ trạng thái thanh toán từ hệ thống
            bool isSuccess = await PollPaymentStatusAsync(paymentData.TransactionId);

            // Bước 3: Trả về kết quả
            if (!isSuccess)
            {
                await contextPage.DisplayAlert("Thất bại", "Giao dịch không thành công hoặc đã hết hạn chờ thanh toán.", "Đóng");
            }

            return isSuccess;
        }
    }

    // ── Data Models ───────────────────────────────────────
    
    public class PaymentRequest
    {
        public string TicketType { get; set; } = "";
        public double Amount { get; set; }
    }

    public class PaymentResponse
    {
        public string TransactionId { get; set; } = "";
        public string PaymentUrl { get; set; } = ""; 
    }

    public class PaymentStatusResponse
    {
        public string Status { get; set; } = "pending"; 
    }
}

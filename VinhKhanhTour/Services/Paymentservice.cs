using VinhKhanhTour.Models;

namespace VinhKhanhTour.Services
{
    /// <summary>
    /// Xử lý tạo booking và luồng thanh toán.
    ///
    /// LUỒNG ĐÃ THIẾT KẾ LẠI:
    ///   - Cash    : Đặt chỗ ngay, không cần cọc, thanh toán tại quán.
    ///   - Ví điện tử (VNPay/MoMo/ZaloPay):
    ///       1. Tạo booking với PaymentStatus = "awaiting_payment"
    ///       2. Mở ví (deeplink hoặc hiển thị QR giả)
    ///       3. Người dùng xác nhận "Tôi đã thanh toán" → PaymentStatus = "paid"
    ///       4. Nếu huỷ → PaymentStatus = "cancelled", booking bị xoá
    ///   - Offline : Chỉ cho phép Cash khi offline.
    /// </summary>
    public class PaymentService
    {
        private static PaymentService? _instance;
        public static PaymentService Instance => _instance ??= new PaymentService();

        private PaymentService() { }

        // ── Tạo booking ───────────────────────────────────────────────────────

        public async Task<Booking> CreateBookingAsync(
            Restaurant restaurant,
            string customerName,
            string customerPhone,
            int guestCount,
            DateTime bookingDateTime,
            string note,
            string paymentMethod,
            double depositAmount = 0)
        {
            var booking = new Booking
            {
                RestaurantId = restaurant.Id,
                RestaurantName = restaurant.Name,
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                GuestCount = guestCount,
                BookingDate = bookingDateTime.ToString("dd/MM/yyyy"),
                BookingTime = bookingDateTime.ToString("HH:mm"),
                Note = note,
                PaymentMethod = paymentMethod,
                // Cash: pending (chờ thanh toán tại quán)
                // Ví:   awaiting_payment (chờ người dùng xác nhận đã chuyển)
                PaymentStatus = paymentMethod == "cash" ? "pending" : "awaiting_payment",
                DepositAmount = depositAmount,
                Status = "confirmed",
                BookingCode = GenerateBookingCode(),
                SyncStatus = "pending",
                CreatedAt = DateTime.Now
            };

            await App.Database.SaveBookingAsync(booking);
            System.Diagnostics.Debug.WriteLine($"[PaymentService] Booking created: {booking.BookingCode}");
            return booking;
        }

        // ── Xác nhận đã thanh toán ví điện tử ────────────────────────────────

        /// <summary>
        /// Gọi khi người dùng bấm "Tôi đã thanh toán" trên màn hình QR.
        /// Cập nhật PaymentStatus → "paid" và thử sync ngay.
        /// </summary>
        public async Task<PaymentResult> ConfirmEWalletPaymentAsync(Booking booking)
        {
            booking.PaymentStatus = "paid";
            booking.SyncStatus = "pending";
            await App.Database.UpdateBookingAsync(booking);

            if (OfflineService.Instance.IsOnline)
                _ = Task.Run(() => OfflineService.Instance.SyncPendingBookingsAsync());

            return new PaymentResult
            {
                Success = true,
                Message = $"Thanh toán {booking.PaymentDisplay} thành công! Cọc: {booking.DepositAmount:N0}đ",
                BookingCode = booking.BookingCode
            };
        }

        // ── Hoàn tất booking Cash ─────────────────────────────────────────────

        public async Task<PaymentResult> FinalizeCashBookingAsync(Booking booking)
        {
            booking.PaymentStatus = "pending"; // Sẽ thu tại quán
            booking.SyncStatus = "pending";
            await App.Database.UpdateBookingAsync(booking);

            if (OfflineService.Instance.IsOnline)
                _ = Task.Run(() => OfflineService.Instance.SyncPendingBookingsAsync());

            return new PaymentResult
            {
                Success = true,
                Message = "Đặt chỗ thành công! Thanh toán tiền mặt tại quán.",
                BookingCode = booking.BookingCode
            };
        }

        // ── Huỷ booking đang chờ thanh toán ──────────────────────────────────

        public async Task<bool> CancelPendingBookingAsync(int bookingId)
        {
            var booking = await App.Database.GetBookingByIdAsync(bookingId);
            if (booking == null) return false;

            // Nếu chưa thanh toán → xoá hẳn khỏi local
            if (booking.PaymentStatus == "awaiting_payment")
            {
                await App.Database.DeleteBookingAsync(bookingId);
                return true;
            }

            // Nếu đã thanh toán → đổi trạng thái thành cancelled (để hoàn tiền)
            booking.Status = "cancelled";
            booking.SyncStatus = "pending";
            await App.Database.UpdateBookingAsync(booking);

            if (OfflineService.Instance.IsOnline)
                _ = Task.Run(() => OfflineService.Instance.SyncPendingBookingsAsync());

            return true;
        }

        // ── Huỷ booking đã xác nhận ───────────────────────────────────────────

        public async Task<bool> CancelBookingAsync(int bookingId)
        {
            var booking = await App.Database.GetBookingByIdAsync(bookingId);
            if (booking == null) return false;

            booking.Status = "cancelled";
            booking.SyncStatus = "pending";
            await App.Database.UpdateBookingAsync(booking);

            if (OfflineService.Instance.IsOnline)
                _ = Task.Run(() => OfflineService.Instance.SyncPendingBookingsAsync());

            return true;
        }

        // ── Thông tin deeplink / QR giả (dùng cho màn hình thanh toán) ────────

        public EWalletPaymentInfo GetEWalletInfo(string method, double amount, string bookingCode)
        {
            return method switch
            {
                "vnpay" => new EWalletPaymentInfo
                {
                    WalletName = "VNPay",
                    Icon = "💳",
                    Color = "#0065A9",
                    // Deep link thật: vnpayqr://pay?... (cần SDK VNPay)
                    // Hiện tại mở website fallback
                    DeepLink = $"https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?vnp_Amount={amount * 100}&vnp_OrderInfo={bookingCode}",
                    QrContent = $"VNPay|{bookingCode}|{amount:N0}",
                    Instruction = $"Mở app VNPay → Quét QR hoặc nhập mã\nSố tiền cọc: {amount:N0}đ",
                    Amount = amount
                },
                "momo" => new EWalletPaymentInfo
                {
                    WalletName = "MoMo",
                    Icon = "🟣",
                    Color = "#B0006D",
                    DeepLink = $"momo://transfer?amount={(int)amount}&note={Uri.EscapeDataString("Coc dat cho " + bookingCode)}",
                    QrContent = $"MoMo|{bookingCode}|{amount:N0}",
                    Instruction = $"Mở app MoMo → Quét QR\nSố tiền cọc: {amount:N0}đ",
                    Amount = amount
                },
                "zalopay" => new EWalletPaymentInfo
                {
                    WalletName = "ZaloPay",
                    Icon = "🔵",
                    Color = "#0068FF",
                    DeepLink = $"zalopay://transfer?amount={(int)amount}&description={Uri.EscapeDataString("Coc dat cho " + bookingCode)}",
                    QrContent = $"ZaloPay|{bookingCode}|{amount:N0}",
                    Instruction = $"Mở app ZaloPay → Quét QR\nSố tiền cọc: {amount:N0}đ",
                    Amount = amount
                },
                _ => throw new ArgumentException($"Unknown payment method: {method}")
            };
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private string GenerateBookingCode()
        {
            var rnd = new Random();
            return $"VK{DateTime.Now:yyyyMMdd}-{rnd.Next(1000, 9999)}";
        }
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string BookingCode { get; set; } = "";
    }

    public class EWalletPaymentInfo
    {
        public string WalletName { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Color { get; set; } = "#333333";
        public string DeepLink { get; set; } = "";
        public string QrContent { get; set; } = "";
        public string Instruction { get; set; } = "";
        public double Amount { get; set; }
    }
}
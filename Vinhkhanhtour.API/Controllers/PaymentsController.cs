using Microsoft.AspNetCore.Mvc;

namespace VinhkhanhTour.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        // Lưu in-memory các giao dịch để phục vụ cho mục đích Đồ án (Demo Polling)
        private static readonly Dictionary<string, DateTime> _transactions = new();

        [HttpPost("create")]
        public IActionResult CreatePayment([FromBody] PaymentRequest request)
        {
            // Gen transaction ID ngẫu nhiên
            var transactionId = $"TXN{DateTime.Now:yyyyMMddHHmmss}-{new Random().Next(1000, 9999)}";
            
            // Lưu thời điểm tạo
            _transactions[transactionId] = DateTime.Now;

            return Ok(new PaymentResponse
            {
                TransactionId = transactionId,
                PaymentUrl = $"https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?vnp_Amount={request.Amount * 100}&vnp_OrderInfo={transactionId}" // Link ảo demo
            });
        }

        [HttpGet("status/{transactionId}")]
        public IActionResult GetPaymentStatus(string transactionId)
        {
            if (!_transactions.ContainsKey(transactionId))
            {
                return NotFound(new { message = "Giao dịch không tồn tại" });
            }

            var createdAt = _transactions[transactionId];
            var elapsedSeconds = (DateTime.Now - createdAt).TotalSeconds;

            // Giả lập: Giao dịch sẽ thành công sau 6 giây
            if (elapsedSeconds > 6)
            {
                // Dọn dẹp bộ nhớ (Tùy chọn)
                _transactions.Remove(transactionId);
                return Ok(new PaymentStatusResponse { Status = "success" });
            }

            return Ok(new PaymentStatusResponse { Status = "pending" });
        }
    }

    // ── Models ───────────────────────────────────────

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

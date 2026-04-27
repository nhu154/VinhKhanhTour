using Microsoft.AspNetCore.Mvc;

namespace VinhkhanhTour.API.Controllers
{
    /// <summary>
    /// Xử lý redirect cho QR code cũ dạng /smarttour-{slug}
    /// Chuyển về deeplink vinhkhanhtour://poi/{id}?autoplay=true
    /// để app MAUI tự mở đúng quán.
    /// </summary>
    [ApiController]
    public class SmartTourController : ControllerBase
    {
        // ── Mapping slug → POI ID ────────────────────────────────────
        // Thêm slug vào đây nếu có QR mới.
        // Slug là phần sau "smarttour-" trong URL QR bảng quán.
        private static readonly Dictionary<string, int> SlugMap = new(StringComparer.OrdinalIgnoreCase)
        {
            // TODO: điền đúng POI ID cho từng quán
            { "red",        1  },  // ← sửa ID này theo quán có QR "smarttour-red"
            { "oc-oanh",    1  },
            { "oc-sau-no",  2  },
            { "oc-thao",    3  },
            { "lang-quan",  4  },
            { "ot-xiem",    5  },
            { "bun-ca",     6  },
            { "chilli",     7  },
            { "the-gioi-bo",8  },
            { "com-chay",   9  },
            { "bo-la-lot",  10 },
            { "bun-thit",   11 },
        };

        // GET /smarttour-{slug}
        [HttpGet("smarttour-{slug}")]
        public IActionResult RedirectToApp(string slug)
        {
            if (!SlugMap.TryGetValue(slug, out int poiId))
            {
                // Slug chưa được map → trả về trang thông báo thân thiện
                return Content($@"
<!DOCTYPE html>
<html lang='vi'>
<head>
  <meta charset='UTF-8'>
  <meta name='viewport' content='width=device-width,initial-scale=1'>
  <title>Vĩnh Khánh Tour</title>
  <style>
    body {{ font-family: system-ui, sans-serif; display:flex; flex-direction:column;
           align-items:center; justify-content:center; min-height:100vh;
           margin:0; background:#f8fafc; color:#1e293b; text-align:center; padding:24px; }}
    h2 {{ font-size:22px; margin-bottom:8px; }}
    p  {{ color:#64748b; font-size:15px; line-height:1.6; }}
    .icon {{ font-size:56px; margin-bottom:16px; }}
  </style>
</head>
<body>
  <div class='icon'>🦪</div>
  <h2>Phố Ẩm Thực Vĩnh Khánh</h2>
  <p>Mã QR chưa được cấu hình.<br>
     Vui lòng liên hệ ban quản lý để được hỗ trợ.<br><br>
     <small style='color:#94a3b8'>Slug: {slug}</small>
  </p>
</body>
</html>", "text/html");
            }

            // Deeplink → app MAUI tự mở RestaurantDetailPage + autoplay audio
            var deepLink = $"vinhkhanhtour://poi/{poiId}?autoplay=true";

            // Trả về trang HTML có cả JS redirect + fallback link
            // iOS/Android sẽ intercept deeplink nếu app đã cài, nếu không có app thì
            // user thấy nút "Mở trong app"
            return Content($@"
<!DOCTYPE html>
<html lang='vi'>
<head>
  <meta charset='UTF-8'>
  <meta name='viewport' content='width=device-width,initial-scale=1'>
  <title>Đang mở Vĩnh Khánh Tour...</title>
  <style>
    body {{ font-family: system-ui, sans-serif; display:flex; flex-direction:column;
           align-items:center; justify-content:center; min-height:100vh;
           margin:0; background:#0a1628; color:#fff; text-align:center; padding:24px; }}
    h2   {{ font-size:20px; margin-bottom:8px; }}
    p    {{ color:rgba(255,255,255,0.55); font-size:14px; line-height:1.6; }}
    .icon {{ font-size:56px; margin-bottom:16px; }}
    .btn {{ display:inline-block; margin-top:24px; padding:14px 32px;
            background:#2563eb; color:#fff; border-radius:14px;
            text-decoration:none; font-weight:700; font-size:16px; }}
  </style>
  <script>
    window.onload = function() {{
      window.location.href = '{deepLink}';
    }};
  </script>
</head>
<body>
  <div class='icon'>🦪</div>
  <h2>Đang mở ứng dụng...</h2>
  <p>Nếu không tự động chuyển,<br>hãy nhấn nút bên dưới.</p>
  <a class='btn' href='{deepLink}'>Mở trong VinhKhánh Tour</a>
</body>
</html>", "text/html");
        }
    }
}
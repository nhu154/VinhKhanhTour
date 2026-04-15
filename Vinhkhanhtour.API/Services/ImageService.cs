using System.Text.RegularExpressions;

namespace VinhkhanhTour.API.Services
{
    /// <summary>
    /// Xử lý ảnh base64 — tách riêng để tránh duplicate trong các Controller.
    /// </summary>
    public class ImageService
    {
        private readonly string _uploadDir;
        private const long MaxBytes = 3 * 1024 * 1024; // 3 MB

        public ImageService(IWebHostEnvironment env)
        {
            _uploadDir = Path.Combine(
                env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                "uploads");
            Directory.CreateDirectory(_uploadDir);
        }

        /// <summary>
        /// Nếu imageData là base64 → lưu thành file, trả về path tương đối.
        /// Nếu đã là URL/path → giữ nguyên.
        /// </summary>
        public string SaveIfBase64(string? imageData, string prefix = "img")
        {
            if (string.IsNullOrEmpty(imageData)) return imageData ?? "";
            if (!imageData.Contains("base64,")) return imageData;

            try
            {
                var match = Regex.Match(imageData, @"data:image/(?<ext>.*?);base64,(?<data>.*)");
                if (!match.Success) return imageData;

                var ext = match.Groups["ext"].Value.Replace("jpeg", "jpg");
                var bytes = Convert.FromBase64String(match.Groups["data"].Value);

                if (bytes.Length > MaxBytes) return "";

                var fileName = $"{prefix}_{Guid.NewGuid():N}.{ext}";
                File.WriteAllBytes(Path.Combine(_uploadDir, fileName), bytes);
                return $"uploads/{fileName}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ImageService] SaveIfBase64 error: {ex.Message}");
                return imageData;
            }
        }

        /// <summary>Upload file multipart — dùng cho endpoint upload-image riêng.</summary>
        public async Task<(bool ok, string urlOrError)> SaveUploadedFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return (false, "Không có file");
            if (file.Length > MaxBytes)
                return (false, "File quá lớn (max 3MB)");

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                return (false, "Chỉ chấp nhận JPG, PNG, WebP");

            var fileName = $"poi_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(_uploadDir, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return (true, $"uploads/{fileName}");
        }
    }
}

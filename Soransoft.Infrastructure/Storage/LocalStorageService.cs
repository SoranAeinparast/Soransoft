using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Soransoft.Application.Interfaces;
using Soransoft.Infrastructure.Imaging;

namespace Soransoft.Infrastructure.Storage
{
    /// <summary>ذخیره‌سازی فایل روی دیسک (wwwroot/uploads) — تصاویر به‌صورت خودکار بهینه می‌شوند</summary>
    public class LocalFileStorage : IFileStorage
    {
        private readonly IWebHostEnvironment _env;
        private readonly IImageOptimizer _optimizer;
        private readonly string[] _allowedImageExt = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".bmp" };
        private readonly string[] _allowedDocExt = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg" };
        private const long MaxImageSize = 20 * 1024 * 1024; // 20MB قبل از بهینه‌سازی
        private const long MaxDocSize = 20 * 1024 * 1024;   // 20MB

        public LocalFileStorage(IWebHostEnvironment env, IImageOptimizer optimizer)
        {
            _env = env;
            _optimizer = optimizer;
        }

        public async Task<string> SaveImageAsync(IFormFile file, string folder, CancellationToken ct = default)
        {
            if (file is null || file.Length == 0) throw new ArgumentException("فایلی انتخاب نشده است.");
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedImageExt.Contains(ext)) throw new InvalidOperationException("فرمت تصویر مجاز نیست.");
            if (file.Length > MaxImageSize) throw new InvalidOperationException("حجم تصویر نباید بیشتر از ۲۰ مگابایت باشد.");

            // بهینه‌سازی: کوچک‌سازی تا ۱۹۲۰px + تبدیل به WebP (SVG/GIF دست‌نخورده)
            await using var source = file.OpenReadStream();
            var result = await _optimizer.SaveOptimizedAsync(source, file.FileName, folder, ct);
            return result.RelativePath;
        }

        /// <summary>ذخیره سند (PDF قرارداد، رسید پرداخت و...) با کنترل فرمت و حجم</summary>
        public async Task<string> SaveDocumentAsync(IFormFile file, string folder, CancellationToken ct = default)
        {
            if (file is null || file.Length == 0) throw new ArgumentException("فایلی انتخاب نشده است.");
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedDocExt.Contains(ext)) throw new InvalidOperationException("فرمت فایل مجاز نیست (PDF/Word/Excel/تصویر).");
            if (file.Length > MaxDocSize) throw new InvalidOperationException("حجم فایل نباید بیشتر از ۲۰ مگابایت باشد.");

            var safeFolder = string.Join('/', folder.Split('/', '\\').Where(p => !string.IsNullOrWhiteSpace(p) && p != "." && p != ".."));
            var dir = Path.Combine(_env.WebRootPath, "uploads", safeFolder);
            Directory.CreateDirectory(dir);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(dir, fileName);
            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream, ct);

            return $"/uploads/{safeFolder}/{fileName}";
        }

        public Task DeleteAsync(string? relativePath, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return Task.CompletedTask;
            var full = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(full)) File.Delete(full);
            return Task.CompletedTask;
        }
    }
}

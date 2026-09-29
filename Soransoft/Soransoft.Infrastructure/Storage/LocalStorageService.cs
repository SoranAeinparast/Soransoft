using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Soransoft.Application.Interfaces;
using Soransoft.Infrastructure.Imaging;
using System.Security;

namespace Soransoft.Infrastructure.Storage
{
    /// <summary>ذخیره‌سازی تصاویر عمومی و اسناد خصوصی روی دیسک</summary>
    public class LocalFileStorage : IFileStorage
    {
        private readonly IWebHostEnvironment _env;
        private readonly IImageOptimizer _optimizer;
        private readonly string[] _allowedImageExt = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".bmp" };
        private readonly string[] _allowedDocExt = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".zip", ".rar", ".mp4", ".webm", ".mov", ".png", ".jpg", ".jpeg", ".webp" };
        private readonly string _privateDocumentsRoot;
        private readonly string _persistentProfileRoot;
        private readonly string _persistentMediaRoot;
        private const long MaxImageSize = 20 * 1024 * 1024; // 20MB قبل از بهینه‌سازی
        private const long MaxDocSize = 20 * 1024 * 1024;   // 20MB

        public LocalFileStorage(IWebHostEnvironment env, IImageOptimizer optimizer, IConfiguration configuration)
        {
            _env = env;
            _optimizer = optimizer;
            _privateDocumentsRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "App_Data", "PrivateDocuments"));
            _persistentProfileRoot = PersistentProfileStorage.ResolveRoot(_env, configuration);
            _persistentMediaRoot = PersistentPublicMediaStorage.ResolveRoot(_env, configuration);
        }

        public async Task<string> SaveImageAsync(IFormFile file, string folder, CancellationToken ct = default)
        {
            if (file is null || file.Length == 0) throw new ArgumentException("فایلی انتخاب نشده است.");
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedImageExt.Contains(ext)) throw new InvalidOperationException("فرمت تصویر مجاز نیست.");
            if (file.Length > MaxImageSize) throw new InvalidOperationException("حجم تصویر نباید بیشتر از ۲۰ مگابایت باشد.");
            var safeFolder = NormalizeFolder(folder);

            // بهینه‌سازی: کوچک‌سازی تا ۱۹۲۰px + تبدیل به WebP (SVG/GIF دست‌نخورده)
            await using var source = file.OpenReadStream();
            var result = await _optimizer.SaveOptimizedAsync(source, file.FileName, safeFolder, ct);
            return result.RelativePath;
        }

        /// <summary>ذخیره سند (PDF قرارداد، رسید پرداخت و...) با کنترل فرمت و حجم</summary>
        public async Task<string> SaveDocumentAsync(IFormFile file, string folder, CancellationToken ct = default)
        {
            if (file is null || file.Length == 0) throw new ArgumentException("فایلی انتخاب نشده است.");
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedDocExt.Contains(ext)) throw new InvalidOperationException("فرمت فایل مجاز نیست (PDF/Word/Excel/تصویر).");
            if (file.Length > MaxDocSize) throw new InvalidOperationException("حجم فایل نباید بیشتر از ۲۰ مگابایت باشد.");

            var safeFolder = NormalizeFolder(folder);
            var storageRoot = PersistentProfileStorage.IsProfileFolder(safeFolder)
                ? _persistentProfileRoot
                : _privateDocumentsRoot;
            var dir = ResolveUnderRoot(storageRoot, safeFolder);
            Directory.CreateDirectory(dir);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(dir, fileName);
            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream, ct);

            return $"/documents/download?path={Uri.EscapeDataString($"{safeFolder}/{fileName}")}";
        }

        public Task DeleteAsync(string? relativePath, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return Task.CompletedTask;

            var privateRelativePath = ExtractPrivateDocumentPath(relativePath);
            string? full = null;
            try
            {
                full = privateRelativePath is not null
                    ? ResolveStoredPrivatePath(privateRelativePath)
                    : ResolvePublicUploadPath(relativePath);
            }
            catch (SecurityException)
            {
                // Invalid persisted paths must not escape their storage root.
            }
            catch (ArgumentException)
            {
                // Invalid persisted paths must not escape their storage root.
            }

            if (full is not null && File.Exists(full)) File.Delete(full);
            if (privateRelativePath is not null && PersistentProfileStorage.IsProfilePath(privateRelativePath))
            {
                var legacy = ResolveUnderRoot(_privateDocumentsRoot, privateRelativePath);
                if (File.Exists(legacy)) File.Delete(legacy);
            }
            return Task.CompletedTask;
        }

        private string ResolveStoredPrivatePath(string relativePath)
        {
            if (!PersistentProfileStorage.IsProfilePath(relativePath))
                return ResolveUnderRoot(_privateDocumentsRoot, relativePath);

            var persistent = ResolveUnderRoot(_persistentProfileRoot, relativePath);
            if (File.Exists(persistent)) return persistent;
            return ResolveUnderRoot(_privateDocumentsRoot, relativePath);
        }

        private string NormalizeFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
                throw new ArgumentException("مسیر ذخیره‌سازی معتبر نیست.");

            var segments = folder.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0 || segments.Any(segment =>
                    segment is "." or ".." ||
                    segment.Contains(':') ||
                    segment.IndexOfAny(Path.GetInvalidPathChars()) >= 0))
                throw new ArgumentException("مسیر ذخیره‌سازی معتبر نیست.");

            var normalized = string.Join('/', segments);
            _ = ResolveUnderRoot(
                Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads"),
                normalized);
            return normalized;
        }

        private static string ResolveUnderRoot(string root, string relativePath)
        {
            var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            var fullPath = Path.GetFullPath(Path.Combine(fullRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
                throw new SecurityException("مسیر فایل مجاز نیست.");
            return fullPath;
        }

        private string? ResolvePublicUploadPath(string relativePath)
        {
            var normalized = relativePath.TrimStart('/').Replace('\\', '/');
            if (!normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase)) return null;

            try
            {
                var relativePath = normalized["uploads/".Length..];
                var persistent = PersistentPublicMediaStorage.ResolveUnderRoot(_persistentMediaRoot, relativePath);
                if (File.Exists(persistent)) return persistent;

                return ResolveUnderRoot(
                    Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads"),
                    relativePath);
            }
            catch (SecurityException)
            {
                return null;
            }
        }

        private static string? ExtractPrivateDocumentPath(string value)
        {
            const string marker = "?path=";
            var markerIndex = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0 || !value[..markerIndex].TrimEnd('/').EndsWith("/documents/download", StringComparison.OrdinalIgnoreCase))
                return null;

            var encodedPath = value[(markerIndex + marker.Length)..].Split('&', 2)[0];
            var path = Uri.UnescapeDataString(encodedPath).Replace('\\', '/').Trim('/');
            if (string.IsNullOrWhiteSpace(path) || path.Split('/').Any(segment => segment is "." or ".."))
                return null;
            return path;
        }
    }
}

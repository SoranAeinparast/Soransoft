using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Soransoft.Application.Interfaces;
using Soransoft.Web.Areas.Admin.Controllers;
using System.Text.RegularExpressions;

namespace Soransoft.Web.Areas.Admin.Controllers
{
    /// <summary>کتابخانه رسانه — مرور، آپلود و حذف تصاویر آپلودشده</summary>
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public partial class MediaLibraryController : AdminBaseController
    {
        private static readonly Regex SafeFolder = new("^[a-zA-Z0-9_-]{1,40}$", RegexOptions.Compiled);

        private readonly IMediaLibraryService _media;
        private readonly IFileStorage _storage;

        public MediaLibraryController(IMediaLibraryService media, IFileStorage storage)
        {
            _media = media;
            _storage = storage;
        }

        public async Task<IActionResult> Index(string? folder, CancellationToken ct)
        {
            var files = await _media.GetFilesAsync(folder, ct);
            var folders = await _media.GetFoldersAsync(ct);

            // تشخیص استفاده هر تصویر در محتواها (مقاله/سرویس/…) برای هشدار پیش از حذف
            var usage = await _media.GetUsageAsync(files.Select(f => f.Url), ct);

            ViewData["Title"] = "کتابخانه رسانه";
            ViewData["Folders"] = folders;
            ViewData["CurrentFolder"] = folder;
            ViewData["Usage"] = usage;
            return View(files);
        }

        /// <summary>صفحه آمار مصرف فضا + نمودارها + پاکسازی فایل‌های بدون ارجاع</summary>
        public async Task<IActionResult> Storage(CancellationToken ct)
        {
            var overview = await _media.GetStorageOverviewAsync(ct);
            ViewData["Title"] = "مصرف فضای ذخیره‌سازی";
            return View(overview);
        }

        /// <summary>حذف همه فایل‌های بدون ارجاع (دوباره محاسبه و حذف امن)</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CleanupOrphans(CancellationToken ct)
        {
            var (deleted, freed) = await _media.CleanupOrphansAsync(ct);
            TempData["Success"] = deleted > 0
                ? $"{deleted} فایل بدون ارجاع حذف شد و {freed / 1024d:N0} کیلوبایت فضا آزاد شد."
                : "فایل بدون ارجاعی برای حذف وجود ندارد.";
            return RedirectToAction(nameof(Storage));
        }

        /// <summary>لیست JSON برای انتخابگر داخل TinyMCE</summary>
        [HttpGet]
        public async Task<IActionResult> List(string? folder, CancellationToken ct)
            => Ok(new { files = await _media.GetFilesAsync(folder, ct) });

        /// <summary>آپلود مستقیم از صفحه کتابخانه</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile? file, string? folder, CancellationToken ct)
        {
            var target = NormalizeFolder(folder);
            if (file is null || file.Length == 0)
            {
                TempData["Error"] = "فایلی انتخاب نشده است.";
                return RedirectToAction(nameof(Index), new { folder = target });
            }

            try
            {
                await _storage.SaveImageAsync(file, target, ct);
                TempData["Success"] = "فایل با موفقیت آپلود شد.";
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index), new { folder = target });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string? path, string? folder, bool force = false, CancellationToken ct = default)
        {
            var name = Path.GetFileName(path?.Replace('\\', '/') ?? "");

            try
            {
                // محافظ سمت سرور: تصویرِ در حال استفاده فقط با تایید صریح (تأیید دیالوگ هشدار) حذف می‌شود
                if (!force && !string.IsNullOrWhiteSpace(path))
                {
                    var usage = await _media.GetUsageAsync(new[] { path }, ct);
                    if (usage.TryGetValue(path!.Trim().ToLowerInvariant(), out var refs) && refs.Count > 0)
                    {
                        var places = string.Join("، ", refs.Select(r => $"{r.KindTitle} «{r.Title}»"));
                        TempData["Error"] = $"حذف انجام نشد: فایل «{name}» در حال استفاده است ({places}).";
                        return RedirectToAction(nameof(Index), new { folder });
                    }
                }

                await _media.DeleteAsync(path ?? "", ct);
                TempData["Success"] = $"فایل «{name}» حذف شد.";
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index), new { folder });
        }

        /// <summary>فقط حروف/عدد/خط تیره — جلوگیری از path traversal در پوشه آپلود</summary>
        private static string NormalizeFolder(string? folder)
        {
            if (string.IsNullOrWhiteSpace(folder)) return "editor";
            var clean = folder.Trim('/');
            return SafeFolder.IsMatch(clean) ? clean : "editor";
        }
    }
}

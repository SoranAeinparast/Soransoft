using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using Soransoft.Application.ViewModels;
using Soransoft.Domain.Entities;
using Soransoft.Infrastructure.Persistence;

namespace Soransoft.Infrastructure.Storage
{
    /// <summary>کتابخانه رسانه روی دیسک — پوشه‌های زیر wwwroot/uploads</summary>
    public class LocalMediaLibraryService : IMediaLibraryService
    {
        private static readonly string[] AllowedImageExt = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg" };

        private readonly IWebHostEnvironment _env;
        private readonly SoransoftDbContext _db;

        public LocalMediaLibraryService(IWebHostEnvironment env, SoransoftDbContext db)
        {
            _env = env;
            _db = db;
        }

        private string UploadsRoot => Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads");

        public Task<IReadOnlyList<MediaFileItem>> GetFilesAsync(string? folder = null, CancellationToken ct = default)
        {
            var root = UploadsRoot;
            var result = new List<MediaFileItem>();

            // پوشه‌های مجاز: فقط زیر uploads و بدون escape (نرمال‌سازی قبل از مقایسه)
            var normalizedRoot = Path.GetFullPath(root);
            var searchDir = string.IsNullOrWhiteSpace(folder)
                ? normalizedRoot
                : Path.GetFullPath(Path.Combine(normalizedRoot, folder.Trim('/').Replace('\\', '/')));

            if (!searchDir.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase) || !Directory.Exists(searchDir))
                return Task.FromResult<IReadOnlyList<MediaFileItem>>(result);

            // بدون پوشه = همه فایل‌ها (بازگشتی در همه زیرپوشه‌ها)
            var recursive = string.IsNullOrWhiteSpace(folder);
            var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            foreach (var file in Directory.EnumerateFiles(searchDir, "*", option))
            {
                ct.ThrowIfCancellationRequested();

                var fi = new FileInfo(file);
                var ext = fi.Extension.ToLowerInvariant();
                var relative = $"/uploads/{Path.GetRelativePath(root, file).Replace('\\', '/')}";
                var relativeDir = Path.GetRelativePath(root, Path.GetDirectoryName(file)!).Replace('\\', '/');

                result.Add(new MediaFileItem
                {
                    Url = relative,
                    Folder = relativeDir == "." ? "(root)" : relativeDir,
                    FileName = fi.Name,
                    SizeBytes = fi.Length,
                    ModifiedAt = fi.LastWriteTime,
                    IsImage = AllowedImageExt.Contains(ext)
                });
            }

            // جدیدترین اول
            result = result.OrderByDescending(f => f.ModifiedAt).ToList();
            return Task.FromResult<IReadOnlyList<MediaFileItem>>(result);
        }

        public Task<IReadOnlyList<MediaFolder>> GetFoldersAsync(CancellationToken ct = default)
        {
            var root = UploadsRoot;
            var result = new List<MediaFolder>();

            if (Directory.Exists(root))
            {
                foreach (var dir in Directory.EnumerateDirectories(root))
                {
                    var count = Directory.EnumerateFiles(dir).Count();
                    if (count == 0) continue; // پوشه‌های خالی نمایش داده نشوند
                    result.Add(new MediaFolder
                    {
                        Name = Path.GetFileName(dir),
                        FileCount = count
                    });
                }
            }

            return Task.FromResult<IReadOnlyList<MediaFolder>>(result);
        }

        public Task DeleteAsync(string relativePath, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("مسیر فایل نامعتبر است.");

            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var root = UploadsRoot;

            // کلاینت مسیر نسبی سایت (مثل /uploads/editor/x.png) می‌فرستد
            var cleaned = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var full = Path.Combine(webRoot, cleaned);

            // جلوگیری از path traversal — مسیر نرمال‌شده باید داخل uploads بماند
            var normalizedRoot = Path.GetFullPath(root);
            var normalizedFull = Path.GetFullPath(full);
            if (!normalizedFull.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("مسیر فایل مجاز نیست.");

            if (File.Exists(normalizedFull)) File.Delete(normalizedFull);
            return Task.CompletedTask;
        }

        // ------------------------------------------------------------------
        // تشخیص استفاده: تصویر در کدام محتواها به کار رفته است؟
        // ------------------------------------------------------------------

        public async Task<IDictionary<string, IReadOnlyList<MediaUsageRef>>> GetUsageAsync(
            IEnumerable<string> paths, CancellationToken ct = default)
        {
            var wanted = paths
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim().ToLowerInvariant())
                .ToHashSet();
            var result = wanted.ToDictionary(p => p, _ => new List<MediaUsageRef>() as IReadOnlyList<MediaUsageRef>);
            if (result.Count == 0) return result;

            void Match(object? value, string kind, string title, string field, string? editUrl)
            {
                if (value is not string s || string.IsNullOrEmpty(s)) return;
                var lower = s.ToLowerInvariant();

                foreach (var path in wanted)
                {
                    // فیلدهای تصویر: مسیر «همان» است؛ فیلدهای HTML: مسیر داخل متن جست‌وجو می‌شود
                    var hit = field == "Image"
                        ? lower.TrimEnd('/').EndsWith(path, StringComparison.Ordinal)
                        : lower.Contains(path, StringComparison.Ordinal);
                    if (!hit) continue;

                    ((List<MediaUsageRef>)result[path]).Add(new MediaUsageRef
                    {
                        KindTitle = kind,
                        Title = title,
                        Field = field,
                        EditUrl = editUrl
                    });
                }
            }

            // مقالات — تصویر شاخص + تصاویر داخل متن
            foreach (var a in await _db.Articles.AsNoTracking()
                .Select(a => new { a.Id, a.Title, a.Image, a.Body }).ToListAsync(ct))
            {
                var url = $"/Admin/Articles/Edit/{a.Id}";
                Match(a.Image, "مقاله", a.Title, "Image", url);
                Match(a.Body, "مقاله", a.Title, "Body", url);
            }

            // سرویس‌ها — تصویر + تصاویر داخل توضیح کامل
            foreach (var s in await _db.Services.AsNoTracking()
                .Select(s => new { s.Id, s.Title, s.Image, s.FullDescription }).ToListAsync(ct))
            {
                var url = $"/Admin/Services/Edit/{s.Id}";
                Match(s.Image, "سرویس", s.Title, "Image", url);
                Match(s.FullDescription, "سرویس", s.Title, "FullDescription", url);
            }

            // نمونه‌کارها — تصویر + توضیح
            foreach (var p in await _db.Portfolios.AsNoTracking()
                .Select(p => new { p.Id, p.Title, p.Image, p.Description }).ToListAsync(ct))
            {
                var url = $"/Admin/Portfolios/Edit/{p.Id}";
                Match(p.Image, "نمونه‌کار", p.Title, "Image", url);
                Match(p.Description, "نمونه‌کار", p.Title, "Description", url);
            }

            // اسلایدرها — فقط تصویر
            foreach (var sl in await _db.Sliders.AsNoTracking()
                .Select(sl => new { sl.Id, sl.Title, sl.Image }).ToListAsync(ct))
            {
                Match(sl.Image, "اسلایدر", sl.Title, "Image", $"/Admin/Sliders/Edit/{sl.Id}");
            }

            // بنرهای پرومو — تصویر بنر
            foreach (var banner in await _db.PromoBanners.AsNoTracking()
                .Select(b => new { b.Id, b.Title, b.Image }).ToListAsync(ct))
            {
                Match(banner.Image, "بنر پرومو", banner.Title, "Image", $"/Admin/PromoBanners/Edit/{banner.Id}");
            }

            // پروژه‌های آماده ارائه — تصویر شاخص و تصاویر توضیحات
            foreach (var project in await _db.SellableProjects.AsNoTracking()
                .Select(p => new { p.Id, p.Title, p.FeaturedImage, p.Description }).ToListAsync(ct))
            {
                Match(project.FeaturedImage, "پروژه قابل ارائه", project.Title, "Image", "/Admin/PortalProjects");
                Match(project.Description, "پروژه قابل ارائه", project.Title, "Description", "/Admin/PortalProjects");
            }

            // اعضای تیم — تصویر
            foreach (var t in await _db.TeamMembers.AsNoTracking()
                .Select(t => new { t.Id, t.FullName, t.Image }).ToListAsync(ct))
            {
                Match(t.Image, "عضو تیم", t.FullName, "Image", $"/Admin/TeamMembers/Edit/{t.Id}");
            }

            // تنظیمات سایت — مقادیر تصویری (لوگو و …) یا HTML
            foreach (var st in await _db.SiteSettings.AsNoTracking()
                .Select(st => new { st.Key, st.Value, st.Title }).ToListAsync(ct))
            {
                Match(st.Value, "تنظیمات سایت",
                      string.IsNullOrWhiteSpace(st.Title) ? st.Key : st.Title,
                      "Value", "/Admin/SiteSettings");
            }

            return result;
        }

        // ------------------------------------------------------------------
        // نمای کلی فضای ذخیره‌سازی + پاکسازی فایل‌های بدون ارجاع
        // ------------------------------------------------------------------

        public async Task<StorageOverview> GetStorageOverviewAsync(CancellationToken ct = default)
        {
            var files = await GetFilesAsync(null, ct);
            var allPaths = files.Select(f => f.Url);
            var usage = await GetUsageAsync(allPaths, ct);
            var used = usage.Where(kv => kv.Value.Count > 0).Select(kv => kv.Key).ToHashSet();

            var overview = new StorageOverview();
            var orphansByFolder = new Dictionary<string, List<MediaFileItem>>();

            foreach (var f in files.Where(f => !used.Contains(f.Url.Trim().ToLowerInvariant())))
            {
                if (!orphansByFolder.TryGetValue(f.Folder, out var list))
                    orphansByFolder[f.Folder] = list = new List<MediaFileItem>();
                list.Add(f);
            }

            foreach (var group in files.GroupBy(f => f.Folder))
            {
                var orphans = orphansByFolder.TryGetValue(group.Key, out var o) ? o : new List<MediaFileItem>();
                overview.Folders.Add(new StorageFolderStats
                {
                    Name = group.Key,
                    FileCount = group.Count(),
                    SizeBytes = group.Sum(f => f.SizeBytes),
                    OrphanCount = orphans.Count,
                    OrphanSizeBytes = orphans.Sum(f => f.SizeBytes)
                });
            }

            overview.Folders = overview.Folders.OrderByDescending(f => f.SizeBytes).ToList();
            overview.OrphanFiles = orphansByFolder.SelectMany(kv => kv.Value)
                .OrderByDescending(f => f.SizeBytes).ToList();
            overview.TotalFiles = files.Count;
            overview.TotalSizeBytes = files.Sum(f => f.SizeBytes);

            try
            {
                var drive = new DriveInfo(Path.GetPathRoot(UploadsRoot)!);
                overview.DriveFreeBytes = drive.AvailableFreeSpace;
                overview.DriveTotalBytes = drive.TotalSize;
            }
            catch
            {
                // درایو در دسترس نیست — مقادیر صفر می‌مانند
            }

            return overview;
        }

        public async Task<(int DeletedCount, long FreedBytes)> CleanupOrphansAsync(CancellationToken ct = default)
        {
            // محاسبه تازه قبل از حذف — اگر بین نمایش و کلیک، محتوایی به فایلی ارجاع داده باشد حذف نمی‌شود
            var overview = await GetStorageOverviewAsync(ct);
            var freed = 0L;
            var deleted = 0;

            // حذف فایل به فایل با همان DeleteAsync امن (محافظت path traversal حفظ می‌شود)
            foreach (var orphan in overview.OrphanFiles)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    await DeleteAsync(orphan.Url, ct);
                    deleted++;
                    freed += orphan.SizeBytes;
                }
                catch (IOException)
                {
                    // فایل قفل است — عبور
                }
            }

            return (deleted, freed);
        }
    }
}

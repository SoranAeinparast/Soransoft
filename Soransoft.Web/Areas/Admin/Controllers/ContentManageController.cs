using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using Soransoft.Application.Services;
using Soransoft.Infrastructure.Persistence;
using System.Security.Cryptography;
using System.Text;

namespace Soransoft.Web.Areas.Admin.Controllers
{
    /// <summary>مدیریت سرویس‌ها</summary>
    public class ServicesController : AdminBaseController
    {
        private const string ServiceSlugRedirectPrefix = "ServiceSlugRedirect:";
        private readonly SoransoftDbContext _db;
        private readonly IFileStorage _storage;
        private readonly ISiteSettingService _settings;
        public ServicesController(SoransoftDbContext db, IFileStorage storage, ISiteSettingService settings)
        {
            _db = db;
            _storage = storage;
            _settings = settings;
        }

        public async Task<IActionResult> Index(CancellationToken ct) =>
            View(await _db.Services
                .Include(service => service.Features.OrderBy(feature => feature.DisplayOrder))
                .Include(service => service.Steps.OrderBy(step => step.DisplayOrder))
                .OrderBy(service => service.DisplayOrder)
                .ToListAsync(ct));

        public IActionResult Create() => View(new Soransoft.Domain.Entities.Service());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Soransoft.Domain.Entities.Service model, IFormFile? imageFile, CancellationToken ct)
        {
            model.Slug = string.IsNullOrWhiteSpace(model.Slug)
                ? SlugGenerator.Generate(model.Title)
                : SlugGenerator.Generate(model.Slug, transliterate: false);
            model.Slug = await EnsureUniqueServiceSlugAsync(model.Slug, null, ct);
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ذخیره نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return View(model);
            }
            model.CreatedAt = DateTime.Now;
            if (imageFile is not null && imageFile.Length > 0)
                model.Image = await _storage.SaveImageAsync(imageFile, "services", ct);
            else
                model.Image = model.Image?.Trim() ?? string.Empty;
            _db.Services.Add(model);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "سرویس با موفقیت ایجاد شد";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            var item = await _db.Services.FindAsync(new object[] { id }, ct);
            if (item is null) return NotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Soransoft.Domain.Entities.Service model, IFormFile? imageFile, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ذخیره نشد: " + string.Join(" | ", ModelState.Keys.Where(k => ModelState[k]!.Errors.Count > 0).Select(k => $"{k}: {string.Join(", ", ModelState[k]!.Errors.Select(e => e.ErrorMessage))}"));
                return View(model);
            }

            // فقط فیلدهای فرم روی موجودیت فعلی کپی می‌شوند تا تاریخ‌های سیستمی حفظ شوند.
            var item = await _db.Services.FirstOrDefaultAsync(s => s.Id == model.Id, ct);
            if (item is null) return NotFound();

            var previousSlug = item.Slug;
            var slug = string.IsNullOrWhiteSpace(model.Slug)
                ? SlugGenerator.Generate(model.Title)
                : SlugGenerator.Generate(model.Slug, transliterate: false);
            item.Slug = await EnsureUniqueServiceSlugAsync(slug, model.Id, ct);

            item.Kind = model.Kind;
            item.Title = model.Title;
            item.ShortDescription = model.ShortDescription;
            item.FullDescription = model.FullDescription;
            item.Icon = model.Icon;
            item.Slogan = model.Slogan;
            item.ComingSoon = model.ComingSoon;
            item.IsActive = model.IsActive;
            item.DisplayOrder = model.DisplayOrder;

            if (imageFile is not null && imageFile.Length > 0)
            {
                item.Image = await _storage.SaveImageAsync(imageFile, "services", ct);
            }
            else
            {
                item.Image = model.Image?.Trim() ?? string.Empty;
            }

            if (!string.Equals(previousSlug, item.Slug, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(previousSlug))
            {
                await PreserveServiceSlugAsync(previousSlug, item.Slug, ct);
            }

            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "سرویس با موفقیت ویرایش شد";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> EnsureUniqueServiceSlugAsync(string slug, int? excludeId, CancellationToken ct)
        {
            var baseSlug = string.IsNullOrWhiteSpace(slug) ? "service" : slug;
            var candidate = baseSlug;
            var suffix = 2;
            while (await _db.Services.AnyAsync(s => s.Slug == candidate && (excludeId == null || s.Id != excludeId), ct)
                || await _db.SiteSettings.AnyAsync(s => s.Key == ServiceSlugRedirectKey(candidate), ct))
                candidate = $"{baseSlug}-{suffix++}";
            return candidate;
        }

        private async Task PreserveServiceSlugAsync(string oldSlug, string newSlug, CancellationToken ct)
        {
            oldSlug = oldSlug.Trim().ToLowerInvariant();
            newSlug = newSlug.Trim().ToLowerInvariant();

            var aliases = await _db.SiteSettings
                .Where(s => s.Key.StartsWith(ServiceSlugRedirectPrefix) && s.Value == oldSlug)
                .ToListAsync(ct);
            foreach (var alias in aliases)
            {
                alias.Value = newSlug;
                alias.UpdatedAt = DateTime.Now;
            }

            var key = ServiceSlugRedirectKey(oldSlug);
            var redirect = await _db.SiteSettings.FirstOrDefaultAsync(s => s.Key == key, ct);
            if (redirect is null)
            {
                _db.SiteSettings.Add(new Soransoft.Domain.Entities.SiteSetting
                {
                    Key = key,
                    Title = $"آدرس قدیمی سرویس: {oldSlug}",
                    Value = newSlug,
                    Group = "ریدایرکت‌ها",
                    Type = "text",
                    DisplayOrder = 1,
                });
            }
            else
            {
                redirect.Value = newSlug;
                redirect.UpdatedAt = DateTime.Now;
            }

            await _db.SaveChangesAsync(ct);
            await _settings.InvalidateCacheAsync();
        }

        private static string ServiceSlugRedirectKey(string slug)
        {
            var normalized = slug.Trim().ToLowerInvariant();
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();
            return $"{ServiceSlugRedirectPrefix}{hash}";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var item = await _db.Services.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.IsDeleted = true;
                item.DeletedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = "سرویس حذف شد";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFeature(int serviceId, string title, string? description, int displayOrder, CancellationToken ct)
        {
            var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == serviceId && !s.IsDeleted, ct);
            if (service is null) { TempData["Error"] = "سرویس یافت نشد."; return RedirectToAction(nameof(Index)); }
            if (string.IsNullOrWhiteSpace(title)) { TempData["Error"] = "عنوان ویژگی الزامی است."; return RedirectToAction(nameof(Index)); }

            _db.ServiceFeatures.Add(new Soransoft.Domain.Entities.ServiceFeature
            {
                ServiceId = serviceId,
                Title = title.Trim(),
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                DisplayOrder = Math.Max(0, displayOrder),
            });
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "ویژگی سرویس اضافه شد.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFeature(int id, string title, string? description, int displayOrder, CancellationToken ct)
        {
            var feature = await _db.ServiceFeatures.FirstOrDefaultAsync(f => f.Id == id && !f.Service.IsDeleted, ct);
            if (feature is null) { TempData["Error"] = "ویژگی سرویس یافت نشد."; return RedirectToAction(nameof(Index)); }
            if (string.IsNullOrWhiteSpace(title)) { TempData["Error"] = "عنوان ویژگی الزامی است."; return RedirectToAction(nameof(Index)); }

            feature.Title = title.Trim();
            feature.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            feature.DisplayOrder = Math.Max(0, displayOrder);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "ویژگی سرویس به‌روزرسانی شد.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFeature(int id, CancellationToken ct)
        {
            var feature = await _db.ServiceFeatures.FirstOrDefaultAsync(f => f.Id == id && !f.Service.IsDeleted, ct);
            if (feature is null) { TempData["Error"] = "ویژگی سرویس یافت نشد."; return RedirectToAction(nameof(Index)); }
            _db.ServiceFeatures.Remove(feature);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "ویژگی سرویس حذف شد.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStep(int serviceId, string title, int displayOrder, CancellationToken ct)
        {
            var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == serviceId && !s.IsDeleted, ct);
            if (service is null) { TempData["Error"] = "سرویس یافت نشد."; return RedirectToAction(nameof(Index)); }
            if (string.IsNullOrWhiteSpace(title)) { TempData["Error"] = "عنوان مرحله الزامی است."; return RedirectToAction(nameof(Index)); }

            _db.ServiceSteps.Add(new Soransoft.Domain.Entities.ServiceStep
            {
                ServiceId = serviceId,
                Title = title.Trim(),
                DisplayOrder = Math.Max(0, displayOrder),
            });
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "مرحله سرویس اضافه شد.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStep(int id, string title, int displayOrder, CancellationToken ct)
        {
            var step = await _db.ServiceSteps.FirstOrDefaultAsync(s => s.Id == id && !s.Service.IsDeleted, ct);
            if (step is null) { TempData["Error"] = "مرحله سرویس یافت نشد."; return RedirectToAction(nameof(Index)); }
            if (string.IsNullOrWhiteSpace(title)) { TempData["Error"] = "عنوان مرحله الزامی است."; return RedirectToAction(nameof(Index)); }

            step.Title = title.Trim();
            step.DisplayOrder = Math.Max(0, displayOrder);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "مرحله سرویس به‌روزرسانی شد.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStep(int id, CancellationToken ct)
        {
            var step = await _db.ServiceSteps.FirstOrDefaultAsync(s => s.Id == id && !s.Service.IsDeleted, ct);
            if (step is null) { TempData["Error"] = "مرحله سرویس یافت نشد."; return RedirectToAction(nameof(Index)); }
            _db.ServiceSteps.Remove(step);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "مرحله سرویس حذف شد.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>مدیریت نمونه‌کارها</summary>
    public class PortfoliosController : AdminBaseController
    {
        private readonly SoransoftDbContext _db;
        private readonly IFileStorage _storage;
        public PortfoliosController(SoransoftDbContext db, IFileStorage storage) { _db = db; _storage = storage; }

        public async Task<IActionResult> Index(CancellationToken ct) =>
            View(await _db.Portfolios.OrderBy(p => p.DisplayOrder).ToListAsync(ct));

        public async Task<IActionResult> Create(CancellationToken ct)
        {
            await FillServicesAsync(ct);
            return View(new Soransoft.Domain.Entities.Portfolio());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Soransoft.Domain.Entities.Portfolio model, IFormFile? imageFile, CancellationToken ct)
        {
            if (!IsSafeExternalUrl(model.Url))
            {
                TempData["Error"] = "لینک نمونه‌کار باید http/https باشد.";
                await FillServicesAsync(ct);
                return View(model);
            }
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ذخیره نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                await FillServicesAsync(ct);
                return View(model);
            }
            if (imageFile is not null && imageFile.Length > 0)
                model.Image = await _storage.SaveImageAsync(imageFile, "portfolios", ct);
            else
                model.Image = model.Image?.Trim() ?? string.Empty;
            _db.Portfolios.Add(model);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "نمونه‌کار با موفقیت ایجاد شد";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            var item = await _db.Portfolios.FindAsync(new object[] { id }, ct);
            if (item is null) return NotFound();
            await FillServicesAsync(ct);
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Soransoft.Domain.Entities.Portfolio model, IFormFile? imageFile, CancellationToken ct)
        {
            if (!IsSafeExternalUrl(model.Url))
            {
                TempData["Error"] = "لینک نمونه‌کار باید http/https باشد.";
                await FillServicesAsync(ct);
                return View(model);
            }
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ذخیره نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                await FillServicesAsync(ct);
                return View(model);
            }

            var item = await _db.Portfolios.FirstOrDefaultAsync(p => p.Id == model.Id, ct);
            if (item is null) return NotFound();

            item.Title = model.Title;
            item.Description = model.Description;
            item.Url = model.Url;
            item.ServiceId = model.ServiceId;
            item.ComingSoon = model.ComingSoon;
            item.IsActive = model.IsActive;
            item.DisplayOrder = model.DisplayOrder;

            if (imageFile is not null && imageFile.Length > 0)
            {
                item.Image = await _storage.SaveImageAsync(imageFile, "portfolios", ct);
            }
            else
            {
                item.Image = model.Image?.Trim() ?? string.Empty;
            }
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "نمونه‌کار با موفقیت ویرایش شد";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var item = await _db.Portfolios.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.IsDeleted = true;
                item.DeletedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = "نمونه‌کار حذف شد";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task FillServicesAsync(CancellationToken ct) =>
            ViewBag.Services = await _db.Services.Where(s => !s.IsDeleted).ToListAsync(ct);

        private static bool IsSafeExternalUrl(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            return Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }

    /// <summary>مدیریت مقالات</summary>
    public class ArticlesController : AdminBaseController
    {
        private readonly SoransoftDbContext _db;
        private readonly IFileStorage _storage;
        public ArticlesController(SoransoftDbContext db, IFileStorage storage) { _db = db; _storage = storage; }

        public async Task<IActionResult> Index(CancellationToken ct) =>
            View(await _db.Articles.Include(a => a.ArticleCategory).OrderByDescending(a => a.CreatedAt).ToListAsync(ct));

        public async Task<IActionResult> Create(CancellationToken ct)
        {
            await FillCategoriesAsync(ct);
            return View(new Soransoft.Domain.Entities.Article { PublishedAt = DateTime.Now });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Soransoft.Domain.Entities.Article model, IFormFile? imageFile, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ذخیره نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                await FillCategoriesAsync(ct);
                return View(model);
            }

            // اسلاگ خودکار از عنوان؛ تضمین یکتایی
            if (string.IsNullOrWhiteSpace(model.Slug))
                model.Slug = SlugGenerator.Generate(model.Title);
            model.Slug = await EnsureUniqueSlugAsync(model.Slug, null, ct);

            if (imageFile is not null && imageFile.Length > 0)
                model.Image = await _storage.SaveImageAsync(imageFile, "articles", ct);
            else
                model.Image = model.Image?.Trim() ?? string.Empty;
            _db.Articles.Add(model);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "مقاله با موفقیت ایجاد شد";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            var item = await _db.Articles.FindAsync(new object[] { id }, ct);
            if (item is null) return NotFound();
            await FillCategoriesAsync(ct);
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Soransoft.Domain.Entities.Article model, IFormFile? imageFile, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ذخیره نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                await FillCategoriesAsync(ct);
                return View(model);
            }

            // الگوی صحیح آپدیت: موجودیت از دیتابیس بار و فقط فیلدهای فرم روی آن کپی می‌شود
            // تا فیلدهای خارج از فرم (تصویر، شمارنده بازدید، تاریخ ایجاد) حفظ شوند
            var item = await _db.Articles.FirstOrDefaultAsync(a => a.Id == model.Id, ct);
            if (item is null) return NotFound();

            item.Title = model.Title;
            item.Summary = model.Summary;
            item.Body = model.Body;
            item.AuthorName = model.AuthorName;
            item.ArticleCategoryId = model.ArticleCategoryId;
            item.PublishedAt = model.PublishedAt;
            item.Status = model.Status;
            item.IsFeatured = model.IsFeatured;

            // اسلاگ خودکار از عنوان؛ تضمین یکتایی
            var slug = string.IsNullOrWhiteSpace(model.Slug) ? SlugGenerator.Generate(model.Title) : model.Slug;
            item.Slug = await EnsureUniqueSlugAsync(slug, model.Id, ct);

            if (imageFile is not null && imageFile.Length > 0)
            {
                item.Image = await _storage.SaveImageAsync(imageFile, "articles", ct);
            }
            else
            {
                item.Image = model.Image?.Trim() ?? string.Empty;
            }
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "مقاله با موفقیت ویرایش شد";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var item = await _db.Articles.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.IsDeleted = true;
                item.DeletedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = "مقاله حذف شد";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task FillCategoriesAsync(CancellationToken ct) =>
            ViewBag.Categories = await _db.ArticleCategories.Where(c => !c.IsDeleted).ToListAsync(ct);

        /// <summary>تضمین یکتایی اسلاگ مقاله — در صورت تکرار، پسوند عددی اضافه می‌شود</summary>
        private async Task<string> EnsureUniqueSlugAsync(string slug, int? excludeId, CancellationToken ct)
        {
            var baseSlug = slug;
            var n = 2;
            while (await _db.Articles.AnyAsync(a => a.Slug == slug && (excludeId == null || a.Id != excludeId), ct))
                slug = $"{baseSlug}-{n++}";
            return slug;
        }
    }

    /// <summary>مدیریت دسته‌بندی مقالات</summary>
    public class ArticleCategoriesController : AdminBaseController
    {
        private readonly SoransoftDbContext _db;
        public ArticleCategoriesController(SoransoftDbContext db) => _db = db;

        public async Task<IActionResult> Index(CancellationToken ct) =>
            View(await _db.ArticleCategories.OrderBy(c => c.DisplayOrder).ToListAsync(ct));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Soransoft.Domain.Entities.ArticleCategory model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ذخیره نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Index));
            }
            // اسلاگ خودکار از عنوان در صورت خالی بودن
            if (string.IsNullOrWhiteSpace(model.Slug))
                model.Slug = SlugGenerator.Generate(model.Title);
            model.CreatedAt = DateTime.Now;
            _db.ArticleCategories.Add(model);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = $"دسته‌بندی «{model.Title}» ایجاد شد";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Soransoft.Domain.Entities.ArticleCategory model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ذخیره نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Index));
            }
            // الگوی صحیح: بارگذاری از دیتابیس تا CreatedAt و IsDeleted حفظ شود
            var item = await _db.ArticleCategories.FirstOrDefaultAsync(c => c.Id == model.Id, ct);
            if (item is null) return NotFound();
            item.Title = model.Title;
            item.Slug = string.IsNullOrWhiteSpace(model.Slug) ? SlugGenerator.Generate(model.Title) : model.Slug;
            item.DisplayOrder = model.DisplayOrder;
            item.IsActive = model.IsActive;
            item.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "دسته‌بندی ویرایش شد";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var item = await _db.ArticleCategories.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.IsDeleted = true;
                item.DeletedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = "دسته‌بندی حذف شد";
            }
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>مدیریت تعرفه‌ها (بخش‌ها + پکیج‌ها)</summary>
    public class TariffsController : AdminBaseController
    {
        private readonly SoransoftDbContext _db;
        public TariffsController(SoransoftDbContext db) => _db = db;

        public async Task<IActionResult> Index(CancellationToken ct) =>
            View(await _db.TariffSections
                .Include(t => t.Packages.Where(p => !p.IsDeleted))
                    .ThenInclude(p => p.Items.OrderBy(i => i.DisplayOrder))
                .OrderBy(t => t.DisplayOrder).ToListAsync(ct));

        // ---------------- بخش‌های تعرفه ----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSection(Soransoft.Domain.Entities.TariffSection model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ایجاد بخش نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Index));
            }
            _db.TariffSections.Add(model);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = $"بخش تعرفه «{model.Title}» ایجاد شد";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSection(Soransoft.Domain.Entities.TariffSection model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ویرایش بخش نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Index));
            }
            // الگوی صحیح: بارگذاری از دیتابیس و کپی فیلدهای فرم تا CreatedAt و پکیج‌ها حفظ شوند
            var item = await _db.TariffSections.FirstOrDefaultAsync(t => t.Id == model.Id, ct);
            if (item is null) return NotFound();
            item.Kind = model.Kind;
            item.Title = model.Title;
            item.DisplayOrder = model.DisplayOrder;
            item.IsActive = model.IsActive;
            item.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "بخش تعرفه ویرایش شد";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>فعال/غیرفعال کردن سریع بخش (بدون تغییر بقیه فیلدها)</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSection(int id, CancellationToken ct)
        {
            var item = await _db.TariffSections.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.IsActive = !item.IsActive;
                item.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = $"بخش «{item.Title}» {(item.IsActive ? "فعال" : "غیرفعال")} شد";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSection(int id, CancellationToken ct)
        {
            var item = await _db.TariffSections.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.IsDeleted = true;
                item.DeletedAt = DateTime.Now;
                item.IsActive = false;
                // پکیج‌های زیرمجموعه هم غیرفعال می‌شوند تا از سایت حذف شوند
                var packages = await _db.TariffPackages.Where(p => p.TariffSectionId == id && !p.IsDeleted).ToListAsync(ct);
                foreach (var p in packages) { p.IsActive = false; p.UpdatedAt = DateTime.Now; }
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = $"بخش تعرفه «{item.Title}» حذف شد";
            }
            return RedirectToAction(nameof(Index));
        }

        // ---------------- پکیج‌های تعرفه ----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePackage(Soransoft.Domain.Entities.TariffPackage model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ایجاد پکیج نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Index));
            }
            _db.TariffPackages.Add(model);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = $"پکیج «{model.Title}» ایجاد شد";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPackage(Soransoft.Domain.Entities.TariffPackage model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ویرایش پکیج نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Index));
            }
            // الگوی صحیح: بارگذاری از دیتابیس و کپی فیلدهای فرم تا CreatedAt حفظ شود
            var item = await _db.TariffPackages.FirstOrDefaultAsync(p => p.Id == model.Id, ct);
            if (item is null) return NotFound();
            item.Title = model.Title;
            item.Price = model.Price;
            item.PriceNote = model.PriceNote;
            item.PriceSuffix = model.PriceSuffix;
            item.Duration = model.Duration;
            item.IsInstallmentAvailable = model.IsInstallmentAvailable;
            item.HasFreeSupport = model.HasFreeSupport;
            item.DisplayOrder = model.DisplayOrder;
            item.IsActive = model.IsActive;
            item.IsPopular = model.IsPopular;
            item.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "پکیج ویرایش شد";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>فعال/غیرفعال کردن سریع پکیج</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePackage(int id, CancellationToken ct)
        {
            var item = await _db.TariffPackages.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.IsActive = !item.IsActive;
                item.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = $"پکیج «{item.Title}» {(item.IsActive ? "فعال" : "غیرفعال")} شد";
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>برجسته‌کردن پکیج (نشان «محبوب‌ترین»)</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePopular(int id, CancellationToken ct)
        {
            var item = await _db.TariffPackages.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.IsPopular = !item.IsPopular;
                item.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = $"پکیج «{item.Title}» {(item.IsPopular ? "به عنوان محبوب‌ترین برجسته" : "از حالت محبوب خارج")} شد";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePackage(int id, CancellationToken ct)
        {
            var item = await _db.TariffPackages.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.IsDeleted = true;
                item.DeletedAt = DateTime.Now;
                item.IsActive = false;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = $"پکیج «{item.Title}» حذف شد";
            }
            return RedirectToAction(nameof(Index));
        }

        // ---------------- ردیف‌های ویژگی پکیج ----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(int packageId, string title, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["Error"] = "عنوان ردیف نمی‌تواند خالی باشد";
                return RedirectToAction(nameof(Index));
            }
            var pkg = await _db.TariffPackages.FirstOrDefaultAsync(p => p.Id == packageId, ct);
            if (pkg is null) return NotFound();
            var order = await _db.TariffItems.Where(i => i.TariffPackageId == packageId).MaxAsync(i => (int?)i.DisplayOrder, ct) ?? 0;
            _db.TariffItems.Add(new Soransoft.Domain.Entities.TariffItem
            {
                TariffPackageId = packageId,
                Title = title.Trim(),
                DisplayOrder = order + 1,
            });
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "ردیف ویژگی اضافه شد";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditItem(int id, string title, CancellationToken ct)
        {
            var item = await _db.TariffItems.FindAsync(new object[] { id }, ct);
            if (item is null) return NotFound();
            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["Error"] = "عنوان ردیف نمی‌تواند خالی باشد";
                return RedirectToAction(nameof(Index));
            }
            item.Title = title.Trim();
            item.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "ردیف ویرایش شد";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteItem(int id, CancellationToken ct)
        {
            var item = await _db.TariffItems.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                _db.TariffItems.Remove(item);
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = "ردیف حذف شد";
            }
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>مدیریت اعضای تیم</summary>
    public class TeamMembersController : AdminBaseController
    {
        private readonly SoransoftDbContext _db;
        private readonly IFileStorage _storage;
        public TeamMembersController(SoransoftDbContext db, IFileStorage storage) { _db = db; _storage = storage; }

        public async Task<IActionResult> Index(CancellationToken ct) =>
            View(await _db.TeamMembers.OrderBy(t => t.DisplayOrder).ThenBy(t => t.Id).ToListAsync(ct));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Soransoft.Domain.Entities.TeamMember model, IFormFile? imageFile, CancellationToken ct)
        {
            if (!IsSafeExternalUrl(model.LinkedinUrl))
            {
                TempData["Error"] = "لینک لینکدین باید http/https باشد.";
                return RedirectToAction(nameof(Index));
            }
            if (ModelState.IsValid)
            {
                if (imageFile is not null && imageFile.Length > 0)
                    model.Image = await _storage.SaveImageAsync(imageFile, "team", ct);
                else
                    model.Image = model.Image?.Trim() ?? string.Empty;
                _db.TeamMembers.Add(model);
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = "عضو تیم ایجاد شد";
            }
            else
            {
                TempData["Error"] = "ذخیره نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Soransoft.Domain.Entities.TeamMember model, IFormFile? imageFile, CancellationToken ct)
        {
            if (!IsSafeExternalUrl(model.LinkedinUrl))
            {
                TempData["Error"] = "لینک لینکدین باید http/https باشد.";
                return RedirectToAction(nameof(Index));
            }
            if (ModelState.IsValid)
            {
                var item = await _db.TeamMembers.FirstOrDefaultAsync(t => t.Id == model.Id, ct);
                if (item is not null)
                {
                    item.FullName = model.FullName;
                    item.Role = model.Role;
                    item.LinkedinUrl = model.LinkedinUrl;
                    item.IsActive = model.IsActive;
                    item.DisplayOrder = model.DisplayOrder;

                    if (imageFile is not null && imageFile.Length > 0)
                    {
                        item.Image = await _storage.SaveImageAsync(imageFile, "team", ct);
                    }
                    else
                    {
                        item.Image = model.Image?.Trim() ?? string.Empty;
                    }
                    await _db.SaveChangesAsync(ct);
                    TempData["Success"] = "عضو تیم ویرایش شد";
                }
            }
            else
            {
                TempData["Error"] = "ذخیره نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDisplayOrder(
            [FromForm(Name = "id")] int id,
            [FromForm(Name = "displayOrder")] int displayOrder,
            CancellationToken ct)
        {
            var item = await _db.TeamMembers.FirstOrDefaultAsync(t => t.Id == id, ct);
            if (item is null)
            {
                TempData["Error"] = "عضو تیم یافت نشد.";
                return RedirectToAction(nameof(Index));
            }

            item.DisplayOrder = Math.Max(0, displayOrder);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = $"ترتیب نمایش «{item.FullName}» به {item.DisplayOrder} تغییر کرد.";
            return RedirectToAction(nameof(Index));
        }

        private static bool IsSafeExternalUrl(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            return Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>فعال/غیرفعال کردن سریع عضو تیم</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id, CancellationToken ct)
        {
            var item = await _db.TeamMembers.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.IsActive = !item.IsActive;
                item.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = $"عضو «{item.FullName}» {(item.IsActive ? "فعال" : "غیرفعال")} شد";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var item = await _db.TeamMembers.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.IsDeleted = true;
                item.DeletedAt = DateTime.Now;
                item.IsActive = false;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = $"عضو تیم «{item.FullName}» حذف شد";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

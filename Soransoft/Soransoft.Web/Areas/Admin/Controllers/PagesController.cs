using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using Soransoft.Application.Services;
using Soransoft.Domain.Entities;
using Soransoft.Infrastructure.Persistence;

namespace Soransoft.Web.Areas.Admin.Controllers
{
    /// <summary>مدیریت صفحات محتوایی اسلاگ‌دار</summary>
    public class PagesController : AdminBaseController
    {
        private readonly SoransoftDbContext _db;
        private readonly IFileStorage _storage;

        public PagesController(SoransoftDbContext db, IFileStorage storage)
        {
            _db = db;
            _storage = storage;
        }

        public async Task<IActionResult> Index(CancellationToken ct) =>
            View(await _db.SitePages.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Title).ToListAsync(ct));

        public IActionResult Create() => View(new SitePage());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SitePage model, IFormFile? imageFile, CancellationToken ct)
        {
            model.Slug = await EnsureUniqueSlugAsync(NormalizeSlug(model.Slug, model.Title), null, ct);
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ذخیره نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return View(model);
            }

            if (imageFile is not null && imageFile.Length > 0)
                model.Image = await _storage.SaveImageAsync(imageFile, "site-pages", ct);
            else
                model.Image = model.Image?.Trim() ?? string.Empty;

            _db.SitePages.Add(model);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "صفحه با موفقیت ایجاد شد";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            var item = await _db.SitePages.FindAsync(new object[] { id }, ct);
            return item is null ? NotFound() : View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SitePage model, IFormFile? imageFile, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ذخیره نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return View(model);
            }

            var item = await _db.SitePages.FirstOrDefaultAsync(p => p.Id == model.Id, ct);
            if (item is null) return NotFound();

            item.Title = model.Title;
            item.Slug = await EnsureUniqueSlugAsync(NormalizeSlug(model.Slug, model.Title), model.Id, ct);
            item.Summary = model.Summary;
            item.Body = model.Body;
            item.SeoTitle = model.SeoTitle;
            item.SeoDescription = model.SeoDescription;
            item.DisplayOrder = model.DisplayOrder;
            item.IsPublished = model.IsPublished;

            if (imageFile is not null && imageFile.Length > 0)
                item.Image = await _storage.SaveImageAsync(imageFile, "site-pages", ct);
            else
                item.Image = model.Image?.Trim() ?? string.Empty;

            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "صفحه با موفقیت ویرایش شد";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var item = await _db.SitePages.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.IsDeleted = true;
                item.DeletedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = "صفحه حذف شد";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> EnsureUniqueSlugAsync(string slug, int? excludeId, CancellationToken ct)
        {
            var baseSlug = string.IsNullOrWhiteSpace(slug) ? "page" : slug;
            var candidate = baseSlug;
            var suffix = 2;
            while (await _db.SitePages.AnyAsync(p => p.Slug == candidate && (excludeId == null || p.Id != excludeId), ct))
                candidate = $"{baseSlug}-{suffix++}";
            return candidate;
        }

        private static string NormalizeSlug(string? slug, string title) =>
            string.IsNullOrWhiteSpace(slug)
                ? SlugGenerator.Generate(title)
                : SlugGenerator.Generate(slug, transliterate: false);
    }
}

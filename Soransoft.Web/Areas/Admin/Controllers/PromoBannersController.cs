using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using Soransoft.Domain.Entities;
using Soransoft.Domain.Enums;
using Soransoft.Infrastructure.Persistence;

namespace Soransoft.Web.Areas.Admin.Controllers
{
    /// <summary>مدیریت بنرهای تبلیغاتی و پرومو</summary>
    public class PromoBannersController : AdminBaseController
    {
        private readonly SoransoftDbContext _db;
        private readonly IFileStorage _storage;

        public PromoBannersController(SoransoftDbContext db, IFileStorage storage)
        {
            _db = db;
            _storage = storage;
        }

        public async Task<IActionResult> Index(CancellationToken ct) =>
            View(await _db.PromoBanners.OrderBy(b => b.Placement).ThenBy(b => b.DisplayOrder).ThenBy(b => b.Id).ToListAsync(ct));

        public IActionResult Create() => View(new PromoBanner());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PromoBanner model, IFormFile? imageFile, CancellationToken ct)
        {
            if (!Validate(model))
            {
                TempData["Error"] = "عنوان و لینک بنر را بررسی کنید.";
                return View(model);
            }

            model.Title = model.Title.Trim();
            model.Text = model.Text?.Trim() ?? string.Empty;
            model.Badge = model.Badge?.Trim();
            model.ButtonText = model.ButtonText?.Trim();
            model.Link = model.Link?.Trim();

            if (imageFile is not null && imageFile.Length > 0)
                model.Image = await _storage.SaveImageAsync(imageFile, "promo-banners", ct);
            else
                model.Image = string.IsNullOrWhiteSpace(model.Image) ? null : model.Image.Trim();

            _db.PromoBanners.Add(model);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "بنر پرومو با موفقیت ایجاد شد";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            var item = await _db.PromoBanners.FindAsync(new object[] { id }, ct);
            return item is null ? NotFound() : View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PromoBanner model, IFormFile? imageFile, CancellationToken ct)
        {
            if (!Validate(model))
            {
                TempData["Error"] = "عنوان و لینک بنر را بررسی کنید.";
                return View(model);
            }

            var item = await _db.PromoBanners.FirstOrDefaultAsync(b => b.Id == model.Id, ct);
            if (item is null) return NotFound();

            item.Title = model.Title.Trim();
            item.Text = model.Text?.Trim() ?? string.Empty;
            item.Badge = model.Badge?.Trim();
            item.ButtonText = model.ButtonText?.Trim();
            item.Link = model.Link?.Trim();
            item.Type = model.Type;
            item.ScrollDirection = model.ScrollDirection;
            item.Placement = model.Placement;
            item.DisplayOrder = model.DisplayOrder;
            item.IsActive = model.IsActive;
            item.OpenInNewTab = model.OpenInNewTab;

            if (imageFile is not null && imageFile.Length > 0)
                item.Image = await _storage.SaveImageAsync(imageFile, "promo-banners", ct);
            else
                item.Image = string.IsNullOrWhiteSpace(model.Image) ? null : model.Image.Trim();

            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "بنر پرومو با موفقیت ویرایش شد";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var item = await _db.PromoBanners.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.IsDeleted = true;
                item.DeletedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = "بنر پرومو حذف شد";
            }
            return RedirectToAction(nameof(Index));
        }

        private static bool Validate(PromoBanner model)
        {
            if (string.IsNullOrWhiteSpace(model.Title))
                return false;
            if (!Enum.IsDefined(typeof(PromoBannerType), model.Type) || !Enum.IsDefined(typeof(PromoBannerPlacement), model.Placement))
                return false;
            if (!Enum.IsDefined(typeof(PromoBannerScrollDirection), model.ScrollDirection))
                return false;
            if (!IsSafeLink(model.Link))
                return false;
            return true;
        }

        private static bool IsSafeLink(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            value = value.Trim();
            if (value.StartsWith("/", StringComparison.Ordinal) && !value.StartsWith("//", StringComparison.Ordinal)) return true;
            return Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using Soransoft.Domain.Entities;
using Soransoft.Infrastructure.Persistence;

namespace Soransoft.Web.Areas.Admin.Controllers
{
    /// <summary>مدیریت اسلایدر صفحه اصلی</summary>
    public class SlidersController : AdminBaseController
    {
        private readonly SoransoftDbContext _db;
        private readonly IFileStorage _storage;

        public SlidersController(SoransoftDbContext db, IFileStorage storage)
        {
            _db = db;
            _storage = storage;
        }

        public async Task<IActionResult> Index(CancellationToken ct) =>
            View(await _db.Sliders.OrderBy(s => s.DisplayOrder).ToListAsync(ct));

        public IActionResult Create() => View(new Slider());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Slider model, IFormFile? imageFile, CancellationToken ct)
        {
            if (!IsSafeLink(model.Link))
            {
                TempData["Error"] = "لینک اسلاید باید http/https یا مسیر داخلی باشد.";
                return View(model);
            }
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ذخیره نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return View(model);
            }

            if (imageFile is not null && imageFile.Length > 0)
                model.Image = await _storage.SaveImageAsync(imageFile, "sliders", ct);

            _db.Sliders.Add(model);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "اسلاید با موفقیت ایجاد شد";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            var item = await _db.Sliders.FindAsync(new object[] { id }, ct);
            if (item is null) return NotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Slider model, IFormFile? imageFile, CancellationToken ct)
        {
            if (!IsSafeLink(model.Link))
            {
                TempData["Error"] = "لینک اسلاید باید http/https یا مسیر داخلی باشد.";
                return View(model);
            }
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ذخیره نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return View(model);
            }

            var item = await _db.Sliders.FirstOrDefaultAsync(s => s.Id == model.Id, ct);
            if (item is null) return NotFound();

            item.Title = model.Title;
            item.SubTitle = model.SubTitle;
            item.Link = model.Link;
            item.IsActive = model.IsActive;
            item.DisplayOrder = model.DisplayOrder;

            if (imageFile is not null && imageFile.Length > 0)
            {
                await _storage.DeleteAsync(item.Image, ct);
                item.Image = await _storage.SaveImageAsync(imageFile, "sliders", ct);
            }

            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "اسلاید با موفقیت ویرایش شد";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var item = await _db.Sliders.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                await _storage.DeleteAsync(item.Image, ct);
                item.IsDeleted = true;
                item.DeletedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = "اسلاید حذف شد";
            }
            return RedirectToAction(nameof(Index));
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

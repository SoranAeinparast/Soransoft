using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using Soransoft.Domain.Entities;
using Soransoft.Infrastructure.Persistence;
using Soransoft.Web.Models.Admin;
using System.Globalization;

namespace Soransoft.Web.Areas.Admin.Controllers
{
    /// <summary>مدیریت اسلایدر صفحه اصلی</summary>
    public class SlidersController : AdminBaseController
    {
        private readonly SoransoftDbContext _db;
        private readonly IFileStorage _storage;
        private readonly ISiteSettingService _settings;

        public SlidersController(SoransoftDbContext db, IFileStorage storage, ISiteSettingService settings)
        {
            _db = db;
            _storage = storage;
            _settings = settings;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var settings = await _settings.GetAllAsync(ct);
            return View(new SlidersIndexViewModel
            {
                Sliders = await _db.Sliders.OrderBy(s => s.DisplayOrder).ToListAsync(ct),
                Settings = new SliderSettingsViewModel
                {
                    TransitionEffect = settings.GetValueOrDefault("SliderTransition", "fade") == "slide" ? "slide" : "fade",
                    Height = ParseInt(settings.GetValueOrDefault("SliderHeight", "560"), 560, 300, 800),
                    OverlayOpacity = ParseDecimal(settings.GetValueOrDefault("SliderOverlayOpacity", "0.25"), .25m, 0, 1),
                    Interval = ParseInt(settings.GetValueOrDefault("SliderInterval", "5000"), 5000, 2000, 30000),
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSettings(SliderSettingsViewModel model, CancellationToken ct)
        {
            var values = new Dictionary<string, string>
            {
                ["SliderTransition"] = model.TransitionEffect == "slide" ? "slide" : "fade",
                ["SliderHeight"] = ParseInt(model.Height.ToString(CultureInfo.InvariantCulture), 560, 300, 800).ToString(CultureInfo.InvariantCulture),
                ["SliderOverlayOpacity"] = ParseDecimal(model.OverlayOpacity.ToString(CultureInfo.InvariantCulture), .25m, 0, 1).ToString("0.##", CultureInfo.InvariantCulture),
                ["SliderInterval"] = ParseInt(model.Interval.ToString(CultureInfo.InvariantCulture), 5000, 2000, 30000).ToString(CultureInfo.InvariantCulture),
            };

            foreach (var pair in values)
            {
                var setting = await _db.SiteSettings.FirstOrDefaultAsync(s => s.Key == pair.Key, ct);
                if (setting is null)
                {
                    _db.SiteSettings.Add(new SiteSetting
                    {
                        Key = pair.Key,
                        Title = pair.Key,
                        Value = pair.Value,
                        Group = "اسلایدر",
                        Type = "text",
                        DisplayOrder = values.Keys.ToList().IndexOf(pair.Key) + 1,
                    });
                }
                else
                {
                    setting.Value = pair.Value;
                }
            }

            await _db.SaveChangesAsync(ct);
            await _settings.InvalidateCacheAsync();
            TempData["Success"] = "تنظیمات اسلایدر ذخیره شد";
            return RedirectToAction(nameof(Index));
        }

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
            if (value.StartsWith('/', StringComparison.Ordinal) && !value.StartsWith("//", StringComparison.Ordinal)) return true;
            return Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        private static int ParseInt(string value, int fallback, int min, int max) =>
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? Math.Clamp(parsed, min, max) : fallback;

        private static decimal ParseDecimal(string value, decimal fallback, decimal min, decimal max) =>
            decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
                ? Math.Clamp(parsed, min, max) : fallback;
    }
}

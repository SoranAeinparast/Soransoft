using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Domain.Enums;
using Soransoft.Infrastructure.Persistence;
using Soransoft.Application.Interfaces;

namespace Soransoft.Web.Areas.Admin.Controllers
{
    /// <summary>مدیریت سفارش‌های پروژه</summary>
    public class ProjectOrdersController : AdminBaseController
    {
        private readonly SoransoftDbContext _db;
        public ProjectOrdersController(SoransoftDbContext db) => _db = db;

        public async Task<IActionResult> Index(CancellationToken ct) =>
            View(await _db.ProjectOrders.OrderByDescending(o => o.CreatedAt).ToListAsync(ct));

        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            var item = await _db.ProjectOrders.FindAsync(new object[] { id }, ct);
            if (item is null) return NotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, ProjectOrderStatus status, string? adminNote, CancellationToken ct)
        {
            var item = await _db.ProjectOrders.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.Status = status;
                item.AdminNote = adminNote;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = "وضعیت سفارش تغییر کرد";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var item = await _db.ProjectOrders.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                _db.ProjectOrders.Remove(item);
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = "سفارش حذف شد";
            }
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>مدیریت پیام‌های تماس</summary>
    public class ContactMessagesController : AdminBaseController
    {
        private readonly SoransoftDbContext _db;
        public ContactMessagesController(SoransoftDbContext db) => _db = db;

        public async Task<IActionResult> Index(CancellationToken ct) =>
            View(await _db.ContactMessages.OrderByDescending(m => m.CreatedAt).ToListAsync(ct));

        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            var item = await _db.ContactMessages.FindAsync(new object[] { id }, ct);
            if (item is null) return NotFound();
            item.Status = MessageStatus.Read;
            await _db.SaveChangesAsync(ct);
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var item = await _db.ContactMessages.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                _db.ContactMessages.Remove(item);
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = "پیام حذف شد";
            }
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>مدیریت درخواست‌های مشاوره</summary>
    public class ConsultationRequestsController : AdminBaseController
    {
        private readonly SoransoftDbContext _db;
        public ConsultationRequestsController(SoransoftDbContext db) => _db = db;

        public async Task<IActionResult> Index(CancellationToken ct) =>
            View(await _db.ConsultationRequests.OrderByDescending(c => c.CreatedAt).ToListAsync(ct));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAnswered(int id, CancellationToken ct)
        {
            var item = await _db.ConsultationRequests.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.Status = MessageStatus.Answered;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = "به عنوان پاسخ‌داده‌شده علامت خورد";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var item = await _db.ConsultationRequests.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                _db.ConsultationRequests.Remove(item);
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = "درخواست حذف شد";
            }
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>مدیریت تنظیمات سایت</summary>
    public class SiteSettingsController : AdminBaseController
    {
        private readonly SoransoftDbContext _db;
        private readonly ISiteSettingService _settings;
        private readonly IFileStorage _storage;
        public SiteSettingsController(SoransoftDbContext db, ISiteSettingService settings, IFileStorage storage)
        {
            _db = db;
            _settings = settings;
            _storage = storage;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var settings = await _db.SiteSettings
                .OrderBy(s => s.Group).ThenBy(s => s.DisplayOrder)
                .ToListAsync(ct);
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(List<Soransoft.Domain.Entities.SiteSetting> settings, CancellationToken ct)
        {
            var form = await Request.ReadFormAsync(ct);
            var rejected = false;
            foreach (var input in settings)
            {
                var entity = await _db.SiteSettings.FirstOrDefaultAsync(s => s.Id == input.Id, ct);
                if (entity is null) continue;
                var value = input.Value?.Trim() ?? string.Empty;
                if (entity.Key.EndsWith("Url", StringComparison.OrdinalIgnoreCase) && !IsSafeLink(value))
                {
                    rejected = true;
                    TempData["Error"] = $"مقدار تنظیم «{entity.Title}» باید یک لینک امن http/https یا مسیر داخلی باشد.";
                    continue;
                }
                var oldValue = entity.Value;
                entity.Value = value;
                var image = form.Files.GetFile($"settingFile_{entity.Id}");
                if (entity.Type.Equals("image", StringComparison.OrdinalIgnoreCase) && image is not null && image.Length > 0)
                {
                    entity.Value = await _storage.SaveImageAsync(image, "site-settings", ct);
                    await _storage.DeleteAsync(oldValue, ct);
                }
                entity.UpdatedAt = DateTime.Now;
            }
            await _db.SaveChangesAsync(ct);
            await _settings.InvalidateCacheAsync();
            if (!rejected) TempData["Success"] = "تنظیمات ذخیره شد";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>افزودن تنظیم جدید (کلید/مقدار) به گروه دلخواه</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string key, string title, string value, string group, string type, IFormFile? imageFile, CancellationToken ct)
        {
            key = key?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(title))
            {
                TempData["Error"] = "کلید و عنوان تنظیم الزامی است";
                return RedirectToAction(nameof(Index));
            }
            if (await _db.SiteSettings.AnyAsync(s => s.Key == key, ct))
            {
                TempData["Error"] = $"تنظیمی با کلید «{key}» از قبل وجود دارد";
                return RedirectToAction(nameof(Index));
            }
            value = value?.Trim() ?? string.Empty;
            if (string.Equals(type, "image", StringComparison.OrdinalIgnoreCase) && imageFile is not null && imageFile.Length > 0)
                value = await _storage.SaveImageAsync(imageFile, "site-settings", ct);
            if (key.EndsWith("Url", StringComparison.OrdinalIgnoreCase) && !IsSafeLink(value))
            {
                TempData["Error"] = "لینک تنظیم باید http/https یا مسیر داخلی باشد.";
                return RedirectToAction(nameof(Index));
            }
            var order = await _db.SiteSettings.Where(s => s.Group == group).MaxAsync(s => (int?)s.DisplayOrder, ct) ?? 0;
            _db.SiteSettings.Add(new Soransoft.Domain.Entities.SiteSetting
            {
                Key = key,
                Title = title.Trim(),
                Value = value,
                Group = string.IsNullOrWhiteSpace(group) ? "General" : group.Trim(),
                Type = string.IsNullOrWhiteSpace(type) ? "text" : type.Trim(),
                DisplayOrder = order + 1,
            });
            await _db.SaveChangesAsync(ct);
            await _settings.InvalidateCacheAsync();
            TempData["Success"] = $"تنظیم «{title}» اضافه شد";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>حذف کامل یک تنظیم از دیتابیس</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var item = await _db.SiteSettings.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                _db.SiteSettings.Remove(item);
                await _db.SaveChangesAsync(ct);
                await _settings.InvalidateCacheAsync();
                TempData["Success"] = $"تنظیم «{item.Title}» حذف شد";
            }
            return RedirectToAction(nameof(Index));
        }

        private static bool IsSafeLink(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            if (value.StartsWith("/", StringComparison.Ordinal) && !value.StartsWith("//", StringComparison.Ordinal)) return true;
            return Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }

    /// <summary>مدیریت منوها</summary>
    public class MenuItemsController : AdminBaseController
    {
        private readonly SoransoftDbContext _db;
        public MenuItemsController(SoransoftDbContext db) => _db = db;

        public async Task<IActionResult> Index(CancellationToken ct) =>
            View(await _db.MenuItems.OrderBy(m => m.Position).ThenBy(m => m.DisplayOrder).ToListAsync(ct));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Soransoft.Domain.Entities.MenuItem model, CancellationToken ct)
        {
            model.Url = model.Url?.Trim() ?? string.Empty;
            if (!IsSafeLink(model.Url))
            {
                TempData["Error"] = "آدرس منو باید http/https یا مسیر داخلی باشد.";
                return RedirectToAction(nameof(Index));
            }
            if (ModelState.IsValid)
            {
                _db.MenuItems.Add(model);
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = "آیتم منو ایجاد شد";
            }
            else
            {
                TempData["Error"] = "ذخیره نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Soransoft.Domain.Entities.MenuItem model, CancellationToken ct)
        {
            model.Url = model.Url?.Trim() ?? string.Empty;
            if (!IsSafeLink(model.Url))
            {
                TempData["Error"] = "آدرس منو باید http/https یا مسیر داخلی باشد.";
                return RedirectToAction(nameof(Index));
            }
            if (ModelState.IsValid)
            {
                var item = await _db.MenuItems.FirstOrDefaultAsync(m => m.Id == model.Id, ct);
                if (item is not null)
                {
                    item.Title = model.Title;
                    item.Url = model.Url;
                    item.Position = model.Position;
                    item.DisplayOrder = model.DisplayOrder;
                    item.IsActive = model.IsActive;
                    await _db.SaveChangesAsync(ct);
                    TempData["Success"] = "آیتم منو ویرایش شد";
                }
            }
            else
            {
                TempData["Error"] = "ذخیره نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            }
            return RedirectToAction(nameof(Index));
        }

        private static bool IsSafeLink(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            value = value.Trim();
            if (value.StartsWith("/", StringComparison.Ordinal) && !value.StartsWith("//", StringComparison.Ordinal)) return true;
            return Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>فعال/غیرفعال کردن سریع آیتم منو</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id, CancellationToken ct)
        {
            var item = await _db.MenuItems.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.IsActive = !item.IsActive;
                item.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = $"منوی «{item.Title}» {(item.IsActive ? "فعال" : "غیرفعال")} شد";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var item = await _db.MenuItems.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.IsDeleted = true;
                item.DeletedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = $"منوی «{item.Title}» حذف شد";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

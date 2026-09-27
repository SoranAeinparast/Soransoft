using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Domain.Entities;
using Soransoft.Infrastructure.Persistence;

namespace Soransoft.Web.Areas.Admin.Controllers
{
    /// <summary>فرم ثبت/ویرایش آیتم کاتالوگ هزینه شخص ثالث</summary>
    public class ThirdPartyCostFormModel
    {
        public int Id { get; set; }
        public ThirdPartyCostType Type { get; set; } = ThirdPartyCostType.Other;
        public string Title { get; set; } = string.Empty;
        public string? TechnicalSpecs { get; set; }
        public long DefaultAmount { get; set; }
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// کاتالوگ هزینه‌های شخص ثالث (هاست، دامنه، گواهینامه، سرویس/API و...)
    /// که مدیر ایجاد/ویرایش/حذف می‌کند و روی قرارداد مشتری اعمال می‌شود.
    /// </summary>
    [Area("Admin")]
    [Route("Admin/PortalCosts/{action=Index}/{id?}")]
    [Authorize(Policy = "AdminOnly")]
    public class PortalCostsController : Controller
    {
        private readonly SoransoftDbContext _db;

        public PortalCostsController(SoransoftDbContext db) => _db = db;

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var items = await _db.ThirdPartyCostItems.AsNoTracking()
                .OrderBy(i => i.Type).ThenBy(i => i.DisplayOrder).ThenBy(i => i.Id)
                .ToListAsync(ct);

            ViewBag.UsageCounts = await _db.ContractThirdPartyCosts.AsNoTracking()
                .GroupBy(c => c.ItemId)
                .Select(g => new { ItemId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ItemId ?? 0, x => x.Count, ct);

            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(ThirdPartyCostFormModel model, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(model.Title))
            {
                TempData["Error"] = "عنوان آیتم الزامی است.";
                return RedirectToAction(nameof(Index));
            }

            ThirdPartyCostItem item;
            if (model.Id == 0)
            {
                item = new ThirdPartyCostItem();
                _db.ThirdPartyCostItems.Add(item);
            }
            else
            {
                var existing = await _db.ThirdPartyCostItems.FirstOrDefaultAsync(i => i.Id == model.Id, ct);
                if (existing is null) { TempData["Error"] = "آیتم یافت نشد."; return RedirectToAction(nameof(Index)); }
                item = existing;
            }

            item.Type = model.Type;
            item.Title = model.Title.Trim();
            item.TechnicalSpecs = string.IsNullOrWhiteSpace(model.TechnicalSpecs) ? null : model.TechnicalSpecs.Trim();
            item.DefaultAmount = model.DefaultAmount;
            item.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            item.DisplayOrder = model.DisplayOrder;
            item.IsActive = model.IsActive;

            await _db.SaveChangesAsync(ct);
            TempData["Success"] = model.Id == 0 ? "آیتم هزینه شخص ثالث ثبت شد." : "آیتم به‌روزرسانی شد.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var item = await _db.ThirdPartyCostItems.FirstOrDefaultAsync(i => i.Id == id, ct);
            if (item is null) { TempData["Error"] = "آیتم یافت نشد."; return RedirectToAction(nameof(Index)); }

            // اگر روی قراردادی اعمال شده، ابتدا حذف فیزیکی نیست؛ فقط از فهرست فعال خارج می‌شود
            var used = await _db.ContractThirdPartyCosts.AnyAsync(c => c.ItemId == id, ct);
            if (used)
            {
                item.IsActive = false;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = "این آیتم روی قراردادهایی اعمال شده است؛ از فهرست فعال خارج شد (سوابق حفظ می‌شود).";
                return RedirectToAction(nameof(Index));
            }

            item.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "آیتم حذف شد.";
            return RedirectToAction(nameof(Index));
        }
    }
}

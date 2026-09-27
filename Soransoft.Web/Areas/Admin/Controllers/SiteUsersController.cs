using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Infrastructure.Persistence;
using Soransoft.Domain.Enums;

namespace Soransoft.Web.Areas.Admin.Controllers
{
    /// <summary>مدیریت کاربران عادی سایت (ثبت‌نام‌کرده)</summary>
    public class SiteUsersController : AdminBaseController
    {
        private readonly SoransoftDbContext _db;
        public SiteUsersController(SoransoftDbContext db) => _db = db;

        public async Task<IActionResult> Index(CancellationToken ct) =>
            View(await _db.SiteUsers
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync(ct));

        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            var item = await _db.SiteUsers
                .Include(u => u.ProjectOrders)
                .FirstOrDefaultAsync(u => u.Id == id, ct);
            if (item is null) return NotFound();
            return View(item);
        }

        /// <summary>فعال/غیرفعال کردن کاربر (کاربر غیرفعال نمی‌تواند وارد شود)</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id, CancellationToken ct)
        {
            var item = await _db.SiteUsers.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.IsActive = !item.IsActive;
                item.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = $"کاربر «{item.FullName}» {(item.IsActive ? "فعال" : "غیرفعال")} شد";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var item = await _db.SiteUsers.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.IsDeleted = true;
                item.DeletedAt = DateTime.Now;
                item.IsActive = false;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = $"کاربر «{item.FullName}» حذف شد";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

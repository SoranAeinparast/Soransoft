using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Infrastructure.Persistence;

namespace Soransoft.Web.Areas.Admin.Controllers
{
    /// <summary>مشاهده و مدیریت لاگ خطاهای برنامه</summary>
    public class ErrorLogsController : AdminBaseController
    {
        private const int PageSize = 30;
        private readonly SoransoftDbContext _db;

        public ErrorLogsController(SoransoftDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? severity, int page = 1, CancellationToken ct = default)
        {
            page = Math.Max(1, page);

            var query = _db.ErrorLogs.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(severity))
                query = query.Where(e => e.Severity == severity);

            var total = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync(ct);

            ViewBag.CurrentSeverity = severity;
            ViewBag.CurrentPage = page;
            ViewBag.TotalCount = total;
            ViewBag.PageSize = PageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)PageSize);

            return View(items);
        }

        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            var item = await _db.ErrorLogs.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
            if (item is null) return NotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var item = await _db.ErrorLogs.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                _db.ErrorLogs.Remove(item);
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = "لاگ حذف شد";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearAll(CancellationToken ct)
        {
            // حذف دسته‌ای بدون بارگذاری موجودیت‌ها
            await _db.ErrorLogs.ExecuteDeleteAsync(ct);
            TempData["Success"] = "همه لاگ‌ها پاک شدند";
            return RedirectToAction(nameof(Index));
        }
    }
}

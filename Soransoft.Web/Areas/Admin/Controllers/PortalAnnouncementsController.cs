using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using Soransoft.Domain.Entities;
using Soransoft.Infrastructure.Persistence;
using System.ComponentModel.DataAnnotations;

namespace Soransoft.Web.Areas.Admin.Controllers
{
    /// <summary>فرم ارسال اطلاعیه</summary>
    public class AnnouncementCreateModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "عنوان الزامی است")]
        public string Title { get; set; } = string.Empty;
        [Required(ErrorMessage = "متن اطلاعیه الزامی است")]
        public string Body { get; set; } = string.Empty;
        /// <summary>All / Sales / Dev</summary>
        public string Audience { get; set; } = "All";
        /// <summary>گیرنده مشخص (اختیاری)</summary>
        public int? PartnerId { get; set; }
        public bool IsImportant { get; set; }
    }

    /// <summary>مرکز ارتباطی: اطلاعیه عمومی/تیمی/اختصاصی به همکاران</summary>
    [Area("Admin")]
    [Route("Admin/PortalAnnouncements/{action=Index}/{id?}")]
    [Authorize(Policy = "AdminOnly")]
    public class PortalAnnouncementsController : Controller
    {
        private readonly SoransoftDbContext _db;
        private readonly IPartnerPortalService _portal;

        public PortalAnnouncementsController(SoransoftDbContext db, IPartnerPortalService portal)
        {
            _db = db;
            _portal = portal;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var items = await _db.Announcements.AsNoTracking()
                .Include(a => a.Partner)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(ct);
            ViewBag.Partners = await _db.Partners.AsNoTracking()
                .Where(p => !p.IsDeleted && p.IsActive)
                .Select(p => new { p.Id, p.FullName })
                .ToListAsync(ct);
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(AnnouncementCreateModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Index));
            }

            if (!await ValidateAudienceAsync(model.Audience, model.PartnerId, ct))
                return RedirectToAction(nameof(Index));

            var announcement = new Announcement
            {
                Title = model.Title.Trim(),
                Body = model.Body.Trim(),
                Audience = model.PartnerId is null ? model.Audience : "All",
                PartnerId = model.PartnerId,
                IsImportant = model.IsImportant,
            };

            var recipients = await _portal.SendAnnouncementAsync(announcement, ct);
            TempData["Success"] = $"اطلاعیه برای {recipients} همکار ارسال شد.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AnnouncementCreateModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Index));
            }
            if (!await ValidateAudienceAsync(model.Audience, model.PartnerId, ct))
                return RedirectToAction(nameof(Index));

            var announcement = await _db.Announcements.FirstOrDefaultAsync(a => a.Id == model.Id && !a.IsDeleted, ct);
            if (announcement is null) { TempData["Error"] = "اطلاعیه یافت نشد."; return RedirectToAction(nameof(Index)); }

            announcement.Title = model.Title.Trim();
            announcement.Body = model.Body.Trim();
            announcement.Audience = model.PartnerId is null ? model.Audience : "All";
            announcement.PartnerId = model.PartnerId;
            announcement.IsImportant = model.IsImportant;
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "اطلاعیه به‌روزرسانی شد.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var a = await _db.Announcements.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
            if (a is null) { TempData["Error"] = "اطلاعیه یافت نشد."; return RedirectToAction(nameof(Index)); }
            a.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "اطلاعیه حذف شد.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> ValidateAudienceAsync(string audience, int? partnerId, CancellationToken ct)
        {
            if (partnerId is not null)
            {
                if (!await _db.Partners.AnyAsync(p => p.Id == partnerId && !p.IsDeleted && p.IsActive, ct))
                {
                    TempData["Error"] = "همکار انتخاب‌شده معتبر نیست.";
                    return false;
                }
                return true;
            }

            if (audience is not ("All" or "Sales" or "Dev"))
            {
                TempData["Error"] = "مخاطب اطلاعیه معتبر نیست.";
                return false;
            }
            return true;
        }
    }
}

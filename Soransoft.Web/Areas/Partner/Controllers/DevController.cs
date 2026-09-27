using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using Soransoft.Domain.Entities;
using Soransoft.Infrastructure.Persistence;
using TaskStatus = Soransoft.Domain.Entities.TaskStatus;

namespace Soransoft.Web.Areas.Partner.Controllers
{
    /// <summary>فرم ثبت زمان کار</summary>
    public class TimeLogInputModel
    {
        public int? TaskId { get; set; }
        public int? ProjectId { get; set; }
        [Required(ErrorMessage = "مدت کار را وارد کنید")]
        [Range(1, 24 * 60, ErrorMessage = "مدت باید بین ۱ دقیقه تا ۲۴ ساعت باشد")]
        public int Minutes { get; set; }
        [Required(ErrorMessage = "تاریخ کار الزامی است")]
        public DateTime WorkDate { get; set; } = DateTime.Now;
        public string? Note { get; set; }
    }

    /// <summary>پنل فنی همکار — تسک‌های خودش + مستندات پروژه‌های عضو</summary>
    public class DevController : PartnerBaseController
    {
        private readonly SoransoftDbContext _db;
        private readonly IPartnerPortalService _portal;

        public DevController(SoransoftDbContext db, IPartnerPortalService portal)
        {
            _db = db;
            _portal = portal;
        }

        private bool NotDev() => !IsDevSide;

        // ============================================================ برد تسک‌ها

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            if (NotDev()) return Forbidden();

            var tasks = await _db.DevTasks.AsNoTracking()
                .Include(t => t.Project)
                .Include(t => t.Sprint)
                .Where(t => !t.IsDeleted && (t.PartnerId == PartnerId
                    || (Role == PartnerRole.TechManager && t.PartnerId != null)))
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync(ct);
            return View(tasks);
        }

        /// <summary>تغییر وضعیت تسک روی برد (To Do → In Progress → Testing → Done)</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeTaskStatus(int id, TaskStatus status, CancellationToken ct)
        {
            if (NotDev()) return Forbidden();

            var task = await _db.DevTasks.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, ct);
            if (task is null) { TempData["Error"] = "تسک یافت نشد."; return RedirectToAction(nameof(Index)); }

            // توسعه‌دهنده فقط تسک خودش؛ مدیر فنی همه
            if (task.PartnerId != PartnerId && Role != PartnerRole.TechManager)
            { TempData["Error"] = "این تسک به شما اختصاص ندارد."; return RedirectToAction(nameof(Index)); }

            var result = await _portal.ChangeTaskStatusAsync(id, status, ct);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // ============================================================ ثبت زمان

        public async Task<IActionResult> TimeLogs(CancellationToken ct)
        {
            if (NotDev()) return Forbidden();

            var logs = await _db.TimeLogs.AsNoTracking()
                .Include(t => t.Task)
                .Include(t => t.Project)
                .Where(t => t.PartnerId == PartnerId)
                .OrderByDescending(t => t.WorkDate).ThenByDescending(t => t.CreatedAt)
                .Take(100)
                .ToListAsync(ct);

            var myTasks = await _db.DevTasks.AsNoTracking()
                .Where(t => !t.IsDeleted && t.PartnerId == PartnerId && t.Status != TaskStatus.Done)
                .Select(t => new { t.Id, t.Title })
                .ToListAsync(ct);
            ViewBag.MyTasks = myTasks;

            var myProjects = await _db.ProjectMembers.AsNoTracking()
                .Where(m => m.PartnerId == PartnerId)
                .Select(m => m.Project).Where(p => p != null && p.IsActive)
                .Select(p => new { p!.Id, p.Title })
                .ToListAsync(ct);
            ViewBag.MyProjects = myProjects;

            ViewBag.TotalMinutes = await _db.TimeLogs.AsNoTracking()
                .Where(t => t.PartnerId == PartnerId)
                .SumAsync(t => (int?)t.Minutes, ct) ?? 0;

            return View(logs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTimeLog(TimeLogInputModel model, CancellationToken ct)
        {
            if (NotDev()) return Forbidden();
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(TimeLogs));
            }

            // تسک انتخابی باید مال خودش باشد
            if (model.TaskId is not null)
            {
                var ok = await _db.DevTasks.AnyAsync(t => t.Id == model.TaskId && t.PartnerId == PartnerId && !t.IsDeleted, ct);
                if (!ok) { TempData["Error"] = "تسک نامعتبر است."; return RedirectToAction(nameof(TimeLogs)); }
            }

            _db.TimeLogs.Add(new TimeLog
            {
                PartnerId = PartnerId,
                TaskId = model.TaskId,
                ProjectId = model.ProjectId,
                Minutes = model.Minutes,
                WorkDate = model.WorkDate,
                Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim(),
            });
            await _db.SaveChangesAsync(ct);

            TempData["Success"] = "زمان کار ثبت شد.";
            return RedirectToAction(nameof(TimeLogs));
        }

        // ============================================================ مستندات فنی

        public async Task<IActionResult> Documents(CancellationToken ct)
        {
            if (NotDev()) return Forbidden();

            var myProjectIds = await _db.ProjectMembers.AsNoTracking()
                .Where(m => m.PartnerId == PartnerId)
                .Select(m => m.ProjectId)
                .ToListAsync(ct);

            var projects = await _db.DevProjects.AsNoTracking()
                .Where(p => !p.IsDeleted && myProjectIds.Contains(p.Id))
                .Include(p => p.Documents.Where(d => !d.IsDeleted && d.IsActive))
                .OrderBy(p => p.Title)
                .ToListAsync(ct);

            return View(projects);
        }

        // ============================================================ پروفایل فنی

        [HttpGet]
        public async Task<IActionResult> Profile(CancellationToken ct)
        {
            if (NotDev()) return Forbidden();

            var me = await _db.Partners.AsNoTracking().FirstAsync(p => p.Id == PartnerId, ct);
            var memberships = await _db.ProjectMembers.AsNoTracking()
                .Include(m => m.Project)
                .Where(m => m.PartnerId == PartnerId)
                .ToListAsync(ct);

            ViewBag.Memberships = memberships;
            return View(me);
        }

        /// <summary>ویرایش مهارت‌ها توسط خود توسعه‌دهنده</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSkills(string? skills, CancellationToken ct)
        {
            if (NotDev()) return Forbidden();

            var me = await _db.Partners.FirstOrDefaultAsync(p => p.Id == PartnerId, ct);
            if (me is null) return Forbidden();
            me.Skills = string.IsNullOrWhiteSpace(skills) ? null : skills.Trim();
            await _db.SaveChangesAsync(ct);

            TempData["Success"] = "مهارت‌ها به‌روزرسانی شد.";
            return RedirectToAction(nameof(Profile));
        }

        // ============================================================ اطلاعیه‌ها

        public async Task<IActionResult> Notifications(CancellationToken ct)
        {
            var audienceKey = IsSalesSide ? "Sales" : "Dev";
            var items = await _db.Announcements.AsNoTracking()
                .Where(a => !a.IsDeleted && (
                    a.PartnerId == PartnerId ||
                    (a.PartnerId == null && (a.Audience == "All" || a.Audience == audienceKey))))
                .OrderByDescending(a => a.IsImportant).ThenByDescending(a => a.CreatedAt)
                .ToListAsync(ct);
            return View(items);
        }

        /// <summary>علامت‌گذاری اطلاعیه به‌عنوان خوانده‌شده</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id, CancellationToken ct)
        {
            var exists = await _db.Announcements.AnyAsync(a => a.Id == id && !a.IsDeleted, ct);
            if (!exists) { TempData["Error"] = "اطلاعیه یافت نشد."; return RedirectToAction(nameof(Notifications)); }

            var already = await _db.PartnerNotificationReads.AnyAsync(r => r.AnnouncementId == id && r.PartnerId == PartnerId, ct);
            if (!already)
            {
                _db.PartnerNotificationReads.Add(new PartnerNotificationRead { AnnouncementId = id, PartnerId = PartnerId });
                await _db.SaveChangesAsync(ct);
            }
            return RedirectToAction(nameof(Notifications));
        }
    }
}

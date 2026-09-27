using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Domain.Entities;
using Soransoft.Infrastructure.Persistence;
using System.ComponentModel.DataAnnotations;
// حرف‌گرفتن ابهام: TaskStatus پرتال جای System.Threading.Tasks.TaskStatus می‌نشیند
using TaskStatus = Soransoft.Domain.Entities.TaskStatus;

namespace Soransoft.Web.Areas.Admin.Controllers
{
    /// <summary>فرم ساخت تسک</summary>
    public class DevTaskCreateModel
    {
        [Required(ErrorMessage = "عنوان تسک الزامی است")]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ProjectId { get; set; }
        public int? SprintId { get; set; }
        public int? AssigneeId { get; set; }
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public DateTime? DueDate { get; set; }
    }

    /// <summary>نظارت بر تیم فنی: تسک‌ها، پروژه‌ها، اسپرینت‌ها و بار کاری</summary>
    [Area("Admin")]
    [Route("Admin/PortalDev/{action=Index}/{id?}")]
    [Authorize(Policy = "AdminOnly")]
    public class PortalDevController : Controller
    {
        private readonly SoransoftDbContext _db;

        public PortalDevController(SoransoftDbContext db) => _db = db;

        // ============================================================ برد تسک‌ها

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var tasks = await _db.DevTasks.AsNoTracking()
                .Include(t => t.Project)
                .Include(t => t.Sprint)
                .Include(t => t.Partner)
                .Where(t => !t.IsDeleted)
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync(ct);

            ViewBag.Projects = await _db.DevProjects.AsNoTracking().Where(p => !p.IsDeleted && p.IsActive).Select(p => new { p.Id, p.Title }).ToListAsync(ct);
            ViewBag.Sprints = await _db.Sprints.AsNoTracking().Where(s => !s.IsDeleted && s.IsActive).Select(s => new { s.Id, s.Title }).ToListAsync(ct);
            ViewBag.Devs = await _db.Partners.AsNoTracking()
                .Where(p => !p.IsDeleted && p.IsActive && (p.Role == PartnerRole.Developer || p.Role == PartnerRole.TechManager))
                .Select(p => new { p.Id, p.FullName })
                .ToListAsync(ct);
            return View(tasks);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTask(DevTaskCreateModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Index));
            }

            _db.DevTasks.Add(new DevTask
            {
                Title = model.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                ProjectId = model.ProjectId,
                SprintId = model.SprintId,
                PartnerId = model.AssigneeId,
                Priority = model.Priority,
                DueDate = model.DueDate,
                Status = TaskStatus.ToDo,
                DisplayOrder = (await _db.DevTasks.AnyAsync(ct) ? await _db.DevTasks.MaxAsync(t => t.DisplayOrder, ct) : 0) + 1,
            });
            await _db.SaveChangesAsync(ct);

            TempData["Success"] = "تسک ساخته شد.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(int id, int? assigneeId, CancellationToken ct)
        {
            var t = await _db.DevTasks.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
            if (t is null) { TempData["Error"] = "تسک یافت نشد."; return RedirectToAction(nameof(Index)); }
            t.PartnerId = assigneeId;
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "تسک تخصیص یافت.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(int id, TaskStatus status, CancellationToken ct)
        {
            var t = await _db.DevTasks.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
            if (t is null) { TempData["Error"] = "تسک یافت نشد."; return RedirectToAction(nameof(Index)); }
            t.Status = status;
            t.CompletedAt = status == TaskStatus.Done ? DateTime.Now : null;
            await _db.SaveChangesAsync(ct);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTask(int id, CancellationToken ct)
        {
            var t = await _db.DevTasks.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
            if (t is null) { TempData["Error"] = "تسک یافت نشد."; return RedirectToAction(nameof(Index)); }
            t.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "تسک حذف شد.";
            return RedirectToAction(nameof(Index));
        }

        // ============================================================ پروژه‌ها و اسپرینت‌ها

        public async Task<IActionResult> Projects(CancellationToken ct)
        {
            var projects = await _db.DevProjects.AsNoTracking()
                .Include(p => p.Tasks.Where(t => !t.IsDeleted))
                .Include(p => p.Members).ThenInclude(m => m.Partner)
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.Title)
                .ToListAsync(ct);

            ViewBag.Devs = await _db.Partners.AsNoTracking()
                .Where(p => !p.IsDeleted && p.IsActive && (p.Role == PartnerRole.Developer || p.Role == PartnerRole.TechManager))
                .Select(p => new { p.Id, p.FullName })
                .ToListAsync(ct);
            return View(projects);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProject(string title, string? description, string? statusText, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(title)) { TempData["Error"] = "عنوان پروژه الزامی است."; return RedirectToAction(nameof(Projects)); }
            _db.DevProjects.Add(new DevProject { Title = title.Trim(), Description = description?.Trim(), StatusText = statusText?.Trim() });
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "پروژه ساخته شد.";
            return RedirectToAction(nameof(Projects));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(int projectId, int partnerId, string? roleInProject, CancellationToken ct)
        {
            var exists = await _db.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.PartnerId == partnerId, ct);
            if (!exists)
            {
                _db.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, PartnerId = partnerId, RoleInProject = roleInProject?.Trim() });
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = "عضو به پروژه اضافه شد.";
            }
            return RedirectToAction(nameof(Projects));
        }

        /// <summary>به‌روزرسانی وضعیت کلی پروژه (مثلاً: در مرحله تست)</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProjectStatus(int id, string statusText, CancellationToken ct)
        {
            var p = await _db.DevProjects.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
            if (p is null) { TempData["Error"] = "پروژه یافت نشد."; return RedirectToAction(nameof(Projects)); }
            p.StatusText = string.IsNullOrWhiteSpace(statusText) ? null : statusText.Trim();
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "وضعیت پروژه به‌روزرسانی شد.";
            return RedirectToAction(nameof(Projects));
        }

        // ============================================================ بار کاری تیم

        public async Task<IActionResult> Workload(CancellationToken ct)
        {
            var devs = await _db.Partners.AsNoTracking()
                .Where(p => !p.IsDeleted && p.IsActive && (p.Role == PartnerRole.Developer || p.Role == PartnerRole.TechManager))
                .Select(p => new
                {
                    p.Id,
                    p.FullName,
                    p.TechLevel,
                    p.Skills,
                    OpenTasks = p.Tasks!.Count(t => !t.IsDeleted && t.Status != TaskStatus.Done),
                    DoneTasks = p.Tasks!.Count(t => !t.IsDeleted && t.Status == TaskStatus.Done),
                    HighPriority = p.Tasks!.Count(t => !t.IsDeleted && t.Status != TaskStatus.Done && (t.Priority == TaskPriority.High || t.Priority == TaskPriority.Critical)),
                    WeekMinutes = p.TimeLogs!.Where(t => t.WorkDate >= DateTime.Now.AddDays(-7)).Sum(t => (int?)t.Minutes) ?? 0,
                })
                .OrderByDescending(p => p.OpenTasks)
                .ToListAsync(ct);
            return View(devs);
        }

        // ============================================================ تیکت‌های فنی

        public async Task<IActionResult> Tickets(CancellationToken ct)
        {
            var tickets = await _db.SupportTickets.AsNoTracking()
                .Include(t => t.Contract)
                .Include(t => t.Partner)
                .OrderBy(t => t.Status).ThenByDescending(t => t.CreatedAt)
                .ToListAsync(ct);
            return View(tickets);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AnswerTicket(int id, string response, TicketStatus status, CancellationToken ct)
        {
            var t = await _db.SupportTickets.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (t is null) { TempData["Error"] = "تیکت یافت نشد."; return RedirectToAction(nameof(Tickets)); }
            t.Response = string.IsNullOrWhiteSpace(response) ? t.Response : response.Trim();
            t.Status = status;
            if (status == TicketStatus.Answered || status == TicketStatus.Closed) t.AnsweredAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "پاسخ ثبت شد و برای همکار نمایش داده می‌شود.";
            return RedirectToAction(nameof(Tickets));
        }
    }
}

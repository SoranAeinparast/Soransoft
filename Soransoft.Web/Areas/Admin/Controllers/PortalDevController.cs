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

    /// <summary>فرم ویرایش کامل تسک</summary>
    public class DevTaskEditModel : DevTaskCreateModel
    {
        public int Id { get; set; }
        public TaskStatus Status { get; set; } = TaskStatus.ToDo;
        public int DisplayOrder { get; set; }
    }

    /// <summary>فرم مدیریت پروژه</summary>
    public class DevProjectFormModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "عنوان پروژه الزامی است")]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? StatusText { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>فرم مدیریت اسپرینت</summary>
    public class SprintFormModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "عنوان اسپرینت الزامی است")]
        public string Title { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Goal { get; set; }
        public bool IsActive { get; set; } = true;
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

            if (!await ValidateTaskReferencesAsync(model.ProjectId, model.SprintId, model.AssigneeId, ct))
                return RedirectToAction(nameof(Index));
            if (!Enum.IsDefined(model.Priority))
            {
                TempData["Error"] = "اولویت تسک معتبر نیست.";
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
        public async Task<IActionResult> EditTask(DevTaskEditModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Index));
            }

            var task = await _db.DevTasks.FirstOrDefaultAsync(t => t.Id == model.Id && !t.IsDeleted, ct);
            if (task is null) { TempData["Error"] = "تسک یافت نشد."; return RedirectToAction(nameof(Index)); }
            if (!await ValidateTaskReferencesAsync(model.ProjectId, model.SprintId, model.AssigneeId, ct))
                return RedirectToAction(nameof(Index));
            if (!Enum.IsDefined(model.Priority) || !Enum.IsDefined(model.Status))
            {
                TempData["Error"] = "اولویت یا وضعیت تسک معتبر نیست.";
                return RedirectToAction(nameof(Index));
            }

            task.Title = model.Title.Trim();
            task.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            task.ProjectId = model.ProjectId;
            task.SprintId = model.SprintId;
            task.PartnerId = model.AssigneeId;
            task.Priority = model.Priority;
            task.Status = model.Status;
            task.DueDate = model.DueDate;
            task.DisplayOrder = model.DisplayOrder;
            task.CompletedAt = model.Status == TaskStatus.Done ? task.CompletedAt ?? DateTime.Now : null;

            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "تسک به‌روزرسانی شد.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(int id, int? assigneeId, CancellationToken ct)
        {
            var t = await _db.DevTasks.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
            if (t is null) { TempData["Error"] = "تسک یافت نشد."; return RedirectToAction(nameof(Index)); }
            if (!await ValidateAssigneeAsync(assigneeId, ct)) return RedirectToAction(nameof(Index));
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
            if (!Enum.IsDefined(status)) { TempData["Error"] = "وضعیت تسک معتبر نیست."; return RedirectToAction(nameof(Index)); }
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
        public async Task<IActionResult> EditProject(DevProjectFormModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Projects));
            }

            var project = await _db.DevProjects.FirstOrDefaultAsync(p => p.Id == model.Id && !p.IsDeleted, ct);
            if (project is null) { TempData["Error"] = "پروژه یافت نشد."; return RedirectToAction(nameof(Projects)); }

            project.Title = model.Title.Trim();
            project.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            project.StatusText = string.IsNullOrWhiteSpace(model.StatusText) ? null : model.StatusText.Trim();
            project.IsActive = model.IsActive;
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "پروژه به‌روزرسانی شد.";
            return RedirectToAction(nameof(Projects));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProject(int id, CancellationToken ct)
        {
            var project = await _db.DevProjects.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
            if (project is null) { TempData["Error"] = "پروژه یافت نشد."; return RedirectToAction(nameof(Projects)); }
            project.IsDeleted = true;
            project.IsActive = false;
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "پروژه حذف شد؛ سابقه تسک‌ها حفظ شد.";
            return RedirectToAction(nameof(Projects));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(int projectId, int partnerId, string? roleInProject, CancellationToken ct)
        {
            var project = await _db.DevProjects.FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted, ct);
            if (project is null) { TempData["Error"] = "پروژه یافت نشد."; return RedirectToAction(nameof(Projects)); }
            if (!await ValidateAssigneeAsync(partnerId, ct)) return RedirectToAction(nameof(Projects));
            var exists = await _db.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.PartnerId == partnerId, ct);
            if (!exists)
            {
                _db.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, PartnerId = partnerId, RoleInProject = roleInProject?.Trim() });
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = "عضو به پروژه اضافه شد.";
            }
            return RedirectToAction(nameof(Projects));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMember(int id, string? roleInProject, CancellationToken ct)
        {
            var member = await _db.ProjectMembers.FirstOrDefaultAsync(m => m.Id == id, ct);
            if (member is null) { TempData["Error"] = "عضویت پروژه یافت نشد."; return RedirectToAction(nameof(Projects)); }
            member.RoleInProject = string.IsNullOrWhiteSpace(roleInProject) ? null : roleInProject.Trim();
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "نقش عضو پروژه به‌روزرسانی شد.";
            return RedirectToAction(nameof(Projects));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMember(int id, CancellationToken ct)
        {
            var member = await _db.ProjectMembers.FirstOrDefaultAsync(m => m.Id == id, ct);
            if (member is null) { TempData["Error"] = "عضویت پروژه یافت نشد."; return RedirectToAction(nameof(Projects)); }
            _db.ProjectMembers.Remove(member);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "عضو از پروژه حذف شد.";
            return RedirectToAction(nameof(Projects));
        }

        public async Task<IActionResult> Sprints(CancellationToken ct)
        {
            var sprints = await _db.Sprints.AsNoTracking()
                .Where(s => !s.IsDeleted)
                .OrderByDescending(s => s.IsActive).ThenByDescending(s => s.StartDate).ThenBy(s => s.Title)
                .ToListAsync(ct);
            ViewBag.TaskCounts = await _db.DevTasks.AsNoTracking()
                .Where(t => !t.IsDeleted && t.SprintId != null)
                .GroupBy(t => t.SprintId!.Value)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count, ct);
            return View(sprints);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSprint(SprintFormModel model, CancellationToken ct)
        {
            if (!ValidateSprint(model)) return RedirectToAction(nameof(Sprints));
            _db.Sprints.Add(new Sprint
            {
                Title = model.Title.Trim(),
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Goal = string.IsNullOrWhiteSpace(model.Goal) ? null : model.Goal.Trim(),
                IsActive = model.IsActive,
            });
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "اسپرینت ساخته شد.";
            return RedirectToAction(nameof(Sprints));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSprint(SprintFormModel model, CancellationToken ct)
        {
            if (!ValidateSprint(model)) return RedirectToAction(nameof(Sprints));
            var sprint = await _db.Sprints.FirstOrDefaultAsync(s => s.Id == model.Id && !s.IsDeleted, ct);
            if (sprint is null) { TempData["Error"] = "اسپرینت یافت نشد."; return RedirectToAction(nameof(Sprints)); }
            sprint.Title = model.Title.Trim();
            sprint.StartDate = model.StartDate;
            sprint.EndDate = model.EndDate;
            sprint.Goal = string.IsNullOrWhiteSpace(model.Goal) ? null : model.Goal.Trim();
            sprint.IsActive = model.IsActive;
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "اسپرینت به‌روزرسانی شد.";
            return RedirectToAction(nameof(Sprints));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSprint(int id, CancellationToken ct)
        {
            var sprint = await _db.Sprints.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, ct);
            if (sprint is null) { TempData["Error"] = "اسپرینت یافت نشد."; return RedirectToAction(nameof(Sprints)); }
            sprint.IsDeleted = true;
            sprint.IsActive = false;
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "اسپرینت حذف شد؛ تسک‌های قبلی حفظ شدند.";
            return RedirectToAction(nameof(Sprints));
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

        private async Task<bool> ValidateTaskReferencesAsync(int? projectId, int? sprintId, int? assigneeId, CancellationToken ct)
        {
            if (projectId is int project && !await _db.DevProjects.AnyAsync(p => p.Id == project && !p.IsDeleted, ct))
            {
                TempData["Error"] = "پروژه انتخاب‌شده معتبر نیست.";
                return false;
            }
            if (sprintId is int sprint && !await _db.Sprints.AnyAsync(s => s.Id == sprint && !s.IsDeleted && s.IsActive, ct))
            {
                TempData["Error"] = "اسپرینت انتخاب‌شده معتبر نیست.";
                return false;
            }
            return await ValidateAssigneeAsync(assigneeId, ct);
        }

        private async Task<bool> ValidateAssigneeAsync(int? assigneeId, CancellationToken ct)
        {
            if (assigneeId is null) return true;
            if (!await _db.Partners.AnyAsync(p => p.Id == assigneeId && !p.IsDeleted && p.IsActive
                && (p.Role == PartnerRole.Developer || p.Role == PartnerRole.TechManager), ct))
            {
                TempData["Error"] = "توسعه‌دهنده انتخاب‌شده معتبر نیست.";
                return false;
            }
            return true;
        }

        private bool ValidateSprint(SprintFormModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return false;
            }
            if (model.StartDate is DateTime start && model.EndDate is DateTime end && start > end)
            {
                TempData["Error"] = "تاریخ پایان اسپرینت نمی‌تواند قبل از تاریخ شروع باشد.";
                return false;
            }
            return true;
        }
    }
}

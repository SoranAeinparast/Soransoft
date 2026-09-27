using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Infrastructure.Persistence;

namespace Soransoft.Web.Areas.Partner.Controllers
{
    /// <summary>داشبورد پرتال همکار — بسته به نقش، آمار فروش یا فنی نمایش می‌دهد</summary>
    public class DashboardController : PartnerBaseController
    {
        private readonly SoransoftDbContext _db;

        public DashboardController(SoransoftDbContext db) => _db = db;

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var pid = PartnerId;

            // اطلاعیه‌های مرتبط با این همکار (همگانی + اختصاصی + مخاطب نقش)
            var audienceKey = IsSalesSide ? "Sales" : "Dev";
            var announcements = await _db.Announcements.AsNoTracking()
                .Where(a => !a.IsDeleted && (
                    a.PartnerId == pid ||
                    (a.PartnerId == null && (a.Audience == "All" || a.Audience == audienceKey))))
                .OrderByDescending(a => a.IsImportant).ThenByDescending(a => a.CreatedAt)
                .Take(5)
                .Select(a => new { a.Id, a.Title, a.Body, a.IsImportant, a.CreatedAt })
                .ToListAsync(ct);
            ViewBag.Announcements = announcements;

            if (IsSalesSide)
            {
                var visibleLeads = CanSeeAllSales
                    ? _db.Leads.AsNoTracking().Where(l => !l.IsDeleted)
                    : _db.Leads.AsNoTracking().Where(l => !l.IsDeleted && l.PartnerId == pid);

                var visibleContracts = CanSeeAllSales
                    ? _db.PartnerContracts.AsNoTracking().Where(c => !c.IsDeleted)
                    : _db.PartnerContracts.AsNoTracking().Where(c => !c.IsDeleted && c.PartnerId == pid);

                ViewBag.LeadCount = await visibleLeads.CountAsync(ct);
                ViewBag.OpenLeadCount = await visibleLeads.CountAsync(l => l.Stage != Soransoft.Domain.Entities.LeadStage.Contracted && l.Stage != Soransoft.Domain.Entities.LeadStage.Lost, ct);
                ViewBag.ContractCount = await visibleContracts.CountAsync(ct);
                ViewBag.PendingContractCount = await visibleContracts.CountAsync(c => c.Status == Soransoft.Domain.Entities.ContractStatus.PendingApproval, ct);

                var contractIds = await visibleContracts.Select(c => c.Id).ToListAsync(ct);
                ViewBag.PaidAmount = await _db.ContractPaymentStages
                    .Where(i => contractIds.Contains(i.ContractId) && i.Status == Soransoft.Domain.Entities.PaymentStageStatus.Paid)
                    .SumAsync(i => (long?)i.Amount, ct) ?? 0;
                ViewBag.TotalAmount = await _db.PartnerContracts
                    .Where(c => contractIds.Contains(c.Id))
                    .SumAsync(c => (long?)c.TotalAmount, ct) ?? 0;
                ViewBag.TicketCount = await _db.SupportTickets.AsNoTracking()
                    .Where(t => t.PartnerId == pid && t.Status != Soransoft.Domain.Entities.TicketStatus.Closed)
                    .CountAsync(ct);
            }
            else
            {
                ViewBag.MyTaskCount = await _db.DevTasks.AsNoTracking()
                    .Where(t => !t.IsDeleted && t.PartnerId == pid && t.Status != Soransoft.Domain.Entities.TaskStatus.Done)
                    .CountAsync(ct);
                ViewBag.DoneTaskCount = await _db.DevTasks.AsNoTracking()
                    .Where(t => !t.IsDeleted && t.PartnerId == pid && t.Status == Soransoft.Domain.Entities.TaskStatus.Done)
                    .CountAsync(ct);
                var weekAgo = DateTime.Now.AddDays(-7);
                ViewBag.WeekMinutes = await _db.TimeLogs.AsNoTracking()
                    .Where(t => t.PartnerId == pid && t.WorkDate >= weekAgo)
                    .SumAsync(t => (int?)t.Minutes, ct) ?? 0;
                ViewBag.ProjectCount = await _db.ProjectMembers.AsNoTracking()
                    .Where(m => m.PartnerId == pid)
                    .Select(m => m.ProjectId).Distinct().CountAsync(ct);
            }

            ViewData["Title"] = "داشبورد";
            return View();
        }
    }
}

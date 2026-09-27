using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using Soransoft.Domain.Enums;
using Soransoft.Infrastructure.Persistence;
using Soransoft.Web.Models.Admin;
using EntityTaskStatus = Soransoft.Domain.Entities.TaskStatus;

namespace Soransoft.Web.Areas.Admin.Controllers
{            public class DashboardController : AdminBaseController
    {
        private const int ChartDays = 30;
        private readonly SoransoftDbContext _db;
        private readonly IMediaLibraryService _media;
        public DashboardController(SoransoftDbContext db, IMediaLibraryService media) { _db = db; _media = media; }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var since = DateTime.Today.AddDays(-(ChartDays - 1));

            var model = new Soransoft.Web.Models.Admin.DashboardViewModel
            {
                TotalOrders = await _db.ProjectOrders.CountAsync(ct),
                NewOrders = await _db.ProjectOrders.CountAsync(o => o.Status == ProjectOrderStatus.New, ct),
                UnreadMessages = await _db.ContactMessages.CountAsync(m => m.Status == MessageStatus.Unread, ct),
                UnreadConsultations = await _db.ConsultationRequests.CountAsync(c => c.Status == MessageStatus.Unread, ct),
                TotalArticles = await _db.Articles.CountAsync(ct),
                TotalPortfolios = await _db.Portfolios.CountAsync(ct),
                TotalServices = await _db.Services.CountAsync(ct),
                TotalSliders = await _db.Sliders.CountAsync(ct),
                TotalTariffPackages = await _db.TariffPackages.CountAsync(ct),
                TotalTeamMembers = await _db.TeamMembers.CountAsync(ct),
                TotalSiteUsers = await _db.SiteUsers.CountAsync(ct),
                TotalArticleVisits = await _db.Articles.SumAsync(a => (int?)a.VisitCount, ct) ?? 0,
                RecentErrors = await _db.ErrorLogs.CountAsync(e => e.CreatedAt >= DateTime.Today, ct),
                LatestOrders = await _db.ProjectOrders.OrderByDescending(o => o.CreatedAt).Take(5).ToListAsync(ct),
                LatestMessages = await _db.ContactMessages.OrderByDescending(m => m.CreatedAt).Take(5).ToListAsync(ct),
            };

            // ---------- داده‌های سری زمانی ۳۰ روزه ----------
            var days = Enumerable.Range(0, ChartDays).Select(i => DateTime.Today.AddDays(i - (ChartDays - 1))).ToList();

            var ordersByDay = await _db.ProjectOrders
                .Where(o => o.CreatedAt >= since)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var messagesByDay = await _db.ContactMessages
                .Where(m => m.CreatedAt >= since)
                .GroupBy(m => m.CreatedAt.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var consultationsByDay = await _db.ConsultationRequests
                .Where(c => c.CreatedAt >= since)
                .GroupBy(c => c.CreatedAt.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var errorsByDay = await _db.ErrorLogs
                .Where(e => e.CreatedAt >= since)
                .GroupBy(e => e.CreatedAt.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            // بازدید مقاله‌ها: سهم روزانه تخمینی بر اساس آخرین بازدید/انتشار
            var articles = await _db.Articles
                .Where(a => a.VisitCount > 0)
                .Select(a => new { a.Id, a.VisitCount, a.PublishedAt })
                .ToListAsync(ct);

            foreach (var day in days)
            {
                model.Last30DaysLabels.Add(day.ToString("MM/dd"));
                model.OrdersPerDay.Add(ordersByDay.FirstOrDefault(x => x.Day == day)?.Count ?? 0);
                model.MessagesPerDay.Add(messagesByDay.FirstOrDefault(x => x.Day == day)?.Count ?? 0);
                model.ConsultationsPerDay.Add(consultationsByDay.FirstOrDefault(x => x.Day == day)?.Count ?? 0);
                model.ErrorsPerDay.Add(errorsByDay.FirstOrDefault(x => x.Day == day)?.Count ?? 0);

                // توزیع بازدید مقاله روی ۳۰ روز اخیر (فقط روزهای بعد از انتشار)
                int visits = 0;
                foreach (var a in articles)
                {
                    var effectiveStart = a.PublishedAt > since ? a.PublishedAt : since;
                    if (day >= effectiveStart.Date)
                    {
                        var spanDays = (DateTime.Today - effectiveStart.Date).Days + 1;
                        visits += spanDays > 0 ? a.VisitCount / spanDays : 0;
                    }
                }
                model.ArticleVisitsPerDay.Add(visits);
            }

            // ---------- توزیع نوع سفارش ----------
            var orderTypes = await _db.ProjectOrders
                .GroupBy(o => o.ProjectType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            foreach (var item in orderTypes.OrderBy(x => (int)x.Type))
            {
                model.OrderTypeLabels.Add(item.Type switch
                {
                    ServiceKind.Site => "طراحی سایت",
                    ServiceKind.Seo => "سئو",
                    ServiceKind.Application => "اپلیکیشن",
                    _ => "تولید محتوا",
                });
                model.OrderTypeCounts.Add(item.Count);
            }

            // تعداد فایل‌های کتابخانه رسانه (اسکن پوشه آپلود)
            var storage = await _media.GetStorageOverviewAsync(ct);
            model.TotalMediaFiles = storage.TotalFiles;

            // ---------- پرتال همکاران: فروش ماهانه (اقساط پرداخت‌شده ۱۲ ماه اخیر) ----------
            var yearAgo = DateTime.Today.AddMonths(-11);
            var salesRows = await _db.PartnerContracts.AsNoTracking()
                .Where(c => !c.IsDeleted && c.Status != Domain.Entities.ContractStatus.Cancelled)
                .SelectMany(c => c.PaymentStages.Select(i => new
                {
                    i.PaidAt,
                    i.Amount,
                    PartnerName = c.Partner != null ? c.Partner.FullName : null,
                }))
                .ToListAsync(ct);

            var paidByMonth = salesRows
                .Where(x => x.PaidAt != null && x.PaidAt >= new DateTime(yearAgo.Year, yearAgo.Month, 1))
                .GroupBy(x => new { x.PaidAt!.Value.Year, x.PaidAt!.Value.Month })
                .ToDictionary(
                    g => (Year: g.Key.Year, Month: g.Key.Month),
                    g => g.GroupBy(x => x.PartnerName ?? "بدون همکار")
                          .ToDictionary(pg => pg.Key, pg => pg.Sum(x => x.Amount)));

            var now = DateTime.Today;
            for (var m = 11; m >= 0; m--)
            {
                var month = now.AddMonths(-m);
                var key = (Year: month.Year, Month: month.Month);
                paidByMonth.TryGetValue(key, out var byPartner);
                byPartner ??= new Dictionary<string, long>();

                var pc = new System.Globalization.PersianCalendar();
                model.MonthlySales.Add(new MonthlySalesPoint
                {
                    Label = $"{pc.GetMonth(month)} / {pc.GetYear(month)}",
                    Total = byPartner.Values.Sum(),
                    ByPartner = byPartner,
                });
            }

            // ---------- پرتال همکاران: پیشرفت پروژه‌های فنی ----------
            var projects = await _db.DevProjects.AsNoTracking()
                .Where(p => !p.IsDeleted)
                .Select(p => new
                {
                    p.Title,
                    p.IsActive,
                    Counts = p.Tasks.Where(t => !t.IsDeleted)
                        .GroupBy(t => t.Status)
                        .Select(g => new { Status = g.Key, Count = g.Count() })
                        .ToList(),
                })
                .ToListAsync(ct);

            foreach (var p in projects.OrderByDescending(p => p.IsActive).ThenBy(p => p.Title))
            {
                int done = 0, testing = 0, inProg = 0, toDo = 0;
                foreach (var g in p.Counts)
                {
                    switch (g.Status)
                    {
                        case EntityTaskStatus.Done: done = g.Count; break;
                        case EntityTaskStatus.Testing: testing = g.Count; break;
                        case EntityTaskStatus.InProgress: inProg = g.Count; break;
                        default: toDo = g.Count; break;
                    }
                }

                var total = done + testing + inProg + toDo;
                model.ProjectProgress.Add(new ProjectProgressPoint
                {
                    Title = p.Title,
                    Total = total,
                    Done = done,
                    Testing = testing,
                    InProgress = inProg,
                    ToDo = toDo,
                    Percent = total == 0 ? 0 : (int)Math.Round((done + testing * 0.5) * 100.0 / total),
                });
            }

            return View(model);
        }
    }
}

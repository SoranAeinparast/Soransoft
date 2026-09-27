using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using Soransoft.Domain.Entities;
using Soransoft.Infrastructure.Persistence;
using System.ComponentModel.DataAnnotations;
using TaskStatus = Soransoft.Domain.Entities.TaskStatus;

namespace Soransoft.Web.Areas.Partner.Controllers
{
    /// <summary>فرم ثبت/ویرایش لید</summary>
    public class LeadInputModel
    {
        [Required(ErrorMessage = "نام مشتری الزامی است")]
        public string CustomerName { get; set; } = string.Empty;
        [Required(ErrorMessage = "شماره مشتری الزامی است")]
        [RegularExpression(@"^09\d{9}$", ErrorMessage = "شماره موبایل معتبر نیست")]
        public string CustomerMobile { get; set; } = string.Empty;
        [Required(ErrorMessage = "شرح نیاز مشتری الزامی است")]
        public string Requirement { get; set; } = string.Empty;
        public long? EstimatedAmount { get; set; }
    }

    /// <summary>فرم ثبت درخواست بررسی فنی</summary>
    public class TicketInputModel
    {
        public int ContractId { get; set; }
        [Required(ErrorMessage = "موضوع الزامی است")]
        public string Subject { get; set; } = string.Empty;
        [Required(ErrorMessage = "شرح درخواست الزامی است")]
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>ردیف گزارش پورسانت همکار (یک قرارداد)</summary>
    public class CommissionRow
    {
        public PartnerContract Contract { get; set; } = null!;
        public CommissionRate? Rate { get; set; }
        /// <summary>نرخ از قرارداد همکاری فی‌مابین آمده (نرخ مجزا روی قرارداد تعریف نشده)</summary>
        public bool RateFromAgreement { get; set; }
        public decimal Commission { get; set; }
        /// <summary>مجموع مراحل پرداخت‌شده</summary>
        public long Paid { get; set; }
        /// <summary>مجموع هزینه‌های شخص ثالث کسرشده</summary>
        public long ThirdParty { get; set; }
        /// <summary>مبلغ کل قرارداد</summary>
        public long Total { get; set; }
    }

    /// <summary>پنل فروش همکار — فقط داده‌های خودش (مگر سطح ارشد داشته باشد)</summary>
    public class SalesController : PartnerBaseController
    {
        private readonly SoransoftDbContext _db;
        private readonly IPartnerPortalService _portal;

        public SalesController(SoransoftDbContext db, IPartnerPortalService portal)
        {
            _db = db;
            _portal = portal;
        }

        /// <summary>لیدهای قابل مشاهده برای این همکار</summary>
        private IQueryable<Lead> VisibleLeads => CanSeeAllSales
            ? _db.Leads.Where(l => !l.IsDeleted)
            : _db.Leads.Where(l => !l.IsDeleted && l.PartnerId == PartnerId);

        private IQueryable<PartnerContract> VisibleContracts => CanSeeAllSales
            ? _db.PartnerContracts.Where(c => !c.IsDeleted)
            : _db.PartnerContracts.Where(c => !c.IsDeleted && c.PartnerId == PartnerId);

        private IActionResult NotSales() => IsSalesSide ? null! : Forbidden()!;

        // ============================================================ لیدها

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            if (!IsSalesSide) return Forbidden();
            var leads = await VisibleLeads
                .Include(l => l.Partner)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync(ct);
            return View(leads);
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (!IsSalesSide) return Forbidden();
            return View(new LeadInputModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeadInputModel model, CancellationToken ct)
        {
            if (!IsSalesSide) return Forbidden();
            if (!ModelState.IsValid) return View(model);

            var lead = new Lead
            {
                CustomerName = model.CustomerName.Trim(),
                CustomerMobile = model.CustomerMobile.Trim(),
                Requirement = model.Requirement.Trim(),
                EstimatedAmount = model.EstimatedAmount,
                PartnerId = PartnerId,
                Stage = LeadStage.New,
            };
            _db.Leads.Add(lead);
            await _db.SaveChangesAsync(ct);

            TempData["Success"] = "لید ثبت شد؛ پس از تایید مدیر به قرارداد تبدیل می‌شود.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>تغییر وضعیت لید توسط خود فروشنده (مثلاً منصرف شدن مشتری)</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStage(int id, LeadStage stage, string? note, CancellationToken ct)
        {
            if (!IsSalesSide) return Forbidden();

            var lead = await VisibleLeads.FirstOrDefaultAsync(l => l.Id == id, ct);
            if (lead is null) { TempData["Error"] = "لید یافت نشد."; return RedirectToAction(nameof(Index)); }

            var result = await _portal.UpdateLeadStageAsync(id, stage, note, ct);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // ============================================================ قراردادها

        public async Task<IActionResult> Contracts(CancellationToken ct)
        {
            if (!IsSalesSide) return Forbidden();
            var contracts = await VisibleContracts
                .Include(c => c.Partner)
                .Include(c => c.PaymentStages)
                .Include(c => c.ThirdPartyCosts)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync(ct);

            // قرارداد همکاری فی‌مابین خود همکار (جدا از قراردادهای مشتریان) تا در همین صفحه هم دیده شود
            ViewBag.MyAgreements = await _db.CooperationAgreements.AsNoTracking()
                .Where(a => a.PartnerId == PartnerId)
                .OrderBy(a => a.Status == AgreementStatus.Active ? 0 : 1)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync(ct);

            return View(contracts);
        }

        /// <summary>آپلود فایل PDF قرارداد توسط فروشنده</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadContract(int id, IFormFile? file, [FromServices] IPartnerDocumentService docs, CancellationToken ct)
        {
            if (!IsSalesSide) return Forbidden();

            var contract = await VisibleContracts.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (contract is null) { TempData["Error"] = "قرارداد یافت نشد یا به شما تعلق ندارد."; return RedirectToAction(nameof(Contracts)); }

            var result = await docs.UploadContractFileAsync(id, file, ct);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Contracts));
        }

        // ============================================================ کمیسیون

        public async Task<IActionResult> Commissions(CancellationToken ct)
        {
            if (!IsSalesSide) return Forbidden();

            var contracts = await VisibleContracts
                .Include(c => c.Partner)
                .Include(c => c.PaymentStages)
                .Include(c => c.ThirdPartyCosts)
                .Include(c => c.CommissionRates)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync(ct);

            // قرارداد همکاری فعال همکار فروش — نرخ پیش‌فرض در صورت نبود نرخ مجزا روی قرارداد
            var agreement = await _db.CooperationAgreements.AsNoTracking()
                .Where(a => a.PartnerId == PartnerId && a.Kind == AgreementKind.Sales && a.Status == AgreementStatus.Active)
                .OrderByDescending(a => a.SignedAt)
                .FirstOrDefaultAsync(ct);

            var rows = new List<CommissionRow>();
            foreach (var c in contracts)
            {
                var rate = c.CommissionRates.FirstOrDefault(r => r.PartnerId == PartnerId && r.IsActive)
                           ?? c.CommissionRates.FirstOrDefault(r => r.PartnerId == PartnerId);
                var fromAgreement = false;
                if (rate is null && agreement is not null)
                {
                    // نرخ توافق‌شده در قرارداد فی‌مابین به‌عنوان پیش‌فرض اعمال می‌شود
                    rate = new CommissionRate
                    {
                        ContractId = c.Id,
                        PartnerId = PartnerId,
                        Method = agreement.CommissionMethod,
                        Value = agreement.CommissionValue,
                        TiersJson = agreement.TiersJson,
                        BasedOnPaidStages = agreement.BasedOnPaidStages,
                        IsActive = true,
                    };
                    fromAgreement = true;
                }

                var paid = c.PaymentStages.Where(i => i.Status == PaymentStageStatus.Paid).Sum(i => (long)i.Amount);
                var thirdParty = c.ThirdPartyCosts.Where(x => x.DeductFromFirstPayment).Sum(x => x.Amount);
                var commission = rate is null ? 0m : await _portal.ComputeCommissionAsync(rate, ct);
                rows.Add(new CommissionRow
                {
                    Contract = c,
                    Rate = rate,
                    RateFromAgreement = fromAgreement,
                    Commission = commission,
                    Paid = paid,
                    ThirdParty = thirdParty,
                    Total = c.TotalAmount,
                });
            }

            ViewBag.Rows = rows;
            ViewBag.HasActiveAgreement = agreement is not null;
            return View();
        }

        // ============================================================ تیکت فنی

        public async Task<IActionResult> Tickets(CancellationToken ct)
        {
            if (!IsSalesSide) return Forbidden();
            var tickets = await _db.SupportTickets.AsNoTracking()
                .Include(t => t.Contract)
                .Where(t => t.PartnerId == PartnerId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(ct);
            var myContracts = await VisibleContracts.Select(c => new { c.Id, c.Title }).ToListAsync(ct);
            ViewBag.MyContracts = myContracts;
            return View(tickets);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTicket(TicketInputModel model, CancellationToken ct)
        {
            if (!IsSalesSide) return Forbidden();
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Tickets));
            }

            var contract = await VisibleContracts.FirstOrDefaultAsync(c => c.Id == model.ContractId, ct);
            if (contract is null) { TempData["Error"] = "قرارداد یافت نشد یا به شما تعلق ندارد."; return RedirectToAction(nameof(Tickets)); }

            _db.SupportTickets.Add(new SupportTicket
            {
                ContractId = contract.Id,
                PartnerId = PartnerId,
                Subject = model.Subject.Trim(),
                Description = model.Description.Trim(),
                Status = TicketStatus.New,
            });
            await _db.SaveChangesAsync(ct);

            TempData["Success"] = "درخواست بررسی فنی ثبت شد؛ تیم فنی پیگیری می‌کند.";
            return RedirectToAction(nameof(Tickets));
        }
    }
}

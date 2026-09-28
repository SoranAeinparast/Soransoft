using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using Soransoft.Domain.Entities;
using Soransoft.Infrastructure.Persistence;
using System.ComponentModel.DataAnnotations;
using TaskStatus = Soransoft.Domain.Entities.TaskStatus;

namespace Soransoft.Web.Areas.Partner.Controllers
{
    /// <summary>فرم رسمی معرفی مشتری و ثبت سرنخ فروش</summary>
    public class LeadInputModel
    {
        [Required(ErrorMessage = "کد ملی همکار فروش الزامی است")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "کد ملی باید ۱۰ رقم باشد")]
        public string SalesPartnerNationalId { get; set; } = string.Empty;
        public DateTime? IntroducedAt { get; set; } = DateTime.Now;
        public bool IsFollowUp { get; set; }

        [Required(ErrorMessage = "نام مشتری الزامی است")]
        public string CustomerName { get; set; } = string.Empty;
        [Required(ErrorMessage = "شماره مشتری الزامی است")]
        [RegularExpression(@"^09\d{9}$", ErrorMessage = "شماره موبایل معتبر نیست")]
        public string CustomerMobile { get; set; } = string.Empty;
        public string? BusinessName { get; set; }
        public string? DecisionMakerName { get; set; }
        public string? DecisionMakerRole { get; set; }
        public string? CustomerLandline { get; set; }
        [EmailAddress(ErrorMessage = "ایمیل مشتری معتبر نیست")]
        public string? CustomerEmail { get; set; }
        public string? CurrentWebsite { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? FullAddress { get; set; }
        public string? SocialMedia { get; set; }

        public bool? HasWebsite { get; set; }
        public string? BusinessType { get; set; }
        public string? CurrentSystem { get; set; }
        public bool? HasSimilarPlatform { get; set; }
        public DateTime? ExpectedStartDate { get; set; }
        public long? BudgetAmount { get; set; }

        [Required(ErrorMessage = "شرح نیاز مشتری الزامی است")]
        public string Requirement { get; set; } = string.Empty;
        public string? SecondaryRequirements { get; set; }
        public long? EstimatedAmount { get; set; }
        public string? IntroductionMethod { get; set; }
        public DateTime? FirstContactDate { get; set; }
        public bool? DecisionMakerConfirmed { get; set; }
        public LeadNegotiationLevel? NegotiationLevel { get; set; }
        public bool? NearContract { get; set; }
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
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            if (!IsSalesSide) return Forbidden();
            var agreement = await _db.CooperationAgreements.AsNoTracking()
                .Where(a => a.PartnerId == PartnerId && a.Kind == AgreementKind.Sales && a.Status == AgreementStatus.Active)
                .OrderByDescending(a => a.SignedAt)
                .FirstOrDefaultAsync(ct);
            ViewBag.ActiveAgreement = agreement;
            var partnerNationalId = await _db.Partners.AsNoTracking()
                .Where(p => p.Id == PartnerId)
                .Select(p => p.NationalId)
                .FirstOrDefaultAsync(ct);
            return View(new LeadInputModel { SalesPartnerNationalId = partnerNationalId ?? string.Empty });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            if (!IsSalesSide) return Forbidden();

            var lead = await VisibleLeads.FirstOrDefaultAsync(l => l.Id == id, ct);
            if (lead is null) return NotFound();

            await LoadActiveAgreementAsync(ct);
            ViewData["IsEdit"] = true;
            ViewData["EditId"] = id;
            return View("Create", ToInputModel(lead));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeadInputModel model, CancellationToken ct)
        {
            if (!IsSalesSide) return Forbidden();
            var agreement = await _db.CooperationAgreements.AsNoTracking()
                .Where(a => a.PartnerId == PartnerId && a.Kind == AgreementKind.Sales && a.Status == AgreementStatus.Active)
                .OrderByDescending(a => a.SignedAt)
                .FirstOrDefaultAsync(ct);
            ViewBag.ActiveAgreement = agreement;
            if (!ModelState.IsValid) return View(model);

            var lead = new Lead
            {
                FormNo = $"LD-{DateTime.Now:yyyyMMddHHmmssfff}",
                IntroducedAt = model.IntroducedAt ?? DateTime.Now,
                IsFollowUp = model.IsFollowUp,
                SalesPartnerNationalId = model.SalesPartnerNationalId.Trim(),
                SalesPartnerAgreementNo = agreement?.AgreementNo,
                SalesPartnerBankAccount = agreement is null ? null : string.Join(" — ", new[] { agreement.BankName, agreement.BankAccountIban, agreement.BankAccountHolder }.Where(x => !string.IsNullOrWhiteSpace(x))),
                CustomerName = model.CustomerName.Trim(),
                CustomerMobile = model.CustomerMobile.Trim(),
                BusinessName = Clean(model.BusinessName),
                DecisionMakerName = Clean(model.DecisionMakerName),
                DecisionMakerRole = Clean(model.DecisionMakerRole),
                CustomerLandline = Clean(model.CustomerLandline),
                CustomerEmail = Clean(model.CustomerEmail),
                CurrentWebsite = model.HasWebsite == true ? Clean(model.CurrentWebsite) : null,
                Province = Clean(model.Province),
                City = Clean(model.City),
                FullAddress = Clean(model.FullAddress),
                SocialMedia = Clean(model.SocialMedia),
                HasWebsite = model.HasWebsite,
                BusinessType = Clean(model.BusinessType),
                CurrentSystem = Clean(model.CurrentSystem),
                HasSimilarPlatform = model.HasSimilarPlatform,
                ExpectedStartDate = model.ExpectedStartDate,
                BudgetAmount = model.BudgetAmount,
                Requirement = model.Requirement.Trim(),
                SecondaryRequirements = Clean(model.SecondaryRequirements),
                EstimatedAmount = model.EstimatedAmount,
                IntroductionMethod = Clean(model.IntroductionMethod),
                FirstContactDate = model.FirstContactDate,
                DecisionMakerConfirmed = model.DecisionMakerConfirmed,
                NegotiationLevel = model.NegotiationLevel,
                NearContract = model.NearContract,
                PartnerId = PartnerId,
                Stage = LeadStage.New,
                ReviewStatus = LeadReviewStatus.Pending,
            };
            var result = await _portal.CreateLeadAsync(PartnerId, lead, ct);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return View(model);
            }

            TempData["Success"] = "لید ثبت شد؛ پس از تایید مدیر به قرارداد تبدیل می‌شود.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LeadInputModel model, CancellationToken ct)
        {
            if (!IsSalesSide) return Forbidden();

            var lead = await VisibleLeads.FirstOrDefaultAsync(l => l.Id == id, ct);
            if (lead is null) return NotFound();

            await LoadActiveAgreementAsync(ct);
            ViewData["IsEdit"] = true;
            ViewData["EditId"] = id;
            if (!ModelState.IsValid) return View("Create", model);

            lead.IntroducedAt = model.IntroducedAt ?? lead.IntroducedAt;
            lead.IsFollowUp = model.IsFollowUp;
            lead.SalesPartnerNationalId = model.SalesPartnerNationalId.Trim();
            lead.CustomerName = model.CustomerName.Trim();
            lead.CustomerMobile = model.CustomerMobile.Trim();
            lead.BusinessName = Clean(model.BusinessName);
            lead.DecisionMakerName = Clean(model.DecisionMakerName);
            lead.DecisionMakerRole = Clean(model.DecisionMakerRole);
            lead.CustomerLandline = Clean(model.CustomerLandline);
            lead.CustomerEmail = Clean(model.CustomerEmail);
            lead.CurrentWebsite = model.HasWebsite == true ? Clean(model.CurrentWebsite) : null;
            lead.HasWebsite = model.HasWebsite;
            lead.Province = Clean(model.Province);
            lead.City = Clean(model.City);
            lead.FullAddress = Clean(model.FullAddress);
            lead.SocialMedia = Clean(model.SocialMedia);
            lead.BusinessType = Clean(model.BusinessType);
            lead.CurrentSystem = Clean(model.CurrentSystem);
            lead.HasSimilarPlatform = model.HasSimilarPlatform;
            lead.ExpectedStartDate = model.ExpectedStartDate;
            lead.BudgetAmount = model.BudgetAmount;
            lead.Requirement = model.Requirement.Trim();
            lead.SecondaryRequirements = Clean(model.SecondaryRequirements);
            lead.EstimatedAmount = model.EstimatedAmount;
            lead.IntroductionMethod = Clean(model.IntroductionMethod);
            lead.FirstContactDate = model.FirstContactDate;
            lead.DecisionMakerConfirmed = model.DecisionMakerConfirmed;
            lead.NegotiationLevel = model.NegotiationLevel;
            lead.NearContract = model.NearContract;

            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "اطلاعات لید با موفقیت ویرایش شد.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Print(int id, CancellationToken ct)
        {
            if (!IsSalesSide) return Forbidden();

            var lead = await VisibleLeads
                .Include(l => l.Partner)
                .FirstOrDefaultAsync(l => l.Id == id, ct);
            if (lead is null) return NotFound();

            return View(lead);
        }

        private async Task LoadActiveAgreementAsync(CancellationToken ct)
        {
            ViewBag.ActiveAgreement = await _db.CooperationAgreements.AsNoTracking()
                .Where(a => a.PartnerId == PartnerId && a.Kind == AgreementKind.Sales && a.Status == AgreementStatus.Active)
                .OrderByDescending(a => a.SignedAt)
                .FirstOrDefaultAsync(ct);
        }

        private static LeadInputModel ToInputModel(Lead lead) => new()
        {
            SalesPartnerNationalId = lead.SalesPartnerNationalId ?? string.Empty,
            IntroducedAt = lead.IntroducedAt,
            IsFollowUp = lead.IsFollowUp,
            CustomerName = lead.CustomerName,
            CustomerMobile = lead.CustomerMobile,
            BusinessName = lead.BusinessName,
            DecisionMakerName = lead.DecisionMakerName,
            DecisionMakerRole = lead.DecisionMakerRole,
            CustomerLandline = lead.CustomerLandline,
            CustomerEmail = lead.CustomerEmail,
            CurrentWebsite = lead.CurrentWebsite,
            Province = lead.Province,
            City = lead.City,
            FullAddress = lead.FullAddress,
            SocialMedia = lead.SocialMedia,
            HasWebsite = lead.HasWebsite ?? !string.IsNullOrWhiteSpace(lead.CurrentWebsite),
            BusinessType = lead.BusinessType,
            CurrentSystem = lead.CurrentSystem,
            HasSimilarPlatform = lead.HasSimilarPlatform,
            ExpectedStartDate = lead.ExpectedStartDate,
            BudgetAmount = lead.BudgetAmount,
            Requirement = lead.Requirement,
            SecondaryRequirements = lead.SecondaryRequirements,
            EstimatedAmount = lead.EstimatedAmount,
            IntroductionMethod = lead.IntroductionMethod,
            FirstContactDate = lead.FirstContactDate,
            DecisionMakerConfirmed = lead.DecisionMakerConfirmed,
            NegotiationLevel = lead.NegotiationLevel,
            NearContract = lead.NearContract,
        };

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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

        // ============================================================ پروژه‌های قابل ارائه به مشتری

        public async Task<IActionResult> Catalog(CancellationToken ct)
        {
            if (!IsSalesSide) return Forbidden();
            if (!await HasActiveSalesAgreementAsync(ct))
            {
                ViewBag.HasActiveAgreement = false;
                return View(new List<SellableProject>());
            }

            var projects = await VisibleSellableProjects()
                .Include(p => p.Documents.Where(d => !d.IsDeleted && d.IsActive).OrderBy(d => d.DisplayOrder).ThenBy(d => d.Title))
                .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Title)
                .ToListAsync(ct);
            ViewBag.HasActiveAgreement = true;
            return View(projects);
        }

        public async Task<IActionResult> Project(int id, CancellationToken ct)
        {
            if (!IsSalesSide) return Forbidden();
            if (!await HasActiveSalesAgreementAsync(ct)) return Forbid();

            var project = await VisibleSellableProjects()
                .Include(p => p.Documents.Where(d => !d.IsDeleted && d.IsActive).OrderBy(d => d.DisplayOrder).ThenBy(d => d.Title))
                .Include(p => p.Comments.Where(c => !c.IsDeleted).OrderByDescending(c => c.CreatedAt))
                .FirstOrDefaultAsync(p => p.Id == id, ct);
            if (project is null) return NotFound();
            return View(project);
        }

        private IQueryable<SellableProject> VisibleSellableProjects()
        {
            var query = _db.SellableProjects
                .Where(p => p.IsActive && !p.IsDeleted);
            return CanSeeAllSales
                ? query
                : query.Where(p => p.PartnerAccess.Any(a => a.PartnerId == PartnerId && a.IsActive));
        }

        private Task<bool> HasActiveSalesAgreementAsync(CancellationToken ct)
        {
            var now = DateTime.Now;
            return _db.CooperationAgreements.AsNoTracking().AnyAsync(a =>
                a.PartnerId == PartnerId &&
                a.Kind == AgreementKind.Sales &&
                a.Status == AgreementStatus.Active &&
                a.StartDate <= now &&
                (!a.EndDate.HasValue || a.EndDate.Value >= now), ct);
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

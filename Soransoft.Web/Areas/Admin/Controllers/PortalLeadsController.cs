using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using Soransoft.Domain.Entities;
using Soransoft.Infrastructure.Persistence;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Soransoft.Web.Areas.Admin.Controllers
{
    /// <summary>فرم تبدیل لید به قرارداد</summary>
    public class ConvertLeadModel
    {
        public int LeadId { get; set; }
        [Required(ErrorMessage = "عنوان قرارداد الزامی است")]
        public string Title { get; set; } = string.Empty;
        [Required(ErrorMessage = "مبلغ کل الزامی است")]
        [Range(1, long.MaxValue, ErrorMessage = "مبلغ معتبر نیست")]
        public long TotalAmount { get; set; }
        /// <summary>ایجاد خودکار سه مرحله پرداخت استاندارد (۳۰٪ / ۴۰٪ / ۳۰٪)</summary>
        public bool CreateDefaultStages { get; set; } = true;
        public DateTime? LicenseExpiresAt { get; set; }
        public CommissionMethod Method { get; set; } = CommissionMethod.FixedPercent;
        public decimal Value { get; set; }
        /// <summary>پله‌ها برای روش پله‌ای — هر خط: «تا مبلغ|درصد»</summary>
        public string? TiersText { get; set; }
    }

    /// <summary>فرم ثبت/ویرایش مرحله پرداخت</summary>
    public class PaymentStageFormModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "قرارداد الزامی است")]
        public int ContractId { get; set; }
        public int Number { get; set; }
        [Required(ErrorMessage = "عنوان مرحله الزامی است")]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public long Amount { get; set; }
        public decimal? PercentOfTotal { get; set; }
        public DateTime? DueDate { get; set; }
        public string? DocumentNote { get; set; }
        public string? ChequeNo { get; set; }
        public string? ChequeBank { get; set; }
        public DateTime? ChequeDueDate { get; set; }
    }

    /// <summary>قیف فروش لیدها در پنل مدیر</summary>
    [Area("Admin")]
    [Route("Admin/PortalLeads/{action=Index}/{id?}")]
    [Authorize(Policy = "AdminOnly")]
    public class PortalLeadsController : Controller
    {
        private readonly SoransoftDbContext _db;
        private readonly IPartnerPortalService _portal;

        public PortalLeadsController(SoransoftDbContext db, IPartnerPortalService portal)
        {
            _db = db;
            _portal = portal;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var leads = await _db.Leads.AsNoTracking()
                .Include(l => l.Partner)
                .Include(l => l.History)
                .OrderBy(l => l.Stage).ThenByDescending(l => l.CreatedAt)
                .ToListAsync(ct);

            ViewBag.SalesPartners = await _db.Partners.AsNoTracking()
                .Where(p => !p.IsDeleted && (p.Role == PartnerRole.Sales || p.Role == PartnerRole.SalesManager))
                .Select(p => new { p.Id, p.FullName })
                .ToListAsync(ct);
            return View(leads);
        }

        /// <summary>تایید لید و تبدیل به قرارداد + نرخ پورسانت + مراحل پرداخت استاندارد</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Convert(ConvertLeadModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Index));
            }

            var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == model.LeadId && !l.IsDeleted, ct);
            if (lead is null) { TempData["Error"] = "لید یافت نشد."; return RedirectToAction(nameof(Index)); }
            if (lead.Stage == LeadStage.Contracted) { TempData["Error"] = "این لید قبلاً به قرارداد تبدیل شده است."; return RedirectToAction(nameof(Index)); }

            var contract = await _portal.ConvertLeadToContractAsync(model.LeadId, model.TotalAmount, model.Title, ct);
            if (contract is null) { TempData["Error"] = "تبدیل لید ناموفق بود."; return RedirectToAction(nameof(Index)); }

            contract.LicenseExpiresAt = model.LicenseExpiresAt;
            contract.Status = ContractStatus.Active;

            // مراحل پرداخت استاندارد (قابل ویرایش/حذف پس از ایجاد)
            if (model.CreateDefaultStages && model.TotalAmount > 0)
            {
                var templates = new (string Title, decimal Percent)[]
                {
                    ("مرحله اول: هم‌زمان با امضای قرارداد", 30m),
                    ("مرحله دوم: پس از راه‌اندازی اولیه و ارائه به مشتری", 40m),
                    ("مرحله سوم: پس از آموزش و تحویل قطعی به مشتری", 30m),
                };
                var number = 1;
                foreach (var t in templates)
                {
                    _db.ContractPaymentStages.Add(new ContractPaymentStage
                    {
                        ContractId = contract.Id,
                        Number = number,
                        Title = t.Title,
                        PercentOfTotal = t.Percent,
                        Amount = (long)Math.Round(model.TotalAmount * t.Percent / 100m, 0),
                        DueDate = DateTime.Now.AddMonths(number - 1),
                    });
                    number++;
                }
            }

            // نرخ پورسانت مجزا برای فروشنده‌ی لید
            if (lead.PartnerId is int pid && model.Value > 0)
            {
                _db.CommissionRates.Add(new CommissionRate
                {
                    ContractId = contract.Id,
                    PartnerId = pid,
                    Method = model.Method,
                    Value = model.Value,
                    TiersJson = model.Method == CommissionMethod.Tiered ? ParseTiers(model.TiersText) : null,
                    BasedOnPaidStages = true,
                    IsActive = true,
                });
            }

            await _db.SaveChangesAsync(ct);
            TempData["Success"] = $"لید به قرارداد «{contract.Title}» تبدیل شد؛ مراحل پرداخت و مبنای پورسانت را در صفحه قراردادها بازبینی کنید.";
            return RedirectToAction(nameof(Contracts));
        }

        // ============================================================ قراردادها، مراحل و هزینه شخص ثالث

        public async Task<IActionResult> Contracts(CancellationToken ct)
        {
            var contracts = await _db.PartnerContracts.AsNoTracking()
                .Include(c => c.Partner)
                .Include(c => c.PaymentStages)
                .Include(c => c.ThirdPartyCosts)
                .Include(c => c.CommissionRates)
                .OrderBy(c => c.Status).ThenByDescending(c => c.CreatedAt)
                .ToListAsync(ct);

            ViewBag.SalesPartners = await _db.Partners.AsNoTracking()
                .Where(p => !p.IsDeleted && (p.Role == PartnerRole.Sales || p.Role == PartnerRole.SalesManager))
                .Select(p => new { p.Id, p.FullName })
                .ToListAsync(ct);

            ViewBag.CostCatalogue = await _db.ThirdPartyCostItems.AsNoTracking()
                .Where(i => i.IsActive)
                .OrderBy(i => i.Type).ThenBy(i => i.DisplayOrder)
                .ToListAsync(ct);

            // پورسانت محاسبه‌شده و مبنای خالص هر نرخ (مبنا پس از کسر هزینه شخص ثالث)
            var commissionInfo = new Dictionary<int, (decimal Commission, long Base)>();
            foreach (var c in contracts)
            {
                foreach (var r in c.CommissionRates)
                {
                    commissionInfo[r.Id] = (await _portal.ComputeCommissionAsync(r, ct), await _portal.GetCommissionBaseAsync(r, ct));
                }
            }
            ViewBag.CommissionInfo = commissionInfo;

            // مجموع هزینه شخص ثالث هر قرارداد
            ViewBag.ThirdPartyTotals = contracts.ToDictionary(
                c => c.Id,
                c => c.ThirdPartyCosts.Where(x => x.DeductFromFirstPayment).Sum(x => x.Amount));

            return View(contracts);
        }

        /// <summary>افزودن/ویرایش مرحله پرداخت</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveStage(PaymentStageFormModel model, IFormFile? document, [FromServices] IPartnerDocumentService docs, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Contracts));
            }

            var stage = new ContractPaymentStage
            {
                Id = model.Id,
                ContractId = model.ContractId,
                Number = model.Number,
                Title = model.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                Amount = model.Amount,
                PercentOfTotal = model.PercentOfTotal,
                DueDate = model.DueDate,
                DocumentNote = string.IsNullOrWhiteSpace(model.DocumentNote) ? null : model.DocumentNote.Trim(),
                ChequeNo = string.IsNullOrWhiteSpace(model.ChequeNo) ? null : model.ChequeNo.Trim(),
                ChequeBank = string.IsNullOrWhiteSpace(model.ChequeBank) ? null : model.ChequeBank.Trim(),
                ChequeDueDate = model.ChequeDueDate,
            };

            // ثبت سند/چک همراه مرحله
            if (document is { Length: > 0 })
            {
                var path = await docs.SaveStageDocumentAsync(document, ct);
                if (path is null) { TempData["Error"] = "سند مرحله نامعتبر است (PDF یا تصویر تا ۲۰ مگابایت)."; return RedirectToAction(nameof(Contracts)); }
                stage.DocumentFile = path;
            }

            var result = await _portal.SavePaymentStageAsync(stage, model.Id == 0, ct);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Contracts));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStage(int id, CancellationToken ct)
        {
            var result = await _portal.DeletePaymentStageAsync(id, ct);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Contracts));
        }

        /// <summary>تایید/رد رسید مرحله — پورسانت بر مراحل پرداخت‌شده به‌روز می‌شود</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStageStatus(int id, PaymentStageStatus status, string? note, CancellationToken ct)
        {
            var result = await _portal.UpdatePaymentStageStatusAsync(id, status, note, ct);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Contracts));
        }

        /// <summary>آپلود رسید پرداخت یا سند/چک یک مرحله</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadStageDoc(int id, string kind, IFormFile? file, [FromServices] IPartnerDocumentService docs, CancellationToken ct)
        {
            var stage = await _db.ContractPaymentStages.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (stage is null) { TempData["Error"] = "مرحله پرداخت یافت نشد."; return RedirectToAction(nameof(Contracts)); }

            var path = await docs.SaveStageDocumentAsync(file!, ct);
            if (path is null) { TempData["Error"] = "فایل نامعتبر است (PDF یا تصویر تا ۲۰ مگابایت)."; return RedirectToAction(nameof(Contracts)); }

            if (kind == "receipt") stage.ReceiptFile = path;
            else stage.DocumentFile = path;
            await _db.SaveChangesAsync(ct);

            TempData["Success"] = "سند مرحله بارگذاری شد.";
            return RedirectToAction(nameof(Contracts));
        }

        /// <summary>افزودن هزینه شخص ثالث به قرارداد (از کاتالوگ یا دستی)</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddThirdPartyCost(int contractId, int? itemId, string? title, long amount, string? note, bool deductFromFirstPayment = true, CancellationToken ct = default)
        {
            var contract = await _db.PartnerContracts.FirstOrDefaultAsync(c => c.Id == contractId, ct);
            if (contract is null) { TempData["Error"] = "قرارداد یافت نشد."; return RedirectToAction(nameof(Contracts)); }

            ThirdPartyCostItem? item = null;
            if (itemId is int iid && iid > 0)
                item = await _db.ThirdPartyCostItems.FirstOrDefaultAsync(i => i.Id == iid, ct);

            var resolvedTitle = !string.IsNullOrWhiteSpace(title) ? title.Trim() : item?.Title;
            if (string.IsNullOrWhiteSpace(resolvedTitle)) { TempData["Error"] = "عنوان هزینه الزامی است."; return RedirectToAction(nameof(Contracts)); }
            if (amount <= 0 && item is not null) amount = item.DefaultAmount;
            if (amount <= 0) { TempData["Error"] = "مبلغ هزینه معتبر نیست."; return RedirectToAction(nameof(Contracts)); }

            _db.ContractThirdPartyCosts.Add(new ContractThirdPartyCost
            {
                ContractId = contractId,
                ItemId = item?.Id,
                Type = item?.Type ?? ThirdPartyCostType.Other,
                Title = resolvedTitle,
                TechnicalSpecs = item?.TechnicalSpecs,
                Amount = amount,
                Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
                DeductFromFirstPayment = deductFromFirstPayment,
            });
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = $"هزینه شخص ثالث «{resolvedTitle}» ثبت شد و از مبنای پورسانت کسر می‌شود.";
            return RedirectToAction(nameof(Contracts));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteThirdPartyCost(int id, CancellationToken ct)
        {
            var row = await _db.ContractThirdPartyCosts.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (row is null) { TempData["Error"] = "هزینه یافت نشد."; return RedirectToAction(nameof(Contracts)); }
            _db.ContractThirdPartyCosts.Remove(row);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "هزینه شخص ثالث حذف شد.";
            return RedirectToAction(nameof(Contracts));
        }

        /// <summary>تعریف/ویرایش نرخ پورسانت مجزا برای یک همکار روی یک قرارداد</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetCommission(int contractId, int partnerId, CommissionMethod method, decimal value, bool basedOnPaid, string? tiersText, CancellationToken ct)
        {
            if (value <= 0) { TempData["Error"] = "مقدار پورسانت معتبر نیست."; return RedirectToAction(nameof(Contracts)); }
            if (method == CommissionMethod.Tiered && string.IsNullOrWhiteSpace(tiersText))
            {
                TempData["Error"] = "برای روش پله‌ای، حداقل یک پله وارد کنید (مثال: 100000000|5).";
                return RedirectToAction(nameof(Contracts));
            }

            var rate = await _db.CommissionRates.FirstOrDefaultAsync(r => r.ContractId == contractId && r.PartnerId == partnerId, ct);
            if (rate is null)
            {
                rate = new CommissionRate { ContractId = contractId, PartnerId = partnerId };
                _db.CommissionRates.Add(rate);
            }
            rate.Method = method;
            rate.Value = value;
            rate.TiersJson = method == CommissionMethod.Tiered ? ParseTiers(tiersText) : null;
            rate.BasedOnPaidStages = basedOnPaid;
            rate.IsActive = true;
            await _db.SaveChangesAsync(ct);

            TempData["Success"] = "نرخ پورسانت ذخیره شد (مبنا: خالص پس از کسر هزینه‌های شخص ثالث).";
            return RedirectToAction(nameof(Contracts));
        }

        /// <summary>تبدیل متن پله‌ها («تا مبلغ|درصد» در هر خط) به JSON</summary>
        private static string? ParseTiers(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var tiers = new List<object>();
            foreach (var raw in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = raw.Split('|');
                if (parts.Length != 2) continue;
                var upTo = parts[0].Replace(",", "").Replace("،", "").Trim();
                var percent = parts[1].Trim();
                if (long.TryParse(upTo, out var u) && decimal.TryParse(percent, out var p))
                    tiers.Add(new { upTo = u, percent = p });
            }
            return tiers.Count == 0 ? null : JsonSerializer.Serialize(tiers);
        }
    }
}

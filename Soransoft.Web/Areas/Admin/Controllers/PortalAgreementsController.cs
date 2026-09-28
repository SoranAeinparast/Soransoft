using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using Soransoft.Domain.Entities;
using Soransoft.Infrastructure.Persistence;
using Soransoft.Web.Models;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Soransoft.Web.Areas.Admin.Controllers
{
    /// <summary>فرم ثبت/ویرایش قرارداد همکاری</summary>
    public class AgreementFormModel : IValidatableObject
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "انتخاب همکار الزامی است")]
        public int PartnerId { get; set; }
        [Required(ErrorMessage = "نوع قرارداد الزامی است")]
        public AgreementKind Kind { get; set; } = AgreementKind.Sales;
        public bool UseSystemNumber { get; set; } = true;
        public string? AgreementNo { get; set; }
        public List<int> SellableProjectIds { get; set; } = new();
        [Required(ErrorMessage = "عنوان قرارداد الزامی است")]
        public string Title { get; set; } = string.Empty;
        [Required(ErrorMessage = "موضوع قرارداد الزامی است")]
        public string Subject { get; set; } = string.Empty;
        /// <summary>شرح مفاد و تعهدات (HTML)</summary>
        public string? Terms { get; set; }
        [Required(ErrorMessage = "تاریخ شروع الزامی است")]
        public DateTime StartDate { get; set; } = DateTime.Now;
        [Required(ErrorMessage = "تاریخ انعقاد الزامی است")]
        public DateTime SignedAt { get; set; } = DateTime.Now;
        public DateTime? EndDate { get; set; }
        public long? TotalAmount { get; set; }
        /// <summary>سقف نامحدود — تعداد قرارداد با مشتری محدود نیست و پورسانت درصدی است</summary>
        public bool IsUnlimitedAmount { get; set; }

        // مفاد مالی
        public CommissionMethod CommissionMethod { get; set; } = CommissionMethod.FixedPercent;
        public decimal CommissionValue { get; set; }
        public bool BasedOnPaidStages { get; set; } = true;
        public long? MonthlySalary { get; set; }
        public string? SettlementTerms { get; set; }

        // حساب بانکی همکار
        public string? BankName { get; set; }
        public string? BankAccountIban { get; set; }
        public string? BankAccountHolder { get; set; }

        public string? AdminNote { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Enum.IsDefined(Kind))
                yield return new ValidationResult("نوع قرارداد معتبر نیست.", new[] { nameof(Kind) });
            if (!Enum.IsDefined(CommissionMethod))
                yield return new ValidationResult("روش پورسانت معتبر نیست.", new[] { nameof(CommissionMethod) });
            if (!UseSystemNumber && string.IsNullOrWhiteSpace(AgreementNo))
                yield return new ValidationResult("برای شماره دستی قرارداد مقدار وارد کنید.", new[] { nameof(AgreementNo) });
            if (!string.IsNullOrWhiteSpace(AgreementNo) && AgreementNo.Trim().Length > 60)
                yield return new ValidationResult("شماره قرارداد نمی‌تواند بیشتر از ۶۰ کاراکتر باشد.", new[] { nameof(AgreementNo) });
            if (EndDate is DateTime end && end < StartDate)
                yield return new ValidationResult("تاریخ پایان نمی‌تواند قبل از تاریخ شروع باشد.", new[] { nameof(EndDate) });
            if (!IsUnlimitedAmount && (!TotalAmount.HasValue || TotalAmount.Value <= 0))
                yield return new ValidationResult("مبلغ قرارداد باید بزرگ‌تر از صفر باشد یا سقف نامحدود انتخاب شود.", new[] { nameof(TotalAmount) });
            if (CommissionValue < 0 || (CommissionMethod == CommissionMethod.FixedPercent && CommissionValue > 100))
                yield return new ValidationResult("مقدار پورسانت معتبر نیست.", new[] { nameof(CommissionValue) });
        }
    }

    /// <summary>قراردادهای همکاری فی‌مابین — ملاک طرفین</summary>
    [Area("Admin")]
    [Route("Admin/PortalAgreements/{action=Index}/{id?}")]
    [Authorize(Policy = "AdminOnly")]
    public class PortalAgreementsController : Controller
    {
        private readonly SoransoftDbContext _db;
        private readonly IPartnerDocumentService _docs;

        public PortalAgreementsController(SoransoftDbContext db, IPartnerDocumentService docs)
        {
            _db = db;
            _docs = docs;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var items = await _db.CooperationAgreements.AsNoTracking()
                .Include(a => a.Partner)
                .OrderBy(a => a.Status).ThenByDescending(a => a.CreatedAt)
                .ToListAsync(ct);

            await FillPartnersAsync(ct);
            await FillSellableProjectsAsync(ct);
            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            var agreement = await _db.CooperationAgreements.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
            if (agreement is null) return NotFound();
            await FillPartnersAsync(ct);
            await FillSellableProjectsAsync(ct);
            return View(new AgreementFormModel
            {
                Id = agreement.Id,
                PartnerId = agreement.PartnerId,
                Kind = agreement.Kind,
                UseSystemNumber = IsSystemNumber(agreement.AgreementNo),
                AgreementNo = agreement.AgreementNo,
                SellableProjectIds = await _db.SellableProjectPartners.AsNoTracking()
                    .Where(x => x.PartnerId == agreement.PartnerId && x.IsActive)
                    .Select(x => x.SellableProjectId)
                    .ToListAsync(ct),
                Title = agreement.Title,
                Subject = agreement.Subject,
                Terms = agreement.Terms,
                StartDate = agreement.StartDate,
                SignedAt = agreement.SignedAt,
                EndDate = agreement.EndDate,
                TotalAmount = agreement.TotalAmount,
                IsUnlimitedAmount = agreement.IsUnlimitedAmount,
                CommissionMethod = agreement.CommissionMethod,
                CommissionValue = agreement.CommissionValue,
                BasedOnPaidStages = agreement.BasedOnPaidStages,
                MonthlySalary = agreement.MonthlySalary,
                SettlementTerms = agreement.SettlementTerms,
                BankName = agreement.BankName,
                BankAccountIban = agreement.BankAccountIban,
                BankAccountHolder = agreement.BankAccountHolder,
                AdminNote = agreement.AdminNote,
            });
        }

        /// <summary>ذخیره (ایجاد/ویرایش) قرارداد همکاری</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(AgreementFormModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Index));
            }

            var partner = await _db.Partners.AsNoTracking().FirstOrDefaultAsync(p => p.Id == model.PartnerId && !p.IsDeleted, ct);
            if (partner is null) { TempData["Error"] = "همکار یافت نشد."; return RedirectToAction(nameof(Index)); }

            CooperationAgreement? agreement;
            int? previousPartnerId = null;
            if (model.Id == 0)
            {
                agreement = new CooperationAgreement { Status = AgreementStatus.Draft };
                _db.CooperationAgreements.Add(agreement);
            }
            else
            {
                agreement = await _db.CooperationAgreements.FirstOrDefaultAsync(a => a.Id == model.Id, ct);
                if (agreement is null) { TempData["Error"] = "قرارداد یافت نشد."; return RedirectToAction(nameof(Index)); }
                previousPartnerId = agreement.PartnerId;
            }

            var requestedAgreementNo = model.UseSystemNumber
                ? (model.Id == 0 || !IsSystemNumber(agreement.AgreementNo)
                    ? await NextAgreementNoAsync(model.Kind, ct)
                    : agreement.AgreementNo)
                : model.AgreementNo?.Trim();

            if (string.IsNullOrWhiteSpace(requestedAgreementNo))
            {
                TempData["Error"] = "شماره قرارداد الزامی است.";
                return RedirectToAction(model.Id == 0 ? nameof(Index) : nameof(Edit), new { id = model.Id });
            }

            var duplicateNumber = await _db.CooperationAgreements.AnyAsync(a =>
                a.Id != model.Id && a.AgreementNo == requestedAgreementNo, ct);
            if (duplicateNumber)
            {
                TempData["Error"] = $"شماره قرارداد «{requestedAgreementNo}» قبلاً ثبت شده است.";
                return RedirectToAction(model.Id == 0 ? nameof(Index) : nameof(Edit), new { id = model.Id });
            }

            agreement.PartnerId = model.PartnerId;
            agreement.Kind = model.Kind;
            agreement.AgreementNo = requestedAgreementNo;
            agreement.Title = model.Title.Trim();
            agreement.Subject = model.Subject.Trim();
            agreement.Terms = model.Terms ?? "";
            agreement.StartDate = model.StartDate;
            agreement.SignedAt = model.SignedAt;
            agreement.EndDate = model.EndDate;
            agreement.IsUnlimitedAmount = model.IsUnlimitedAmount;
            agreement.TotalAmount = model.IsUnlimitedAmount ? null : model.TotalAmount;
            agreement.CommissionMethod = model.CommissionMethod;
            agreement.CommissionValue = model.CommissionValue;
            agreement.BasedOnPaidStages = model.BasedOnPaidStages;
            agreement.MonthlySalary = model.MonthlySalary;
            agreement.SettlementTerms = string.IsNullOrWhiteSpace(model.SettlementTerms) ? null : model.SettlementTerms.Trim();
            agreement.BankName = string.IsNullOrWhiteSpace(model.BankName) ? null : model.BankName.Trim();
            agreement.BankAccountIban = string.IsNullOrWhiteSpace(model.BankAccountIban) ? null : model.BankAccountIban.Trim().Replace(" ", "");
            agreement.BankAccountHolder = string.IsNullOrWhiteSpace(model.BankAccountHolder) ? null : model.BankAccountHolder.Trim();
            agreement.AdminNote = string.IsNullOrWhiteSpace(model.AdminNote) ? null : model.AdminNote.Trim();

            await _db.SaveChangesAsync(ct);
            if (previousPartnerId.HasValue && previousPartnerId.Value != model.PartnerId)
                await SyncSellableProjectAccessAsync(previousPartnerId.Value, Array.Empty<int>(), ct);
            await SyncSellableProjectAccessAsync(
                model.PartnerId,
                model.Kind == AgreementKind.Sales ? model.SellableProjectIds : Array.Empty<int>(),
                ct);
            TempData["Success"] = model.Id == 0
                ? $"قرارداد {agreement.AgreementNo} به‌عنوان پیش‌نویس ثبت شد؛ پس از آپلود PDF و فعال‌سازی، ملاک طرفین می‌شود."
                : "اطلاعات قرارداد به‌روزرسانی شد.";
            return RedirectToAction(nameof(Details), new { id = agreement.Id });
        }

        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            var agreement = await _db.CooperationAgreements.AsNoTracking()
                .Include(a => a.Partner)
                .FirstOrDefaultAsync(a => a.Id == id, ct);
            if (agreement is null) { TempData["Error"] = "قرارداد یافت نشد."; return RedirectToAction(nameof(Index)); }
            return View(agreement);
        }

        /// <summary>آپلود PDF قرارداد امضاشده (نگارش جدید، فایل قبلی حفظ در تاریخچه)</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPdf(int id, IFormFile? file, CancellationToken ct)
        {
            var agreement = await _db.CooperationAgreements.FirstOrDefaultAsync(a => a.Id == id, ct);
            if (agreement is null) { TempData["Error"] = "قرارداد یافت نشد."; return RedirectToAction(nameof(Index)); }
            if (file is null || file.Length == 0) { TempData["Error"] = "فایلی انتخاب نشده است."; return RedirectToAction(nameof(Details), new { id }); }

            var path = await _docs.SaveAgreementFileAsync(file, ct);
            if (path is null) { TempData["Error"] = "فقط فایل PDF تا ۲۰ مگابایت مجاز است."; return RedirectToAction(nameof(Details), new { id }); }

            var previousPath = agreement.ContractFile;
            agreement.ContractFile = path;
            await _db.SaveChangesAsync(ct);
            if (!string.IsNullOrWhiteSpace(previousPath) && !string.Equals(previousPath, path, StringComparison.Ordinal))
                await _docs.DeleteFileAsync(previousPath, ct);
            TempData["Success"] = "PDF قرارداد بارگذاری شد.";
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>فعال‌سازی — از این لحظه ملاک طرفین</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id, CancellationToken ct)
        {
            var agreement = await _db.CooperationAgreements.FirstOrDefaultAsync(a => a.Id == id, ct);
            if (agreement is null) { TempData["Error"] = "قرارداد یافت نشد."; return RedirectToAction(nameof(Index)); }
            if (agreement.Status != AgreementStatus.Draft) { TempData["Error"] = "فقط پیش‌نویس قابل فعال‌سازی است."; return RedirectToAction(nameof(Details), new { id }); }
            if (string.IsNullOrWhiteSpace(agreement.ContractFile))
            {
                TempData["Error"] = "پیش از فعال‌سازی، PDF قرارداد امضاشده را بارگذاری کنید.";
                return RedirectToAction(nameof(Details), new { id });
            }

            agreement.Status = AgreementStatus.Active;
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = $"قرارداد {agreement.AgreementNo} فعال شد و از این لحظه ملاک طرفین است (در پرتال همکار قابل مشاهده).";
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>فسخ/خاتمه قرارداد</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Terminate(int id, string? note, CancellationToken ct)
        {
            var agreement = await _db.CooperationAgreements.FirstOrDefaultAsync(a => a.Id == id, ct);
            if (agreement is null) { TempData["Error"] = "قرارداد یافت نشد."; return RedirectToAction(nameof(Index)); }
            if (agreement.Status != AgreementStatus.Active) { TempData["Error"] = "فقط قرارداد فعال قابل فسخ است."; return RedirectToAction(nameof(Details), new { id }); }

            agreement.Status = AgreementStatus.Terminated;
            agreement.AdminNote = string.IsNullOrWhiteSpace(note)
                ? agreement.AdminNote
                : (agreement.AdminNote is null ? "" : agreement.AdminNote + "\n") + $"[فسخ {PersianDisplay.Date(DateTime.Now)}] " + note.Trim();
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "قرارداد فسخ شد.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var agreement = await _db.CooperationAgreements.FirstOrDefaultAsync(a => a.Id == id, ct);
            if (agreement is null) { TempData["Error"] = "قرارداد یافت نشد."; return RedirectToAction(nameof(Index)); }
            if (agreement.Status == AgreementStatus.Active)
            {
                TempData["Error"] = "قرارداد فعال قابل حذف نیست؛ ابتدا فسخ کنید.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!string.IsNullOrWhiteSpace(agreement.ContractFile))
                await _docs.DeleteFileAsync(agreement.ContractFile, ct);
            _db.CooperationAgreements.Remove(agreement);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "قرارداد حذف شد.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>تولید شماره قرارداد یکتا: SA/TA-1404-001</summary>
        private async Task<string> NextAgreementNoAsync(AgreementKind kind, CancellationToken ct)
        {
            var year = new PersianCalendar().GetYear(DateTime.Now);
            var prefix = (kind == AgreementKind.Sales ? "SA" : "TA") + "-" + year + "-";
            var count = await _db.CooperationAgreements.CountAsync(a => a.AgreementNo.StartsWith(prefix), ct);
            return prefix + (count + 1).ToString("000");
        }

        private static bool IsSystemNumber(string? agreementNo) =>
            !string.IsNullOrWhiteSpace(agreementNo)
            && (agreementNo.StartsWith("SA-", StringComparison.OrdinalIgnoreCase)
                || agreementNo.StartsWith("TA-", StringComparison.OrdinalIgnoreCase));

        private async Task FillPartnersAsync(CancellationToken ct)
        {
            ViewBag.Partners = await _db.Partners.AsNoTracking()
                .Where(p => !p.IsDeleted && p.IsActive)
                .Select(p => new { p.Id, p.FullName, p.Role })
                .ToListAsync(ct);
        }

        private async Task FillSellableProjectsAsync(CancellationToken ct)
        {
            ViewBag.SellableProjects = await _db.SellableProjects.AsNoTracking()
                .Where(p => p.IsActive && !p.IsDeleted)
                .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Title)
                .Select(p => new { p.Id, p.Title })
                .ToListAsync(ct);
        }

        private async Task SyncSellableProjectAccessAsync(int partnerId, IEnumerable<int> projectIds, CancellationToken ct)
        {
            var existing = await _db.SellableProjectPartners
                .Where(x => x.PartnerId == partnerId)
                .ToListAsync(ct);
            _db.SellableProjectPartners.RemoveRange(existing);

            var ids = projectIds.Distinct().ToList();
            if (ids.Count > 0)
            {
                var validIds = await _db.SellableProjects.AsNoTracking()
                    .Where(p => ids.Contains(p.Id) && p.IsActive && !p.IsDeleted)
                    .Select(p => p.Id)
                    .ToListAsync(ct);
                _db.SellableProjectPartners.AddRange(validIds.Select(id => new SellableProjectPartner
                {
                    PartnerId = partnerId,
                    SellableProjectId = id,
                    IsActive = true,
                }));
            }
            await _db.SaveChangesAsync(ct);
        }
    }
}

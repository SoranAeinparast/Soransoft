using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using Soransoft.Domain.Entities;
using Soransoft.Infrastructure.Persistence;
using System.ComponentModel.DataAnnotations;

namespace Soransoft.Web.Areas.Admin.Controllers
{
    /// <summary>فرم ثبت/ویرایش قرارداد همکاری</summary>
    public class AgreementFormModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "انتخاب همکار الزامی است")]
        public int PartnerId { get; set; }
        [Required(ErrorMessage = "نوع قرارداد الزامی است")]
        public AgreementKind Kind { get; set; } = AgreementKind.Sales;
        public string? AgreementNo { get; set; }
        [Required(ErrorMessage = "عنوان قرارداد الزامی است")]
        public string Title { get; set; } = string.Empty;
        [Required(ErrorMessage = "موضوع قرارداد الزامی است")]
        public string Subject { get; set; } = string.Empty;
        /// <summary>شرح مفاد و تعهدات (HTML)</summary>
        public string? Terms { get; set; }
        [Required(ErrorMessage = "تاریخ شروع الزامی است")]
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime? EndDate { get; set; }
        public long? TotalAmount { get; set; }
        /// <summary>سقف نامحدود — تعداد قرارداد با مشتری محدود نیست و پورسانت درصدی است</summary>
        public bool IsUnlimitedAmount { get; set; }

        // مفاد مالی
        public CommissionMethod CommissionMethod { get; set; } = CommissionMethod.FixedPercent;
        [Range(0, 100, ErrorMessage = "درصد پورسانت بین ۰ تا ۱۰۰")]
        public decimal CommissionValue { get; set; }
        public bool BasedOnPaidStages { get; set; } = true;
        public long? MonthlySalary { get; set; }
        public string? SettlementTerms { get; set; }

        // حساب بانکی همکار
        public string? BankName { get; set; }
        public string? BankAccountIban { get; set; }
        public string? BankAccountHolder { get; set; }

        public string? AdminNote { get; set; }
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

            ViewBag.Partners = await _db.Partners.AsNoTracking()
                .Where(p => !p.IsDeleted && p.IsActive)
                .Select(p => new { p.Id, p.FullName, p.Role })
                .ToListAsync(ct);
            return View(items);
        }

        /// <summary>ذخیره (ایجاد/ویرایش) قرارداد — ویرایش فقط در وضعیت Draft</summary>
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
            if (model.Id == 0)
            {
                agreement = new CooperationAgreement { Status = AgreementStatus.Draft };
                agreement.AgreementNo = await NextAgreementNoAsync(model.Kind, ct);
                _db.CooperationAgreements.Add(agreement);
            }
            else
            {
                agreement = await _db.CooperationAgreements.FirstOrDefaultAsync(a => a.Id == model.Id, ct);
                if (agreement is null) { TempData["Error"] = "قرارداد یافت نشد."; return RedirectToAction(nameof(Index)); }
                if (agreement.Status != AgreementStatus.Draft)
                {
                    TempData["Error"] = "قرارداد فعال/خاتمه‌یافته قابل ویرایش نیست؛ برای تغییر، قرارداد جدید ثبت کنید.";
                    return RedirectToAction(nameof(Details), new { id = agreement.Id });
                }
            }

            agreement.PartnerId = model.PartnerId;
            agreement.Kind = model.Kind;
            agreement.Title = model.Title.Trim();
            agreement.Subject = model.Subject.Trim();
            agreement.Terms = model.Terms ?? "";
            agreement.StartDate = model.StartDate;
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
            TempData["Success"] = model.Id == 0
                ? $"قرارداد {agreement.AgreementNo} به‌عنوان پیش‌نویس ثبت شد؛ پس از آپلود PDF و فعال‌سازی، ملاک طرفین می‌شود."
                : "پیش‌نویس به‌روزرسانی شد.";
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

            if (!string.IsNullOrWhiteSpace(agreement.ContractFile))
                await _docs.DeleteFileAsync(agreement.ContractFile, ct);
            agreement.ContractFile = path;
            await _db.SaveChangesAsync(ct);
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
                : (agreement.AdminNote is null ? "" : agreement.AdminNote + "\n") + $"[فسخ {DateTime.Now:yyyy/MM/dd}] " + note.Trim();
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
            var year = DateTime.Now.Year - 621; // تقریب سال شمسی برای شماره‌گذاری
            var prefix = (kind == AgreementKind.Sales ? "SA" : "TA") + "-" + year + "-";
            var count = await _db.CooperationAgreements.CountAsync(a => a.AgreementNo.StartsWith(prefix), ct);
            return prefix + (count + 1).ToString("000");
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Domain.Entities;
using Soransoft.Infrastructure.Persistence;

namespace Soransoft.Web.Areas.Partner.Controllers
{
    /// <summary>قرارداد همکاری فی‌مابین — فقط قراردادهای خود همکار</summary>
    public class AgreementController : PartnerBaseController
    {
        private readonly SoransoftDbContext _db;

        public AgreementController(SoransoftDbContext db) => _db = db;

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var agreements = await _db.CooperationAgreements.AsNoTracking()
                .Include(a => a.CancellationRequests.OrderByDescending(r => r.CreatedAt))
                .Where(a => a.PartnerId == PartnerId)
                .OrderBy(a => a.Status == AgreementStatus.Active ? 0 : 1)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync(ct);
            return View(agreements);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestCancellation(int id, string? reason, DateTime requestedTerminationDate, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10)
            {
                TempData["Error"] = "دلیل فسخ باید حداقل ۱۰ کاراکتر باشد.";
                return RedirectToAction(nameof(Index));
            }
            if (requestedTerminationDate == default)
            {
                TempData["Error"] = "تاریخ پیشنهادی فسخ را وارد کنید.";
                return RedirectToAction(nameof(Index));
            }

            var agreement = await _db.CooperationAgreements.FirstOrDefaultAsync(a =>
                a.Id == id && a.PartnerId == PartnerId, ct);
            if (agreement is null) { TempData["Error"] = "قرارداد یافت نشد."; return RedirectToAction(nameof(Index)); }
            if (agreement.Status != AgreementStatus.Active)
            {
                TempData["Error"] = "فقط قرارداد فعال امکان درخواست فسخ دارد.";
                return RedirectToAction(nameof(Index));
            }

            var hasPending = await _db.AgreementCancellationRequests.AnyAsync(r =>
                r.AgreementId == agreement.Id && r.Status == AgreementCancellationStatus.Pending, ct);
            if (hasPending)
            {
                TempData["Error"] = "برای این قرارداد یک درخواست فسخ در انتظار بررسی وجود دارد.";
                return RedirectToAction(nameof(Index));
            }

            _db.AgreementCancellationRequests.Add(new AgreementCancellationRequest
            {
                AgreementId = agreement.Id,
                PartnerId = PartnerId,
                Reason = reason.Trim(),
                RequestedTerminationDate = requestedTerminationDate.Date,
                Status = AgreementCancellationStatus.Pending,
            });
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "درخواست فسخ برای بررسی مدیر ثبت شد.";
            return RedirectToAction(nameof(Index));
        }
    }
}
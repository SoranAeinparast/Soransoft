using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using Soransoft.Domain.Entities;
using Soransoft.Infrastructure.Persistence;

namespace Soransoft.Web.Areas.Partner.Controllers
{
    /// <summary>کیف پول همکار — فقط تراکنش‌های خودش</summary>
    public class WalletController : PartnerBaseController
    {
        private readonly SoransoftDbContext _db;
        private readonly IWalletService _wallet;

        public WalletController(SoransoftDbContext db, IWalletService wallet)
        {
            _db = db;
            _wallet = wallet;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var pid = PartnerId;

            ViewBag.Balance = await _wallet.GetBalanceAsync(pid, ct);
            ViewBag.Available = await _wallet.GetAvailableAsync(pid, ct);

            var txs = await _db.WalletTransactions.AsNoTracking()
                .Where(t => t.PartnerId == pid)
                .OrderByDescending(t => t.Id)
                .Take(100)
                .ToListAsync(ct);

            var requests = await _db.WithdrawalRequests.AsNoTracking()
                .Where(r => r.PartnerId == pid)
                .OrderByDescending(r => r.CreatedAt)
                .Take(20)
                .ToListAsync(ct);
            ViewBag.Requests = requests;

            // حساب بانکی از قرارداد فعال (مبنای درخواست برداشت)
            var agreement = await _db.CooperationAgreements.AsNoTracking()
                .Where(a => a.PartnerId == pid && a.Status == AgreementStatus.Active)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new { a.BankName, a.BankAccountIban, a.BankAccountHolder })
                .FirstOrDefaultAsync(ct);
            ViewBag.AgreementBank = agreement;

            return View(txs);
        }

        /// <summary>درخواست واریز همه یا بخشی از موجودی</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestWithdrawal(decimal amount, string? note, CancellationToken ct)
        {
            // حساب مقصد از قرارداد فعال گرفته می‌شود — همکار نمی‌تواند حساب دلخواه بدهد
            var agreement = await _db.CooperationAgreements.AsNoTracking()
                .Where(a => a.PartnerId == PartnerId && a.Status == AgreementStatus.Active)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (agreement is null || string.IsNullOrWhiteSpace(agreement.BankAccountIban))
            {
                TempData["Error"] = "برای درخواست برداشت، ابتدا قرارداد همکاری فعال با حساب بانکی باید توسط مدیر ثبت شود.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _wallet.RequestWithdrawalAsync(PartnerId, amount, note,
                agreement.BankAccountIban, agreement.BankName ?? "", agreement.BankAccountHolder ?? "", ct);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}

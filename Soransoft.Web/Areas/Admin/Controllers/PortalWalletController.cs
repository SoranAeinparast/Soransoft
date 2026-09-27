using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using Soransoft.Domain.Entities;
using Soransoft.Infrastructure.Persistence;
using System.ComponentModel.DataAnnotations;
// حرف‌گرفتن تداخل namespace Areas.Partner با موجودیت Partner
using PartnerEntity = Soransoft.Domain.Entities.Partner;

namespace Soransoft.Web.Areas.Admin.Controllers
{
    /// <summary>فرم واریز به کیف پول همکار</summary>
    public class DepositFormModel
    {
        [Required(ErrorMessage = "انتخاب همکار الزامی است")]
        public int PartnerId { get; set; }
        [Required(ErrorMessage = "مبلغ الزامی است")]
        [Range(1, long.MaxValue, ErrorMessage = "مبلغ معتبر نیست")]
        public decimal Amount { get; set; }
        [Required(ErrorMessage = "توضیح الزامی است (ردپای مالی)")]
        public string Description { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public string? BankTrackingNo { get; set; }
    }

    /// <summary>کیف پول همکاران در پنل مدیر</summary>
    [Area("Admin")]
    [Route("Admin/PortalWallet/{action=Index}/{partnerId?}")]
    [Authorize(Policy = "AdminOnly")]
    public class PortalWalletController : Controller
    {
        private readonly SoransoftDbContext _db;
        private readonly IWalletService _wallet;
        private readonly IPartnerDocumentService _docs;

        public PortalWalletController(SoransoftDbContext db, IWalletService wallet, IPartnerDocumentService docs)
        {
            _db = db;
            _wallet = wallet;
            _docs = docs;
        }

    /// <summary>موجودی همه همکاران + صف درخواست‌های برداشت</summary>
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var partners = await _db.Partners.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.FullName)
            .ToListAsync(ct);

        var rows = new List<(PartnerEntity Partner, decimal Balance, decimal Locked)>();
            foreach (var p in partners)
            {
                var balance = await _wallet.GetBalanceAsync(p.Id, ct);
                var locked = await _db.WithdrawalRequests
                    .Where(r => r.PartnerId == p.Id && r.Status == WalletTxStatus.Pending)
                    .SumAsync(r => (decimal?)r.Amount, ct) ?? 0m;
                rows.Add((p, balance, locked));
            }
            ViewBag.Rows = rows;

            ViewBag.PendingRequests = await _db.WithdrawalRequests.AsNoTracking()
                .Include(r => r.Partner)
                .Where(r => r.Status == WalletTxStatus.Pending)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync(ct);

            return View();
        }

        /// <summary>واریز سهم همکار (با فیش واریز و شماره پیگیری)</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deposit(DepositFormModel model, IFormFile? receipt, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Index));
            }

            var result = await _wallet.DepositAsync(model.PartnerId, model.Amount, model.Description,
                model.Reference, model.BankTrackingNo, null, ct);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            var newBalance = await _wallet.GetBalanceAsync(model.PartnerId, ct);

            // فیش واریز به‌عنوان مستند تراکنش (آخرین ردیف دفتر این همکار)
            if (receipt is not null && receipt.Length > 0)
            {
                var path = await _docs.SaveDocumentFileAsync(receipt, ct);
                if (path is not null)
                {
                    var last = await _db.WalletTransactions
                        .Where(t => t.PartnerId == model.PartnerId)
                        .OrderByDescending(t => t.Id).FirstOrDefaultAsync(ct);
                    if (last is not null)
                    {
                        last.DocumentFile = path;
                        await _db.SaveChangesAsync(ct);
                    }
                }
            }

            TempData["Success"] = $"واریز {model.Amount:N0} تومان به کیف پول همکار ثبت شد. موجودی جدید: {newBalance:N0}";
            return RedirectToAction(nameof(Ledger), new { partnerId = model.PartnerId });
        }

        /// <summary>اصلاحیه بدهی (منفی/مثبت) با توضیح اجباری</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adjust(int partnerId, decimal amount, string description, CancellationToken ct)
        {
            var result = await _wallet.AdjustAsync(partnerId, amount, description ?? "", ct);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Ledger), new { partnerId });
        }

        /// <summary>تایید درخواست برداشت + ثبت شماره پیگیری واریز</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveWithdrawal(int id, string? bankTrackingNo, string? response, CancellationToken ct)
        {
            var result = await _wallet.ApproveWithdrawalAsync(id, response, bankTrackingNo, ct);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectWithdrawal(int id, string? response, CancellationToken ct)
        {
            var result = await _wallet.RejectWithdrawalAsync(id, response, ct);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        /// <summary>دفتر کل یک همکار — همه ردپاها</summary>
        public async Task<IActionResult> Ledger(int partnerId, CancellationToken ct)
        {
            var partner = await _db.Partners.AsNoTracking().FirstOrDefaultAsync(p => p.Id == partnerId, ct);
            if (partner is null) { TempData["Error"] = "همکار یافت نشد."; return RedirectToAction(nameof(Index)); }

            var txs = await _db.WalletTransactions.AsNoTracking()
                .Where(t => t.PartnerId == partnerId)
                .OrderByDescending(t => t.Id)
                .Take(200)
                .ToListAsync(ct);

            ViewBag.Partner = partner;
            ViewBag.Balance = await _wallet.GetBalanceAsync(partnerId, ct);
            ViewBag.Available = await _wallet.GetAvailableAsync(partnerId, ct);
            return View(txs);
        }
    }
}

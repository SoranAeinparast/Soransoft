using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using Soransoft.Domain.Entities;
using System.Data;

namespace Soransoft.Infrastructure.Persistence
{
    /// <summary>نتیجه عملیات کیف پول — همان PortalResult با موجودی نهایی</summary>
    public class WalletResult : PortalResult
    {
        public decimal Balance { get; set; }
        public static WalletResult Ok(string m, decimal bal) => new() { Success = true, Message = m, Balance = bal };
        public static new WalletResult Fail(string m) => new() { Success = false, Message = m };
    }

    /// <summary>
    /// سرویس کیف پول همکاران — دفتر کل فقط-الحاقی با موجودی لحظه‌ای (BalanceAfter).
    /// قواعد امنیتی:
    /// ۱) موجودی = مجموع Amount تراکنش‌های Confirmed (هیچ فیلد موجودی قابل‌دستکاری وجود ندارد)
    /// ۲) ردیف‌های دفتر هرگز Update/Delete نمی‌شوند؛ اصلاح فقط با تراکنش Adjustment جدید
    /// ۳) درخواست برداشت موجودی را «قفل» می‌کند (Pending از موجودی قابل‌برداشت کم می‌شود)
    /// ۴) تایید برداشت در یک تراکنش دیتابیس اتمی: ایجاد Withdrawal منفی + ثبت لینک در درخواست
    /// ۵) موجودی منفی در هیچ مسیری ممکن نیست (چک سمت سرویس + چک SQL در لحظه تایید)
    /// </summary>
    public class WalletService : IWalletService
    {
        private readonly SoransoftDbContext _db;

        public WalletService(SoransoftDbContext db) => _db = db;

        /// <summary>موجودی قطعی (تراکنش‌های Confirmed)</summary>
        public async Task<decimal> GetBalanceAsync(int partnerId, CancellationToken ct = default) =>
            await _db.WalletTransactions
                .Where(t => t.PartnerId == partnerId && t.Status == WalletTxStatus.Confirmed)
                .SumAsync(t => (decimal?)t.Amount, ct) ?? 0m;

        /// <summary>موجودی قابل‌برداشت = موجودی − درخواست‌های برداشت در انتظار تایید</summary>
        public async Task<decimal> GetAvailableAsync(int partnerId, CancellationToken ct = default)
        {
            var balance = await GetBalanceAsync(partnerId, ct);
            var locked = await _db.WithdrawalRequests
                .Where(r => r.PartnerId == partnerId && r.Status == WalletTxStatus.Pending)
                .SumAsync(r => (decimal?)r.Amount, ct) ?? 0m;
            return balance - locked;
        }

        /// <summary>واریز سهم همکار توسط مدیر (با فیش و شماره پیگیری؛ ردپای کامل)</summary>
        public async Task<PortalResult> DepositAsync(int partnerId, decimal amount, string description, string? reference = null,
            string? bankTrackingNo = null, int? contractId = null, CancellationToken ct = default)
        {
            if (amount <= 0) return WalletResult.Fail("مبلغ واریز باید مثبت باشد.");

            return await ExecuteLedgerAsync(partnerId, WalletTxType.Deposit, amount, description, reference, bankTrackingNo, ct);
        }

        /// <summary>اصلاحیه بدهی توسط مدیر (مثبت یا منفی — با توضیح اجباری)</summary>
        public async Task<PortalResult> AdjustAsync(int partnerId, decimal signedAmount, string description, CancellationToken ct = default)
        {
            if (signedAmount == 0) return WalletResult.Fail("مبلغ اصلاحیه نمی‌تواند صفر باشد.");
            if (string.IsNullOrWhiteSpace(description)) return WalletResult.Fail("برای اصلاحیه، توضیح الزامی است.");

            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            if (!await LockPartnerAsync(partnerId, ct))
                return WalletResult.Fail("همکار یافت نشد.");

            // قفل ردیف همکار تا پایان تراکنش برقرار است؛ بنابراین این بررسی با درج اصلاحیه اتمی است.
            if (signedAmount < 0)
            {
                var available = await GetAvailableAsync(partnerId, ct);
                if (available + signedAmount < 0)
                    return WalletResult.Fail("اصلاحیه منفی، موجودی را منفی می‌کند؛ ابتدا درخواست‌های در انتظار را رد کنید.");
            }

            try
            {
                var result = await AddLedgerAsync(partnerId, WalletTxType.Adjustment, signedAmount,
                    description, null, null, ct);
                await transaction.CommitAsync(ct);
                return result;
            }
            catch (DbUpdateConcurrencyException)
            {
                return WalletResult.Fail("اطلاعات کیف پول همزمان تغییر کرد؛ دوباره تلاش کنید.");
            }
        }

        /// <summary>ثبت درخواست برداشت توسط همکار (کل یا بخشی از موجودی قابل‌برداشت)</summary>
        public async Task<PortalResult> RequestWithdrawalAsync(int partnerId, decimal amount, string? note,
            string iban, string bank, string holder, CancellationToken ct = default)
        {
            if (amount <= 0) return WalletResult.Fail("مبلغ درخواست باید مثبت باشد.");
            if (string.IsNullOrWhiteSpace(iban)) return WalletResult.Fail("شماره شبا مقصد الزامی است.");

            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            if (!await LockPartnerAsync(partnerId, ct))
                return WalletResult.Fail("همکار یافت نشد.");

            var balance = await GetBalanceAsync(partnerId, ct);
            var locked = await PendingWithdrawalTotalAsync(partnerId, ct);
            var available = balance - locked;
            if (amount > available)
                return WalletResult.Fail($"موجودی قابل‌برداشت شما {available:N0} تومان است (موجودی قطعی منهای درخواست‌های در انتظار تایید).");

            try
            {
                var wr = new WithdrawalRequest
                {
                    PartnerId = partnerId,
                    Amount = amount,
                    Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
                    DestinationIban = iban.Trim(),
                    DestinationBank = bank.Trim(),
                    DestinationHolder = holder.Trim(),
                    Status = WalletTxStatus.Pending,
                };
                _db.WithdrawalRequests.Add(wr);
                await _db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return WalletResult.Ok("درخواست برداشت ثبت شد و پس از تایید مدیر به حسابتان واریز می‌شود.", available - amount);
            }
            catch (DbUpdateConcurrencyException)
            {
                return WalletResult.Fail("اطلاعات کیف پول همزمان تغییر کرد؛ دوباره تلاش کنید.");
            }
        }

        /// <summary>تایید برداشت توسط مدیر — اتمی: تراکنش منفی + لینک به درخواست</summary>
        public async Task<PortalResult> ApproveWithdrawalAsync(int requestId, string? adminResponse, string? bankTrackingNo, CancellationToken ct = default)
        {
            // برای انتخاب ردیف قفل‌شونده، ابتدا فقط شناسه همکار را می‌خوانیم؛ وضعیت در تراکنش دوباره بررسی می‌شود.
            var requestInfo = await _db.WithdrawalRequests.AsNoTracking()
                .Where(r => r.Id == requestId)
                .Select(r => new { r.PartnerId })
                .FirstOrDefaultAsync(ct);
            if (requestInfo is null) return WalletResult.Fail("درخواست یافت نشد.");

            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            if (!await LockPartnerAsync(requestInfo.PartnerId, ct))
                return WalletResult.Fail("همکار یافت نشد.");

            var wr = await _db.WithdrawalRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct);
            if (wr is null) return WalletResult.Fail("درخواست یافت نشد.");
            if (wr.Status != WalletTxStatus.Pending) return WalletResult.Fail("این درخواست قبلاً تصمیم‌گیری شده است.");

            // قفل همکار همه‌ی درج‌های دفتر و درخواست‌های برداشت را برای این همکار پشت سر هم اجرا می‌کند.
            var balanceBefore = await GetBalanceAsync(wr.PartnerId, ct);
            var otherLocked = await PendingWithdrawalTotalAsync(wr.PartnerId, ct, wr.Id);
            if (wr.Amount > balanceBefore - otherLocked)
                return WalletResult.Fail("موجودی کافی نیست (احتمالاً درخواست‌های دیگری تایید شده‌اند). این درخواست را رد کنید.");

            try
            {
                var now = DateTime.Now;
                var adminNote = string.IsNullOrWhiteSpace(adminResponse) ? null : adminResponse.Trim();
                var tx = new WalletTransaction
                {
                    PartnerId = wr.PartnerId,
                    Type = WalletTxType.Withdrawal,
                    Status = WalletTxStatus.Confirmed,
                    Amount = -wr.Amount,
                    Description = $"برداشت تاییدشده — واریز به شبا {wr.DestinationIban}" +
                                  (adminNote is null ? "" : $" — {adminNote}"),
                    Reference = $"WR#{wr.Id}",
                    WithdrawalRequestId = wr.Id,
                    BankTrackingNo = string.IsNullOrWhiteSpace(bankTrackingNo) ? null : bankTrackingNo.Trim(),
                    PaidAt = now,
                    BalanceAfter = balanceBefore - wr.Amount,
                };

                // EF ابتدا تراکنش را درج و سپس FK درخواست را در همان Save و همان تراکنش DB تنظیم می‌کند.
                wr.WalletTransaction = tx;
                wr.Status = WalletTxStatus.Confirmed;
                wr.AdminResponse = adminNote;
                wr.DecidedAt = now;
                _db.WalletTransactions.Add(tx);
                await _db.SaveChangesAsync(ct);

                var balance = await GetBalanceAsync(wr.PartnerId, ct);
                await transaction.CommitAsync(ct);
                return WalletResult.Ok($"برداشت {wr.Amount:N0} تومان تایید و از کیف پول کسر شد.", balance);
            }
            catch (DbUpdateConcurrencyException)
            {
                return WalletResult.Fail("این درخواست همزمان توسط کاربر دیگری تصمیم‌گیری شد؛ صفحه را تازه کنید.");
            }
        }

        /// <summary>رد درخواست برداشت — بدون هیچ تراکنش مالی؛ فقط وضعیت درخواست</summary>
        public async Task<PortalResult> RejectWithdrawalAsync(int requestId, string? adminResponse, CancellationToken ct = default)
        {
            var requestInfo = await _db.WithdrawalRequests.AsNoTracking()
                .Where(r => r.Id == requestId)
                .Select(r => new { r.PartnerId })
                .FirstOrDefaultAsync(ct);
            if (requestInfo is null) return WalletResult.Fail("درخواست یافت نشد.");

            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            if (!await LockPartnerAsync(requestInfo.PartnerId, ct))
                return WalletResult.Fail("همکار یافت نشد.");

            var wr = await _db.WithdrawalRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct);
            if (wr is null) return WalletResult.Fail("درخواست یافت نشد.");
            if (wr.Status != WalletTxStatus.Pending) return WalletResult.Fail("این درخواست قبلاً تصمیم‌گیری شده است.");

            try
            {
                wr.Status = WalletTxStatus.Rejected;
                wr.AdminResponse = string.IsNullOrWhiteSpace(adminResponse) ? null : adminResponse.Trim();
                wr.DecidedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);

                var available = await GetAvailableAsync(wr.PartnerId, ct);
                await transaction.CommitAsync(ct);
                return WalletResult.Ok("درخواست رد شد؛ مبلغ به موجودی قابل‌برداشت برگشت.", available);
            }
            catch (DbUpdateConcurrencyException)
            {
                return WalletResult.Fail("این درخواست همزمان توسط کاربر دیگری تصمیم‌گیری شد؛ صفحه را تازه کنید.");
            }
        }

        // ---------- هسته دفتر کل ----------

        /// <summary>درج یک تراکنش Confirmed با محاسبه BalanceAfter</summary>
        private async Task<WalletResult> ExecuteLedgerAsync(int partnerId, WalletTxType type, decimal signedAmount,
            string description, string? reference, string? bankTrackingNo, CancellationToken ct)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            if (!await LockPartnerAsync(partnerId, ct))
                return WalletResult.Fail("همکار یافت نشد.");

            try
            {
                var result = await AddLedgerAsync(partnerId, type, signedAmount, description, reference, bankTrackingNo, ct);
                await transaction.CommitAsync(ct);
                return result;
            }
            catch (DbUpdateConcurrencyException)
            {
                return WalletResult.Fail("اطلاعات کیف پول همزمان تغییر کرد؛ دوباره تلاش کنید.");
            }
        }

        /// <summary>درج تراکنش داخل تراکنش بازِ کیف پول</summary>
        private async Task<WalletResult> AddLedgerAsync(int partnerId, WalletTxType type, decimal signedAmount,
            string description, string? reference, string? bankTrackingNo, CancellationToken ct)
        {
            var tx = new WalletTransaction
            {
                PartnerId = partnerId,
                Type = type,
                Status = WalletTxStatus.Confirmed,
                Amount = signedAmount,
                Description = description.Trim(),
                Reference = reference,
                BankTrackingNo = bankTrackingNo,
                PaidAt = type == WalletTxType.Deposit ? DateTime.Now : null,
            };
            tx.BalanceAfter = await ProjectedBalanceAsync(partnerId, signedAmount, ct);

            _db.WalletTransactions.Add(tx);
            await _db.SaveChangesAsync(ct);

            var balance = await GetBalanceAsync(partnerId, ct);
            return WalletResult.Ok("ثبت شد.", balance);
        }

        /// <summary>قفل ردیف پایدار همکار؛ همه‌ی عملیات مالی این سرویس همین قفل را می‌گیرند.</summary>
        private async Task<bool> LockPartnerAsync(int partnerId, CancellationToken ct) =>
            await _db.Partners
                .FromSqlInterpolated($"SELECT * FROM [Partners] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {partnerId} AND [IsDeleted] = 0")
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(ct);

        private async Task<decimal> PendingWithdrawalTotalAsync(int partnerId, CancellationToken ct, int? excludedRequestId = null) =>
            await _db.WithdrawalRequests
                .Where(r => r.PartnerId == partnerId
                         && r.Status == WalletTxStatus.Pending
                         && (!excludedRequestId.HasValue || r.Id != excludedRequestId.Value))
                .SumAsync(r => (decimal?)r.Amount, ct) ?? 0m;

        /// <summary>موجودی پیش‌بینی‌شده پس از افزودن یک مقدار (برای ستون BalanceAfter)</summary>
        private async Task<decimal> ProjectedBalanceAsync(int partnerId, decimal delta, CancellationToken ct) =>
            await GetBalanceAsync(partnerId, ct) + delta;
    }
}

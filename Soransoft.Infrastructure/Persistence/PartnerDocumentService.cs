using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Soransoft.Application.Interfaces;
using Soransoft.Domain.Entities;

namespace Soransoft.Infrastructure.Persistence
{
    /// <summary>سرویس اسناد پرتال همکاران — آپلود/جایگزینی فایل قرارداد (PDF)</summary>
    public class PartnerDocumentService : IPartnerDocumentService
    {
        private readonly SoransoftDbContext _db;
        private readonly IFileStorage _storage;
        private readonly ILogger<PartnerDocumentService> _logger;

        public PartnerDocumentService(
            SoransoftDbContext db,
            IFileStorage storage,
            ILogger<PartnerDocumentService> logger)
        {
            _db = db;
            _storage = storage;
            _logger = logger;
        }

        /// <summary>آپلود/جایگزینی فایل قرارداد (PDF) و حذف فایل قبلی</summary>
        public async Task<PortalResult> UploadContractFileAsync(int contractId, IFormFile? file, CancellationToken ct = default)
        {
            if (file is null || file.Length == 0)
                return PortalResult.Fail("فایلی انتخاب نشده است.");

            var contract = await _db.PartnerContracts.FirstOrDefaultAsync(c => c.Id == contractId, ct);
            if (contract is null)
                return PortalResult.Fail("قرارداد یافت نشد.");

            try
            {
                var path = await _storage.SaveDocumentAsync(file, "partners/contracts", ct);
                var previousPath = contract.ContractFile;
                contract.ContractFile = path;
                try
                {
                    await _db.SaveChangesAsync(ct);
                }
                catch
                {
                    await _storage.DeleteAsync(path, ct);
                    throw;
                }

                if (!string.IsNullOrWhiteSpace(previousPath) && !string.Equals(previousPath, path, StringComparison.Ordinal))
                {
                    try { await _storage.DeleteAsync(previousPath, ct); }
                    catch (Exception ex) { _logger.LogWarning(ex, "فایل قبلی قرارداد حذف نشد {ContractId}", contractId); }
                }
                return PortalResult.Ok("فایل قرارداد با موفقیت بارگذاری شد.");
            }
            catch (ArgumentException) { return PortalResult.Fail("فایل انتخاب‌شده معتبر نیست."); }
            catch (InvalidOperationException) { return PortalResult.Fail("فایل انتخاب‌شده معتبر نیست."); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در بارگذاری فایل قرارداد {ContractId}", contractId);
                return PortalResult.Fail("بارگذاری فایل انجام نشد. دوباره تلاش کنید.");
            }
        }

        /// <summary>ذخیره PDF قرارداد همکاری — فقط PDF تا ۲۰ مگابایت</summary>
        public async Task<string?> SaveAgreementFileAsync(IFormFile file, CancellationToken ct = default)
        {
            if (file is null || file.Length == 0) return null;
            if (file.Length > 20 * 1024 * 1024) return null;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".pdf") return null;

            try
            {
                return await _storage.SaveDocumentAsync(file, "partners/agreements", ct);
            }
            catch (ArgumentException) { return null; }
            catch (InvalidOperationException) { return null; }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در بارگذاری قرارداد همکاری");
                return null;
            }
        }

        /// <summary>ذخیره مستند مالی (فیش واریز) — PDF یا تصویر تا ۲۰ مگابایت</summary>
        public async Task<string?> SaveDocumentFileAsync(IFormFile file, CancellationToken ct = default)
        {
            if (file is null || file.Length == 0 || file.Length > 20 * 1024 * 1024) return null;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(ext)) return null;

            try { return await _storage.SaveDocumentAsync(file, "partners/wallet-docs", ct); }
            catch (ArgumentException) { return null; }
            catch (InvalidOperationException) { return null; }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در بارگذاری سند مالی");
                return null;
            }
        }

        /// <summary>ذخیره سند/رسید مرحله پرداخت (PDF یا تصویر تا ۲۰ مگابایت)</summary>
        public async Task<string?> SaveStageDocumentAsync(IFormFile file, CancellationToken ct = default)
        {
            if (file is null || file.Length == 0 || file.Length > 20 * 1024 * 1024) return null;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(ext)) return null;

            try { return await _storage.SaveDocumentAsync(file, "partners/contract-stages", ct); }
            catch (ArgumentException) { return null; }
            catch (InvalidOperationException) { return null; }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در بارگذاری سند مرحله قرارداد");
                return null;
            }
        }

        /// <summary>حذف فایل سند از دیسک (خطا نادیده — حذف فایل نباید عملیات اصلی را بشکند)</summary>
        public async Task DeleteFileAsync(string? relativePath, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;
            try { await _storage.DeleteAsync(relativePath, ct); }
            catch { /* فایل از قبل حذف شده یا دسترسی ندارد */ }
        }
    }
}

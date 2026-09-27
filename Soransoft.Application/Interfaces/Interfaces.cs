using Microsoft.AspNetCore.Http;
using Soransoft.Domain.Entities;
using System.Linq.Expressions;

namespace Soransoft.Application.Interfaces
{
    /// <summary>واحد کار</summary>
    public interface IUnitOfWork : IAsyncDisposable
    {
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }

    /// <summary>Repository عمومی</summary>
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<List<T>> ListAsync(CancellationToken ct = default);
        Task<List<T>> ListAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);
        Task AddAsync(T entity, CancellationToken ct = default);
        Task UpdateAsync(T entity, CancellationToken ct = default);
        Task DeleteAsync(T entity, CancellationToken ct = default);
        Task SaveAsync(CancellationToken ct = default);
    }

    /// <summary>هش رمز عبور</summary>
    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string password, string hash);
    }

    /// <summary>ذخیره‌سازی فایل</summary>
    public interface IFileStorage
    {
        Task<string> SaveImageAsync(IFormFile file, string folder, CancellationToken ct = default);
        /// <summary>ذخیره سند (PDF و...) بدون بهینه‌سازی تصویر</summary>
        Task<string> SaveDocumentAsync(IFormFile file, string folder, CancellationToken ct = default);
        Task DeleteAsync(string? relativePath, CancellationToken ct = default);
    }

    /// <summary>تنظیمات سایت</summary>
    public interface ISiteSettingService
    {
        Task<string> GetAsync(string key, CancellationToken ct = default);
        Task<IDictionary<string, string>> GetAllAsync(CancellationToken ct = default);
        Task<IDictionary<string, SiteSetting>> GetAllEntitiesAsync(CancellationToken ct = default);
        Task UpdateAsync(IEnumerable<(string Key, string Value)> updates, CancellationToken ct = default);
    }

    /// <summary>زمینه‌ی سایت</summary>
    public interface ISiteContext
    {
        Task<string> GetSettingAsync(string key, CancellationToken ct = default);
    }

    /// <summary>نتیجه عمومی عملیات پرتال همکاران</summary>
    public class PortalResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public static PortalResult Ok(string message = "با موفقیت انجام شد") => new() { Success = true, Message = message };
        public static PortalResult Fail(string message) => new() { Success = false, Message = message };
    }

    /// <summary>سرویس اسناد پرتال همکاران — آپلود فایل قرارداد</summary>
    public interface IPartnerDocumentService
    {
        Task<PortalResult> UploadContractFileAsync(int contractId, IFormFile? file, CancellationToken ct = default);
        /// <summary>ذخیره PDF قرارداد همکاری (فقط PDF تا ۲۰MB) — مسیر نسبی یا null در صورت نامعتبر بودن</summary>
        Task<string?> SaveAgreementFileAsync(IFormFile file, CancellationToken ct = default);
        /// <summary>ذخیره مستند مالی (PDF یا تصویر فیش واریز تا ۲۰MB)</summary>
        Task<string?> SaveDocumentFileAsync(IFormFile file, CancellationToken ct = default);
        /// <summary>ذخیره سند/رسید مرحله پرداخت (PDF یا تصویر تا ۲۰MB)</summary>
        Task<string?> SaveStageDocumentAsync(IFormFile file, CancellationToken ct = default);
        /// <summary>حذف فایل سند از دیسک</summary>
        Task DeleteFileAsync(string? relativePath, CancellationToken ct = default);
    }

    /// <summary>سرویس کیف پول همکاران — دفتر کل فقط-الحاقی</summary>
    public interface IWalletService
    {
        Task<decimal> GetBalanceAsync(int partnerId, CancellationToken ct = default);
        /// <summary>موجودی قطعی منهای درخواست‌های برداشت در انتظار تایید</summary>
        Task<decimal> GetAvailableAsync(int partnerId, CancellationToken ct = default);
        Task<PortalResult> DepositAsync(int partnerId, decimal amount, string description, string? reference = null, string? bankTrackingNo = null, int? contractId = null, CancellationToken ct = default);
        Task<PortalResult> AdjustAsync(int partnerId, decimal signedAmount, string description, CancellationToken ct = default);
        Task<PortalResult> RequestWithdrawalAsync(int partnerId, decimal amount, string? note, string iban, string bank, string holder, CancellationToken ct = default);
        Task<PortalResult> ApproveWithdrawalAsync(int requestId, string? adminResponse, string? bankTrackingNo, CancellationToken ct = default);
        Task<PortalResult> RejectWithdrawalAsync(int requestId, string? adminResponse, CancellationToken ct = default);
    }

    /// <summary>سرویس پرتال همکاران — منطق مشترک پنل همکار و پنل ادمین</summary>
    public interface IPartnerPortalService
    {
        // ---------- لیدها ----------
        Task<PortalResult> CreateLeadAsync(int partnerId, Lead lead, CancellationToken ct = default);
        Task<PortalResult> UpdateLeadStageAsync(int leadId, LeadStage newStage, string? note, CancellationToken ct = default);
        /// <summary>تایید لید و تبدیل به قرارداد در انتظار تایید (می‌تواند نرخ پورسانت اولیه بگیرد)</summary>
        Task<PartnerContract?> ConvertLeadToContractAsync(int leadId, long totalAmount, string title, CancellationToken ct = default);

        // ---------- قراردادها و مراحل پرداخت ----------
        /// <summary>تعریف/ویرایش یک مرحله پرداخت روی قرارداد (شماره‌گذاری خودکار در صورت نبود)</summary>
        Task<PortalResult> SavePaymentStageAsync(ContractPaymentStage stage, bool isNew, CancellationToken ct = default);
        /// <summary>حذف مرحله پرداخت (فقط پیش از پرداخت)</summary>
        Task<PortalResult> DeletePaymentStageAsync(int stageId, CancellationToken ct = default);
        Task<PortalResult> UpdatePaymentStageStatusAsync(int stageId, PaymentStageStatus status, string? adminNote, CancellationToken ct = default);
        /// <summary>مجموع هزینه‌های شخص ثالث یک قرارداد (کسر‌شونده از مبنای پورسانت)</summary>
        Task<long> GetThirdPartyCostTotalAsync(int contractId, CancellationToken ct = default);
        /// <summary>مبنای خالص پورسانت = مبلغ مبنای قرارداد منهای هزینه‌های شخص ثالث (و در حالت مرحله‌ای، مراحل پرداخت‌شده)</summary>
        Task<long> GetCommissionBaseAsync(CommissionRate rate, CancellationToken ct = default);
        /// <summary>مبلغ پورسانت قابل‌پرداخت برای یک قرارداد/همکار بر اساس نرخ تعریف‌شده (خالص پس از هزینه شخص ثالث)</summary>
        Task<decimal> ComputeCommissionAsync(CommissionRate rate, CancellationToken ct = default);

        // ---------- تسک‌ها ----------
        Task<PortalResult> ChangeTaskStatusAsync(int taskId, Soransoft.Domain.Entities.TaskStatus newStatus, CancellationToken ct = default);

        // ---------- اطلاعیه‌ها ----------
        /// <summary>ارسال اطلاعیه و بازگرداندن تعداد گیرندگان</summary>
        Task<int> SendAnnouncementAsync(Announcement announcement, CancellationToken ct = default);
    }
}

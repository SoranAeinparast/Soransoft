using Soransoft.Application.ViewModels;

namespace Soransoft.Application.Interfaces
{
    /// <summary>کتابخانه رسانه — مرور و حذف تصاویر آپلودشده در wwwroot/uploads</summary>
    public interface IMediaLibraryService
    {
        /// <summary>لیست فایل‌های یک پوشه (null = کل پوشه uploads)</summary>
        Task<IReadOnlyList<MediaFileItem>> GetFilesAsync(string? folder = null, CancellationToken ct = default);

        /// <summary>زیرپوشه‌های دارای فایل داخل uploads</summary>
        Task<IReadOnlyList<MediaFolder>> GetFoldersAsync(CancellationToken ct = default);

        /// <summary>حذف امن یک فایل بر اساس مسیر نسبی</summary>
        Task DeleteAsync(string relativePath, CancellationToken ct = default);

        /// <summary>برای هر مسیر تصویر، فهرست محتواهایی که از آن استفاده می‌کنند (مقاله/سرویس/…)</summary>
        Task<IDictionary<string, IReadOnlyList<MediaUsageRef>>> GetUsageAsync(
            IEnumerable<string> paths, CancellationToken ct = default);

        /// <summary>نمای کلی فضای ذخیره‌سازی — آمار هر پوشه + فایل‌های بدون ارجاع</summary>
        Task<StorageOverview> GetStorageOverviewAsync(CancellationToken ct = default);

        /// <summary>حذف همه فایل‌های بدون ارجاع؛ خروجی: (تعداد حذف‌شده، بایت آزادشده)</summary>
        Task<(int DeletedCount, long FreedBytes)> CleanupOrphansAsync(CancellationToken ct = default);
    }
}

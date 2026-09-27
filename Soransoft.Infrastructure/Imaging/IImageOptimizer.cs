using Soransoft.Application.Interfaces;

namespace Soransoft.Infrastructure.Imaging
{
    /// <summary>نتیجه بهینه‌سازی یک تصویر</summary>
    public record ImageOptimizeResult(string RelativePath, long Bytes);

    /// <summary>بهینه‌سازی تصاویر آپلودی: تغییر اندازه + تبدیل به WebP</summary>
    public interface IImageOptimizer
    {
        /// <summary>
        /// فایل تصویر را (در صورت لزوم) کوچک و با فرمت WebP ذخیره می‌کند.
        /// SVG و GIF بدون تغییر ذخیره می‌شوند (قابل انیمیشن/وکتورند).
        /// </summary>
        /// <returns>مسیر نسبی فایل نهایی + حجم نهایی</returns>
        Task<ImageOptimizeResult> SaveOptimizedAsync(
            Stream source, string originalFileName, string folder, CancellationToken ct = default);
    }
}

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using Soransoft.Application.Interfaces;
using Soransoft.Infrastructure.Storage;

namespace Soransoft.Infrastructure.Imaging
{
    /// <summary>
    /// بهینه‌ساز تصویر با SkiaSharp:
    /// — تصاویر بزرگ‌تر از ۱۹۲۰px متناسب کوچک می‌شوند (هیچ‌وقت بالاسکیل نمی‌کند)
    /// — خروجی WebP با کیفیت ۸۰ (حجم معمولاً چند برابر کمتر از JPG/PNG)
    /// — SVG (وکتور) و GIF (متحرک) بدون تغییر ذخیره می‌شوند
    /// — چرخش EXIF گوشی‌ها روی خروجی اعمال می‌شود
    /// </summary>
    public class SkiaImageOptimizer : IImageOptimizer
    {
        /// <summary>بیشترین بُعد مجاز (طول یا عرض)</summary>
        public const int MaxDimension = 1920;

        /// <summary>کیفیت انکود WebP</summary>
        public const int WebpQuality = 80;

        private static readonly HashSet<string> PassthroughExt = new(StringComparer.OrdinalIgnoreCase)
            { ".svg", ".gif" };

        private static readonly HashSet<string> OptimizableExt = new(StringComparer.OrdinalIgnoreCase)
            { ".jpg", ".jpeg", ".png", ".webp", ".bmp" };

        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SkiaImageOptimizer> _logger;

        public SkiaImageOptimizer(IWebHostEnvironment env, IConfiguration configuration, ILogger<SkiaImageOptimizer> logger)
        {
            _env = env;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ImageOptimizeResult> SaveOptimizedAsync(
            Stream source, string originalFileName, string folder, CancellationToken ct = default)
        {
            if (source is null || !source.CanRead)
                throw new ArgumentException("استریم فایل خواندنی نیست.");

            var ext = Path.GetExtension(originalFileName ?? "").ToLowerInvariant();
            var safeFolder = folder.Trim('/').Replace('\\', '/');

            // SVG/GIF: بدون تغییر ذخیره می‌شوند (وکتور/متحرک)
            if (PassthroughExt.Contains(ext))
                return await WritePassthroughAsync(source, safeFolder, ext, ct);

            if (!OptimizableExt.Contains(ext))
                throw new InvalidOperationException("فرمت تصویر مجاز نیست.");

            // رمزگشایی — فایل خراب/ناتمام اینجا رد می‌شود
            using var codec = SKCodec.Create(source);
            if (codec is null)
                throw new InvalidOperationException("فایل تصویر قابل خواندن نیست (خراب است).");

            var width = codec.Info.Width;
            var height = codec.Info.Height;
            var longest = Math.Max(width, height);
            var needsResize = longest > MaxDimension;

            // نمونه‌گیری بومی از کدک (JPEG تا ۱/۸ بدون decode کامل) — نزدیک‌ترین اندازه پشتیبانی‌شده
            var scale = needsResize ? MaxDimension / (float)longest : 1f;
            var scaled = codec.GetScaledDimensions(scale);
            int targetW = Math.Max(1, scaled.Width);
            int targetH = Math.Max(1, scaled.Height);

            var info = new SKImageInfo(targetW, targetH, codec.Info.ColorType, codec.Info.AlphaType);
            SKBitmap bitmap;
            using (var decode = SKBitmap.Decode(codec, info))
            {
                if (decode is null)
                    throw new InvalidOperationException("رمزگشایی تصویر ناموفق بود.");

                if (decode.Width == targetW && decode.Height == targetH)
                {
                    bitmap = decode.Copy() ?? throw new InvalidOperationException("کپی تصویر ناموفق بود.");
                }
                else
                {
                    // کدک نمونه‌گیری نکرد (مثل PNG) — کشیدن روی بوم با نمونه‌گیری باکیفیت
                    bitmap = new SKBitmap(targetW, targetH);
                    using var canvas = new SKCanvas(bitmap);
                    using var paint = new SKPaint { IsAntialias = true };
                    using var srcImage = SKImage.FromBitmap(decode);
                    canvas.DrawImage(srcImage, new SKRect(0, 0, targetW, targetH), SKSamplingOptions.Default, paint);
                }
            }

            // اعمال جهت‌گیری EXIF؛ اگر چرخشی انجام شد بیت‌مپ جدید ساخته شده است
            var oriented = ApplyOrigin(bitmap, codec.EncodedOrigin);
            if (!ReferenceEquals(oriented, bitmap)) bitmap.Dispose();

            using (oriented)
            {
                using var image = SKImage.FromBitmap(oriented);
                using var data = image.Encode(SKEncodedImageFormat.Webp, WebpQuality);
                if (data is null || data.Size == 0)
                    throw new InvalidOperationException("انکود تصویر ناموفق بود.");

                var fileName = $"{Guid.NewGuid():N}.webp";
                 var folderPath = Path.Combine(PersistentPublicMediaStorage.ResolveRoot(_env, _configuration), safeFolder);
                Directory.CreateDirectory(folderPath);

                await using var output = File.Create(Path.Combine(folderPath, fileName));
                await data.AsStream().CopyToAsync(output, ct);

                var relative = $"/uploads/{safeFolder}/{fileName}";
                _logger.LogInformation(
                    "تصویر بهینه شد: {Original} → {Path} ({W}×{H}, {Bytes} بایت)",
                    originalFileName, relative, oriented.Width, oriented.Height, data.Size);

                return new ImageOptimizeResult(relative, data.Size);
            }
        }

        /// <summary>اعمال چرخش EXIF (عکس‌های گوشی) — در نبود چرخش، همان بیت‌مپ برگردانده می‌شود</summary>
        private static SKBitmap ApplyOrigin(SKBitmap bitmap, SKEncodedOrigin origin)
        {
            var rotate = origin switch
            {
                SKEncodedOrigin.TopRight or SKEncodedOrigin.BottomRight => 180,
                SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightBottom => 90,
                SKEncodedOrigin.LeftBottom or SKEncodedOrigin.RightTop => 270,
                _ => 0
            };

            if (rotate == 0) return bitmap;

            var swapped = rotate is 90 or 270;
            var result = new SKBitmap(swapped ? bitmap.Height : bitmap.Width,
                                      swapped ? bitmap.Width : bitmap.Height);
            using var canvas = new SKCanvas(result);
            using var paint = new SKPaint { IsAntialias = true };

            using var srcImage = SKImage.FromBitmap(bitmap);
            canvas.Translate(result.Width / 2f, result.Height / 2f);
            canvas.RotateDegrees(rotate);
            canvas.DrawImage(srcImage,
                new SKRect(-bitmap.Width / 2f, -bitmap.Height / 2f, bitmap.Width / 2f, bitmap.Height / 2f),
                SKSamplingOptions.Default, paint);

            return result;
        }

        private async Task<ImageOptimizeResult> WritePassthroughAsync(
            Stream source, string safeFolder, string ext, CancellationToken ct)
        {
            var fileName = $"{Guid.NewGuid():N}{ext}";
             var folderPath = Path.Combine(PersistentPublicMediaStorage.ResolveRoot(_env, _configuration), safeFolder);
            Directory.CreateDirectory(folderPath);

            await using var output = File.Create(Path.Combine(folderPath, fileName));
            await source.CopyToAsync(output, ct);

            return new ImageOptimizeResult($"/uploads/{safeFolder}/{fileName}", output.Length);
        }

    }
}

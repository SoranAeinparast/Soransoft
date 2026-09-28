using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using SkiaSharp;
using Soransoft.Domain.Entities;
using Soransoft.Infrastructure.Persistence;
using Soransoft.Web.Services;
using System.Globalization;
using PartnerEntity = Soransoft.Domain.Entities.Partner;

namespace Soransoft.Web.Areas.Partner.Controllers
{
    /// <summary>صدور کارت تصویری همکار و درج QR استعلام اصالت.</summary>
    public sealed class IdentityCardController : PartnerBaseController
    {
        private readonly SoransoftDbContext _db;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public IdentityCardController(SoransoftDbContext db, IWebHostEnvironment environment, IConfiguration configuration)
        {
            _db = db;
            _environment = environment;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Download(CancellationToken ct)
        {
            var partner = await _db.Partners.AsNoTracking().FirstOrDefaultAsync(p => p.Id == PartnerId, ct);
            if (partner is null) return Forbidden();

            var templatePath = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "card-templates", "partner-id-card.png");
            if (!System.IO.File.Exists(templatePath))
            {
                ViewData["TemplatePath"] = "Soransoft.Web/wwwroot/card-templates/partner-id-card.png";
                return View("TemplateMissing");
            }

            var personnelCode = PersonnelCode(partner.Id);
            var verificationUrl = VerificationUrl(personnelCode);
            var output = RenderCard(partner, personnelCode, verificationUrl, templatePath);
            return File(output, "image/png", $"soransoft-id-card-{personnelCode}.png");
        }

        private byte[] RenderCard(PartnerEntity partner, string personnelCode, string verificationUrl, string templatePath)
        {
            using var template = SKBitmap.Decode(templatePath) ?? throw new InvalidOperationException("قالب کارت قابل خواندن نیست.");
            using var canvas = new SKCanvas(template);
            using var textPaint = new SKPaint { Color = new SKColor(0x00, 0x1D, 0x43), IsAntialias = true };
            using var regularTypeface = LoadTypeface("Vazirmatn-Regular");
            using var boldTypeface = LoadTypeface("Vazirmatn-Bold");
            using var nameFont = new SKFont(boldTypeface, 31);
            using var roleFont = new SKFont(regularTypeface, 27);
            using var valueFont = new SKFont(regularTypeface, 18);

            var width = template.Width;
            var height = template.Height;
            var scaleX = width / 528f;
            var scaleY = height / 802f;
            var photoBox = CardRect(52, 229, 224, 410, scaleX, scaleY);
            var qrBox = CardRect(340, 584, 475, 718, scaleX, scaleY);

            var photoPath = ResolvePrivatePath(partner.PersonalPhotoPath);
            if (photoPath is not null && System.IO.File.Exists(photoPath))
            {
                using var photo = SKBitmap.Decode(photoPath);
                if (photo is not null) DrawImageCover(canvas, photo, photoBox);
            }

            var rightText = 491 * scaleX;
            DrawText(canvas, textPaint, nameFont, PersianTextShaper.Shape(partner.FullName), rightText, 295 * scaleY, SKTextAlign.Right);
            DrawText(canvas, textPaint, roleFont, PersianTextShaper.Shape(RoleTitle(partner.Role)), rightText, 340 * scaleY, SKTextAlign.Right);

            var valueX = 207 * scaleX;
            DrawText(canvas, textPaint, valueFont, personnelCode, valueX, 468 * scaleY, SKTextAlign.Left);
            DrawText(canvas, textPaint, valueFont, partner.Mobile ?? "ثبت نشده", valueX, 503 * scaleY, SKTextAlign.Left);
            DrawText(canvas, textPaint, valueFont, partner.Email ?? "ثبت نشده", valueX, 541 * scaleY, SKTextAlign.Left);

            using var qrData = new QRCodeGenerator().CreateQrCode(verificationUrl, QRCodeGenerator.ECCLevel.Q);
            var qrBytes = new PngByteQRCode(qrData).GetGraphic(12);
            using var qr = SKBitmap.Decode(qrBytes);
            if (qr is not null) canvas.DrawBitmap(qr, qrBox);

            using var image = SKImage.FromBitmap(template);
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            return encoded.ToArray();
        }

        private SKTypeface LoadTypeface(string fontName)
        {
            var fontsRoot = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "fonts");
            var encoded = string.Concat(Directory.GetFiles(fontsRoot, $"{fontName}.ttf.b64.part*")
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(System.IO.File.ReadAllText));
            if (string.IsNullOrWhiteSpace(encoded))
                throw new InvalidOperationException($"فایل فونت {fontName} در پروژه پیدا نشد.");

            using var data = SKData.CreateCopy(Convert.FromBase64String(encoded));
            return SKTypeface.FromData(data) ?? SKTypeface.FromFamilyName("DejaVu Sans") ?? throw new InvalidOperationException("فونت کارت شناسایی قابل بارگذاری نیست.");
        }

        private static void DrawText(SKCanvas canvas, SKPaint paint, SKFont font, string? text, float x, float y, SKTextAlign align)
        {
            if (!string.IsNullOrWhiteSpace(text)) canvas.DrawText(text.Trim(), x, y, align, font, paint);
        }

        private static SKRect CardRect(float left, float top, float right, float bottom, float scaleX, float scaleY) =>
            new(left * scaleX, top * scaleY, right * scaleX, bottom * scaleY);

        private static void DrawImageCover(SKCanvas canvas, SKBitmap image, SKRect destination)
        {
            var sourceRatio = image.Width / (float)image.Height;
            var destinationRatio = destination.Width / destination.Height;
            SKRect source;
            if (sourceRatio > destinationRatio)
            {
                var sourceWidth = image.Height * destinationRatio;
                var left = (image.Width - sourceWidth) / 2;
                source = new SKRect(left, 0, left + sourceWidth, image.Height);
            }
            else
            {
                var sourceHeight = image.Width / destinationRatio;
                var top = (image.Height - sourceHeight) / 2;
                source = new SKRect(0, top, image.Width, top + sourceHeight);
            }

            canvas.DrawBitmap(image, source, destination);
        }

        private string VerificationUrl(string personnelCode) =>
            $"{(_configuration["CardVerificationBaseUrl"] ?? "https://verify.soransoft.ir").TrimEnd('/')}/card/{Uri.EscapeDataString(personnelCode)}";

        private string? ResolvePrivatePath(string? storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath)) return null;
            const string marker = "?path=";
            var index = storedPath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return null;
            var relative = Uri.UnescapeDataString(storedPath[(index + marker.Length)..].Split('&', 2)[0]).Replace('\\', '/').Trim('/');
            if (string.IsNullOrWhiteSpace(relative) || relative.Split('/').Any(x => x is "." or ".." || x.Contains(':') || x.Contains('\0'))) return null;
            var root = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "App_Data", "PrivateDocuments"));
            var full = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            return full.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? full : null;
        }

        private static string PersonnelCode(int id) => $"P-{id.ToString("D5", CultureInfo.InvariantCulture)}";

        private static string RoleTitle(PartnerRole role) => role switch
        {
            PartnerRole.Sales => "کارشناس فروش",
            PartnerRole.SalesManager => "مدیر فروش",
            PartnerRole.Developer => "توسعه دهنده",
            PartnerRole.TechManager => "مدیر فنی",
            _ => "همکار سوران‌سافت",
        };

    }
}

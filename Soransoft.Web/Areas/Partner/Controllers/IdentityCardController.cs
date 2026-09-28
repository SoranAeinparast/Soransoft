using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using SkiaSharp;
using Soransoft.Domain.Entities;
using Soransoft.Infrastructure.Persistence;
using System.Globalization;

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

            var templatePath = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "card-templates", "partner-id-card.jpg");
            if (!System.IO.File.Exists(templatePath))
            {
                ViewData["TemplatePath"] = "Soransoft.Web/wwwroot/card-templates/partner-id-card.jpg";
                return View("TemplateMissing");
            }

            var agreementEndDate = await _db.CooperationAgreements.AsNoTracking()
                .Where(a => a.PartnerId == partner.Id && !a.IsDeleted)
                .OrderByDescending(a => a.EndDate ?? DateTime.MaxValue)
                .ThenByDescending(a => a.CreatedAt)
                .Select(a => a.EndDate)
                .FirstOrDefaultAsync(ct);

            var personnelCode = PersonnelCode(partner.Id);
            var verificationUrl = VerificationUrl(personnelCode);
            var output = RenderCard(partner, personnelCode, agreementEndDate, verificationUrl, templatePath);
            return File(output, "image/jpeg", $"soransoft-id-card-{personnelCode}.jpg");
        }

        private byte[] RenderCard(Partner partner, string personnelCode, DateTime? agreementEndDate, string verificationUrl, string templatePath)
        {
            using var template = SKBitmap.Decode(templatePath) ?? throw new InvalidOperationException("قالب کارت قابل خواندن نیست.");
            using var canvas = new SKCanvas(template);
            using var textPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Arial") };

            var width = template.Width;
            var height = template.Height;
            var photoBox = new SKRect(width * .07f, height * .18f, width * .29f, height * .78f);
            var qrBox = new SKRect(width * .77f, height * .62f, width * .96f, height * .91f);

            var photoPath = ResolvePrivatePath(partner.PersonalPhotoPath);
            if (photoPath is not null && System.IO.File.Exists(photoPath))
            {
                using var photo = SKBitmap.Decode(photoPath);
                if (photo is not null) canvas.DrawBitmap(photo, photoBox);
            }

            textPaint.TextSize = height * .055f;
            DrawText(canvas, textPaint, partner.FullName, width * .34f, height * .32f);
            textPaint.TextSize = height * .038f;
            DrawText(canvas, textPaint, $"کد پرسنلی: {personnelCode}", width * .34f, height * .42f);
            DrawText(canvas, textPaint, $"تماس: {partner.Mobile ?? "ثبت نشده"}", width * .34f, height * .49f);
            DrawText(canvas, textPaint, $"ایمیل: {partner.Email ?? "ثبت نشده"}", width * .34f, height * .56f);
            DrawText(canvas, textPaint, $"اعتبار تا: {PersianDate(agreementEndDate)}", width * .34f, height * .63f);

            using var qrData = new QRCodeGenerator().CreateQrCode(verificationUrl, QRCodeGenerator.ECCLevel.Q);
            var qrBytes = new PngByteQRCode(qrData).GetGraphic(12);
            using var qr = SKBitmap.Decode(qrBytes);
            if (qr is not null) canvas.DrawBitmap(qr, qrBox);

            using var image = SKImage.FromBitmap(template);
            using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, 94);
            return encoded.ToArray();
        }

        private static void DrawText(SKCanvas canvas, SKPaint paint, string text, float x, float y) => canvas.DrawText(text, x, y, paint);

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

        private static string PersianDate(DateTime? value) => value is null ? "ثبت نشده" : value.Value.ToString("yyyy/MM/dd", CultureInfo.GetCultureInfo("fa-IR"));
    }
}

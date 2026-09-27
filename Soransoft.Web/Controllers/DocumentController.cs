using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Soransoft.Infrastructure.Persistence;
using System.Security.Claims;

namespace Soransoft.Web.Controllers
{
    /// <summary>تحویل کنترل‌شده اسناد پرتال؛ فایل‌ها مستقیماً از wwwroot سرو نمی‌شوند.</summary>
    [Authorize]
    [Route("documents")]
    public sealed class DocumentController : Controller
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg", ".webp",
        };

        private readonly SoransoftDbContext _db;
        private readonly string _documentsRoot;
        private readonly FileExtensionContentTypeProvider _contentTypes = new();

        public DocumentController(SoransoftDbContext db, IWebHostEnvironment environment)
        {
            _db = db;
            _documentsRoot = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data", "PrivateDocuments"));
        }

        [HttpGet("download")]
        public async Task<IActionResult> Download(string? path, CancellationToken ct)
        {
            if (User.HasClaim("MustChangePassword", "1"))
                return Forbid();

            var relativePath = NormalizeRelativePath(path);
            if (relativePath is null || !await CanAccessAsync(relativePath, ct))
                return NotFound();

            var fullPath = ResolvePrivatePath(relativePath);
            if (fullPath is null || !System.IO.File.Exists(fullPath))
                return NotFound();

            var contentType = _contentTypes.TryGetContentType(fullPath, out var detectedType)
                ? detectedType
                : "application/octet-stream";
            Response.Headers.CacheControl = "no-store";
            Response.Headers.Pragma = "no-cache";
            return PhysicalFile(fullPath, contentType, Path.GetFileName(fullPath), enableRangeProcessing: true);
        }

        private async Task<bool> CanAccessAsync(string relativePath, CancellationToken ct)
        {
            var storedPath = BuildStoredPath(relativePath);
            if (User.IsInRole("Admin"))
            {
                return await _db.PartnerContracts.AsNoTracking()
                           .AnyAsync(c => !c.IsDeleted && c.ContractFile == storedPath, ct)
                    || await _db.CooperationAgreements.AsNoTracking()
                           .AnyAsync(a => !a.IsDeleted && a.ContractFile == storedPath, ct)
                    || await _db.ContractPaymentStages.AsNoTracking()
                           .AnyAsync(s => !s.Contract.IsDeleted &&
                               (s.ReceiptFile == storedPath || s.DocumentFile == storedPath), ct)
                    || await _db.WalletTransactions.AsNoTracking()
                           .AnyAsync(t => t.DocumentFile == storedPath, ct);
            }

            if (!User.HasClaim("UserType", "Partner") ||
                !int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var partnerId))
                return false;

            var partner = await _db.Partners.AsNoTracking()
                .Where(p => p.Id == partnerId && p.IsActive && !p.IsDeleted)
                .Select(p => new { p.Id, p.CanSeeAllSalesData })
                .FirstOrDefaultAsync(ct);
            if (partner is null) return false;

            return await _db.PartnerContracts.AsNoTracking()
                       .AnyAsync(c => !c.IsDeleted && c.ContractFile == storedPath &&
                           (partner.CanSeeAllSalesData || c.PartnerId == partner.Id), ct)
                || await _db.CooperationAgreements.AsNoTracking()
                       .AnyAsync(a => !a.IsDeleted && a.PartnerId == partner.Id && a.ContractFile == storedPath, ct)
                || await _db.ContractPaymentStages.AsNoTracking()
                       .AnyAsync(s => !s.Contract.IsDeleted &&
                           (s.ReceiptFile == storedPath || s.DocumentFile == storedPath) &&
                           (partner.CanSeeAllSalesData || s.Contract.PartnerId == partner.Id), ct)
                || await _db.WalletTransactions.AsNoTracking()
                       .AnyAsync(t => t.PartnerId == partner.Id && t.DocumentFile == storedPath, ct);
        }

        private string? ResolvePrivatePath(string relativePath)
        {
            try
            {
                var root = _documentsRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;
                var fullPath = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
                return fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? fullPath : null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static string? NormalizeRelativePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;

            var normalized = path.Replace('\\', '/').Trim('/');
            var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2 || !segments[0].Equals("partners", StringComparison.OrdinalIgnoreCase))
                return null;
            if (segments.Any(segment => segment is "." or ".." || segment.Contains(':') || segment.Contains('\0')))
                return null;

            var extension = Path.GetExtension(segments[^1]);
            return AllowedExtensions.Contains(extension) ? string.Join('/', segments) : null;
        }

        private static string BuildStoredPath(string relativePath) =>
            $"/documents/download?path={Uri.EscapeDataString(relativePath)}";
    }
}

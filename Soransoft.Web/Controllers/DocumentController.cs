using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Soransoft.Domain.Entities;
using Soransoft.Infrastructure.Persistence;
using Soransoft.Infrastructure.Storage;
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
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".zip", ".rar", ".mp4", ".webm", ".mov", ".png", ".jpg", ".jpeg", ".webp",
        };

        private readonly SoransoftDbContext _db;
        private readonly string _documentsRoot;
        private readonly string _persistentProfileRoot;
        private readonly string _legacyUploadsRoot;
        private readonly FileExtensionContentTypeProvider _contentTypes = new();

        public DocumentController(SoransoftDbContext db, IWebHostEnvironment environment, IConfiguration configuration)
        {
            _db = db;
            _documentsRoot = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data", "PrivateDocuments"));
            _persistentProfileRoot = PersistentProfileStorage.ResolveRoot(environment, configuration);
            _legacyUploadsRoot = Path.GetFullPath(Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "uploads"));
        }

        [HttpGet("download")]
        public async Task<IActionResult> Download(string? path, bool inline = false, CancellationToken ct = default)
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
            return inline
                ? File(System.IO.File.OpenRead(fullPath), contentType, enableRangeProcessing: true)
                : PhysicalFile(fullPath, contentType, Path.GetFileName(fullPath), enableRangeProcessing: true);
        }

        [HttpGet("sellable-project/{id:int}")]
        public async Task<IActionResult> DownloadSellableProject(int id, CancellationToken ct)
        {
            if (User.HasClaim("MustChangePassword", "1"))
                return Forbid();

            var document = await _db.SellableProjectDocuments.AsNoTracking()
                .Where(d => d.Id == id && !d.IsDeleted && d.IsActive && !d.Project.IsDeleted && d.Project.IsActive)
                .Select(d => new { d.StoredPath, d.SellableProjectId })
                .FirstOrDefaultAsync(ct);
            if (document is null || string.IsNullOrWhiteSpace(document.StoredPath))
                return NotFound();

            if (!await CanAccessSellableProjectAsync(document.SellableProjectId, ct))
                return NotFound();

            var relativePath = ExtractStoredRelativePath(document.StoredPath);
            if (relativePath is null)
                return NotFound();

            return ServePrivateFile(relativePath);
        }

        private async Task<bool> CanAccessSellableProjectAsync(int projectId, CancellationToken ct)
        {
            if (User.IsInRole("Admin")) return true;
            if (!User.HasClaim("UserType", "Partner") ||
                !int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var partnerId))
                return false;

            var partner = await _db.Partners.AsNoTracking()
                .Where(p => p.Id == partnerId && p.IsActive && !p.IsDeleted)
                .Select(p => new { p.Id, p.CanSeeAllSalesData, p.Role })
                .FirstOrDefaultAsync(ct);
            if (partner is null || partner.Role is not (PartnerRole.Sales or PartnerRole.SalesManager))
                return false;

            var now = DateTime.Now;
            var hasActiveSalesAgreement = await _db.CooperationAgreements.AsNoTracking().AnyAsync(a =>
                a.PartnerId == partner.Id &&
                a.Kind == AgreementKind.Sales &&
                a.Status == AgreementStatus.Active &&
                a.StartDate <= now &&
                (!a.EndDate.HasValue || a.EndDate.Value >= now), ct);
            if (!hasActiveSalesAgreement) return false;

            return partner.CanSeeAllSalesData || await _db.SellableProjectPartners.AsNoTracking().AnyAsync(a =>
                a.SellableProjectId == projectId && a.PartnerId == partner.Id && a.IsActive, ct);
        }

        private IActionResult ServePrivateFile(string relativePath)
        {
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
                            .AnyAsync(t => t.DocumentFile == storedPath, ct)
                       || await _db.SellableProjectDocuments.AsNoTracking()
                              .AnyAsync(d => !d.IsDeleted && d.IsActive && d.StoredPath == storedPath, ct)
                      || await _db.Partners.AsNoTracking()
                             .AnyAsync(p => !p.IsDeleted &&
                                 (p.PersonalPhotoPath == storedPath ||
                                  p.NationalCardFrontPath == storedPath ||
                                  p.NationalCardBackPath == storedPath ||
                                  p.BirthCertificatePath == storedPath ||
                                  p.IdentityDocumentPath == storedPath), ct)
                      || await _db.PartnerDocuments.AsNoTracking()
                             .AnyAsync(d => !d.IsDeleted && d.StoredPath == storedPath, ct);
            }

            if (!User.HasClaim("UserType", "Partner") ||
                !int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var partnerId))
                return false;

            var partner = await _db.Partners.AsNoTracking()
                .Where(p => p.Id == partnerId && p.IsActive && !p.IsDeleted)
                .Select(p => new { p.Id, p.CanSeeAllSalesData, p.Role })
                .FirstOrDefaultAsync(ct);
            if (partner is null) return false;

            var now = DateTime.Now;
            var hasActiveSalesAgreement = partner.Role is PartnerRole.Sales or PartnerRole.SalesManager
                && await _db.CooperationAgreements.AsNoTracking().AnyAsync(a =>
                    a.PartnerId == partner.Id &&
                    a.Kind == AgreementKind.Sales &&
                    a.Status == AgreementStatus.Active &&
                    a.StartDate <= now &&
                    (!a.EndDate.HasValue || a.EndDate.Value >= now), ct);

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
                       .AnyAsync(t => t.PartnerId == partner.Id && t.DocumentFile == storedPath, ct)
                 || (hasActiveSalesAgreement && await _db.SellableProjectDocuments.AsNoTracking()
                         .AnyAsync(d => d.IsActive && d.StoredPath == storedPath &&
                             d.Project.IsActive &&
                             (partner.CanSeeAllSalesData || d.Project.PartnerAccess.Any(a => a.PartnerId == partner.Id && a.IsActive)), ct))
                 || await _db.Partners.AsNoTracking()
                        .AnyAsync(p => p.Id == partner.Id &&
                            (p.PersonalPhotoPath == storedPath ||
                             p.NationalCardFrontPath == storedPath ||
                             p.NationalCardBackPath == storedPath ||
                             p.BirthCertificatePath == storedPath ||
                             p.IdentityDocumentPath == storedPath), ct)
                 || await _db.PartnerDocuments.AsNoTracking()
                        .AnyAsync(d => !d.IsDeleted && d.PartnerId == partner.Id && d.StoredPath == storedPath, ct);
        }

        private string? ResolvePrivatePath(string relativePath)
        {
            try
            {
                var roots = PersistentProfileStorage.IsProfilePath(relativePath)
                    ? new[] { _persistentProfileRoot, _documentsRoot, _legacyUploadsRoot }
                    : new[] { _documentsRoot, _legacyUploadsRoot };

                string? firstPath = null;
                foreach (var rootPath in roots)
                {
                    var root = rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        + Path.DirectorySeparatorChar;
                    var fullPath = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
                    if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return null;
                    firstPath ??= fullPath;
                    if (System.IO.File.Exists(fullPath)) return fullPath;
                }

                return firstPath;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static string? ExtractStoredRelativePath(string? storedPath)
        {
            const string marker = "?path=";
            if (string.IsNullOrWhiteSpace(storedPath)) return null;

            const string legacyPrefix = "uploads/";
            var normalizedStoredPath = storedPath.TrimStart('/');
            if (normalizedStoredPath.StartsWith(legacyPrefix + "partners/", StringComparison.OrdinalIgnoreCase))
            {
                var legacyPath = normalizedStoredPath[legacyPrefix.Length..].Split('?', 2)[0];
                return NormalizeRelativePath(legacyPath);
            }

            var markerIndex = storedPath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0 || !storedPath[..markerIndex].TrimEnd('/').EndsWith("/documents/download", StringComparison.OrdinalIgnoreCase))
                return null;

            var encodedPath = storedPath[(markerIndex + marker.Length)..].Split('&', 2)[0];
            return NormalizeRelativePath(Uri.UnescapeDataString(encodedPath));
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

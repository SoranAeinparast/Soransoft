using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Infrastructure.Persistence;
using System.Globalization;

namespace Soransoft.Web.Controllers
{
    [AllowAnonymous]
    [Route("verify")]
    public sealed class CardVerificationController : Controller
    {
        private readonly SoransoftDbContext _db;

        public CardVerificationController(SoransoftDbContext db) => _db = db;

        [HttpGet("card/{code}")]
        public async Task<IActionResult> Card(string code, CancellationToken ct)
        {
            if (!TryGetPartnerId(code, out var partnerId)) return View("Card", CardVerificationResult.Invalid("کد کارت معتبر نیست."));

            var partner = await _db.Partners.AsNoTracking().FirstOrDefaultAsync(p => p.Id == partnerId, ct);
            if (partner is null) return View("Card", CardVerificationResult.Invalid("کارت در سامانه پیدا نشد."));

            var agreementEnd = await _db.CooperationAgreements.AsNoTracking()
                .Where(a => a.PartnerId == partner.Id && !a.IsDeleted)
                .OrderByDescending(a => a.EndDate ?? DateTime.MaxValue)
                .ThenByDescending(a => a.CreatedAt)
                .Select(a => a.EndDate)
                .FirstOrDefaultAsync(ct);
            var valid = partner.IsActive && (!agreementEnd.HasValue || agreementEnd.Value.Date >= DateTime.Today);

            return View("Card", new CardVerificationResult
            {
                IsValid = valid,
                Message = valid ? "این کارت در سامانه سوران‌سافت معتبر است." : "اعتبار این کارت به پایان رسیده یا حساب همکار غیرفعال است.",
                FullName = partner.FullName,
                PersonnelCode = code.ToUpperInvariant(),
                Role = RoleTitle(partner.Role),
                ExpiresAt = agreementEnd,
            });
        }

        private static bool TryGetPartnerId(string code, out int id)
        {
            id = 0;
            var raw = code.Trim().ToUpperInvariant();
            return raw.StartsWith("P-", StringComparison.Ordinal) && int.TryParse(raw[2..], NumberStyles.None, CultureInfo.InvariantCulture, out id) && id > 0;
        }

        private static string RoleTitle(Soransoft.Domain.Entities.PartnerRole role) => role switch
        {
            Soransoft.Domain.Entities.PartnerRole.Sales or Soransoft.Domain.Entities.PartnerRole.SalesManager => "همکار فروش",
            Soransoft.Domain.Entities.PartnerRole.Developer or Soransoft.Domain.Entities.PartnerRole.TechManager => "همکار توسعه‌دهنده",
            _ => "همکار سوران‌سافت",
        };
    }

    public sealed class CardVerificationResult
    {
        public bool IsValid { get; init; }
        public string Message { get; init; } = string.Empty;
        public string? FullName { get; init; }
        public string? PersonnelCode { get; init; }
        public string? Role { get; init; }
        public DateTime? ExpiresAt { get; init; }
        public static CardVerificationResult Invalid(string message) => new() { IsValid = false, Message = message };
    }
}

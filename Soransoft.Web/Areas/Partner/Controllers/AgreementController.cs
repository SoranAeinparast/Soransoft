using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Domain.Entities;
using Soransoft.Infrastructure.Persistence;

namespace Soransoft.Web.Areas.Partner.Controllers
{
    /// <summary>قرارداد همکاری فی‌مابین — فقط قراردادهای خود همکار</summary>
    public class AgreementController : PartnerBaseController
    {
        private readonly SoransoftDbContext _db;

        public AgreementController(SoransoftDbContext db) => _db = db;

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var agreements = await _db.CooperationAgreements.AsNoTracking()
                .Where(a => a.PartnerId == PartnerId)
                .OrderBy(a => a.Status == AgreementStatus.Active ? 0 : 1)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync(ct);
            return View(agreements);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using Soransoft.Domain.Entities;
using Soransoft.Infrastructure.Persistence;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Soransoft.Web.Areas.Admin.Controllers
{
    /// <summary>فرم ساخت حساب همکار</summary>
    public class PartnerCreateModel
    {
        [Required(ErrorMessage = "نام کاربری الزامی است")]
        [RegularExpression(@"^[a-zA-Z0-9._-]{3,30}$", ErrorMessage = "نام کاربری فقط حروف انگلیسی، عدد و . _ - (۳ تا ۳۰ کاراکتر)")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "رمز عبور الزامی است")]
        [MinLength(12, ErrorMessage = "رمز عبور حداقل ۱۲ کاراکتر باشد")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "نام کامل الزامی است")]
        public string FullName { get; set; } = string.Empty;
        public string? Mobile { get; set; }
        [EmailAddress(ErrorMessage = "ایمیل معتبر نیست")]
        public string? Email { get; set; }
        public PartnerRole Role { get; set; } = PartnerRole.Sales;
        /// <summary>سطح ارشدی: دیدن داده‌های همه همکاران فروش</summary>
        public bool CanSeeAllSalesData { get; set; }
        public string? TechLevel { get; set; }
        public string? Skills { get; set; }
        public string? AdminNote { get; set; }
    }

    /// <summary>فرم ویرایش حساب همکار؛ رمز عبور از مسیر بازنشانی جداگانه تغییر می‌کند</summary>
    public class PartnerEditModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "نام کاربری الزامی است")]
        [RegularExpression(@"^[a-zA-Z0-9._-]{3,30}$", ErrorMessage = "نام کاربری فقط حروف انگلیسی، عدد و . _ - (۳ تا ۳۰ کاراکتر)")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "نام کامل الزامی است")]
        public string FullName { get; set; } = string.Empty;

        public string? Mobile { get; set; }

        [EmailAddress(ErrorMessage = "ایمیل معتبر نیست")]
        public string? Email { get; set; }

        public PartnerRole Role { get; set; } = PartnerRole.Sales;
        public bool CanSeeAllSalesData { get; set; }
        public string? TechLevel { get; set; }
        public string? Skills { get; set; }
        public string? AdminNote { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>مدیریت اکانت‌های پرتال همکاران توسط مدیر</summary>
    [Area("Admin")]
    [Route("Admin/PortalPartners/{action=Index}/{id?}")]
    [Authorize(Policy = "AdminOnly")]
    public class PortalPartnersController : Controller
    {
        private readonly SoransoftDbContext _db;
        private readonly IPasswordHasher _hasher;

        public PortalPartnersController(SoransoftDbContext db, IPasswordHasher hasher)
        {
            _db = db;
            _hasher = hasher;
        }

        private int? CurrentAdminId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var partners = await _db.Partners.AsNoTracking()
                .Include(p => p.Leads)
                .Include(p => p.Contracts)
                .Include(p => p.Tasks)
                .OrderBy(p => p.Role).ThenBy(p => p.FullName)
                .ToListAsync(ct);

            var roles = await _db.Partners.AsNoTracking().Select(p => p.Role).ToListAsync(ct);
            ViewBag.SalesCount = roles.Count(r => r == PartnerRole.Sales || r == PartnerRole.SalesManager);
            ViewBag.DevCount = roles.Count(r => r == PartnerRole.Developer || r == PartnerRole.TechManager);
            return View(partners);
        }

        /// <summary>صندوق ورودی درخواست‌های بازنشانی رمز همکاران</summary>
        public async Task<IActionResult> ResetRequests(CancellationToken ct)
        {
            ViewBag.PendingCount = await _db.PasswordResetRequests
                .CountAsync(r => r.Status == PasswordResetStatus.Pending, ct);

            var items = await _db.PasswordResetRequests.AsNoTracking()
                .Include(r => r.Partner)
                .OrderBy(r => r.Status == PasswordResetStatus.Pending ? 0 : 1)
                .ThenByDescending(r => r.CreatedAt)
                .ToListAsync(ct);
            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            var partner = await _db.Partners.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
            if (partner is null) return NotFound();

            return View(new PartnerEditModel
            {
                Id = partner.Id,
                Username = partner.Username,
                FullName = partner.FullName,
                Mobile = partner.Mobile,
                Email = partner.Email,
                Role = partner.Role,
                CanSeeAllSalesData = partner.CanSeeAllSalesData,
                TechLevel = partner.TechLevel,
                Skills = partner.Skills,
                AdminNote = partner.AdminNote,
                IsActive = partner.IsActive,
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PartnerEditModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(model);
            if (!Enum.IsDefined(model.Role))
            {
                ModelState.AddModelError(nameof(model.Role), "نقش همکار معتبر نیست.");
                return View(model);
            }

            var username = model.Username.Trim().ToLowerInvariant();
            if (await _db.Partners.AnyAsync(p => p.Id != model.Id && p.Username == username, ct))
            {
                ModelState.AddModelError(nameof(model.Username), "این نام کاربری قبلاً استفاده شده است.");
                return View(model);
            }

            var partner = await _db.Partners.FirstOrDefaultAsync(p => p.Id == model.Id, ct);
            if (partner is null) return NotFound();

            partner.Username = username;
            partner.FullName = model.FullName.Trim();
            partner.Mobile = string.IsNullOrWhiteSpace(model.Mobile) ? null : model.Mobile.Trim();
            partner.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
            partner.Role = model.Role;
            partner.CanSeeAllSalesData = model.CanSeeAllSalesData;
            partner.TechLevel = string.IsNullOrWhiteSpace(model.TechLevel) ? null : model.TechLevel.Trim();
            partner.Skills = string.IsNullOrWhiteSpace(model.Skills) ? null : model.Skills.Trim();
            partner.AdminNote = string.IsNullOrWhiteSpace(model.AdminNote) ? null : model.AdminNote.Trim();
            partner.IsActive = model.IsActive;

            await _db.SaveChangesAsync(ct);
            TempData["Success"] = $"حساب همکار «{partner.FullName}» به‌روزرسانی شد.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>تایید درخواست و تعیین رمز جدید (تکمیل خودکار درخواست)</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveReset(int id, string password, string? response, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
            {
                TempData["Error"] = "رمز جدید حداقل ۱۲ کاراکتر باشد.";
                return RedirectToAction(nameof(ResetRequests));
            }

            var req = await _db.PasswordResetRequests
                .Include(r => r.Partner)
                .FirstOrDefaultAsync(r => r.Id == id, ct);
            if (req is null || req.Status != PasswordResetStatus.Pending)
            {
                TempData["Error"] = "درخواست یافت نشد یا قبلاً تصمیم‌گیری شده است.";
                return RedirectToAction(nameof(ResetRequests));
            }

            req.Partner.PasswordHash = _hasher.Hash(password);
            req.Status = PasswordResetStatus.Approved;
            req.AdminResponse = string.IsNullOrWhiteSpace(response) ? null : response.Trim();
            req.DecidedAt = DateTime.Now;
            req.DecidedByAdminId = CurrentAdminId;

            // سایر درخواست‌های باز همان همکار هم بسته شوند
            var otherOpen = await _db.PasswordResetRequests
                .Where(r => r.PartnerId == req.PartnerId && r.Id != req.Id && r.Status == PasswordResetStatus.Pending)
                .ToListAsync(ct);
            foreach (var o in otherOpen)
            {
                o.Status = PasswordResetStatus.Approved;
                o.AdminResponse = "در همین راستا با تایید یک درخواست بسته شد";
                o.DecidedAt = DateTime.Now;
                o.DecidedByAdminId = CurrentAdminId;
            }

            await _db.SaveChangesAsync(ct);
            TempData["Success"] = $"رمز «{req.Partner.FullName}» بازنشانی شد. رمز جدید را در اختیارش قرار دهید.";
            return RedirectToAction(nameof(ResetRequests));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PartnerCreateModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Index));
            }
            if (!Enum.IsDefined(model.Role))
            {
                TempData["Error"] = "نقش همکار معتبر نیست.";
                return RedirectToAction(nameof(Index));
            }

            var username = model.Username.Trim().ToLowerInvariant();
            if (await _db.Partners.AnyAsync(p => p.Username == username, ct))
            {
                TempData["Error"] = "این نام کاربری قبلاً استفاده شده است.";
                return RedirectToAction(nameof(Index));
            }

            _db.Partners.Add(new Soransoft.Domain.Entities.Partner
            {
                Username = username,
                PasswordHash = _hasher.Hash(model.Password),
                FullName = model.FullName.Trim(),
                Mobile = string.IsNullOrWhiteSpace(model.Mobile) ? null : model.Mobile.Trim(),
                Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
                Role = model.Role,
                CanSeeAllSalesData = model.CanSeeAllSalesData,
                TechLevel = string.IsNullOrWhiteSpace(model.TechLevel) ? null : model.TechLevel.Trim(),
                Skills = string.IsNullOrWhiteSpace(model.Skills) ? null : model.Skills.Trim(),
                AdminNote = string.IsNullOrWhiteSpace(model.AdminNote) ? null : model.AdminNote.Trim(),
                IsActive = true,
            });
            await _db.SaveChangesAsync(ct);

            TempData["Success"] = $"حساب همکار «{model.FullName}» ساخته شد. نام کاربری و رمز را در اختیارش قرار دهید.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>فعال/غیرفعال کردن حساب (غیرفعال = عدم امکان ورود)</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id, CancellationToken ct)
        {
            var p = await _db.Partners.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (p is null) { TempData["Error"] = "همکار یافت نشد."; return RedirectToAction(nameof(Index)); }

            p.IsActive = !p.IsActive;
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = p.IsActive ? "حساب فعال شد." : "حساب غیرفعال شد (ورود مسدود می‌شود).";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>تعیین رمز جدید توسط مدیر (بازنشانی مستقیم — درخواست‌های بازِ همان همکار را هم می‌بندد)</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id, string password, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
            {
                TempData["Error"] = "رمز جدید حداقل ۱۲ کاراکتر باشد.";
                return RedirectToAction(nameof(Index));
            }

            var p = await _db.Partners.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (p is null) { TempData["Error"] = "همکار یافت نشد."; return RedirectToAction(nameof(Index)); }

            p.PasswordHash = _hasher.Hash(password);

            // اگر برای این همکار درخواست بازنشانی بازی وجود دارد، خودکار بسته شود
            var openRequests = await _db.PasswordResetRequests
                .Where(r => r.PartnerId == id && r.Status == PasswordResetStatus.Pending)
                .ToListAsync(ct);
            foreach (var req in openRequests)
            {
                req.Status = PasswordResetStatus.Approved;
                req.AdminResponse = "بازنشانی مستقیم توسط مدیر از صفحه همکاران";
                req.DecidedAt = DateTime.Now;
                req.DecidedByAdminId = CurrentAdminId;
            }

            await _db.SaveChangesAsync(ct);
            TempData["Success"] = $"رمز «{p.FullName}» بازنشانی شد. رمز جدید را در اختیارش قرار دهید.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>رد درخواست بازنشانی رمز</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectReset(int id, string? response, CancellationToken ct)
        {
            var req = await _db.PasswordResetRequests.FirstOrDefaultAsync(r => r.Id == id, ct);
            if (req is null || req.Status != PasswordResetStatus.Pending)
            {
                TempData["Error"] = "درخواست یافت نشد یا قبلاً تصمیم‌گیری شده است.";
                return RedirectToAction(nameof(ResetRequests));
            }

            req.Status = PasswordResetStatus.Rejected;
            req.AdminResponse = string.IsNullOrWhiteSpace(response) ? null : response.Trim();
            req.DecidedAt = DateTime.Now;
            req.DecidedByAdminId = CurrentAdminId;
            await _db.SaveChangesAsync(ct);

            TempData["Success"] = "درخواست رد شد.";
            return RedirectToAction(nameof(ResetRequests));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var p = await _db.Partners.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (p is null) { TempData["Error"] = "همکار یافت نشد."; return RedirectToAction(nameof(Index)); }

            p.IsDeleted = true;
            p.IsActive = false;
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "حساب همکار حذف شد (حذف نرم — سابقه حفظ می‌شود).";
            return RedirectToAction(nameof(Index));
        }
    }
}

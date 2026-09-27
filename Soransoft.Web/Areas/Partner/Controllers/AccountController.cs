using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Infrastructure.Persistence;
using Soransoft.Application.Interfaces;
using Soransoft.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Soransoft.Web.Areas.Partner.Controllers
{
    /// <summary>فرم «فراموشی رمز» همکار — فقط نام کاربری + پیام اختیاری؛ پاسخ همیشه عمومی است</summary>
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "نام کاربری الزامی است")]
        [Display(Name = "نام کاربری")]
        public string Username { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "پیام حداکثر ۵۰۰ کاراکتر است")]
        [Display(Name = "پیام به مدیر (اختیاری)")]
        public string? Message { get; set; }
    }

    /// <summary>فرم ورود همکار</summary>
    public class PartnerLoginViewModel
    {
        [Required(ErrorMessage = "نام کاربری الزامی است")]
        [Display(Name = "نام کاربری")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "رمز عبور الزامی است")]
        [DataType(DataType.Password)]
        [Display(Name = "رمز عبور")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>ورود/خروج همکاران — کاملاً جدا از «ورود/عضویت» کاربران عادی سایت</summary>
    [Area("Partner")]
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private const string Scheme = "PartnerAuth";
        private readonly SoransoftDbContext _db;
        private readonly IPasswordHasher _hasher;

        public AccountController(SoransoftDbContext db, IPasswordHasher hasher)
        {
            _db = db;
            _hasher = hasher;
        }

        // GET /Partner/Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (IsPartnerSignedIn())
                return RedirectToAction("Index", "Dashboard", new { area = "Partner" });

            ViewData["ReturnUrl"] = returnUrl;
            return View(new PartnerLoginViewModel());
        }

        // POST /Partner/Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(PartnerLoginViewModel model, string? returnUrl = null, CancellationToken ct = default)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(model);

            var username = model.Username.Trim();
            var partner = await _db.Partners.FirstOrDefaultAsync(p => p.Username == username, ct);

            if (partner is null || !_hasher.Verify(model.Password, partner.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "نام کاربری یا رمز عبور اشتباه است.");
                return View(model);
            }
            if (!partner.IsActive)
            {
                ModelState.AddModelError(string.Empty, "حساب شما توسط مدیر غیرفعال شده است. با مدیر تماس بگیرید.");
                return View(model);
            }

            partner.LastLoginAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, partner.Id.ToString()),
                new(ClaimTypes.Name, partner.FullName),
                new("PartnerUsername", partner.Username),
                new("PartnerRole", ((int)partner.Role).ToString()),
                new("CanSeeAllSales", partner.CanSeeAllSalesData ? "1" : "0"),
                new("UserType", "Partner"),
            };
            var identity = new ClaimsIdentity(claims, Scheme);
            await HttpContext.SignInAsync(
                Scheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = false });

            TempData["Success"] = $"خوش آمدید {partner.FullName}";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Dashboard", new { area = "Partner" });
        }

        // GET /Partner/Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

        // POST /Partner/Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return View(model);

            var username = model.Username.Trim();
            var partner = await _db.Partners.FirstOrDefaultAsync(p => p.Username == username, ct);

            // پاسخ عمومی: افشای وجود/نبودن نام کاربری ممنوع
            if (partner is null || partner.IsDeleted)
            {
                TempData["Success"] = "درخواست شما ثبت شد. نتیجه توسط مدیر اطلاع داده می‌شود.";
                return RedirectToAction(nameof(Login));
            }

            // هر همکار حداکثر یک درخواست باز در زمانی واحد داشته باشد
            var hasOpen = await _db.PasswordResetRequests
                .AnyAsync(r => r.PartnerId == partner.Id && r.Status == PasswordResetStatus.Pending, ct);
            if (hasOpen)
            {
                TempData["Success"] = "درخواست شما قبلاً ثبت شده و در انتظار بررسی مدیر است.";
                return RedirectToAction(nameof(Login));
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            _db.PasswordResetRequests.Add(new PasswordResetRequest
            {
                PartnerId = partner.Id,
                Message = string.IsNullOrWhiteSpace(model.Message) ? null : model.Message.Trim(),
            });
            await _db.SaveChangesAsync(ct);

            TempData["Success"] = "درخواست بازنشانی رمز ثبت شد و در صندوق مدیر قرار گرفت. نتیجه از طریق مدیر اطلاع داده می‌شود.";
            return RedirectToAction(nameof(Login));
        }

        // GET /Partner/Account/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(Scheme);
            TempData["Success"] = "از پرتال همکاران خارج شدید";
            return RedirectToAction("Login");
        }

        private bool IsPartnerSignedIn() =>
            User.Identity?.IsAuthenticated == true && User.HasClaim("UserType", "Partner");
    }
}

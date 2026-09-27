using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using Soransoft.Domain.Entities;
using Soransoft.Infrastructure.Persistence;
using System.Security.Claims;

namespace Soransoft.Web.Areas.Admin.Controllers
{
    /// <summary>ورود/خروج ادمین (خارج از policy — بدون AdminBaseController)</summary>
    [Area("Admin")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Roles = "Admin")]
    public class AccountController : Controller
    {
        private const string Scheme = CookieAuthenticationDefaults.AuthenticationScheme;
        private readonly SoransoftDbContext _db;
        private readonly IPasswordHasher _hasher;
        private readonly IConfiguration _configuration;

        public AccountController(SoransoftDbContext db, IPasswordHasher hasher, IConfiguration configuration)
        {
            _db = db;
            _hasher = hasher;
            _configuration = configuration;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null, CancellationToken ct = default)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(model);

            var admin = await _db.Admins.FirstOrDefaultAsync(
                a => a.Username == model.Username && !a.IsDeleted && a.IsActive, ct);
            if (admin is null || !_hasher.Verify(model.Password, admin.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "نام کاربری یا رمز عبور اشتباه است");
                return View(model);
            }

            admin.LastLoginAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, admin.Id.ToString()),
                new(ClaimTypes.Name, admin.Username),
                new(ClaimTypes.Role, "Admin"),
            };
            var initialPassword = _configuration["Security:InitialAdmin:Password"];
            if (!string.IsNullOrWhiteSpace(initialPassword) && model.Password == initialPassword)
                claims.Add(new Claim("MustChangePassword", "1"));

            var identity = new ClaimsIdentity(claims, Scheme);
            await HttpContext.SignInAsync(
                Scheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = model.RememberMe });

            if (claims.Any(c => c.Type == "MustChangePassword"))
                return RedirectToAction(nameof(ChangePassword));

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // ---------------- تغییر رمز عبور ----------------
        [HttpGet]
        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(model);

            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(adminId, out var id)) return RedirectToAction("Login");

            var admin = await _db.Admins.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, ct);
            if (admin is null) return RedirectToAction("Login");

            if (!_hasher.Verify(model.CurrentPassword, admin.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "رمز عبور فعلی اشتباه است");
                return View(model);
            }
            if (model.NewPassword == model.CurrentPassword)
            {
                ModelState.AddModelError(string.Empty, "رمز جدید نباید با رمز فعلی یکسان باشد");
                return View(model);
            }

            admin.PasswordHash = _hasher.Hash(model.NewPassword);
            admin.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);

            var authentication = await HttpContext.AuthenticateAsync(Scheme);
            var claims = User.Claims.Where(c => c.Type != "MustChangePassword").ToList();
            var identity = new ClaimsIdentity(claims, Scheme);
            await HttpContext.SignInAsync(
                Scheme,
                new ClaimsPrincipal(identity),
                authentication.Properties ?? new AuthenticationProperties());

            TempData["Success"] = "رمز عبور با موفقیت تغییر کرد";
            return RedirectToAction(nameof(ChangePassword));
        }
    }

    public class ChangePasswordViewModel
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "رمز عبور فعلی الزامی است")]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        [System.ComponentModel.DataAnnotations.Display(Name = "رمز عبور فعلی")]
        public string CurrentPassword { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "رمز جدید الزامی است")]
        [System.ComponentModel.DataAnnotations.MinLength(12, ErrorMessage = "رمز جدید حداقل ۱۲ کاراکتر باشد")]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        [System.ComponentModel.DataAnnotations.Display(Name = "رمز عبور جدید")]
        public string NewPassword { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "تکرار رمز جدید الزامی است")]
        [System.ComponentModel.DataAnnotations.Compare(nameof(NewPassword), ErrorMessage = "تکرار رمز با رمز جدید یکسان نیست")]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        [System.ComponentModel.DataAnnotations.Display(Name = "تکرار رمز جدید")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class LoginViewModel
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "نام کاربری الزامی است")]
        public string Username { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "رمز عبور الزامی است")]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Display(Name = "مرا به خاطر بسپار")]
        public bool RememberMe { get; set; }
    }
}

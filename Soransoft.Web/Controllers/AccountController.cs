using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Soransoft.Application.Interfaces;
using Soransoft.Application.ViewModels;
using Soransoft.Domain.Entities;
using System.Security.Claims;

namespace Soransoft.Web.Controllers
{
    /// <summary>ورود/عضویت کاربران عادی سایت (اسکیم UserAuth، جدا از ادمین)</summary>
    public class AccountController : Controller
    {
        private const string Scheme = "UserAuth";
        private readonly IUserAccountService _accounts;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IUserAccountService accounts, ILogger<AccountController> logger)
        {
            _accounts = accounts;
            _logger = logger;
        }

        // GET /Account/Login
        [HttpGet("Account/Login")]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true && User.HasClaim("UserType", "SiteUser"))
                return RedirectToAction(nameof(PanelController.Index), "Panel");

            ViewData["ReturnUrl"] = returnUrl;
            return View(new UserLoginViewModel());
        }

        // POST /Account/Login
        [HttpPost("Account/Login")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(UserLoginViewModel model, string? returnUrl = null, CancellationToken ct = default)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(model);

            var result = await _accounts.LoginAsync(model, ct);
            if (!result.Success || result.User is null)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            await SignInUserAsync(result.User, model.RememberMe);
            TempData["Success"] = result.Message;

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction(nameof(PanelController.Index), "Panel");
        }

        // GET /Account/Register
        [HttpGet("Account/Register")]
        [AllowAnonymous]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new RegisterViewModel());
        }

        // POST /Account/Register
        [HttpPost("Account/Register")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null, CancellationToken ct = default)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(model);

            var result = await _accounts.RegisterAsync(model, ct);
            if (!result.Success || result.User is null)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            await SignInUserAsync(result.User, isPersistent: true);
            TempData["Success"] = result.Message;

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction(nameof(PanelController.Index), "Panel");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(Scheme);
            TempData["Success"] = "با موفقیت خارج شدید";
            return RedirectToAction("Index", "Home");
        }

        private async Task SignInUserAsync(SiteUser user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.MobilePhone, user.Mobile),
                new("UserType", "SiteUser"),
            };
            var identity = new ClaimsIdentity(claims, Scheme);
            await HttpContext.SignInAsync(
                Scheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = isPersistent });
        }
    }
}

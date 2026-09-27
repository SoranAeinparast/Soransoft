using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Soransoft.Application.Interfaces;
using Soransoft.Application.ViewModels;
using Soransoft.Domain.Enums;
using System.Security.Claims;

namespace Soransoft.Web.Controllers
{
    /// <summary>پنل کاربری کاربران عادی سایت — فقط با اسکیم UserAuth</summary>
    [Authorize(AuthenticationSchemes = "UserAuth")]
    public class PanelController : Controller
    {
        private readonly IUserAccountService _accounts;

        public PanelController(IUserAccountService accounts) => _accounts = accounts;

        /// <summary>Id کاربر جاری از Claims</summary>
        private int CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        // GET /Panel
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var model = new PanelViewModel
            {
                User = await _accounts.GetByIdAsync(CurrentUserId, ct),
                Orders = await _accounts.GetUserOrdersAsync(CurrentUserId, ct),
            };
            if (model.User is null) return Challenge();
            return View(model);
        }

        // GET /Panel/OrderDetails/5
        public async Task<IActionResult> OrderDetails(int id, CancellationToken ct)
        {
            var orders = await _accounts.GetUserOrdersAsync(CurrentUserId, ct);
            var order = orders.FirstOrDefault(o => o.Id == id);
            if (order is null) return NotFound();
            return View(order);
        }

        // GET /Panel/Profile
        public async Task<IActionResult> Profile(CancellationToken ct)
        {
            var user = await _accounts.GetByIdAsync(CurrentUserId, ct);
            if (user is null) return Challenge();

            return View(new UserProfileViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Mobile = user.Mobile,
                LastLoginAt = user.LastLoginAt,
                RegisteredAt = user.CreatedAt,
            });
        }

        // POST /Panel/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(UserProfileViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _accounts.UpdateProfileAsync(CurrentUserId, model, ct);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Profile));
        }

        // GET /Panel/ChangePassword
        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

        // POST /Panel/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _accounts.ChangePasswordAsync(CurrentUserId, model, ct);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Profile));
        }
    }

    /// <summary>مدل ویوی پنل کاربری</summary>
    public class PanelViewModel
    {
        public Soransoft.Domain.Entities.SiteUser? User { get; set; }
        public List<Soransoft.Domain.Entities.ProjectOrder> Orders { get; set; } = new();
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Soransoft.Domain.Entities;
using System.Security.Claims;

namespace Soransoft.Web.Areas.Partner.Controllers
{
    /// <summary>کلاس پایه پنل همکار — استخراج هویت و نقش از کوکی PartnerAuth</summary>
    [Area("Partner")]
    // چالش احراز هویت مستقیماً به اسکیم PartnerAuth هدایت شود تا کاربر ناشناس به صفحه ورود پرتال برود
    [Authorize(Policy = "PartnerOnly", AuthenticationSchemes = "PartnerAuth")]
    public abstract class PartnerBaseController : Controller
    {
        protected int PartnerId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        protected PartnerRole Role =>
            System.Enum.TryParse<PartnerRole>(User.FindFirstValue("PartnerRole"), out var r) ? r : PartnerRole.Sales;

        protected bool IsSalesSide => Role is PartnerRole.Sales or PartnerRole.SalesManager;
        protected bool IsDevSide => Role is PartnerRole.Developer or PartnerRole.TechManager;

        /// <summary>سطح ارشدی: دیدن لیدها/قراردادها/گزارش‌های همه همکاران</summary>
        protected bool CanSeeAllSales => User.HasClaim("CanSeeAllSales", "1");

        /// <summary>مسدود کردن دسترسی نقش فروش به صفحات فنی و برعکس</summary>
        protected IActionResult Forbidden()
        {
            TempData["Error"] = "به این بخش دسترسی ندارید.";
            return RedirectToAction("Index", "Dashboard", new { area = "Partner" });
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Soransoft.Application.Interfaces;
using Soransoft.Application.ViewModels;
using Soransoft.Web.Models;
using System.Security.Claims;

namespace Soransoft.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ISiteService _site;
        private readonly ISiteSettingService _settings;
        private readonly IFormService _forms;
        private readonly IUserAccountService _accounts;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ISiteService site, ISiteSettingService settings, IFormService forms, IUserAccountService accounts, ILogger<HomeController> logger)
        {
            _site = site;
            _settings = settings;
            _forms = forms;
            _accounts = accounts;
            _logger = logger;
        }

        // GET /
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var model = await _site.GetHomePageAsync(ct);
            return View(model);
        }

        // GET /OurServices
        [HttpGet("OurServices")]
        public async Task<IActionResult> OurServices(CancellationToken ct)
        {
            var model = new OurServicesViewModel
            {
                Services = await _site.GetServiceCardsAsync(ct),
            };
            ViewData["Title"] = "خدمات ما";
            return View(model);
        }

        // GET /SiteProject — صفحه اختصاصی طراحی سایت
        [HttpGet("SiteProject")]
        public async Task<IActionResult> SiteProject(CancellationToken ct)
        {
            var service = await _site.GetServiceBySlugAsync("site-project", ct);
            if (service is null) return NotFound();
            ViewData["Title"] = service.Title;
            return View("ServicePage", service);
        }

        // GET /SeoProject — صفحه اختصاصی سئو
        [HttpGet("SeoProject")]
        public async Task<IActionResult> SeoProject(CancellationToken ct)
        {
            var service = await _site.GetServiceBySlugAsync("seo", ct);
            if (service is null) return NotFound();
            ViewData["Title"] = service.Title;
            return View("ServicePage", service);
        }

        // GET /ContentProject — صفحه اختصاصی تولید محتوا
        [HttpGet("ContentProject")]
        public async Task<IActionResult> ContentProject(CancellationToken ct)
        {
            var service = await _site.GetServiceBySlugAsync("content", ct);
            if (service is null) return NotFound();
            ViewData["Title"] = service.Title;
            return View("ServicePage", service);
        }

        // GET /Portfolio
        [HttpGet("Portfolio")]
        public async Task<IActionResult> Portfolio(CancellationToken ct)
        {
            var model = await _site.GetPortfoliosAsync(ct);
            ViewData["Title"] = "نمونه کارها";
            return View(model);
        }

        // GET /Articles
        [HttpGet("Articles")]
        public async Task<IActionResult> Articles(int? categoryId, int page = 1, CancellationToken ct = default)
        {
            const int pageSize = 10;
            page = Math.Max(1, page);
            var model = new ArticlesViewModel
            {
                Categories = await _site.GetArticleCategoriesAsync(ct),
                Articles = await _site.GetArticlesAsync(categoryId, page, pageSize, ct),
                TotalCount = await _site.GetArticlesCountAsync(categoryId, ct),
                CurrentPage = page,
                PageSize = pageSize,
                SelectedCategoryId = categoryId,
            };
            ViewData["Title"] = "مقالات";
            return View(model);
        }

        // GET /Article/{slug} — آدرس تمیز اسلاگ‌دار
        [HttpGet("Article/{slug}")]
        public async Task<IActionResult> ArticleBySlug(string slug, CancellationToken ct)
        {
            var article = await _site.GetArticleBySlugAsync(slug, ct);
            if (article is null) return NotFound();

            await IncrementVisitAsync(article.Id, ct);
            FillArticleSeo(article);
            return View("Article", article);
        }

        // GET /Article/5 → ریدایرکت ۳۰۱ به آدرس اسلاگ‌دار (آدرس‌های قدیمی)
        [HttpGet("Article/{id:int}")]
        public async Task<IActionResult> Article(int id, CancellationToken ct)
        {
            var article = await _site.GetArticleAsync(id, ct);
            if (article is null) return NotFound();

            // ریدایرکت دائمی به URL اسلاگ‌دار برای انتقال اعتبار سئو
            if (!string.IsNullOrWhiteSpace(article.Slug))
                return RedirectPermanent($"/Article/{Uri.EscapeDataString(article.Slug)}");

            await IncrementVisitAsync(article.Id, ct);
            FillArticleSeo(article);
            return View("Article", article);
        }

        /// <summary>افزایش شمارنده بازدید مقاله</summary>
        private async Task IncrementVisitAsync(int id, CancellationToken ct)
        {
            try
            {
                await _site.IncrementArticleVisitAsync(id, ct);
            }
            catch
            {
                // شمارش بازدید نباید رندر صفحه را متوقف کند
            }
        }

        /// <summary>متاتگ‌های Open Graph / Twitter Card برای صفحه مقاله</summary>
        private void FillArticleSeo(Soransoft.Domain.Entities.Article article)
        {
            ViewData["Title"] = article.Title;
            ViewData["Description"] = article.Summary;
            ViewData["OgType"] = "article";
            ViewData["OgImage"] = string.IsNullOrWhiteSpace(article.Image)
                ? null
                : Url.Content($"~/{article.Image.TrimStart('~', '/')}");
            ViewData["Canonical"] = Url.ActionLink(
                nameof(ArticleBySlug), "Home",
                new { slug = article.Slug }, protocol: Request.Scheme);
            ViewData["ArticlePublishedTime"] = article.PublishedAt.ToString("yyyy-MM-ddTHH:mm:sszzz", System.Globalization.CultureInfo.InvariantCulture);
        }

        // GET /Tariff
        [HttpGet("Tariff")]
        public async Task<IActionResult> Tariff(CancellationToken ct)
        {
            var model = await _site.GetHomePageAsync(ct);
            ViewData["Title"] = "تعرفه";
            return View(model);
        }

        // GET /OrderProject
        [HttpGet("OrderProject")]
        public async Task<IActionResult> OrderProject(CancellationToken ct)
        {
            var model = new OrderProjectViewModel();

            // پیش‌پر کردن فرم برای کاربر واردشده
            if (User.Identity?.IsAuthenticated == true && User.HasClaim("UserType", "SiteUser")
                && int.TryParse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier), out var uid))
            {
                var user = await _accounts.GetByIdAsync(uid, ct);
                if (user is not null)
                {
                    model.FullName = user.FullName;
                    model.Mobile = user.Mobile;
                    model.Email = user.Email;
                }
            }

            return View(model);
        }

        // POST /OrderProject
        [HttpPost("OrderProject")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OrderProject(OrderProjectViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _forms.SubmitOrderAsync(model, ct);
            if (result.Success)
            {
                TempData["Success"] = result.Message;
                return RedirectToAction(nameof(OrderProject));
            }
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        // POST /Home/SubmitConsultation — AJAX از فرم مشاوره
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitConsultation(ConsultationRequestViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrWhiteSpace(m));
                return Json(OperationResult.Fail(string.Join(" | ", errors)));
            }

            var result = await _forms.SubmitConsultationAsync(model, ct);
            return Json(result);
        }

        // GET /AboutUs
        [HttpGet("AboutUs")]
        public async Task<IActionResult> AboutUs(CancellationToken ct)
        {
            var model = new AboutUsViewModel
            {
                Settings = await _settings.GetAllAsync(ct),
                Team = await _site.GetTeamMembersAsync(ct),
            };
            ViewData["Title"] = "درباره ما";
            return View(model);
        }

        // GET /ContactUs
        [HttpGet("ContactUs")]
        public async Task<IActionResult> ContactUs(CancellationToken ct)
        {
            ViewBag.Settings = await _settings.GetAllAsync(ct);
            return View(new ContactUsViewModel());
        }

        // POST /ContactUs
        [HttpPost("ContactUs")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ContactUs(ContactUsViewModel model, CancellationToken ct)
        {
            ViewBag.Settings = await _settings.GetAllAsync(ct);
            if (!ModelState.IsValid)
                return View(model);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _forms.SubmitContactAsync(model, ip, ct);
            if (result.Success)
            {
                TempData["Success"] = result.Message;
                return RedirectToAction(nameof(ContactUs));
            }
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

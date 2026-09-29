using Microsoft.AspNetCore.Mvc;
using Soransoft.Application.Interfaces;

namespace Soransoft.Web.Areas.Admin.Controllers
{
    public sealed class FeaturesController : AdminBaseController
    {
        private readonly ISiteFeatureService _features;

        public FeaturesController(ISiteFeatureService features) => _features = features;

        public async Task<IActionResult> Index(CancellationToken ct) =>
            View(await _features.GetAllAsync(ct));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(string key, CancellationToken ct)
        {
            var current = (await _features.GetAllAsync(ct)).FirstOrDefault(f => f.Key == key);
            if (current is null) return NotFound();

            await _features.SetEnabledAsync(key, !current.IsEnabled, ct);
            TempData["Success"] = $"قابلیت «{current.Title}» {(current.IsEnabled ? "غیرفعال" : "فعال")} شد.";
            return RedirectToAction(nameof(Index));
        }
    }
}

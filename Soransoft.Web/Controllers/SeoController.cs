using Microsoft.AspNetCore.Mvc;
using Soransoft.Application.Interfaces;
using System.Text;

namespace Soransoft.Web.Controllers
{
    /// <summary>اندپوینت‌های سئو: sitemap.xml و robots.txt</summary>
    public class SeoController : Controller
    {
        private readonly ISeoService _seo;
        public SeoController(ISeoService seo) => _seo = seo;

        // GET /sitemap.xml
        [HttpGet("sitemap.xml")]
        public async Task<IActionResult> Sitemap(CancellationToken ct)
        {
            var baseUrl = BuildBaseUrl();
            var xml = await BuildSitemapXmlAsync(baseUrl, ct);

            return Content(xml, "application/xml", Encoding.UTF8);
        }

        // GET /robots.txt
        [HttpGet("robots.txt")]
        public IActionResult Robots()
        {
            var baseUrl = BuildBaseUrl();
            var sb = new StringBuilder();
            sb.AppendLine("User-agent: *");
            sb.AppendLine("Allow: /");
            // پنل مدیریت و صفحات خصوصی نباید ایندکس شوند
            sb.AppendLine("Disallow: /Admin");
            sb.AppendLine("Disallow: /Panel");
            sb.AppendLine("Disallow: /Account");
            sb.AppendLine();
            sb.AppendLine("Sitemap: " + baseUrl + "/sitemap.xml");
            return Content(sb.ToString(), "text/plain", Encoding.UTF8);
        }

        /// <summary>آدرس پایه از هدرهای درخواست (پشتیبانی از پروکسی معکوس)</summary>
        private string BuildBaseUrl()
        {
            var request = Request;
            var host = request.Host.Value ?? "localhost";
            return $"{request.Scheme}://{host}";
        }

        private async Task<string> BuildSitemapXmlAsync(string baseUrl, CancellationToken ct)
        {
            var items = await _seo.GetSitemapAsync(baseUrl, ct);

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
            foreach (var item in items)
            {
                sb.AppendLine("  <url>");
                sb.AppendLine($"    <loc>{EscapeXml(item.Url)}</loc>");
                if (item.LastModified is { } lm)
                    sb.AppendLine($"    <lastmod>{lm.ToUniversalTime():yyyy-MM-ddTHH:mm:sszzz}</lastmod>");
                if (!string.IsNullOrWhiteSpace(item.ChangeFrequency))
                    sb.AppendLine($"    <changefreq>{item.ChangeFrequency}</changefreq>");
                sb.AppendLine($"    <priority>{item.Priority.ToString(System.Globalization.CultureInfo.InvariantCulture)}</priority>");
                sb.AppendLine("  </url>");
            }
            sb.AppendLine("</urlset>");
            return sb.ToString();
        }

        private static string EscapeXml(string value) =>
            System.Security.SecurityElement.Escape(value) ?? value;
    }
}

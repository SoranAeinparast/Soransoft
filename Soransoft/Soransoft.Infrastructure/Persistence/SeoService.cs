using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using Soransoft.Domain.Enums;
using Soransoft.Infrastructure.Persistence;

namespace Soransoft.Infrastructure.Persistence
{
    /// <summary>سرویس سئو: تولید نقشه سایت از صفحات ثابت، سرویس‌ها و مقالات اسلاگ‌دار</summary>
    public class SeoService : ISeoService
    {
        private readonly SoransoftDbContext _db;
        public SeoService(SoransoftDbContext db) => _db = db;

        public async Task<List<SitemapItem>> GetSitemapAsync(string baseUrl, CancellationToken ct = default)
        {
            baseUrl = baseUrl.TrimEnd('/');

            var featureSettings = await _db.SiteSettings.AsNoTracking()
                .Where(s => s.Key.StartsWith("Feature:") || s.Key == "Feature:public.services")
                .ToDictionaryAsync(s => s.Key, s => s.Value, ct);
            bool Enabled(string key) => !featureSettings.TryGetValue(SiteFeatures.SettingKey(key), out var value)
                || !bool.TryParse(value, out var enabled)
                || enabled;

            var items = new List<SitemapItem> { new() { Url = $"{baseUrl}/", Priority = 1.0, ChangeFrequency = "daily" } };
            if (Enabled(SiteFeatures.Services)) items.Add(new() { Url = $"{baseUrl}/OurServices", Priority = 0.9 });
            if (Enabled(SiteFeatures.Portfolio)) items.Add(new() { Url = $"{baseUrl}/Portfolio", Priority = 0.9 });
            if (Enabled(SiteFeatures.Pricing)) items.Add(new() { Url = $"{baseUrl}/Tariff", Priority = 0.8 });
            if (Enabled(SiteFeatures.Blog)) items.Add(new() { Url = $"{baseUrl}/Articles", Priority = 0.9, ChangeFrequency = "daily" });
            if (Enabled(SiteFeatures.About)) items.Add(new() { Url = $"{baseUrl}/AboutUs", Priority = 0.7, ChangeFrequency = "monthly" });
            if (Enabled(SiteFeatures.Contact)) items.Add(new() { Url = $"{baseUrl}/ContactUs", Priority = 0.7, ChangeFrequency = "monthly" });
            if (Enabled(SiteFeatures.ProjectOrder)) items.Add(new() { Url = $"{baseUrl}/OrderProject", Priority = 0.8 });

            // صفحات سرویس (اسلاگ‌دار)
            var servicesEnabled = Enabled(SiteFeatures.Services);
            var blogEnabled = Enabled(SiteFeatures.Blog);
            var serviceSlugs = await _db.Services.AsNoTracking()
                .Where(s => s.IsActive && servicesEnabled)
                .Select(s => s.Slug)
                .ToListAsync(ct);
            items.AddRange(serviceSlugs.Select(s => new SitemapItem
            {
                Url = $"{baseUrl}/Service/{Uri.EscapeDataString(s)}",
                Priority = 0.8
            }));

            // مقالات منتشرشده (اسلاگ‌دار؛ مقاله بدون اسلاگ با Id ایندکس می‌شود)
            var articles = await _db.Articles.AsNoTracking()
                .Where(a => a.Status == PublishStatus.Published && !a.IsDeleted && blogEnabled)
                .OrderByDescending(a => a.PublishedAt)
                .Select(a => new ArticleSitemapItem
                {
                    Id = a.Id,
                    Slug = a.Slug ?? string.Empty,
                    LastModified = a.UpdatedAt ?? a.PublishedAt,
                })
                .ToListAsync(ct);

            items.AddRange(articles.Select(a => new SitemapItem
            {
                Url = !string.IsNullOrWhiteSpace(a.Slug)
                    ? $"{baseUrl}/Article/{Uri.EscapeDataString(a.Slug)}"
                    : $"{baseUrl}/Article/{a.Id}",
                LastModified = a.LastModified,
                Priority = 0.6,
                ChangeFrequency = "weekly",
            }));

            var pages = await _db.SitePages.AsNoTracking()
                .Where(p => p.IsPublished && !p.IsDeleted)
                .OrderBy(p => p.DisplayOrder)
                .Select(p => new { p.Slug, p.UpdatedAt })
                .ToListAsync(ct);

            items.AddRange(pages.Select(p => new SitemapItem
            {
                Url = $"{baseUrl}/Page/{Uri.EscapeDataString(p.Slug)}",
                LastModified = p.UpdatedAt,
                Priority = 0.7,
                ChangeFrequency = "monthly",
            }));

            return items;
        }
    }
}

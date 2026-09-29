using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using Soransoft.Application.ViewModels;
using Soransoft.Domain.Entities;
using Soransoft.Domain.Enums;

namespace Soransoft.Infrastructure.Persistence
{
    /// <summary>پیاده‌سازی EF Core برای کوئری‌های سایت</summary>
    public class SiteQueries : ISiteQueries
    {
        private readonly SoransoftDbContext _db;
        public SiteQueries(SoransoftDbContext db) => _db = db;

        public async Task<List<ServiceCardViewModel>> GetActiveServicesAsync(CancellationToken ct = default)
        {
            var services = await _db.Services.AsNoTracking()
                .Include(s => s.Steps)
                .Where(s => s.IsActive)
                .OrderBy(s => s.DisplayOrder).ToListAsync(ct);

            return services.Select(s => new ServiceCardViewModel
            {
                Id = s.Id, Kind = s.Kind, Title = s.Title, ShortDescription = s.ShortDescription,
                Slug = s.Slug, Icon = s.Icon, Image = s.Image, ComingSoon = s.ComingSoon,
                CtaText = s.ComingSoon ? "به زودی" : "بیشتر بدانید"
            }).ToList();
        }

        public async Task<List<PortfolioCardViewModel>> GetPortfoliosAsync(CancellationToken ct = default)
        {
            return await _db.Portfolios.AsNoTracking()
                .Where(p => p.IsActive && (p.ServiceId == null || (p.Service != null && p.Service.IsActive)))
                .OrderBy(p => p.DisplayOrder)
                .Select(p => new PortfolioCardViewModel
                {
                    Id = p.Id, Title = p.Title, Description = p.Description,
                    Image = p.Image, Url = p.Url, ComingSoon = p.ComingSoon,
                    ServiceTitle = p.Service != null ? p.Service.Title : ""
                }).ToListAsync(ct);
        }

        public async Task<List<ArticleCardViewModel>> GetLatestArticlesAsync(int count, CancellationToken ct = default)
        {
            return await _db.Articles.AsNoTracking()
                .Where(a => a.Status == PublishStatus.Published && a.ArticleCategory.IsActive)
                .OrderByDescending(a => a.IsFeatured).ThenByDescending(a => a.PublishedAt).Take(count)
                .Select(a => new ArticleCardViewModel
                {
                    Id = a.Id, Slug = a.Slug, Title = a.Title, Summary = a.Summary, Image = a.Image,
                    AuthorName = a.AuthorName, VisitCount = a.VisitCount,
                    CategoryTitle = a.ArticleCategory.Title, PublishedAt = a.PublishedAt, IsFeatured = a.IsFeatured
                }).ToListAsync(ct);
        }

        public Task<List<ArticleCardViewModel>> GetArticlesAsync(int? categoryId, int page, int pageSize, CancellationToken ct = default)
        {
            var query = _db.Articles.AsNoTracking().Where(a => a.Status == PublishStatus.Published && a.ArticleCategory.IsActive);
            if (categoryId.HasValue) query = query.Where(a => a.ArticleCategoryId == categoryId.Value);
            return query.OrderByDescending(a => a.PublishedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(a => new ArticleCardViewModel
                {
                    Id = a.Id, Slug = a.Slug, Title = a.Title, Summary = a.Summary, Image = a.Image,
                    AuthorName = a.AuthorName, VisitCount = a.VisitCount,
                    CategoryTitle = a.ArticleCategory.Title, PublishedAt = a.PublishedAt, IsFeatured = a.IsFeatured
                }).ToListAsync(ct);
        }

        public Task<int> GetArticlesCountAsync(int? categoryId, CancellationToken ct = default)
        {
            var query = _db.Articles.AsNoTracking().Where(a => a.Status == PublishStatus.Published && a.ArticleCategory.IsActive);
            if (categoryId.HasValue) query = query.Where(a => a.ArticleCategoryId == categoryId.Value);
            return query.CountAsync(ct);
        }

        public async Task<Article?> GetArticleAsync(int id, CancellationToken ct = default)
        {
            var article = await _db.Articles
                .Include(a => a.ArticleCategory)
                .FirstOrDefaultAsync(a => a.Id == id && a.Status == PublishStatus.Published && a.ArticleCategory.IsActive, ct);
            if (article is null) return null;
            return article;
        }

        public Task<Article?> GetArticleBySlugAsync(string slug, CancellationToken ct = default) =>
            _db.Articles.AsNoTracking()
                .Include(a => a.ArticleCategory)
                .FirstOrDefaultAsync(a => a.Slug == slug && a.Status == PublishStatus.Published && a.ArticleCategory.IsActive, ct);

        public async Task IncrementArticleVisitAsync(int id, CancellationToken ct = default)
        {
            await _db.Articles.Where(a => a.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.VisitCount, a => a.VisitCount + 1), ct);
        }

        public async Task<List<ArticleSitemapItem>> GetPublishedArticleSitemapItemsAsync(CancellationToken ct = default)
        {
            var rows = await _db.Articles.AsNoTracking()
                .Where(a => a.Status == PublishStatus.Published && !a.IsDeleted && a.ArticleCategory.IsActive)
                .OrderByDescending(a => a.PublishedAt)
                .Select(a => new { a.Id, a.Slug, a.PublishedAt, a.UpdatedAt })
                .ToListAsync(ct);
            return rows.Select(a => new ArticleSitemapItem
            {
                Id = a.Id,
                Slug = a.Slug ?? string.Empty,
                LastModified = a.UpdatedAt ?? a.PublishedAt,
            }).ToList();
        }

        public async Task<Service?> GetServiceBySlugAsync(string slug, CancellationToken ct = default)
        {
            return await _db.Services.AsNoTracking()
                .Include(s => s.Features.OrderBy(f => f.DisplayOrder))
                .Include(s => s.Steps.OrderBy(s => s.DisplayOrder))
                .FirstOrDefaultAsync(s => s.Slug == slug && s.IsActive, ct);
        }

        public async Task<Service?> GetServiceByKindAsync(ServiceKind kind, CancellationToken ct = default)
        {
            return await _db.Services.AsNoTracking()
                .Include(s => s.Features.OrderBy(f => f.DisplayOrder))
                .Include(s => s.Steps.OrderBy(s => s.DisplayOrder))
                .FirstOrDefaultAsync(s => s.Kind == kind && s.IsActive, ct);
        }

    public Task<List<ArticleCategory>> GetArticleCategoriesAsync(CancellationToken ct = default) =>
            _db.ArticleCategories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync(ct);

        public Task<List<TeamMember>> GetTeamMembersAsync(CancellationToken ct = default) =>
            _db.TeamMembers.AsNoTracking().Where(t => t.IsActive).OrderBy(t => t.DisplayOrder).ToListAsync(ct);

        public Task<List<TariffSection>> GetTariffSectionsAsync(CancellationToken ct = default) =>
            _db.TariffSections.AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.DisplayOrder)
                .Include(t => t.Packages.Where(p => p.IsActive).OrderBy(p => p.DisplayOrder))
                    .ThenInclude(p => p.Items.OrderBy(i => i.DisplayOrder))
                .ToListAsync(ct);

        public async Task<List<ServiceTimelineViewModel>> GetTimelinesAsync(CancellationToken ct = default)
        {
            var services = await _db.Services.AsNoTracking()
                .Include(s => s.Steps)
                .Where(s => s.IsActive)
                .OrderBy(s => s.DisplayOrder).ToListAsync(ct);

            return services.Select(s => new ServiceTimelineViewModel
            {
                ServiceId = s.Id, ServiceTitle = s.Title, Kind = s.Kind,
                Steps = s.Steps.OrderBy(st => st.DisplayOrder).Select(st => st.Title).ToList()
            }).ToList();
        }

        public async Task<List<MenuItemViewModel>> GetMenuAsync(MenuPosition position, CancellationToken ct = default)
        {
            var items = await _db.MenuItems.AsNoTracking()
                .Where(m => m.IsActive && m.Position == position)
                .OrderBy(m => m.DisplayOrder).ToListAsync(ct);
            return items.Select(m => new MenuItemViewModel
            {
                Title = m.Title,
                Url = m.Url,
                FeatureKey = SiteFeatures.FeatureForUrl(m.Url),
            }).ToList();
        }

        public Task<List<Slider>> GetActiveSlidersAsync(CancellationToken ct = default) =>
            _db.Sliders.AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync(ct);
    }
}

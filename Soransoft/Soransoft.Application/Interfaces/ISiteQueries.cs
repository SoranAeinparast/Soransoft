using Soransoft.Application.ViewModels;
using Soransoft.Domain.Entities;
using Soransoft.Domain.Enums;

namespace Soransoft.Application.Interfaces
{
    /// <summary>کوئری‌های داده‌ای برای صفحات عمومی؛ در لایه‌ی Infrastructure با EF پیاده‌سازی می‌شود</summary>
    public interface ISiteQueries
    {
        Task<List<ServiceCardViewModel>> GetActiveServicesAsync(CancellationToken ct = default);
        Task<List<PortfolioCardViewModel>> GetPortfoliosAsync(CancellationToken ct = default);
        Task<List<ArticleCardViewModel>> GetLatestArticlesAsync(int count, CancellationToken ct = default);
        Task<List<ArticleCardViewModel>> GetArticlesAsync(int? categoryId, int page, int pageSize, CancellationToken ct = default);
        Task<int> GetArticlesCountAsync(int? categoryId, CancellationToken ct = default);
        Task<Article?> GetArticleAsync(int id, CancellationToken ct = default);
        Task<Article?> GetArticleBySlugAsync(string slug, CancellationToken ct = default);
        Task<SitePage?> GetPageBySlugAsync(string slug, CancellationToken ct = default);
        /// <summary>افزایش شمارنده بازدید مقاله</summary>
        Task IncrementArticleVisitAsync(int id, CancellationToken ct = default);
        /// <summary>مقالات منتشرشده برای نقشه سایت</summary>
        Task<List<ArticleSitemapItem>> GetPublishedArticleSitemapItemsAsync(CancellationToken ct = default);
        Task<Service?> GetServiceBySlugAsync(string slug, CancellationToken ct = default);
        Task<Service?> GetServiceByKindAsync(ServiceKind kind, CancellationToken ct = default);
        Task<List<ArticleCategory>> GetArticleCategoriesAsync(CancellationToken ct = default);
        Task<List<TeamMember>> GetTeamMembersAsync(CancellationToken ct = default);
        Task<List<TariffSection>> GetTariffSectionsAsync(CancellationToken ct = default);
        Task<List<ServiceTimelineViewModel>> GetTimelinesAsync(CancellationToken ct = default);
        Task<List<MenuItemViewModel>> GetMenuAsync(MenuPosition position, CancellationToken ct = default);
        /// <summary>اسلایدهای فعال صفحه اصلی</summary>
        Task<List<Slider>> GetActiveSlidersAsync(CancellationToken ct = default);
        /// <summary>بنرهای پروموی فعال بر اساس محل نمایش</summary>
        Task<List<PromoBanner>> GetActivePromoBannersAsync(PromoBannerPlacement? placement = null, CancellationToken ct = default);
    }
}

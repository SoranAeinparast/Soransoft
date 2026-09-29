using Soransoft.Application.Interfaces;
using Soransoft.Application.ViewModels;
using Soransoft.Domain.Entities;
using Soransoft.Domain.Enums;

namespace Soransoft.Application.Services
{
    /// <summary>سرویس صفحات عمومی سایت — ارکستراسیون خالص بدون وابستگی به EF</summary>
    public class SiteService : ISiteService
    {
        private readonly ISiteQueries _queries;
        private readonly ISiteSettingService _settings;
        private readonly ISiteFeatureService _features;

        public SiteService(ISiteQueries queries, ISiteSettingService settings, ISiteFeatureService features)
        {
            _queries = queries;
            _settings = settings;
            _features = features;
        }

        public async Task<HomeViewModel> GetHomePageAsync(CancellationToken ct = default)
        {
            // DbContext از عملیات موازی پشتیبانی نمی‌کند؛ کوئری‌ها ترتیبی اجرا می‌شوند
            var services = await _queries.GetActiveServicesAsync(ct);
            var portfolios = await _queries.GetPortfoliosAsync(ct);
            var latestArticles = await _queries.GetLatestArticlesAsync(2, ct);
            var tariffSections = await _queries.GetTariffSectionsAsync(ct);
            var timelines = await _queries.GetTimelinesAsync(ct);
            var settings = await _settings.GetAllAsync(ct);
            var features = (await _features.GetAllAsync(ct)).ToDictionary(f => f.Key, f => f.IsEnabled);

            return new HomeViewModel
            {
                Settings = settings,
                Features = features,
                Services = services,
                Portfolios = portfolios,
                LatestArticles = latestArticles,
                TariffSections = tariffSections.Select(t => new TariffSectionViewModel
                {
                    Id = t.Id,
                    Title = t.Title,
                    Kind = t.Kind,
                    Packages = t.Packages.Where(p => p.IsActive).OrderBy(p => p.DisplayOrder).Select(p => new TariffPackageViewModel
                    {
                        Id = p.Id,
                        Title = p.Title,
                        Price = p.Price,
                        PriceNote = p.PriceNote,
                        PriceSuffix = p.PriceSuffix,
                        Duration = p.Duration,
                        IsInstallmentAvailable = p.IsInstallmentAvailable,
                        HasFreeSupport = p.HasFreeSupport,
                        IsPopular = p.IsPopular,
                        Items = p.Items.OrderBy(i => i.DisplayOrder).Select(i => i.Title).ToList(),
                    }).ToList(),
                }).ToList(),
                Timelines = timelines,
            };
        }

        public Task<List<MenuItemViewModel>> GetMenuAsync(MenuPosition position, CancellationToken ct = default) =>
            _queries.GetMenuAsync(position, ct);

        public Task<List<PortfolioCardViewModel>> GetPortfoliosAsync(CancellationToken ct = default) =>
            _queries.GetPortfoliosAsync(ct);

        public Task<List<ArticleCardViewModel>> GetArticlesAsync(int? categoryId, int page, int pageSize, CancellationToken ct = default) =>
            _queries.GetArticlesAsync(categoryId, page, pageSize, ct);

        public Task<int> GetArticlesCountAsync(int? categoryId, CancellationToken ct = default) =>
            _queries.GetArticlesCountAsync(categoryId, ct);

        public Task<Article?> GetArticleAsync(int id, CancellationToken ct = default) =>
            _queries.GetArticleAsync(id, ct);

        public Task<Article?> GetArticleBySlugAsync(string slug, CancellationToken ct = default) =>
            _queries.GetArticleBySlugAsync(slug, ct);

        public Task IncrementArticleVisitAsync(int id, CancellationToken ct = default) =>
            _queries.IncrementArticleVisitAsync(id, ct);

        public Task<Service?> GetServiceBySlugAsync(string slug, CancellationToken ct = default) =>
            _queries.GetServiceBySlugAsync(slug, ct);

        public Task<Service?> GetServiceByKindAsync(ServiceKind kind, CancellationToken ct = default) =>
            _queries.GetServiceByKindAsync(kind, ct);

        public Task<List<ArticleCategory>> GetArticleCategoriesAsync(CancellationToken ct = default) =>
            _queries.GetArticleCategoriesAsync(ct);

        public Task<List<TeamMember>> GetTeamMembersAsync(CancellationToken ct = default) =>
            _queries.GetTeamMembersAsync(ct);

        public Task<List<TariffSection>> GetTariffSectionsAsync(CancellationToken ct = default) =>
            _queries.GetTariffSectionsAsync(ct);

        public Task<List<ServiceCardViewModel>> GetServiceCardsAsync(CancellationToken ct = default) =>
            _queries.GetActiveServicesAsync(ct);

        public Task<List<Slider>> GetSlidersAsync(CancellationToken ct = default) =>
            _queries.GetActiveSlidersAsync(ct);
    }
}

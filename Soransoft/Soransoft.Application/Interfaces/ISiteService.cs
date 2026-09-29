using Soransoft.Application.ViewModels;
using Soransoft.Domain.Entities;
using Soransoft.Domain.Enums;

namespace Soransoft.Application.Interfaces
{
    /// <summary>سرویس صفحات عمومی سایت</summary>
    public interface ISiteService
    {
        Task<HomeViewModel> GetHomePageAsync(CancellationToken ct = default);
        Task<List<MenuItemViewModel>> GetMenuAsync(MenuPosition position, CancellationToken ct = default);
        Task<List<PortfolioCardViewModel>> GetPortfoliosAsync(CancellationToken ct = default);
        Task<List<ArticleCardViewModel>> GetArticlesAsync(int? categoryId, int page, int pageSize, CancellationToken ct = default);
        Task<int> GetArticlesCountAsync(int? categoryId, CancellationToken ct = default);
        Task<Article?> GetArticleAsync(int id, CancellationToken ct = default);
        Task<Article?> GetArticleBySlugAsync(string slug, CancellationToken ct = default);
        Task<SitePage?> GetPageBySlugAsync(string slug, CancellationToken ct = default);
        Task IncrementArticleVisitAsync(int id, CancellationToken ct = default);
        Task<Service?> GetServiceBySlugAsync(string slug, CancellationToken ct = default);
        Task<Service?> GetServiceByKindAsync(ServiceKind kind, CancellationToken ct = default);
        Task<List<ArticleCategory>> GetArticleCategoriesAsync(CancellationToken ct = default);
        Task<List<TeamMember>> GetTeamMembersAsync(CancellationToken ct = default);
        Task<List<TariffSection>> GetTariffSectionsAsync(CancellationToken ct = default);
        Task<List<ServiceCardViewModel>> GetServiceCardsAsync(CancellationToken ct = default);
        /// <summary>اسلایدهای فعال صفحه اصلی</summary>
        Task<List<Slider>> GetSlidersAsync(CancellationToken ct = default);
        /// <summary>بنرهای پروموی فعال بر اساس محل نمایش</summary>
        Task<List<PromoBanner>> GetPromoBannersAsync(PromoBannerPlacement? placement = null, CancellationToken ct = default);
    }

    /// <summary>سرویس فرم‌های عمومی (سفارش، تماس، مشاوره)</summary>
    public interface IFormService
    {
        Task<OperationResult> SubmitOrderAsync(OrderProjectViewModel model, CancellationToken ct = default);
        Task<OperationResult> SubmitContactAsync(ContactUsViewModel model, string? ip, CancellationToken ct = default);
        Task<OperationResult> SubmitConsultationAsync(ConsultationRequestViewModel model, CancellationToken ct = default);
    }
}

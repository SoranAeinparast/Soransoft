namespace Soransoft.Application.Interfaces
{
    public sealed class SiteFeatureDefinition
    {
        public string Key { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Group { get; init; } = "عمومی";
        public bool DefaultEnabled { get; init; } = true;
    }

    public sealed class SiteFeatureState
    {
        public string Key { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Group { get; init; } = string.Empty;
        public bool IsEnabled { get; init; }
    }

    public static class SiteFeatures
    {
        public const string HomeHero = "public.home.hero";
        public const string HomeServices = "public.home.services";
        public const string HomeTimeline = "public.home.timeline";
        public const string HomePortfolio = "public.home.portfolio";
        public const string HomePricing = "public.home.pricing";
        public const string HomeArticles = "public.home.articles";
        public const string HomeConsultation = "public.home.consultation";
        public const string Services = "public.services";
        public const string Portfolio = "public.portfolio";
        public const string Pricing = "public.pricing";
        public const string Blog = "public.blog";
        public const string About = "public.about";
        public const string Contact = "public.contact";
        public const string ProjectOrder = "public.project-order";
        public const string Consultation = "public.consultation";
        public const string SiteAccount = "public.site-account";
        public const string PartnerPortal = "public.partner-portal";
        public const string CardVerification = "public.card-verification";

        public static IReadOnlyList<SiteFeatureDefinition> All { get; } = new[]
        {
            new SiteFeatureDefinition { Key = HomeHero, Title = "هیرو و اسلایدر صفحه اصلی", Description = "نمایش اسلایدر یا پیام اصلی صفحه اول", Group = "صفحه اصلی" },
            new SiteFeatureDefinition { Key = HomeServices, Title = "بخش خدمات صفحه اصلی", Description = "کارت‌های خدمات و لینک خدمات", Group = "صفحه اصلی" },
            new SiteFeatureDefinition { Key = HomeTimeline, Title = "تایم‌لاین صفحه اصلی", Description = "مراحل اجرای پروژه و شمارش معکوس", Group = "صفحه اصلی" },
            new SiteFeatureDefinition { Key = HomePortfolio, Title = "نمونه‌کارهای صفحه اصلی", Description = "اسلایدر نمونه‌کارها", Group = "صفحه اصلی" },
            new SiteFeatureDefinition { Key = HomePricing, Title = "تعرفه‌های صفحه اصلی", Description = "بخش قیمت‌ها و بسته‌ها در صفحه اول", Group = "صفحه اصلی" },
            new SiteFeatureDefinition { Key = HomeArticles, Title = "مقالات صفحه اصلی", Description = "دو مقاله آخر در صفحه اول", Group = "صفحه اصلی" },
            new SiteFeatureDefinition { Key = HomeConsultation, Title = "فرم مشاوره صفحه اصلی", Description = "فرم مشاوره داخل بخش تعرفه صفحه اول", Group = "صفحه اصلی" },
            new SiteFeatureDefinition { Key = Services, Title = "ماژول خدمات", Description = "صفحه خدمات و صفحات جزئیات سرویس‌ها", Group = "صفحات عمومی" },
            new SiteFeatureDefinition { Key = Portfolio, Title = "ماژول نمونه‌کارها", Description = "صفحه عمومی نمونه‌کارها", Group = "صفحات عمومی" },
            new SiteFeatureDefinition { Key = Pricing, Title = "ماژول تعرفه", Description = "صفحه عمومی تعرفه و قیمت‌ها", Group = "صفحات عمومی" },
            new SiteFeatureDefinition { Key = Blog, Title = "ماژول مقالات", Description = "فهرست و جزئیات مقالات", Group = "صفحات عمومی" },
            new SiteFeatureDefinition { Key = About, Title = "صفحه درباره ما", Description = "معرفی شرکت و اعضای تیم", Group = "صفحات عمومی" },
            new SiteFeatureDefinition { Key = Contact, Title = "صفحه تماس با ما", Description = "اطلاعات تماس و فرم پیام", Group = "صفحات عمومی" },
            new SiteFeatureDefinition { Key = ProjectOrder, Title = "فرم سفارش پروژه", Description = "صفحه و endpoint ثبت سفارش پروژه", Group = "تعاملات" },
            new SiteFeatureDefinition { Key = Consultation, Title = "فرم مشاوره", Description = "فرم ارسال درخواست مشاوره رایگان", Group = "تعاملات" },
            new SiteFeatureDefinition { Key = SiteAccount, Title = "حساب کاربری سایت", Description = "ورود، ثبت‌نام و پنل کاربران سایت", Group = "تعاملات" },
            new SiteFeatureDefinition { Key = PartnerPortal, Title = "پرتال همکاران", Description = "ورود و تمام مسیرهای پرتال همکاران", Group = "تعاملات" },
            new SiteFeatureDefinition { Key = CardVerification, Title = "استعلام کارت همکاران", Description = "صفحه عمومی استعلام کارت شناسایی", Group = "تعاملات" },
        };

        public static string SettingKey(string featureKey) => $"Feature:{featureKey}";

        public static string? FeatureForUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            var path = url.Split('?', '#')[0].TrimEnd('/');
            var lowerPath = path.ToLowerInvariant();
            if (lowerPath.StartsWith("/service/", StringComparison.Ordinal)) return Services;
            if (lowerPath.StartsWith("/article/", StringComparison.Ordinal)) return Blog;
            if (lowerPath.StartsWith("/verify/card", StringComparison.Ordinal)) return CardVerification;
            if (lowerPath.StartsWith("/partner/", StringComparison.Ordinal) || lowerPath.StartsWith("/documents/", StringComparison.Ordinal)) return PartnerPortal;
            if (lowerPath.StartsWith("/account/", StringComparison.Ordinal) || lowerPath.StartsWith("/panel/", StringComparison.Ordinal)) return SiteAccount;

            return lowerPath switch
            {
                "/ourservices" or "/siteproject" or "/seoproject" or "/contentproject" => Services,
                "/portfolio" => Portfolio,
                "/tariff" => Pricing,
                "/articles" => Blog,
                "/aboutus" => About,
                "/contactus" => Contact,
                "/orderproject" => ProjectOrder,
                "/partner" or "/partner/account/login" or "/documents" => PartnerPortal,
                "/account/login" or "/account/register" or "/panel" => SiteAccount,
                _ => null,
            };
        }
    }

    public interface ISiteFeatureService
    {
        Task<bool> IsEnabledAsync(string key, CancellationToken ct = default);
        Task<IReadOnlyList<SiteFeatureState>> GetAllAsync(CancellationToken ct = default);
        Task SetEnabledAsync(string key, bool enabled, CancellationToken ct = default);
    }
}

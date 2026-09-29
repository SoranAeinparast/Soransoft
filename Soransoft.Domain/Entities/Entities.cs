using Soransoft.Domain.Abstractions;
using Soransoft.Domain.Enums;

namespace Soransoft.Domain.Entities
{
    /// <summary>کاربر سایت (ورود/عضویت)</summary>
    public class SiteUser : BaseDeletableEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }

        public virtual ICollection<ProjectOrder> ProjectOrders { get; set; } = new List<ProjectOrder>();
    }

    /// <summary>سرویس (طراحی سایت، سئو، اپلیکیشن، تولید محتوا)</summary>
    public class Service : BaseDeletableEntity
    {
        public ServiceKind Kind { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        /// <summary>متن کامل صفحه‌ی سرویس (HTML)</summary>
        public string FullDescription { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        /// <summary>نشان «به زودی» بودن سرویس</summary>
        public bool ComingSoon { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        /// <summary>عنوان زنگ‌انگیز صفحه سرویس (مثل: برای اولین بودن باید بهترین بود)</summary>
        public string Slogan { get; set; } = string.Empty;

        public virtual ICollection<ServiceFeature> Features { get; set; } = new List<ServiceFeature>();
        public virtual ICollection<ServiceStep> Steps { get; set; } = new List<ServiceStep>();
        public virtual ICollection<Portfolio> Portfolios { get; set; } = new List<Portfolio>();
    }

    /// <summary>ویژگی/مزیت هر سرویس (آیتم‌های تعرفه یا کارت‌های مزایا)</summary>
    public class ServiceFeature : BaseEntity
    {
        public int ServiceId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }

        public virtual Service Service { get; set; } = null!;
    }

    /// <summary>مرحله‌ی اجرای سرویس در تایم‌لاین «هوشمندانه انتخاب کن»</summary>
    public class ServiceStep : BaseEntity
    {
        public int ServiceId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }

        public virtual Service Service { get; set; } = null!;
    }

    /// <summary>نمونه‌کار</summary>
    public class Portfolio : BaseDeletableEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string? Url { get; set; }
        public int? ServiceId { get; set; }
        public bool ComingSoon { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual Service? Service { get; set; }
    }

    /// <summary>دسته‌بندی مقالات</summary>
    public class ArticleCategory : BaseDeletableEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual ICollection<Article> Articles { get; set; } = new List<Article>();
    }

    /// <summary>مقاله‌ی بلاگ</summary>
    public class Article : BaseDeletableEntity
    {
        public string Title { get; set; } = string.Empty;
        /// <summary>خلاصه برای لیست و متا دیسکریپشن</summary>
        public string Summary { get; set; } = string.Empty;
        /// <summary>متن کامل HTML</summary>
        public string Body { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public int ArticleCategoryId { get; set; }
        public DateTime PublishedAt { get; set; }
        public PublishStatus Status { get; set; } = PublishStatus.Draft;
        public int VisitCount { get; set; }
        public string Slug { get; set; } = string.Empty;
        public bool IsFeatured { get; set; }

        public virtual ArticleCategory ArticleCategory { get; set; } = null!;
    }

    /// <summary>بخش تعرفه (طراحی سایت، سئو، اپلیکیشن، تولید محتوا)</summary>
    public class TariffSection : BaseDeletableEntity
    {
        public ServiceKind Kind { get; set; }
        public string Title { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual ICollection<TariffPackage> Packages { get; set; } = new List<TariffPackage>();
    }

    /// <summary>پکیج قیمت‌گذاری داخل هر بخش تعرفه</summary>
    public class TariffPackage : BaseDeletableEntity
    {
        public int TariffSectionId { get; set; }
        public string Title { get; set; } = string.Empty;
        /// <summary>قیمت به تومان؛ null یعنی «تماس بگیرید»</summary>
        public long? Price { get; set; }
        /// <summary>برچسب جای قیمت (به زودی / تماس بگیرید)</summary>
        public string? PriceNote { get; set; }
        /// <summary>متن پله قیمت برای تولید محتوا (مثل: کلمه‌ای ۱۲۰)</summary>
        public string? PriceSuffix { get; set; }
        public string? Duration { get; set; }
        public bool IsInstallmentAvailable { get; set; }
        public bool HasFreeSupport { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsPopular { get; set; }

        public virtual TariffSection TariffSection { get; set; } = null!;
        public virtual ICollection<TariffItem> Items { get; set; } = new List<TariffItem>();
    }

    /// <summary>ردیف ویژگی هر پکیج تعرفه</summary>
    public class TariffItem : BaseEntity
    {
        public int TariffPackageId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }

        public virtual TariffPackage TariffPackage { get; set; } = null!;
    }

    /// <summary>سفارش پروژه (فرم سفارش پروژه)</summary>
    public class ProjectOrder : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string? Email { get; set; }
        public ServiceKind ProjectType { get; set; }
        public long? Budget { get; set; }
        public string Description { get; set; } = string.Empty;
        public ProjectOrderStatus Status { get; set; } = ProjectOrderStatus.New;
        public string? AdminNote { get; set; }
        public int? SiteUserId { get; set; }

        public virtual SiteUser? SiteUser { get; set; }
    }

    /// <summary>پیام فرم تماس با ما</summary>
    public class ContactMessage : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public MessageStatus Status { get; set; } = MessageStatus.Unread;
        public string? AdminNote { get; set; }
        public string? IpAddress { get; set; }
    }

    /// <summary>درخواست مشاوره رایگان (فرم صفحه تعرفه)</summary>
    public class ConsultationRequest : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public ServiceKind ServiceKind { get; set; }
        public string? Description { get; set; }
        public MessageStatus Status { get; set; } = MessageStatus.Unread;
        public string? AdminNote { get; set; }
    }

    /// <summary>عضو تیم (درباره ما)</summary>
    public class TeamMember : BaseDeletableEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string? LinkedinUrl { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>اسلایدر صفحه اصلی</summary>
    public class Slider : BaseDeletableEntity
    {
        public string Title { get; set; } = string.Empty;
        public string SubTitle { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string? Link { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>بنر پرومو با چند نوع و محل نمایش</summary>
    public class PromoBanner : BaseDeletableEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string? Badge { get; set; }
        public string? ButtonText { get; set; }
        public string? Image { get; set; }
        public string? Link { get; set; }
        public PromoBannerType Type { get; set; } = PromoBannerType.FeaturedCard;
        public PromoBannerScrollDirection ScrollDirection { get; set; } = PromoBannerScrollDirection.RightToLeft;
        public PromoBannerPlacement Placement { get; set; } = PromoBannerPlacement.HomeAfterHero;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public bool OpenInNewTab { get; set; }
    }

    /// <summary>تنظیمات سایت (کلید/مقدار)</summary>
    public class SiteSetting : BaseEntity
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Group { get; set; } = "General";
        /// <summary>نوع رندر: text/textarea/image</summary>
        public string Type { get; set; } = "text";
        public int DisplayOrder { get; set; }
    }

    /// <summary>منوی سایت</summary>
    public class MenuItem : BaseDeletableEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public MenuPosition Position { get; set; } = MenuPosition.Main;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>مدیر سایت (پنل ادمین)</summary>
    public class Admin : BaseDeletableEntity
    {
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>لاگ خطاهای برنامه (پنل ادمین)</summary>
    public class ErrorLog : BaseEntity
    {
        public string Message { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public string? Source { get; set; }
        public string? Path { get; set; }
        public string? HttpMethod { get; set; }
        public string? IpAddress { get; set; }
        public string? UserName { get; set; }
        public string Severity { get; set; } = "Error";
    }
}

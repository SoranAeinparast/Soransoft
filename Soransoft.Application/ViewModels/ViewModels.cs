using System.ComponentModel.DataAnnotations;
using Soransoft.Domain.Entities;
using Soransoft.Domain.Enums;

namespace Soransoft.Application.ViewModels
{
    /// <summary>فرم سفارش پروژه</summary>
    public class OrderProjectViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "نام و نام خانوادگی الزامی است")]
        [StringLength(150)]
        [Display(Name = "نام و نام خانوادگی")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "شماره موبایل الزامی است")]
        [StringLength(20)]
        [Display(Name = "شماره موبایل")]
        public string Mobile { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "ایمیل معتبر نیست")]
        [Display(Name = "ایمیل (اختیاری)")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "انتخاب نوع پروژه الزامی است")]
        [Display(Name = "نوع پروژه")]
        public ServiceKind ProjectType { get; set; }

        [Range(0, long.MaxValue, ErrorMessage = "بودجه معتبر نیست")]
        [Display(Name = "بودجه (تومان)")]
        public long? Budget { get; set; }

        [Required(ErrorMessage = "توضیحات پروژه الزامی است")]
        [StringLength(4000)]
        [Display(Name = "توضیحات")]
        public string Description { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Mobile is not null && !System.Text.RegularExpressions.Regex.IsMatch(Mobile, @"^09\d{9}$"))
                yield return new ValidationResult("شماره موبایل باید با 09 شروع شود و 11 رقم باشد", new[] { nameof(Mobile) });
        }
    }

    /// <summary>فرم تماس با ما</summary>
    public class ContactUsViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "نام و نام خانوادگی الزامی است")]
        [StringLength(150)]
        [Display(Name = "نام و نام خانوادگی")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "شماره موبایل الزامی است")]
        [StringLength(20)]
        [Display(Name = "شماره موبایل")]
        public string Mobile { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "ایمیل معتبر نیست")]
        [Display(Name = "ایمیل (اختیاری)")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "موضوع پیام الزامی است")]
        [StringLength(200)]
        [Display(Name = "موضوع")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "متن پیام الزامی است")]
        [StringLength(4000)]
        [Display(Name = "متن پیام")]
        public string Message { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Mobile is not null && !System.Text.RegularExpressions.Regex.IsMatch(Mobile, @"^09\d{9}$"))
                yield return new ValidationResult("شماره موبایل باید با 09 شروع شود و 11 رقم باشد", new[] { nameof(Mobile) });
        }
    }

    /// <summary>فرم درخواست مشاوره رایگان</summary>
    public class ConsultationRequestViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "نام و نام خانوادگی الزامی است")]
        [StringLength(150)]
        [Display(Name = "نام و نام خانوادگی")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "شماره موبایل الزامی است")]
        [StringLength(20)]
        [Display(Name = "شماره موبایل")]
        public string Mobile { get; set; } = string.Empty;

        [Display(Name = "سرویس موردنظر")]
        public ServiceKind ServiceKind { get; set; }

        [StringLength(1000)]
        [Display(Name = "توضیحات")]
        public string? Description { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(Mobile ?? string.Empty, @"^09\d{9}$"))
                yield return new ValidationResult("شماره موبایل باید با 09 شروع شود و 11 رقم باشد", new[] { nameof(Mobile) });
            if (!Enum.IsDefined(typeof(ServiceKind), ServiceKind))
                yield return new ValidationResult("انتخاب سرویس الزامی است", new[] { nameof(ServiceKind) });
        }
    }

    /// <summary>نتیجه‌ی عمومی AJAX</summary>
    public class OperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }

        public static OperationResult Ok(string message = "با موفقیت انجام شد", object? data = null) =>
            new() { Success = true, Message = message, Data = data };

        public static OperationResult Fail(string message) => new() { Success = false, Message = message };
    }

    /// <summary>آیتم منو برای ویو</summary>
    public class MenuItemViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? FeatureKey { get; set; }
    }

    /// <summary>مدل ویوی صفحه اصلی</summary>
    public class HomeViewModel
    {
        public List<MenuItemViewModel> MainMenu { get; set; } = new();
        public List<MenuItemViewModel> FooterMenu { get; set; } = new();
        public List<ServiceCardViewModel> Services { get; set; } = new();
        public List<PortfolioCardViewModel> Portfolios { get; set; } = new();
        public List<ArticleCardViewModel> LatestArticles { get; set; } = new();
        public List<TariffSectionViewModel> TariffSections { get; set; } = new();
        public List<ServiceTimelineViewModel> Timelines { get; set; } = new();
        public List<PromoBanner> PromoBanners { get; set; } = new();
        public IDictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();
        public IDictionary<string, bool> Features { get; set; } = new Dictionary<string, bool>();
    }

    /// <summary>کارت سرویس</summary>
    public class ServiceCardViewModel
    {
        public int Id { get; set; }
        public ServiceKind Kind { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Icon { get; set; } = "bi-globe2";
        public string Image { get; set; } = string.Empty;
        public bool ComingSoon { get; set; }
        public string? CtaText { get; set; }
    }

    /// <summary>کارت نمونه‌کار</summary>
    public class PortfolioCardViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string? Url { get; set; }
        public bool ComingSoon { get; set; }
        public string ServiceTitle { get; set; } = string.Empty;
    }

    /// <summary>کارت مقاله</summary>
    public class ArticleCardViewModel
    {
        public int Id { get; set; }
        /// <summary>اسلاگ تمیز برای URL؛ در صورت خالی بودن از Id استفاده می‌شود</summary>
        public string Slug { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string CategoryTitle { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public int VisitCount { get; set; }
        public bool IsFeatured { get; set; }
    }

    /// <summary>بخش تعرفه برای ویو</summary>
    public class TariffSectionViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public ServiceKind Kind { get; set; }
        public List<TariffPackageViewModel> Packages { get; set; } = new();
    }

    /// <summary>پکیج تعرفه برای ویو</summary>
    public class TariffPackageViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public long? Price { get; set; }
        public string? PriceNote { get; set; }
        public string? PriceSuffix { get; set; }
        public string? Duration { get; set; }
        public bool IsInstallmentAvailable { get; set; }
        public bool HasFreeSupport { get; set; }
        public bool IsPopular { get; set; }
        public List<string> Items { get; set; } = new();
    }

    /// <summary>تایم‌لاین مراحل سرویس</summary>
    public class ServiceTimelineViewModel
    {
        public int ServiceId { get; set; }
        public string ServiceTitle { get; set; } = string.Empty;
        public ServiceKind Kind { get; set; }
        public string ApproximateTime { get; set; } = "زمان تقریبی";
        public List<string> Steps { get; set; } = new();
    }
}

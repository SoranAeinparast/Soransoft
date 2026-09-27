using Soransoft.Domain.Enums;

namespace Soransoft.Web.Models
{
    /// <summary>کمکی‌های نمایشی وضعیت/نوع سفارش برای ویوهای پنل کاربری</summary>
    public static class PanelDisplay
    {
        public static string OrderTypeTitle(ServiceKind kind) => kind switch
        {
            ServiceKind.Site => "طراحی سایت",
            ServiceKind.Seo => "سئو",
            ServiceKind.Application => "اپلیکیشن موبایل",
            _ => "تولید محتوا",
        };

        public static string StatusTitle(ProjectOrderStatus status) => status switch
        {
            ProjectOrderStatus.New => "جدید",
            ProjectOrderStatus.InReview => "در حال بررسی",
            ProjectOrderStatus.Approved => "تایید شده",
            ProjectOrderStatus.InProgress => "در حال انجام",
            ProjectOrderStatus.Delivered => "تحویل شده",
            _ => "لغو شده",
        };

        public static string StatusBadgeClass(ProjectOrderStatus status) => status switch
        {
            ProjectOrderStatus.New => "sn-badge-new",
            ProjectOrderStatus.InReview => "sn-badge-review",
            ProjectOrderStatus.Approved => "sn-badge-approved",
            ProjectOrderStatus.InProgress => "sn-badge-progress",
            ProjectOrderStatus.Delivered => "sn-badge-delivered",
            _ => "sn-badge-cancelled",
        };
    }
}

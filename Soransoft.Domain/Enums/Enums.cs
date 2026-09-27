namespace Soransoft.Domain.Enums
{
    /// <summary>نوع سرویس‌های ارائه‌شده در سایت</summary>
    public enum ServiceKind
    {
        /// <summary>طراحی سایت</summary>
        Site = 1,
        /// <summary>سئو</summary>
        Seo = 2,
        /// <summary>اپلیکیشن موبایل</summary>
        Application = 3,
        /// <summary>تولید محتوا</summary>
        Content = 4
    }

    /// <summary>وضعیت سفارش پروژه</summary>
    public enum ProjectOrderStatus
    {
        /// <summary>جدید</summary>
        New = 1,
        /// <summary>در حال بررسی</summary>
        InReview = 2,
        /// <summary>تایید شده</summary>
        Approved = 3,
        /// <summary>در حال انجام</summary>
        InProgress = 4,
        /// <summary>تحویل شده</summary>
        Delivered = 5,
        /// <summary>لغو شده</summary>
        Cancelled = 6
    }

    /// <summary>وضعیت پیام تماس / درخواست مشاوره</summary>
    public enum MessageStatus
    {
        /// <summary>خوانده‌نشده</summary>
        Unread = 1,
        /// <summary>خوانده‌شده</summary>
        Read = 2,
        /// <summary>پاسخ داده شده</summary>
        Answered = 3
    }

    /// <summary>وضعیت انتشار محتوا</summary>
    public enum PublishStatus
    {
        /// <summary>پیش‌نویس</summary>
        Draft = 1,
        /// <summary>منتشر شده</summary>
        Published = 2,
        /// <summary>بایگانی</summary>
        Archived = 3
    }

    /// <summary>نوع فایل آپلودی</summary>
    public enum FileType
    {
        /// <summary>تصویر</summary>
        Image = 1,
        /// <summary>سند</summary>
        Document = 2
    }

    /// <summary>جهت نمایش منو</summary>
    public enum MenuPosition
    {
        /// <summary>منوی اصلی دسکتاپ</summary>
        Main = 1,
        /// <summary>منوی موبایل</summary>
        Mobile = 2,
        /// <summary>فوتر</summary>
        Footer = 3
    }
}

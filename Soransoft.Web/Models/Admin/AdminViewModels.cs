using Soransoft.Domain.Entities;

namespace Soransoft.Web.Models.Admin
{
    public class DashboardViewModel
    {
        public int TotalOrders { get; set; }
        public int NewOrders { get; set; }
        public int UnreadMessages { get; set; }
        public int UnreadConsultations { get; set; }
        public int TotalArticles { get; set; }
        public int TotalPortfolios { get; set; }
        public List<ProjectOrder> LatestOrders { get; set; } = new();
        public List<ContactMessage> LatestMessages { get; set; } = new();

        // ---------- داده‌های نمودارها (Chart.js) ----------

        /// <summary>برچسب ۳۰ روز اخیر (تاریخ کوتاه)</summary>
        public List<string> Last30DaysLabels { get; set; } = new();

        /// <summary>تعداد سفارش در هر روز</summary>
        public List<int> OrdersPerDay { get; set; } = new();

        /// <summary>مجموع بازدید مقالات در هر روز</summary>
        public List<int> ArticleVisitsPerDay { get; set; } = new();

        /// <summary>تعداد پیام و مشاوره در هر روز</summary>
        public List<int> MessagesPerDay { get; set; } = new();
        public List<int> ConsultationsPerDay { get; set; } = new();

        /// <summary>تعداد خطاهای ثبت‌شده در هر روز</summary>
        public List<int> ErrorsPerDay { get; set; } = new();

        /// <summary>توزیع سفارش‌ها بر اساس نوع سرویس</summary>
        public List<string> OrderTypeLabels { get; set; } = new();
        public List<int> OrderTypeCounts { get; set; } = new();

        /// <summary>مجموع بازدید مقالات (کارت آمار)</summary>
        public int TotalArticleVisits { get; set; }

        // ---------- پرتال همکاران: فروش ماهانه و پیشرفت پروژه‌ها ----------

        /// <summary>فروش ماهانه همکاران — از اقساط پرداخت‌شده قراردادها (۱۲ ماه اخیر)</summary>
        public List<MonthlySalesPoint> MonthlySales { get; set; } = new();

        /// <summary>فروش ماهانه فقط قراردادهای فعال/خاتمه‌یافته (بدون لغوشده) — برای مجموع خط روند</summary>
        public List<long> MonthlySalesTotals => MonthlySales.Select(m => m.Total).ToList();

        /// <summary>پیشرفت پروژه‌های فنی بر اساس وضعیت تسک‌ها</summary>
        public List<ProjectProgressPoint> ProjectProgress { get; set; } = new();

        /// <summary>خطاهای ۲۴ ساعت اخیر</summary>
        public int RecentErrors { get; set; }

        // ---------- آمار محتوا و سیستم ----------
        public int TotalServices { get; set; }
        public int TotalSliders { get; set; }
        public int TotalTariffPackages { get; set; }
        public int TotalTeamMembers { get; set; }
        public int TotalSiteUsers { get; set; }
        public int TotalMediaFiles { get; set; }
        public int TotalSitePages { get; set; }
    }

    public class SlidersIndexViewModel
    {
        public List<Slider> Sliders { get; set; } = new();
        public SliderSettingsViewModel Settings { get; set; } = new();
    }

    public class SliderSettingsViewModel
    {
        public string TransitionEffect { get; set; } = "fade";
        public int Height { get; set; } = 560;
        public decimal OverlayOpacity { get; set; } = .25m;
        public int Interval { get; set; } = 5000;
    }

    /// <summary>یک نقطه نمودار فروش ماهانه</summary>
    public class MonthlySalesPoint
    {
        /// <summary>برچسب ماه شمسی مثل «7 / 1404»</summary>
        public string Label { get; set; } = string.Empty;
        /// <summary>مجموع مبالغ اقساط پرداخت‌شده در آن ماه (تومان)</summary>
        public long Total { get; set; }
        /// <summary>فروش تفکیکی هر همکار فروش در آن ماه (نام → مبلغ)</summary>
        public Dictionary<string, long> ByPartner { get; set; } = new(StringComparer.Ordinal);
    }

    /// <summary>یک نقطه نمودار پیشرفت پروژه فنی</summary>
    public class ProjectProgressPoint
    {
        public string Title { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Done { get; set; }
        public int Testing { get; set; }
        public int InProgress { get; set; }
        public int ToDo { get; set; }
        /// <summary>درصد پیشرفت = (Done + نصف Testing) / کل</summary>
        public int Percent { get; set; }
    }
}

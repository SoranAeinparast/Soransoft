namespace Soransoft.Application.ViewModels
{
    /// <summary>یک فایل تصویری در کتابخانه رسانه</summary>
    public class MediaFileItem
    {
        /// <summary>مسیر نسبی عمومی فایل (مثل /uploads/editor/xxx.png)</summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>نام پوشه (editor، articles و …)</summary>
        public string Folder { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public long SizeBytes { get; set; }

        public DateTime ModifiedAt { get; set; }

        public bool IsImage { get; set; }
    }

    /// <summary>یک زیرپوشه داخل wwwroot/uploads</summary>
    public class MediaFolder
    {
        public string Name { get; set; } = string.Empty;

        public int FileCount { get; set; }
    }

    /// <summary>ارجاع یک تصویر در محتوای سایت (مقاله، سرویس و …)</summary>
    public class MediaUsageRef
    {
        /// <summary>عنوان نوع محتوا — «مقاله»، «سرویس» و …</summary>
        public string KindTitle { get; set; } = string.Empty;

        /// <summary>عنوان رکورد استفاده‌کننده</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>نام فیلد استفاده‌کننده — Image، Body و …</summary>
        public string Field { get; set; } = string.Empty;

        /// <summary>لینک ویرایش در پنل ادمین (در صورت وجود)</summary>
        public string? EditUrl { get; set; }
    }

    /// <summary>آمار فضای ذخیره‌سازی یک پوشه آپلود</summary>
    public class StorageFolderStats
    {
        /// <summary>نام پوشه — «(root)» برای فایل‌های مستقیم زیر uploads</summary>
        public string Name { get; set; } = string.Empty;

        public int FileCount { get; set; }

        public long SizeBytes { get; set; }

        /// <summary>فایل‌های بدون هیچ ارجاع در محتوا</summary>
        public int OrphanCount { get; set; }

        public long OrphanSizeBytes { get; set; }
    }

    /// <summary>نمای کلی فضای ذخیره‌سازی آپلودها</summary>
    public class StorageOverview
    {
        public List<StorageFolderStats> Folders { get; set; } = new();

        /// <summary>فایل‌های بدون ارجاع (یَتیم) — قابل پاکسازی</summary>
        public List<MediaFileItem> OrphanFiles { get; set; } = new();

        public int TotalFiles { get; set; }

        public long TotalSizeBytes { get; set; }

        public int TotalOrphans => OrphanFiles.Count;

        public long TotalOrphanSizeBytes => OrphanFiles.Sum(f => f.SizeBytes);

        public long DriveFreeBytes { get; set; }

        public long DriveTotalBytes { get; set; }
    }
}

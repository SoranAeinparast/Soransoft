namespace Soransoft.Domain.Abstractions
{
    /// <summary>پایه‌ی موجودیت‌ها؛ ثبت خودکار تاریخ ایجاد</summary>
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>موجودیت‌های قابل حذف نرم</summary>
    public abstract class BaseDeletableEntity : BaseEntity
    {
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}

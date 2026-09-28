using Soransoft.Domain.Abstractions;

namespace Soransoft.Domain.Entities
{
    /// <summary>نقش همکار در پرتال</summary>
    public enum PartnerRole
    {
        /// <summary>همکار فروش</summary>
        Sales = 1,
        /// <summary>توسعه‌دهنده</summary>
        Developer = 2,
        /// <summary>مدیر فروش (دیدن همه قراردادها/لیدها)</summary>
        SalesManager = 3,
        /// <summary>مدیر فنی</summary>
        TechManager = 4
    }

    /// <summary>مرحله قیف فروش لید</summary>
    public enum LeadStage
    {
        /// <summary>جدید</summary>
        New = 1,
        /// <summary>در حال مذاکره</summary>
        Negotiating = 2,
        /// <summary>قرارداد بسته شده</summary>
        Contracted = 3,
        /// <summary>منصرف شده</summary>
        Lost = 4
    }

    /// <summary>نتیجه بررسی اولیه لید توسط نماینده تیم توسعه</summary>
    public enum LeadReviewStatus
    {
        Pending = 1,
        Approved = 2,
        NeedsMoreReview = 3,
        Rejected = 4
    }

    /// <summary>میزان مذاکره انجام‌شده با مشتری</summary>
    public enum LeadNegotiationLevel
    {
        Initial = 1,
        Medium = 2,
        Serious = 3
    }

    /// <summary>وضعیت قرارداد</summary>
    public enum ContractStatus
    {
        /// <summary>در انتظار تایید مدیر</summary>
        PendingApproval = 1,
        /// <summary>فعال</summary>
        Active = 2,
        /// <summary>خاتمه یافته</summary>
        Finished = 3,
        /// <summary>لغو شده</summary>
        Cancelled = 4
    }

    /// <summary>
    /// وضعیت مرحله پرداخت مشتری (پرداخت مرحله‌ای بر اساس مفاد قرارداد، نه قسط).
    /// مقادیر عددی با نسخه قبلی (اقساط) سازگار نگه داشته شده تا داده موجود سالم بماند.
    /// </summary>
    public enum PaymentStageStatus
    {
        /// <summary>تعریف‌شده / پرداخت‌نشده</summary>
        Planned = 1,
        /// <summary>در انتظار تایید رسید</summary>
        AwaitingApproval = 2,
        /// <summary>پرداخت شده (تاییدشده)</summary>
        Paid = 3,
        /// <summary>رسید رد شده</summary>
        Rejected = 4
    }

    /// <summary>نوع هزینه شخص ثالث (کاتالوگ مدیر)</summary>
    public enum ThirdPartyCostType
    {
        /// <summary>هاست / سرور</summary>
        Hosting = 1,
        /// <summary>دامنه</summary>
        Domain = 2,
        /// <summary>گواهینامه و مجوز (SSL و...)</summary>
        Certificate = 3,
        /// <summary>سرویس و API (سامانه پیامکی و...)</summary>
        Service = 4,
        /// <summary>سایر</summary>
        Other = 5
    }

    /// <summary>روش محاسبه پورسانت</summary>
    public enum CommissionMethod
    {
        /// <summary>درصدی ثابت روی مبلغ قرارداد</summary>
        FixedPercent = 1,
        /// <summary>مبلغ ثابت (تومان)</summary>
        FixedAmount = 2,
        /// <summary>پله‌ای — درصد هر پله روی بخشی از فروش</summary>
        Tiered = 3
    }

    /// <summary>وضعیت تیکت پشتیبانی فنی</summary>
    public enum TicketStatus
    {
        /// <summary>جدید</summary>
        New = 1,
        /// <summary>در حال بررسی</summary>
        InProgress = 2,
        /// <summary>پاسخ داده شد</summary>
        Answered = 3,
        /// <summary>بسته شده</summary>
        Closed = 4
    }

    /// <summary>وضعیت تسک</summary>
    public enum TaskStatus
    {
        /// <summary>انجام نشده</summary>
        ToDo = 1,
        /// <summary>در حال انجام</summary>
        InProgress = 2,
        /// <summary>در حال تست</summary>
        Testing = 3,
        /// <summary>انجام شده</summary>
        Done = 4
    }

    /// <summary>اولویت تسک</summary>
    public enum TaskPriority
    {
        /// <summary>کم</summary>
        Low = 1,
        /// <summary>متوسط</summary>
        Medium = 2,
        /// <summary>زیاد</summary>
        High = 3,
        /// <summary>بحرانی</summary>
        Critical = 4
    }

    /// <summary>حساب همکار پرتال (فروش / توسعه‌دهنده) — ورود فقط از /Partner/Login</summary>
    public class Partner : BaseDeletableEntity
    {
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        /// <summary>برای ثبت رسمی فرم معرفی مشتری</summary>
        public string? NationalId { get; set; }
        public string? FatherName { get; set; }
        public string? BirthCertificateNumber { get; set; }
        public string? BirthPlace { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? Landline { get; set; }
        public string? Address { get; set; }
        public string? PostalCode { get; set; }
        /// <summary>تصویر پرسنلی خصوصی در App_Data</summary>
        public string? PersonalPhotoPath { get; set; }
        public string? NationalCardFrontPath { get; set; }
        public string? NationalCardBackPath { get; set; }
        public string? BirthCertificatePath { get; set; }
        public string? IdentityDocumentPath { get; set; }
        /// <summary>نقش همکار (فروش، توسعه‌دهنده، مدیر فروش، مدیر فنی)</summary>
        public PartnerRole Role { get; set; } = PartnerRole.Sales;
        /// <summary>سطح دسترسی ارشد: دیدن لیدها/قراردادها/گزارش‌های همه همکاران</summary>
        public bool CanSeeAllSalesData { get; set; }
        /// <summary>سطح تخصص فنی (Junior/Senior و...) — فقط توسعه‌دهندگان</summary>
        public string? TechLevel { get; set; }
        /// <summary>مهارت‌ها (Stack) — فقط توسعه‌دهندگان</summary>
        public string? Skills { get; set; }
        /// <summary>یادداشت مدیر درباره همکار</summary>
        public string? AdminNote { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }

        public virtual ICollection<Lead> Leads { get; set; } = new List<Lead>();
        public virtual ICollection<PartnerContract> Contracts { get; set; } = new List<PartnerContract>();
        public virtual ICollection<DevTask> Tasks { get; set; } = new List<DevTask>();
        public virtual ICollection<TimeLog> TimeLogs { get; set; } = new List<TimeLog>();
        public virtual ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();
        public virtual ICollection<PartnerDocument> Documents { get; set; } = new List<PartnerDocument>();
    }

    /// <summary>مدرک خصوصی بارگذاری‌شده توسط همکار</summary>
    public class PartnerDocument : BaseDeletableEntity
    {
        public int PartnerId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string StoredPath { get; set; } = string.Empty;
        public Partner Partner { get; set; } = null!;
    }

    /// <summary>لید فروش (مشتری احتمالی) — ثبت توسط فروشنده، تایید توسط مدیر</summary>
    public class Lead : BaseDeletableEntity
    {
        public int PartnerId { get; set; }
        public string? FormNo { get; set; }
        public DateTime IntroducedAt { get; set; } = DateTime.Now;
        public string? CustomerCode { get; set; }
        public bool IsFollowUp { get; set; }

        // snapshot مشخصات همکار در زمان ثبت فرم
        public string? SalesPartnerNationalId { get; set; }
        public string? SalesPartnerAgreementNo { get; set; }
        public string? SalesPartnerBankAccount { get; set; }

        public string CustomerName { get; set; } = string.Empty;
        public string CustomerMobile { get; set; } = string.Empty;
        public string? BusinessName { get; set; }
        public string? DecisionMakerName { get; set; }
        public string? DecisionMakerRole { get; set; }
        public string? CustomerLandline { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CurrentWebsite { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? FullAddress { get; set; }
        public string? SocialMedia { get; set; }

        // وضعیت و نوع کسب‌وکار مشتری
        public bool? HasWebsite { get; set; }
        public string? BusinessType { get; set; }
        public string? CurrentSystem { get; set; }
        public bool? HasSimilarPlatform { get; set; }
        public DateTime? ExpectedStartDate { get; set; }
        public long? BudgetAmount { get; set; }

        /// <summary>نیاز / شرح درخواست مشتری</summary>
        public string Requirement { get; set; } = string.Empty;
        public string? SecondaryRequirements { get; set; }
        /// <summary>مبلغ تخمینی فروش (تومان)</summary>
        public long? EstimatedAmount { get; set; }

        // نحوه معرفی و وضعیت مذاکره
        public string? IntroductionMethod { get; set; }
        public DateTime? FirstContactDate { get; set; }
        public bool? DecisionMakerConfirmed { get; set; }
        public LeadNegotiationLevel? NegotiationLevel { get; set; }
        public bool? NearContract { get; set; }

        public LeadStage Stage { get; set; } = LeadStage.New;
        public LeadReviewStatus ReviewStatus { get; set; } = LeadReviewStatus.Pending;
        public bool? ExistingCustomer { get; set; }
        public string? SystemCustomerNo { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public int? ReviewedByAdminId { get; set; }
        public string? ReviewNote { get; set; }
        /// <summary>یادداشت مدیر</summary>
        public string? AdminNote { get; set; }
        /// <summary>تاریخ تبدیل به قرارداد</summary>
        public DateTime? ConvertedAt { get; set; }
        public virtual Partner Partner { get; set; } = null!;
        public virtual ICollection<LeadHistory> History { get; set; } = new List<LeadHistory>();
    }

    /// <summary>تاریخچه تغییر مرحله و بررسی رسمی لید</summary>
    public class LeadHistory : BaseEntity
    {
        public int LeadId { get; set; }
        public string Action { get; set; } = "StageChanged";
        public LeadStage FromStage { get; set; }
        public LeadStage ToStage { get; set; }
        public LeadReviewStatus? FromReviewStatus { get; set; }
        public LeadReviewStatus? ToReviewStatus { get; set; }
        public int? AdminId { get; set; }
        public string? Note { get; set; }
        public virtual Lead Lead { get; set; } = null!;
    }

    /// <summary>قرارداد فی‌مابین با مشتری — پس از تایید لید یا ثبت مستقیم توسط مدیر</summary>
    public class PartnerContract : BaseDeletableEntity
    {
        public string Title { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        /// <summary>مبلغ کل قرارداد (تومان)</summary>
        public long TotalAmount { get; set; }
        public ContractStatus Status { get; set; } = ContractStatus.PendingApproval;
        /// <summary>فروشنده مرتبط</summary>
        public int? PartnerId { get; set; }
        /// <summary>لید مبدأ (در صورت تبدیل شدن)</summary>
        public int? LeadId { get; set; }
        /// <summary>لایسنس/قرارداد تا تاریخ — برای محصولات</summary>
        public DateTime? LicenseExpiresAt { get; set; }
        /// <summary>فایل PDF قرارداد (مسیر نسبی)</summary>
        public string? ContractFile { get; set; }
        public string? Description { get; set; }
        public virtual Partner? Partner { get; set; }
        public virtual Lead? Lead { get; set; }
        public virtual ICollection<ContractPaymentStage> PaymentStages { get; set; } = new List<ContractPaymentStage>();
        public virtual ICollection<ContractThirdPartyCost> ThirdPartyCosts { get; set; } = new List<ContractThirdPartyCost>();
        public virtual ICollection<CommissionRate> CommissionRates { get; set; } = new List<CommissionRate>();
        public virtual ICollection<SupportTicket> Tickets { get; set; } = new List<SupportTicket>();
    }

    /// <summary>
    /// مرحله پرداخت مشتری در یک قرارداد — بر اساس مفاد قرارداد با مشتری تعریف و ویرایش می‌شود.
    /// مثال: مرحله اول هم‌زمان با امضای قرارداد ۳۰٪، مرحله دوم پس از راه‌اندازی اولیه ۴۰٪، مرحله سوم پس از تحویل قطعی ۳۰٪.
    /// برای هر مرحله امکان ثبت اسناد و مدارک و چک و رهگیری وجود دارد.
    /// </summary>
    public class ContractPaymentStage : BaseEntity
    {
        public int ContractId { get; set; }
        /// <summary>شماره ترتیب مرحله (۱، ۲، ۳، ...)</summary>
        public int Number { get; set; }
        /// <summary>عنوان مرحله (مثلاً: پرداخت اول هم‌زمان با امضای قرارداد)</summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>شرح/شرایط مرحله</summary>
        public string? Description { get; set; }
        public long Amount { get; set; }
        /// <summary>درصد این مرحله از مبلغ کل قرارداد (اختیاری، برای نمایش و محاسبه)</summary>
        public decimal? PercentOfTotal { get; set; }
        public DateTime? DueDate { get; set; }
        public PaymentStageStatus Status { get; set; } = PaymentStageStatus.Planned;
        /// <summary>تاریخ پرداخت واقعی (رهگیری)</summary>
        public DateTime? PaidAt { get; set; }
        /// <summary>مسیر فایل رسید پرداخت مشتری</summary>
        public string? ReceiptFile { get; set; }
        /// <summary>مسیر اسناد و مدارک / تصویر چک مرحله</summary>
        public string? DocumentFile { get; set; }
        /// <summary>توضیح سند/چک (مثلاً: چک ۹۰ روزه بانک ملت)</summary>
        public string? DocumentNote { get; set; }
        public string? ChequeNo { get; set; }
        public string? ChequeBank { get; set; }
        public DateTime? ChequeDueDate { get; set; }
        /// <summary>زمان بررسی/تایید مدارک مرحله (رهگیری)</summary>
        public DateTime? DocumentsVerifiedAt { get; set; }
        /// <summary>یادداشت مدیر هنگام تایید/رد</summary>
        public string? AdminNote { get; set; }
        public virtual PartnerContract Contract { get; set; } = null!;
    }

    /// <summary>آیتم کاتالوگ هزینه شخص ثالث — مدیر ایجاد/ویرایش/حذف می‌کند</summary>
    public class ThirdPartyCostItem : BaseDeletableEntity
    {
        public ThirdPartyCostType Type { get; set; } = ThirdPartyCostType.Other;
        public string Title { get; set; } = string.Empty;
        /// <summary>مشخصات فنی (مثلاً: هاست لینوکس ۱۰ گیگ، دامنه .ir، SSL Wildcard)</summary>
        public string? TechnicalSpecs { get; set; }
        /// <summary>هزینه پیش‌فرض (تومان) — هنگام افزودن به قرارداد قابل تغییر است</summary>
        public long DefaultAmount { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// هزینه شخص ثالث اعمال‌شده روی یک قرارداد مشتری.
    /// مجموع این هزینه‌ها از مبلغ کلی قرارداد (در اولین پرداخت مشتری) کسر می‌شود و
    /// باقی‌مانده، مبنای محاسبه پورسانت همکار فروش است.
    /// عنوان/مشخصات فنی در لحظه افزودن عکس‌برداری می‌شود تا تغییر کاتالوگ، قراردادهای گذشته را تغییر ندهد.
    /// </summary>
    public class ContractThirdPartyCost : BaseEntity
    {
        public int ContractId { get; set; }
        /// <summary>آیتم کاتالوگ مبدأ (در صورت ثبت دستی خالی است)</summary>
        public int? ItemId { get; set; }
        public ThirdPartyCostType Type { get; set; } = ThirdPartyCostType.Other;
        public string Title { get; set; } = string.Empty;
        public string? TechnicalSpecs { get; set; }
        public long Amount { get; set; }
        public string? Note { get; set; }
        /// <summary>آیا از اولین پرداخت مشتری کسر شود (پیش‌فرض بله)</summary>
        public bool DeductFromFirstPayment { get; set; } = true;
        public virtual PartnerContract Contract { get; set; } = null!;
        public virtual ThirdPartyCostItem? Item { get; set; }
    }

    /// <summary>نرخ پورسانت هر قرارداد برای هر همکار فروش — همه روش‌های مرسوم</summary>
    public class CommissionRate : BaseEntity
    {
        public int ContractId { get; set; }
        public int PartnerId { get; set; }
        public CommissionMethod Method { get; set; } = CommissionMethod.FixedPercent;
        /// <summary>درصد (برای FixedPercent) یا مبلغ ثابت تومان (برای FixedAmount)</summary>
        public decimal Value { get; set; }
        /// <summary>پله‌های روش Tiered به‌صورت JSON: [{"upTo":100000000,"percent":5},...]</summary>
        public string? TiersJson { get; set; }
        /// <summary>آیا پورسانت بر مراحل پرداخت‌شده مشتری محاسبه شود یا کل مبلغ قرارداد</summary>
        public bool BasedOnPaidStages { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public virtual PartnerContract Contract { get; set; } = null!;
        public virtual Partner Partner { get; set; } = null!;
    }

    /// <summary>درخواست بررسی فنی توسط فروشنده (بدون ورود به کدها)</summary>
    public class SupportTicket : BaseEntity
    {
        public int ContractId { get; set; }
        /// <summary>ثبت‌کننده (فروشنده)</summary>
        public int PartnerId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TicketStatus Status { get; set; } = TicketStatus.New;
        /// <summary>پاسخ تیم فنی/مدیر</summary>
        public string? Response { get; set; }
        public DateTime? AnsweredAt { get; set; }
        public virtual PartnerContract Contract { get; set; } = null!;
        public virtual Partner Partner { get; set; } = null!;
    }

    /// <summary>پروژه توسعه (مثل ACLEDA_BANK یا SugarShop)</summary>
    public class DevProject : BaseDeletableEntity
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        /// <summary>وضعیت کلی به‌صورت متن آزاد (مثل: در مرحله تست)</summary>
        public string? StatusText { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual ICollection<DevTask> Tasks { get; set; } = new List<DevTask>();
        public virtual ICollection<ProjectDocument> Documents { get; set; } = new List<ProjectDocument>();
        public virtual ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
        public virtual ICollection<TimeLog> TimeLogs { get; set; } = new List<TimeLog>();
    }

    /// <summary>عضویت توسعه‌دهنده در پروژه — مبناي سطح دسترسی مستندات</summary>
    public class ProjectMember : BaseEntity
    {
        public int ProjectId { get; set; }
        public int PartnerId { get; set; }
        public string? RoleInProject { get; set; }

        public virtual DevProject Project { get; set; } = null!;
        public virtual Partner Partner { get; set; } = null!;
    }

    /// <summary>اسپرینت (اسپرینت ۱، ۲ و...)</summary>
    public class Sprint : BaseDeletableEntity
    {
        public string Title { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Goal { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual ICollection<DevTask> Tasks { get; set; } = new List<DevTask>();
    }

    /// <summary>تسک تیم فنی — برد To Do / In Progress / Testing / Done</summary>
    public class DevTask : BaseDeletableEntity
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TaskStatus Status { get; set; } = TaskStatus.ToDo;
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        /// <summary>توسعه‌دهنده مسئول</summary>
        public int? PartnerId { get; set; }
        public int? ProjectId { get; set; }
        public int? SprintId { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int DisplayOrder { get; set; }

        public virtual Partner? Partner { get; set; }
        public virtual DevProject? Project { get; set; }
        public virtual Sprint? Sprint { get; set; }
        public virtual ICollection<TimeLog> TimeLogs { get; set; } = new List<TimeLog>();
    }

    /// <summary>ثبت زمان صرف‌شده روی پروژه (Time Tracking)</summary>
    public class TimeLog : BaseEntity
    {
        public int PartnerId { get; set; }
        public int? ProjectId { get; set; }
        public int? TaskId { get; set; }
        /// <summary>مدت به دقیقه</summary>
        public int Minutes { get; set; }
        public DateTime WorkDate { get; set; } = DateTime.Now;
        public string? Note { get; set; }

        public virtual Partner Partner { get; set; } = null!;
        public virtual DevProject? Project { get; set; }
        public virtual DevTask? Task { get; set; }
    }

    /// <summary>مستندات فنی (Wiki) — دسترسی فقط برای اعضای پروژه</summary>
    public class ProjectDocument : BaseDeletableEntity
    {
        public int ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        /// <summary>بدنه مستند (HTML از TinyMCE)</summary>
        public string Body { get; set; } = string.Empty;
        /// <summary>موضوع: Architecture / Database / API / ...</summary>
        public string Category { get; set; } = "General";
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual DevProject Project { get; set; } = null!;
    }

    /// <summary>نوع محتوای مستند پروژه قابل فروش</summary>
    public enum SellableDocumentKind
    {
        Upload = 1,
        Link = 2
    }

    /// <summary>پروژه آماده ارائه به مشتری برای همکاران فروش</summary>
    public class SellableProject : BaseDeletableEntity
    {
        public string Title { get; set; } = string.Empty;
        /// <summary>معرفی کامل پروژه (HTML پاک‌سازی‌شده)</summary>
        public string Description { get; set; } = string.Empty;
        /// <summary>تصویر شاخص عمومی پروژه در کاتالوگ همکار فروش</summary>
        public string? FeaturedImage { get; set; }
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; }

        public virtual ICollection<SellableProjectDocument> Documents { get; set; } = new List<SellableProjectDocument>();
        public virtual ICollection<SellableProjectComment> Comments { get; set; } = new List<SellableProjectComment>();
        public virtual ICollection<SellableProjectPartner> PartnerAccess { get; set; } = new List<SellableProjectPartner>();
    }

    /// <summary>مستند یا لینک مرتبط با پروژه قابل فروش</summary>
    public class SellableProjectDocument : BaseDeletableEntity
    {
        public int SellableProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        /// <summary>کاتالوگ، دمو، تبلیغات، فرم، سایر</summary>
        public string Category { get; set; } = "سایر";
        public SellableDocumentKind Kind { get; set; } = SellableDocumentKind.Upload;
        /// <summary>مسیر خصوصی فایل</summary>
        public string? StoredPath { get; set; }
        public string? ExternalUrl { get; set; }
        public string? OriginalFileName { get; set; }
        public string? ContentType { get; set; }
        public long? SizeBytes { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual SellableProject Project { get; set; } = null!;
    }

    /// <summary>کامنت و دستورالعمل داخلی مدیر درباره پروژه قابل فروش</summary>
    public class SellableProjectComment : BaseDeletableEntity
    {
        public int SellableProjectId { get; set; }
        public string Body { get; set; } = string.Empty;

        public virtual SellableProject Project { get; set; } = null!;
    }

    /// <summary>دسترسی اختصاصی یک همکار فروش به پروژه قابل ارائه</summary>
    public class SellableProjectPartner : BaseEntity
    {
        public int SellableProjectId { get; set; }
        public int PartnerId { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual SellableProject Project { get; set; } = null!;
        public virtual Partner Partner { get; set; } = null!;
    }

    /// <summary>اطلاعیه/دستور مدیر به همکاران</summary>
    public class Announcement : BaseDeletableEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        /// <summary>مخاطب: All / Sales / Dev</summary>
        public string Audience { get; set; } = "All";
        /// <summary>گیرنده مشخص (ارسال به یک همکار)</summary>
        public int? PartnerId { get; set; }
        public bool IsImportant { get; set; }
        public virtual Partner? Partner { get; set; }
    }

    /// <summary>پیام‌های خوانده‌شده هر همکار</summary>
    public class PartnerNotificationRead : BaseEntity
    {
        public int AnnouncementId { get; set; }
        public int PartnerId { get; set; }
        public DateTime ReadAt { get; set; } = DateTime.Now;

        public virtual Announcement Announcement { get; set; } = null!;
        public virtual Partner Partner { get; set; } = null!;
    }

    // ============================================================ قرارداد همکاری فی‌مابین

    /// <summary>نوع قرارداد همکاری بین مدیر و همکار</summary>
    public enum AgreementKind
    {
        /// <summary>همکاری فروش (پورسانتی)</summary>
        Sales = 1,
        /// <summary>همکاری فنی / توسعه‌دهنده</summary>
        Technical = 2
    }

    /// <summary>وضعیت قرارداد همکاری</summary>
    public enum AgreementStatus
    {
        /// <summary>پیش‌نویس</summary>
        Draft = 1,
        /// <summary>فعال (ملاک طرفین)</summary>
        Active = 2,
        /// <summary>خاتمه/فسخ</summary>
        Terminated = 3
    }

    /// <summary>
    /// قرارداد همکاری فی‌مابین مدیر و همکار — ملاک طرفین.
    /// هم PDF بارگذاری‌شده نگه می‌دارد و هم مفاد ساختاریافته (موضوع، مدت، مبلغ، پورسانت، حساب بانکی همکار و...)
    /// </summary>
    public class CooperationAgreement : BaseDeletableEntity
    {
        public AgreementKind Kind { get; set; } = AgreementKind.Sales;
        public AgreementStatus Status { get; set; } = AgreementStatus.Draft;

        public int PartnerId { get; set; }
        public virtual Partner Partner { get; set; } = null!;

        /// <summary>شماره قرارداد (مثل SA-1404-001)</summary>
        public string AgreementNo { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        /// <summary>موضوع قرارداد</summary>
        public string Subject { get; set; } = string.Empty;
        /// <summary>شرح مفاد و تعهدات (HTML آزاد)</summary>
        public string Terms { get; set; } = string.Empty;

        public DateTime StartDate { get; set; } = DateTime.Now;
        /// <summary>تاریخ پایان (null = بدون تاریخ پایان)</summary>
        public DateTime? EndDate { get; set; }

        /// <summary>مبلغ کل / سقف قرارداد (تومان) — برای قرارداد فنی مبلغ پروژه، برای فروشی راهنما</summary>
        public long? TotalAmount { get; set; }
        /// <summary>
        /// سقف نامحدود — همکار در بازه اعتبار قرارداد می‌تواند تعداد نامحدودی قرارداد با مشتری ببندد
        /// و پورسانت صرفاً بر اساس درصد توافق‌شده محاسبه می‌شود (مبلغ کل بی‌معنا است).
        /// </summary>
        public bool IsUnlimitedAmount { get; set; }

        // ---- مفاد مالی ----
        public CommissionMethod CommissionMethod { get; set; } = CommissionMethod.FixedPercent;
        /// <summary>درصد (FixedPercent) یا مبلغ ثابت (FixedAmount)</summary>
        public decimal CommissionValue { get; set; }
        /// <summary>پله‌های Tiered به‌صورت JSON</summary>
        public string? TiersJson { get; set; }
        /// <summary>آیا پورسانت بر مراحل پرداخت‌شده مشتری محاسبه شود</summary>
        public bool BasedOnPaidStages { get; set; } = true;
        /// <summary>ماهانه ثابت (تومان) — معمول قراردادهای فنی</summary>
        public long? MonthlySalary { get; set; }
        /// <summary>نحوه تسویه (متن آزاد: پایان هر ماه، پس از هر واریز مشتری و...)</summary>
        public string? SettlementTerms { get; set; }

        // ---- حساب بانکی همکار (مقصد تسویه) ----
        public string? BankName { get; set; }
        public string? BankAccountIban { get; set; }
        public string? BankAccountHolder { get; set; }

        /// <summary>مسیر فایل PDF قرارداد امضاشده</summary>
        public string? ContractFile { get; set; }
        /// <summary>تاریخ انعقاد</summary>
        public DateTime SignedAt { get; set; } = DateTime.Now;
        /// <summary>یادداشت مدیر</summary>
        public string? AdminNote { get; set; }
        public virtual ICollection<AgreementCancellationRequest> CancellationRequests { get; set; } = new List<AgreementCancellationRequest>();
    }

    /// <summary>درخواست فسخ قرارداد همکاری که باید توسط مدیر بررسی شود</summary>
    public enum AgreementCancellationStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3
    }

    public class AgreementCancellationRequest : BaseEntity
    {
        public int AgreementId { get; set; }
        public int PartnerId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime RequestedTerminationDate { get; set; } = DateTime.Now;
        public AgreementCancellationStatus Status { get; set; } = AgreementCancellationStatus.Pending;
        public string? AdminResponse { get; set; }
        public DateTime? DecidedAt { get; set; }
        public int? DecidedByAdminId { get; set; }

        public virtual CooperationAgreement Agreement { get; set; } = null!;
        public virtual Partner Partner { get; set; } = null!;
    }

    // ============================================================ کیف پول همکاران

    /// <summary>نوع تراکنش کیف پول</summary>
    public enum WalletTxType
    {
        /// <summary>واریز سهم همکار از مدیر</summary>
        Deposit = 1,
        /// <summary>برداشت (واریز به حساب همکار)</summary>
        Withdrawal = 2,
        /// <summary>اصلاحیه بدهی (منفی یا مثبت؛ فقط مدیر)</summary>
        Adjustment = 3
    }

    /// <summary>وضعیت تراکنش</summary>
    public enum WalletTxStatus
    {
        /// <summary>ثبت‌شده (نهایی)</summary>
        Confirmed = 1,
        /// <summary>در انتظار تایید مدیر (درخواست برداشت)</summary>
        Pending = 2,
        /// <summary>رد شده</summary>
        Rejected = 3
    }

    /// <summary>
    /// تراکنش کیف پول همکار — دفتر کل فقط-الحاقی (Append-only).
    /// موجودی = مجموع Amount تراکنش‌های Confirmed؛ ردیف‌ها هرگز Update نمی‌شوند.
    /// </summary>
    public class WalletTransaction : BaseEntity
    {
        public int PartnerId { get; set; }
        public virtual Partner Partner { get; set; } = null!;

        public WalletTxType Type { get; set; }
        public WalletTxStatus Status { get; set; } = WalletTxStatus.Confirmed;

        /// <summary>مبلغ امضادار: واریز +، برداشت −، اصلاحیه ±</summary>
        public decimal Amount { get; set; }

        /// <summary>توضیح تراکنش (ملاک ردپا)</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>موجودی کل دفتر پس از این تراکنش — برای راستی‌آزمایی سریع</summary>
        public decimal BalanceAfter { get; set; }

        /// <summary>مرجع (شماره قرارداد، شماره قسط و...)</summary>
        public string? Reference { get; set; }

        /// <summary>مسیر فایل مستند (فیش واریز / رسید پرداخت به همکار)</summary>
        public string? DocumentFile { get; set; }

        /// <summary>شماره پیگیری/اتورفضایی واریز بانکی</summary>
        public string? BankTrackingNo { get; set; }

        /// <summary>درخواست برداشت مرتبط (برای Deposit حاصل از تسویه درخواست)</summary>
        public int? WithdrawalRequestId { get; set; }

        /// <summary>تاریخ پرداخت واقعی به همکار</summary>
        public DateTime? PaidAt { get; set; }
    }

    /// <summary>درخواست برداشت همکار از کیف پول</summary>
    public class WithdrawalRequest : BaseEntity
    {
        public int PartnerId { get; set; }
        public virtual Partner Partner { get; set; } = null!;

        public decimal Amount { get; set; }
        /// <summary>توضیح/درخواست همکار</summary>
        public string? Note { get; set; }

        /// <summary>حساب مقصد — لحظه درخواست عکس‌برداری می‌شود تا تغییر قرارداد اثر بازگشتی نداشته باشد</summary>
        public string DestinationIban { get; set; } = string.Empty;
        public string DestinationBank { get; set; } = string.Empty;
        public string DestinationHolder { get; set; } = string.Empty;

        public WalletTxStatus Status { get; set; } = WalletTxStatus.Pending;
        /// <summary>پاسخ/یادداشت مدیر هنگام تایید یا رد</summary>
        public string? AdminResponse { get; set; }
        public DateTime? DecidedAt { get; set; }

        /// <summary>تراکنش برداشت ایجادشده پس از تایید</summary>
        public int? WalletTransactionId { get; set; }
        public virtual WalletTransaction? WalletTransaction { get; set; }
    }

    /// <summary>وضعیت درخواست بازنشانی رمز همکار</summary>
    public enum PasswordResetStatus
    {
        /// <summary>در انتظار بررسی مدیر</summary>
        Pending = 1,
        /// <summary>تایید و بازنشانی شده توسط مدیر</summary>
        Approved = 2,
        /// <summary>رد شده توسط مدیر</summary>
        Rejected = 3,
    }

    /// <summary>
    /// درخواست «فراموشی رمز» همکار — در صندوق ورودی مدیر می‌نشیند و
    /// بازنشانی رمز فقط با تایید صریح مدیر انجام می‌شود (بدون ایمیل/پیامک بیرونی)
    /// </summary>
    public class PasswordResetRequest : BaseEntity
    {
        public int PartnerId { get; set; }
        public virtual Partner Partner { get; set; } = null!;

        public PasswordResetStatus Status { get; set; } = PasswordResetStatus.Pending;

        /// <summary>پیام اختیاری همکار هنگام درخواست</summary>
        public string? Message { get; set; }

        /// <summary>پاسخ/یادداشت مدیر هنگام تایید یا رد</summary>
        public string? AdminResponse { get; set; }

        public DateTime? DecidedAt { get; set; }
        public int? DecidedByAdminId { get; set; }
    }
}

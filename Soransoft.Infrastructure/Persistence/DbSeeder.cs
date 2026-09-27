using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Soransoft.Domain.Entities;
using Soransoft.Domain.Enums;
using Soransoft.Application.Interfaces;
// حرف‌گرفتن ابهام: TaskStatus پرتال جای System.Threading.Tasks.TaskStatus می‌نشیند
using TaskStatus = Soransoft.Domain.Entities.TaskStatus;

namespace Soransoft.Infrastructure.Persistence
{
    public sealed class RequiredSetupException : InvalidOperationException
    {
        public RequiredSetupException(string message) : base(message) { }
    }

    /// <summary>بذرگذاری دیتابیس با داده‌های اولیه مشابه سایت مرجع</summary>
    public static class DbSeeder
    {
        public static async Task SeedAsync(
            SoransoftDbContext db,
            IPasswordHasher hasher,
            IConfiguration configuration,
            CancellationToken ct = default)
        {
            await SeedSettingsAsync(db, ct);
            await SeedMenusAsync(db, ct);
            await SeedServicesAsync(db, ct);
            await SeedTariffsAsync(db, ct);
            await SeedCategoriesAsync(db, ct);
            await SeedArticlesAsync(db, ct);
            await SeedTeamAsync(db, ct);
            await SeedAdminAsync(db, hasher, configuration, ct);
            await SeedSlidersAsync(db, ct);
            await SeedPartnerPortalAsync(db, hasher, configuration, ct);
            await db.SaveChangesAsync(ct);
        }

        /// <summary>بذر داده‌های نمونه پرتال همکاران (اکانت‌ها فقط اگر جدول خالی باشد)</summary>
        private static async Task SeedPartnerPortalAsync(
            SoransoftDbContext db,
            IPasswordHasher hasher,
            IConfiguration configuration,
            CancellationToken ct)
        {
            if (await db.Partners.AnyAsync(ct)) return;

            // Partner accounts are optional setup data; never create built-in credentials.
            var configuredPartners = configuration.GetSection("Security:InitialPartners").GetChildren().ToList();
            if (configuredPartners.Count == 0) return;

            static PartnerRole? ReadRole(IConfigurationSection section)
            {
                if (Enum.TryParse<PartnerRole>(section["Role"], true, out var role)) return role;
                if (int.TryParse(section["Role"], out var numericRole) && Enum.IsDefined(typeof(PartnerRole), numericRole))
                    return (PartnerRole)numericRole;
                return null;
            }

            var salesSection = configuredPartners.FirstOrDefault(s => ReadRole(s) == PartnerRole.Sales);
            var salesManagerSection = configuredPartners.FirstOrDefault(s => ReadRole(s) == PartnerRole.SalesManager);
            var devSection = configuredPartners.FirstOrDefault(s => ReadRole(s) == PartnerRole.Developer);
            var techManagerSection = configuredPartners.FirstOrDefault(s => ReadRole(s) == PartnerRole.TechManager);
            if (salesSection is null || salesManagerSection is null || devSection is null || techManagerSection is null)
                return;

            static string Required(IConfigurationSection section, string key)
            {
                var value = section[key];
                if (string.IsNullOrWhiteSpace(value))
                    throw new RequiredSetupException($"Security:InitialPartners requires {key} for every configured partner.");
                return value.Trim();
            }

            Partner BuildPartner(IConfigurationSection section, PartnerRole role)
            {
                var password = Required(section, "Password");
                if (password.Length < 12)
                    throw new RequiredSetupException("Security:InitialPartners passwords must be at least 12 characters.");

                return new Partner
                {
                    Username = Required(section, "Username"),
                    PasswordHash = hasher.Hash(password),
                    FullName = Required(section, "FullName"),
                    Mobile = section["Mobile"]?.Trim(),
                    Email = section["Email"]?.Trim(),
                    Role = role,
                    CanSeeAllSalesData = bool.TryParse(section["CanSeeAllSalesData"], out var canSeeAll) && canSeeAll,
                    TechLevel = section["TechLevel"]?.Trim(),
                    Skills = section["Skills"]?.Trim(),
                    IsActive = true,
                };
            }

            // The four optional setup entries are required only when sample portal data is requested.
            var sales = BuildPartner(salesSection, PartnerRole.Sales);
            var salesManager = BuildPartner(salesManagerSection, PartnerRole.SalesManager);
            var dev = BuildPartner(devSection, PartnerRole.Developer);
            var techManager = BuildPartner(techManagerSection, PartnerRole.TechManager);
            if (!salesManager.CanSeeAllSalesData)
                salesManager.CanSeeAllSalesData = true;
            if (!techManager.CanSeeAllSalesData)
                techManager.CanSeeAllSalesData = true;

            db.Partners.AddRange(sales, salesManager, dev, techManager);
            await db.SaveChangesAsync(ct);

            // ---------- پروژه و اسپرینت فنی ----------
            var sugarShop = new DevProject { Title = "SugarShop", Description = "پلتفرم فروشگاه شیرینی سارا", StatusText = "در مرحله تست", IsActive = true };
            var acleda = new DevProject { Title = "ACLEDA_BANK", Description = "سامانه بانکی ACLEDA", StatusText = "در حال توسعه", IsActive = true };
            db.DevProjects.AddRange(sugarShop, acleda);
            await db.SaveChangesAsync(ct);

            var sprint1 = new Sprint { Title = "اسپرینت ۱ — ماژول پرداخت", Goal = "تکمیل درگاه پرداخت و اتصال بانک", StartDate = DateTime.Now.AddDays(-14), EndDate = DateTime.Now.AddDays(14) };
            db.Sprints.Add(sprint1);
            await db.SaveChangesAsync(ct);

            db.ProjectMembers.AddRange(
                new ProjectMember { ProjectId = sugarShop.Id, PartnerId = dev.Id, RoleInProject = "Backend" },
                new ProjectMember { ProjectId = sugarShop.Id, PartnerId = techManager.Id, RoleInProject = "Tech Lead" },
                new ProjectMember { ProjectId = acleda.Id, PartnerId = dev.Id, RoleInProject = "Fullstack" });

            // ---------- تسک‌های نمونه روی برد ----------
            db.DevTasks.AddRange(
                new DevTask
                {
                    Title = "افزودن ماژول پیامک به SugarShop", Description = "اتصال به سرویس کاوه‌نگار و ارسال کد تایید",
                    ProjectId = sugarShop.Id, SprintId = sprint1.Id, PartnerId = dev.Id,
                    Priority = TaskPriority.High, Status = TaskStatus.ToDo, DueDate = DateTime.Now.AddDays(7),
                },
                new DevTask
                {
                    Title = "رفع باگ درگاه پرداخت ACLEDA", Description = "خطای timeout در تراکنش‌های بالای ۱۰ میلیون",
                    ProjectId = acleda.Id, SprintId = sprint1.Id, PartnerId = dev.Id,
                    Priority = TaskPriority.Critical, Status = TaskStatus.InProgress, DueDate = DateTime.Now.AddDays(3),
                },
                new DevTask
                {
                    Title = "مستندسازی API مشتریان SugarShop",
                    ProjectId = sugarShop.Id, PartnerId = dev.Id,
                    Priority = TaskPriority.Medium, Status = TaskStatus.Testing,
                },
                new DevTask
                {
                    Title = "راه‌اندازی CI/CD پروژه SugarShop",
                    ProjectId = sugarShop.Id, PartnerId = techManager.Id,
                    Priority = TaskPriority.Medium, Status = TaskStatus.Done, CompletedAt = DateTime.Now.AddDays(-2),
                });

            // ---------- مستندات فنی نمونه ----------
            db.ProjectDocuments.AddRange(
                new ProjectDocument
                {
                    ProjectId = sugarShop.Id, Title = "معماری سیستم", Category = "Architecture", DisplayOrder = 1,
                    Body = "<p>معماری SugarShop بر پایه ASP.NET Core 9 MVC با الگوی Clean Architecture است: لایه Domain، Application، Infrastructure و Web. دیتابیس SQL Server و کش MemoryCache.</p>",
                },
                new ProjectDocument
                {
                    ProjectId = sugarShop.Id, Title = "طراحی دیتابیس", Category = "Database", DisplayOrder = 2,
                    Body = "<p>جداول اصلی: Products، Orders، Customers، Payments. رابطه Orders-Products از طریق OrderItems. تمام قیمت‌ها به تومان و نوع long است.</p>",
                });

            // ---------- قیف فروش: لید نمونه ----------
            var lead = new Lead
            {
                PartnerId = sales.Id,
                CustomerName = "شیرینی سرا", CustomerMobile = "09121234567",
                Requirement = "سایت فروشگاهی با درگاه پرداخت و ماژول باشگاه مشتریان",
                EstimatedAmount = 250_000_000,
                Stage = LeadStage.Negotiating,
            };
            lead.History.Add(new LeadHistory { FromStage = LeadStage.New, ToStage = LeadStage.Negotiating, Note = "جلسه اول برگزار شد" });
            db.Leads.Add(lead);
            await db.SaveChangesAsync(ct);

            // ---------- اطلاعیه خوش‌آمد ----------
            db.Announcements.Add(new Announcement
            {
                Title = "به پرتال همکاران خوش آمدید",
                Body = "این پرتال برای مدیریت لیدها، قراردادها، تسک‌ها و مستندات فنی است. رمز عبور خود را نزد مدیر درخواست کنید.",
                Audience = "All",
                IsImportant = true,
            });
            await db.SaveChangesAsync(ct);
        }

        /// <summary>اسلایدرهای صفحه اصلی</summary>
        private static async Task SeedSlidersAsync(SoransoftDbContext db, CancellationToken ct)
        {
            if (await db.Sliders.AnyAsync(ct)) return;
            db.Sliders.AddRange(
                new Slider
                {
                    Title = "هر چه در فکر شماست، ما می‌سازیم",
                    SubTitle = "طراحی سایت حرفه‌ای با کدنویسی اختصاصی",
                    Link = "/OrderProject",
                    DisplayOrder = 1,
                },
                new Slider
                {
                    Title = "صفحه اول گوگل، دیگر رویا نیست",
                    SubTitle = "خدمات سئو با استراتژی محتوایی و فنی",
                    Link = "/SeoProject",
                    DisplayOrder = 2,
                },
                new Slider
                {
                    Title = "محتوای سئو شده، محتوای منحصر به فرد",
                    SubTitle = "تیم تولید محتوا با ابزارهای به‌روز",
                    Link = "/ContentProject",
                    DisplayOrder = 3,
                });
            await db.SaveChangesAsync(ct);
        }

        /// <summary>ایجاد ادمین اولیه فقط با تنظیمات خارج از کد</summary>
        private static async Task SeedAdminAsync(
            SoransoftDbContext db,
            IPasswordHasher hasher,
            IConfiguration configuration,
            CancellationToken ct)
        {
            if (await db.Admins.AnyAsync(ct)) return;

            var username = configuration["Security:InitialAdmin:Username"]?.Trim();
            var password = configuration["Security:InitialAdmin:Password"];
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                throw new RequiredSetupException("Security:InitialAdmin:Username and Security:InitialAdmin:Password are required for first run.");
            if (password.Length < 12)
                throw new RequiredSetupException("Security:InitialAdmin:Password must be at least 12 characters.");

            db.Admins.Add(new Admin
            {
                Username = username,
                PasswordHash = hasher.Hash(password),
                FullName = "مدیر سایت",
                Email = null,
                IsActive = true,
            });
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedSettingsAsync(SoransoftDbContext db, CancellationToken ct)
        {
            if (await db.SiteSettings.AnyAsync(ct)) return;
            var settings = new List<SiteSetting>
            {
                new() { Key = "SiteName", Title = "نام سایت", Value = "سوران سافت", Group = "عمومی", Type = "text", DisplayOrder = 1 },
                new() { Key = "SiteTagline", Title = "شعار سایت", Value = "هر چه در فکر شماست، ما می‌سازیم", Group = "عمومی", Type = "text", DisplayOrder = 2 },
                new() { Key = "SiteDescription", Title = "توضیح متا", Value = "طراحی سایت، سئو، اپلیکیشن موبایل و تولید محتوا", Group = "عمومی", Type = "textarea", DisplayOrder = 3 },
                new() { Key = "LogoText", Title = "متن لوگو", Value = "SORANSOFT", Group = "عمومی", Type = "text", DisplayOrder = 4 },
                new() { Key = "Phone", Title = "تلفن ثابت", Value = "021-28 4 28 140", Group = "تماس", Type = "text", DisplayOrder = 5 },
                new() { Key = "Mobile", Title = "موبایل", Value = "09120000000", Group = "تماس", Type = "text", DisplayOrder = 6 },
                new() { Key = "Address", Title = "آدرس", Value = "تهران، خیابان ولیعصر، مرکز نوآوری سوران", Group = "تماس", Type = "textarea", DisplayOrder = 7 },
                new() { Key = "WorkingHours", Title = "ساعات کاری", Value = "شنبه تا پنجشنبه ۹ تا ۱۸", Group = "تماس", Type = "text", DisplayOrder = 8 },
                new() { Key = "Email", Title = "ایمیل", Value = "info@soransoft.ir", Group = "تماس", Type = "text", DisplayOrder = 9 },
                new() { Key = "InstagramUrl", Title = "اینستاگرام", Value = "https://instagram.com/soransoft", Group = "شبکه‌های اجتماعی", Type = "text", DisplayOrder = 10 },
                new() { Key = "TelegramUrl", Title = "تلگرام", Value = "https://t.me/soransoft", Group = "شبکه‌های اجتماعی", Type = "text", DisplayOrder = 11 },
                new() { Key = "WhatsappUrl", Title = "واتساپ", Value = "https://wa.me/989120000000", Group = "شبکه‌های اجتماعی", Type = "text", DisplayOrder = 12 },
                new() { Key = "AboutText", Title = "متن درباره ما", Value = "تیم سوران سافت با بیش از یک دهه تجربه در طراحی وب، سئو و تولید محتوا، کنار شماست تا کسب‌وکارتان را آنلاین کنند.", Group = "درباره", Type = "textarea", DisplayOrder = 13 },
                new() { Key = "FooterNote", Title = "متن فوتر", Value = "Designed with ❤️ by Soransoft team", Group = "عمومی", Type = "text", DisplayOrder = 14 },
                new() { Key = "ConsultationNote", Title = "یادداشت فرم مشاوره", Value = "جهت دریافت مشاوره رایگان و قیمت دقیق اطلاعات زیر را پر کنید", Group = "عمومی", Type = "text", DisplayOrder = 15 },
            };
            db.SiteSettings.AddRange(settings);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedMenusAsync(SoransoftDbContext db, CancellationToken ct)
        {
            if (await db.MenuItems.AnyAsync(ct)) return;
            var menus = new List<MenuItem>
            {
                new() { Title = "خانه", Url = "/", Position = MenuPosition.Main, DisplayOrder = 1 },
                new() { Title = "خدمات", Url = "/OurServices", Position = MenuPosition.Main, DisplayOrder = 2 },
                new() { Title = "نمونه کارها", Url = "/Portfolio", Position = MenuPosition.Main, DisplayOrder = 3 },
                new() { Title = "سفارش پروژه", Url = "/OrderProject", Position = MenuPosition.Main, DisplayOrder = 4 },
                new() { Title = "تعرفه", Url = "/Tariff", Position = MenuPosition.Main, DisplayOrder = 5 },
                new() { Title = "مقالات", Url = "/Articles", Position = MenuPosition.Main, DisplayOrder = 6 },
                new() { Title = "درباره ما", Url = "/AboutUs", Position = MenuPosition.Main, DisplayOrder = 7 },
                new() { Title = "تماس با ما", Url = "/ContactUs", Position = MenuPosition.Main, DisplayOrder = 8 },
            };
            // منوی موبایل همان آیتم‌ها
            menus.AddRange(menus.Take(8).Select(m => new MenuItem
            {
                Title = m.Title, Url = m.Url, Position = MenuPosition.Mobile,
                DisplayOrder = m.DisplayOrder
            }));
            // فوتر
            menus.Add(new MenuItem { Title = "خدمات ما", Url = "/OurServices", Position = MenuPosition.Footer, DisplayOrder = 1 });
            menus.Add(new MenuItem { Title = "نمونه کارها", Url = "/Portfolio", Position = MenuPosition.Footer, DisplayOrder = 2 });
            menus.Add(new MenuItem { Title = "مقالات", Url = "/Articles", Position = MenuPosition.Footer, DisplayOrder = 3 });
            menus.Add(new MenuItem { Title = "درباره ما", Url = "/AboutUs", Position = MenuPosition.Footer, DisplayOrder = 4 });
            menus.Add(new MenuItem { Title = "تماس با ما", Url = "/ContactUs", Position = MenuPosition.Footer, DisplayOrder = 5 });
            db.MenuItems.AddRange(menus);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedServicesAsync(SoransoftDbContext db, CancellationToken ct)
        {
            if (await db.Services.AnyAsync(ct)) return;

            // ابتدا سرویس‌ها ذخیره می‌شوند تا Id آن‌ها برای نمونه‌کارها معتبر باشد

            var site = new Service
            {
                Kind = ServiceKind.Site, Title = "طراحی سایت", Slug = "site-project",
                ShortDescription = "وردپرس یا کدنویسی اختصاصی؟ کدوم مورد نیاز شماست؟ ما شما رو راهنمایی می کنیم",
                Slogan = "برای اولین بودن باید بهترین بود",
                Icon = "bi-globe2", DisplayOrder = 1,
                FullDescription = "<p>در دنیای دیجیتال امروزی، بسیار مهم است که با مشتریان و کاربران خود در ارتباط باشید تا از بازارهای جدید استفاده کنید و تجارت خود را گسترش دهید. آمارها نشان می‌دهد که تقریباً ۴.۵۷ میلیارد نفر در سراسر جهان کاربر فعال اینترنت هستند.</p><p>بدون حضور در دنیای وب، شما به‌طور خودکار فرصت برقراری ارتباط با مخاطبان هدف خود و محبوبیت برند خود را از دست می‌دهید. با طراحی یک وب‌سایت و صرف هزینه‌ای اندک، در دنیای اینترنت نیز حضور داشته باشید.</p><p>ما در سوران سافت خدمات طراحی وب‌سایت سازگار با سئو را ارائه می‌دهیم که هدف آن افزایش رتبه‌ی وب‌سایت شما در نتایج موتورهای جستجو، افزایش تعامل با مشتری‌ها و افزایش درآمد شما است.</p><h3>چرا باید طراحی وب‌سایت خود را به سوران سافت بسپارید؟</h3><ul><li>بهینه‌سازی نرخ تبدیل (CRO)</li><li>طراحی ریسپانسیو مطابق استانداردهای روز</li><li>افزایش رتبه در موتورهای جستجو</li><li>کاهش هزینه‌ی نگهداری</li><li>بهینه‌سازی تجربه‌ی کاربری</li><li>افزایش شهرت برند</li></ul><p>طراحی سایت اختصاصی با ASP.NET Core و کدنویسی اختصاصی، سبک‌تر و سریع‌تر از قالب‌های آماده است و گوگل عاشق سرعت است!</p>"
            };
            site.Features.Add(new ServiceFeature { Title = "طراحی ریسپانسیو", Description = "سازگار با موبایل، تبلت و دسکتاپ" });
            site.Features.Add(new ServiceFeature { Title = "کدنویسی اختصاصی", Description = "بدون قالب آماده، کاملاً اختصاصی" });
            site.Features.Add(new ServiceFeature { Title = "سئو شده", Description = "مطابق اصول سئوی گوگل" });
            site.Features.Add(new ServiceFeature { Title = "پنل مدیریت", Description = "کنترل کامل محتوای سایت" });
            site.Steps.Add(new ServiceStep { Title = "ثبت سفارش", DisplayOrder = 1 });
            site.Steps.Add(new ServiceStep { Title = "واریز پیش پرداخت", DisplayOrder = 2 });
            site.Steps.Add(new ServiceStep { Title = "تایید طراحی", DisplayOrder = 3 });
            site.Steps.Add(new ServiceStep { Title = "تحویل پروژه", DisplayOrder = 4 });
            site.Steps.Add(new ServiceStep { Title = "آموزش کار با پروژه", DisplayOrder = 5 });

            var seo = new Service
            {
                Kind = ServiceKind.Seo, Title = "سئوی سایت", Slug = "seo",
                ShortDescription = "باورت میشه سایتت صفحه اول گوگل باشه؟! باور کن! با سوران سافت شدنیه",
                Slogan = "صفحه اول گوگل، دیگر رویا نیست",
                Icon = "bi-graph-up-arrow", DisplayOrder = 2,
                FullDescription = "<p>اومدن صفحه اول گوگل شوخی نیست! ما هم نیومدیم شوخی کنیم! لغت دلخواهت رو به ما بده و تماشا کن چه گرد و خاکی توی گوگل به پا می‌کنیم.</p><p>بالای هشتاد درصد از مشتری‌های شما از گوگل میان پس سئو رو دست کم نگیر. برای رسیدن به صفحه اول گوگل هم کار فنی لازمه هم کار محتوایی که سوران سافت در هر دو زمینه تخصص و تجربه دارد.</p>"
            };
            seo.Steps.Add(new ServiceStep { Title = "ثبت سفارش", DisplayOrder = 1 });
            seo.Steps.Add(new ServiceStep { Title = "مشاوره", DisplayOrder = 2 });
            seo.Steps.Add(new ServiceStep { Title = "ارزیابی کلمات کلیدی", DisplayOrder = 3 });
            seo.Steps.Add(new ServiceStep { Title = "تدوین استراتژی", DisplayOrder = 4 });
            seo.Steps.Add(new ServiceStep { Title = "ارائه گزارش", DisplayOrder = 5 });

            var app = new Service
            {
                Kind = ServiceKind.Application, Title = "اپلیکیشن موبایل", Slug = "application",
                ShortDescription = "اندروید، ای او اس، مک، ویندوز! دیگه کجا میخوای اجرا بشه؟ اپلیکیشنت همه جا هست",
                Slogan = "برند شما در جیب مشتری", Icon = "bi-phone", DisplayOrder = 3, ComingSoon = true,
                FullDescription = "<p>اپلیکیشن موبایل، نزدیک‌ترین نقطه‌ی تماس برند شما با مشتری است. به‌زودی خدمات اپلیکیشن سوران سافت آغاز می‌شود!</p>"
            };
            app.Steps.Add(new ServiceStep { Title = "ثبت سفارش", DisplayOrder = 1 });
            app.Steps.Add(new ServiceStep { Title = "واریز پیش پرداخت", DisplayOrder = 2 });
            app.Steps.Add(new ServiceStep { Title = "تایید طراحی", DisplayOrder = 3 });
            app.Steps.Add(new ServiceStep { Title = "تحویل پروژه", DisplayOrder = 4 });
            app.Steps.Add(new ServiceStep { Title = "آموزش کار با پروژه", DisplayOrder = 5 });

            var content = new Service
            {
                Kind = ServiceKind.Content, Title = "تولید محتوا", Slug = "content",
                ShortDescription = "سایت داری محتوا نداره؟ ما محتواش رو می سازیم با یک تیم حرفه ای و ابزارهای به روز",
                Slogan = "محتوای سئو شده، محتوای منحصر به فرد",
                Icon = "bi-pencil-square", DisplayOrder = 4,
                FullDescription = "<p>برای سایتت محتوای تخصصی نیاز داری یا محتوای عمومی؟ فرقی نداره، سوران سافت از پس جفتش مثل آب خوردن بر میاد. تیم تولید محتوا کاملاً منعطف است و شما حتی می‌توانی لحن دلخواهت را به تیم بدهی تا با آن لحن محتوا تولید شود.</p><p>محتوای تولیدشده تمام اصول سئو در آن رعایت می‌شود و این‌طوری با یک تیر دو نشان می‌زنی!</p>"
            };
            content.Steps.Add(new ServiceStep { Title = "ثبت سفارش", DisplayOrder = 1 });
            content.Steps.Add(new ServiceStep { Title = "دریافت موضوع", DisplayOrder = 2 });
            content.Steps.Add(new ServiceStep { Title = "یافتن منابع", DisplayOrder = 3 });
            content.Steps.Add(new ServiceStep { Title = "تولید محتوا", DisplayOrder = 4 });
            content.Steps.Add(new ServiceStep { Title = "ارائه مقالات", DisplayOrder = 5 });

            db.Services.AddRange(site, seo, app, content);
            await db.SaveChangesAsync(ct);

            // نمونه‌کارها (بعد از ذخیره سرویس‌ها تا ServiceId معتبر باشد)
            var portfolios = new List<Portfolio>
            {
                new() { Title = "سایت موسوی کباب", Description = "سایت رستورانی موسوی کباب، فروش آنلاین غذا", Url = "https://mosavikabab.com", ServiceId = site.Id, DisplayOrder = 1 },
                new() { Title = "سایت همایار", Description = "سایت شرکت همایار در حال تکمیل است و به زودی بالا می آید", ComingSoon = true, ServiceId = site.Id, DisplayOrder = 2 },
                new() { Title = "سایت همافارمد", Description = "شرکت تولید کننده دارو که در سال 2008 تاسیس شده است", Url = "https://hamapharmed.com", ServiceId = site.Id, DisplayOrder = 3 },
                new() { Title = "سایت پیلو", Description = "تولید کننده قابلمه، ماهیتابه و گریل", Url = "https://pilo.ir", ServiceId = site.Id, DisplayOrder = 4 },
                new() { Title = "سایت هیل فیت", Description = "هیلفیت از طریق علم ورزشی، تغذیه و روانشناسی به جامعه کمک می‌کند تا سبک زندگی سالم ایجاد کنیم", Url = "https://hillfit.ir", ServiceId = site.Id, DisplayOrder = 5 },
                new() { Title = "سایت ایران قسط", Description = "فروش قسطی کالا و خدمات", Url = "https://iranqest.ir", ServiceId = site.Id, DisplayOrder = 6 },
                new() { Title = "سایت آقای طرح پارچه", Description = "فروش انلاین پارچه مبل با امکان مشاهده زنده پارچه انتخابی بر روی مبل", Url = "https://aghayetarh.ir", ServiceId = site.Id, DisplayOrder = 7 },
                new() { Title = "سایت پرو ابزار", Description = "مرجع فروش آنلاین ابزار آلات و قطعات صنعتی", Url = "https://proabzar.com", ServiceId = site.Id, DisplayOrder = 8 },
                new() { Title = "سایت گرین باکس", Description = "سفارش پکیج های پذیرایی برای ایونت های مختلف در کوتاه ترین زمان و تحویل در محل", Url = "https://greenbox.ir", ServiceId = site.Id, DisplayOrder = 9 },
                new() { Title = "سایت مای شیائومی", Description = "مرجع تخصصی فروش محصولات شیائومی در ایران به همراه مجله خبری", Url = "https://myxiaomi.ir", ServiceId = site.Id, DisplayOrder = 10 },
                new() { Title = "سایت ژوابکس", Description = "شرکت وارد کننده فیلرهای زیبایی ژوابکس سوییس", Url = "https://juvax.com", ServiceId = site.Id, DisplayOrder = 11 },
            };
            db.Portfolios.AddRange(portfolios);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedTariffsAsync(SoransoftDbContext db, CancellationToken ct)
        {
            if (await db.TariffSections.AnyAsync(ct)) return;

            var siteSection = new TariffSection { Kind = ServiceKind.Site, Title = "طراحی سایت", DisplayOrder = 1 };
            siteSection.Packages.Add(new TariffPackage { Title = "وب سایت شرکتی", Duration = "مدت زمان چهار الی شش هفته", IsInstallmentAvailable = true, HasFreeSupport = true, Price = 30_000_000, DisplayOrder = 1,
                Items = { new TariffItem { Title = "طراحی اختصاصی", DisplayOrder = 1 }, new TariffItem { Title = "پنل مدیریت کامل", DisplayOrder = 2 }, new TariffItem { Title = "بهینه برای سئو", DisplayOrder = 3 } } });
            siteSection.Packages.Add(new TariffPackage { Title = "وب سایت فروشگاهی", Duration = "مدت زمان شش الی هشت هفته", IsInstallmentAvailable = true, HasFreeSupport = true, Price = 35_000_000, DisplayOrder = 2,
                Items = { new TariffItem { Title = "درگاه پرداخت", DisplayOrder = 1 }, new TariffItem { Title = "مدیریت سفارش‌ها", DisplayOrder = 2 }, new TariffItem { Title = "پنل مدیریت فروشگاه", DisplayOrder = 3 } } });
            siteSection.Packages.Add(new TariffPackage { Title = "وب سایت مجله خبری", Duration = "مدت زمان شش الی هشت هفته", IsInstallmentAvailable = true, HasFreeSupport = true, Price = 35_000_000, DisplayOrder = 3 });
            siteSection.Packages.Add(new TariffPackage { Title = "وب سایت شخصی", Duration = "مدت زمان چهار الی شش هفته", IsInstallmentAvailable = true, HasFreeSupport = true, Price = 25_000_000, DisplayOrder = 4 });

            var seoSection = new TariffSection { Kind = ServiceKind.Seo, Title = "سئو", DisplayOrder = 2 };
            seoSection.Packages.Add(new TariffPackage { Title = "پروژه سئو", IsInstallmentAvailable = true, PriceNote = "برای قیمت تماس بگیرید", DisplayOrder = 1,
                Items = { new TariffItem { Title = "آنالیز کلمه کلیدی", DisplayOrder = 1 }, new TariffItem { Title = "تدوین استراتژی", DisplayOrder = 2 }, new TariffItem { Title = "تولید محتوا", DisplayOrder = 3 } } });

            var appSection = new TariffSection { Kind = ServiceKind.Application, Title = "اپلیکیشن", DisplayOrder = 3 };
            appSection.Packages.Add(new TariffPackage { Title = "اپلیکیشن موبایل", Duration = "مدت زمان یک الی سه ماه", IsInstallmentAvailable = true, HasFreeSupport = true, PriceNote = "به زودی", DisplayOrder = 1 });

            var contentSection = new TariffSection { Kind = ServiceKind.Content, Title = "تولید محتوا", DisplayOrder = 4 };
            (string title, long? price, string suffix)[] contentPackages =
            {
                ("1 تا 30 مقاله", null, "کلمه ای 120"),
                ("30 تا 60 مقاله", null, "کلمه ای 110"),
                ("60 تا 90 مقاله", null, "کلمه ای 100"),
                ("90 مقاله به بالا", null, "کلمه ای 90"),
            };
            foreach (var (title, price, suffix) in contentPackages)
                contentSection.Packages.Add(new TariffPackage { Title = title, Price = price, PriceSuffix = suffix, DisplayOrder = contentSection.Packages.Count + 1,
                    Items = { new TariffItem { Title = "محتوای سئو شده", DisplayOrder = 1 }, new TariffItem { Title = "محتوای منحصر به فرد", DisplayOrder = 2 }, new TariffItem { Title = "تحویل سریع", DisplayOrder = 3 } } });

            // پکیج‌های محتوای اختصاصی
            (string title, string suffix)[] customPackages =
            {
                ("1 تا 30 مقاله", "کلمه ای 150"),
                ("30 تا 60 مقاله", "کلمه ای 135"),
                ("60 تا 90 مقاله", "کلمه ای 120"),
                ("90 مقاله به بالا", "کلمه ای 105"),
            };
            foreach (var (title, suffix) in customPackages)
                contentSection.Packages.Add(new TariffPackage { Title = title, PriceSuffix = suffix, DisplayOrder = contentSection.Packages.Count + 1, IsPopular = false,
                    Items = { new TariffItem { Title = "محتوای اختصاصی", DisplayOrder = 1 }, new TariffItem { Title = "محتوای سئو شده", DisplayOrder = 2 }, new TariffItem { Title = "تحویل سریع", DisplayOrder = 3 } } });

            db.TariffSections.AddRange(siteSection, seoSection, appSection, contentSection);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedCategoriesAsync(SoransoftDbContext db, CancellationToken ct)
        {
            if (await db.ArticleCategories.AnyAsync(ct)) return;
            db.ArticleCategories.AddRange(
                new ArticleCategory { Title = "مقاله‌ها", Slug = "articles", DisplayOrder = 1 },
                new ArticleCategory { Title = "اخبار", Slug = "news", DisplayOrder = 2 },
                new ArticleCategory { Title = "آموزش", Slug = "education", DisplayOrder = 3 },
                new ArticleCategory { Title = "طراحی سایت", Slug = "site-design", DisplayOrder = 4 },
                new ArticleCategory { Title = "افزونه", Slug = "plugins", DisplayOrder = 5 });
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedArticlesAsync(SoransoftDbContext db, CancellationToken ct)
        {
            if (await db.Articles.AnyAsync(ct)) return;
            var cats = await db.ArticleCategories.ToDictionaryAsync(c => c.Slug, c => c.Id, ct);
            db.Articles.AddRange(
                new Article
                {
                    Title = "نقشه سایت یا سایت مپ چیست؟", Slug = "what-is-sitemap", Summary = "هر آنچه باید درباره نقشه سایت و نقش آن در ایندکس شدن سریع‌تر بدانید.",
                    Body = "<p>نقشه سایت (Sitemap) فایلی است که ساختار صفحات سایت شما را به موتورهای جستجو معرفی می‌کند و ایندکس شدن سریع‌تر صفحات را ممکن می‌سازد. در این مقاله با انواع سایت مپ، ساخت و ثبت آن در سرچ کنسول آشنا می‌شوید.</p>",
                    AuthorName = "تیم سوران سافت", ArticleCategoryId = cats["articles"], PublishedAt = DateTime.Now.AddDays(-10), Status = PublishStatus.Published, VisitCount = 42
                },
                new Article
                {
                    Title = "چرا بازدید سایت من کم است؟", Slug = "why-my-website-has-low-traffic", Summary = "دلایل رایج کم بودن بازدید سایت و راه‌حل‌های عملی برای رفع آن‌ها.",
                    Body = "<p>کم بودن بازدید سایت دلایل مختلفی دارد: ضعف سئوی فنی، محتوای بی‌کیفیت، سرعت پایین سایت و نبود بک‌لینک‌های معتبر. در این مقاله هر یک را بررسی می‌کنیم و راه‌حل عملی ارائه می‌دهیم.</p>",
                    AuthorName = "تیم سوران سافت", ArticleCategoryId = cats["education"], PublishedAt = DateTime.Now.AddDays(-7), Status = PublishStatus.Published, VisitCount = 87
                },
                new Article
                {
                    Title = "چرا نمی‌توان برای بالا رفتن رتبه سایت در گوگل زمان تعیین کرد؟", Slug = "google-ranking-time-expectations", Summary = "چرا هیچ‌کس نمی‌تواند زمان دقیق رسیدن به صفحه اول گوگل را تضمین کند؟",
                    Body = "<p>الگوریتم گوگل صدها سیگنال را بررسی می‌کند و رقابت کلمات کلیدی متفاوت است؛ به همین دلیل هیچ‌کس نمی‌تواند زمان دقیقی برای رتبه گرفتن تعیین کند، اما با استراتژی درست مسیر رشد قابل پیش‌بینی می‌شود.</p>",
                    AuthorName = "تیم سوران سافت", ArticleCategoryId = cats["articles"], PublishedAt = DateTime.Now.AddDays(-3), Status = PublishStatus.Published, VisitCount = 65
                },
                new Article
                {
                    Title = "چرا سرعت سایت‌های ASP.NET بیشتر از سایت‌های وردپرسی است؟", Slug = "aspnet-vs-wordpress-speed", Summary = "مقایسه فنی سرعت سایت‌های کدنویسی‌شده با ASP.NET و وردپرس.",
                    Body = "<p>ASP.NET Core با کامپایل پیش‌دستانه، کش سمت سرور و معماری سبک، عملکرد به‌مراتب بهتری نسبت به وردپرس ارائه می‌دهد. سرعت سایت یکی از فاکتورهای مهم سئو است.</p>",
                    AuthorName = "تیم سوران سافت", ArticleCategoryId = cats["site-design"], PublishedAt = DateTime.Now.AddDays(-1), Status = PublishStatus.Published, VisitCount = 120
                });
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedTeamAsync(SoransoftDbContext db, CancellationToken ct)
        {
            if (await db.TeamMembers.AnyAsync(ct)) return;
            db.TeamMembers.AddRange(
                new TeamMember { FullName = "علی رضایی", Role = "مدیر بخش برنامه نویسی", DisplayOrder = 1 },
                new TeamMember { FullName = "سارا محمدی", Role = "مدیر بخش طراحی و سئو", DisplayOrder = 2 },
                new TeamMember { FullName = "امیر کریمی", Role = "مدیر بخش تولید محتوا", DisplayOrder = 3 });
            await db.SaveChangesAsync(ct);
        }
    }
}

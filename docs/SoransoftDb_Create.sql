-- ============================================================================
--  SoransoftDb — اسکریپت کامل ساخت دیتابیس برای SQL Server (SSMS)
--  منبع: dotnet ef migrations script (InitialCreate → AddErrorLogTable)
--  + داده‌های اولیه مطابق DbSeeder (ادمین، تنظیمات، منوها، سرویس‌ها، تعرفه‌ها…)
--
--  نحوه استفاده:
--    1) SSMS را باز کنید و به instance سرور وصل شوید (مثلاً localhost یا .\SQLEXPRESS)
--    2) کل این فایل را در یک New Query اجرا کنید (F5)
--    3) ConnectionStrings:DefaultConnection در Soransoft.Web/appsettings.json را چنین بگذارید:
--         Server=YOUR_SERVER;Database=SoransoftDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
--       (یا با کاربر SQL: Server=...;Database=SoransoftDb;User Id=sa;Password=***;TrustServerCertificate=True)
--
--  نکات:
--   • ران‌های بعدی بی‌خطر است (idempotent): INSERT ها با NOT EXISTS محافظت شده‌اند.
--   • جدول __SoransoftMigrationsHistory از قبل پر می‌شود تا برنامه EF Migration اجرا نکند.
--   • هش رمز ادمین با ASP.NET Core Identity V3 (PBKDF2) است و همان است که برنامه Verify می‌کند.
-- ============================================================================

IF DB_ID(N'SoransoftDb') IS NULL
    CREATE DATABASE [SoransoftDb];
GO
USE [SoransoftDb];
GO

/* ============================================================================
   1) اسکیمای جداول (معادل دقیق مایگریشن‌های EF)
   ============================================================================ */

IF OBJECT_ID(N'[__SoransoftMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__SoransoftMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___SoransoftMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* اگر جداول از قبل ساخته شده‌اند، کل بلاک اسکیما رد می‌شود (اجرای مجدد بی‌خطر) */
IF OBJECT_ID(N'[Admins]', N'U') IS NULL
BEGIN

BEGIN TRANSACTION;

CREATE TABLE [Admins] (
    [Id] int NOT NULL IDENTITY,
    [Username] nvarchar(80) NOT NULL,
    [PasswordHash] nvarchar(500) NOT NULL,
    [FullName] nvarchar(150) NOT NULL,
    [Email] nvarchar(200) NULL,
    [LastLoginAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_Admins] PRIMARY KEY ([Id])
);

CREATE TABLE [ArticleCategories] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(100) NOT NULL,
    [Slug] nvarchar(120) NOT NULL,
    [DisplayOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_ArticleCategories] PRIMARY KEY ([Id])
);

CREATE TABLE [ConsultationRequests] (
    [Id] int NOT NULL IDENTITY,
    [FullName] nvarchar(150) NOT NULL,
    [Mobile] nvarchar(20) NOT NULL,
    [ServiceKind] int NOT NULL,
    [Description] nvarchar(max) NULL,
    [Status] int NOT NULL,
    [AdminNote] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_ConsultationRequests] PRIMARY KEY ([Id])
);

CREATE TABLE [ContactMessages] (
    [Id] int NOT NULL IDENTITY,
    [FullName] nvarchar(150) NOT NULL,
    [Mobile] nvarchar(20) NOT NULL,
    [Email] nvarchar(200) NULL,
    [Subject] nvarchar(200) NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [AdminNote] nvarchar(max) NULL,
    [IpAddress] nvarchar(50) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_ContactMessages] PRIMARY KEY ([Id])
);

CREATE TABLE [MenuItems] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(100) NOT NULL,
    [Url] nvarchar(300) NOT NULL,
    [Position] int NOT NULL,
    [DisplayOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_MenuItems] PRIMARY KEY ([Id])
);

CREATE TABLE [Services] (
    [Id] int NOT NULL IDENTITY,
    [Kind] int NOT NULL,
    [Title] nvarchar(100) NOT NULL,
    [ShortDescription] nvarchar(500) NOT NULL,
    [FullDescription] nvarchar(max) NOT NULL,
    [Slug] nvarchar(120) NOT NULL,
    [Icon] nvarchar(60) NOT NULL,
    [Image] nvarchar(300) NOT NULL,
    [ComingSoon] bit NOT NULL,
    [DisplayOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [Slogan] nvarchar(200) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_Services] PRIMARY KEY ([Id])
);

CREATE TABLE [SiteSettings] (
    [Id] int NOT NULL IDENTITY,
    [Key] nvarchar(100) NOT NULL,
    [Value] nvarchar(max) NOT NULL,
    [Title] nvarchar(150) NOT NULL,
    [Group] nvarchar(60) NOT NULL,
    [Type] nvarchar(20) NOT NULL,
    [DisplayOrder] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_SiteSettings] PRIMARY KEY ([Id])
);

CREATE TABLE [SiteUsers] (
    [Id] int NOT NULL IDENTITY,
    [FullName] nvarchar(150) NOT NULL,
    [Mobile] nvarchar(20) NOT NULL,
    [Email] nvarchar(200) NOT NULL,
    [PasswordHash] nvarchar(500) NOT NULL,
    [IsActive] bit NOT NULL,
    [LastLoginAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_SiteUsers] PRIMARY KEY ([Id])
);

CREATE TABLE [Sliders] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NOT NULL,
    [SubTitle] nvarchar(300) NOT NULL,
    [Image] nvarchar(300) NOT NULL,
    [Link] nvarchar(300) NULL,
    [DisplayOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_Sliders] PRIMARY KEY ([Id])
);

CREATE TABLE [TariffSections] (
    [Id] int NOT NULL IDENTITY,
    [Kind] int NOT NULL,
    [Title] nvarchar(100) NOT NULL,
    [DisplayOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_TariffSections] PRIMARY KEY ([Id])
);

CREATE TABLE [TeamMembers] (
    [Id] int NOT NULL IDENTITY,
    [FullName] nvarchar(150) NOT NULL,
    [Role] nvarchar(120) NOT NULL,
    [Image] nvarchar(300) NOT NULL,
    [LinkedinUrl] nvarchar(300) NULL,
    [DisplayOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_TeamMembers] PRIMARY KEY ([Id])
);

CREATE TABLE [Articles] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(250) NOT NULL,
    [Summary] nvarchar(1000) NOT NULL,
    [Body] nvarchar(max) NOT NULL,
    [Image] nvarchar(300) NOT NULL,
    [AuthorName] nvarchar(120) NOT NULL,
    [ArticleCategoryId] int NOT NULL,
    [PublishedAt] datetime2 NOT NULL,
    [Status] int NOT NULL,
    [VisitCount] int NOT NULL,
    [Slug] nvarchar(280) NOT NULL,
    [IsFeatured] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_Articles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Articles_ArticleCategories_ArticleCategoryId] FOREIGN KEY ([ArticleCategoryId]) REFERENCES [ArticleCategories] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Portfolios] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(150) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [Image] nvarchar(300) NOT NULL,
    [Url] nvarchar(300) NULL,
    [ServiceId] int NULL,
    [ComingSoon] bit NOT NULL,
    [DisplayOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_Portfolios] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Portfolios_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([Id]) ON DELETE SET NULL
);

CREATE TABLE [ServiceFeatures] (
    [Id] int NOT NULL IDENTITY,
    [ServiceId] int NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NULL,
    [DisplayOrder] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_ServiceFeatures] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ServiceFeatures_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ServiceSteps] (
    [Id] int NOT NULL IDENTITY,
    [ServiceId] int NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [DisplayOrder] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_ServiceSteps] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ServiceSteps_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ProjectOrders] (
    [Id] int NOT NULL IDENTITY,
    [FullName] nvarchar(150) NOT NULL,
    [Mobile] nvarchar(20) NOT NULL,
    [Email] nvarchar(200) NULL,
    [ProjectType] int NOT NULL,
    [Budget] bigint NULL,
    [Description] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [AdminNote] nvarchar(max) NULL,
    [SiteUserId] int NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_ProjectOrders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProjectOrders_SiteUsers_SiteUserId] FOREIGN KEY ([SiteUserId]) REFERENCES [SiteUsers] ([Id])
);

CREATE TABLE [TariffPackages] (
    [Id] int NOT NULL IDENTITY,
    [TariffSectionId] int NOT NULL,
    [Title] nvarchar(150) NOT NULL,
    [Price] bigint NULL,
    [PriceNote] nvarchar(100) NULL,
    [PriceSuffix] nvarchar(100) NULL,
    [Duration] nvarchar(100) NULL,
    [IsInstallmentAvailable] bit NOT NULL,
    [HasFreeSupport] bit NOT NULL,
    [DisplayOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [IsPopular] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_TariffPackages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TariffPackages_TariffSections_TariffSectionId] FOREIGN KEY ([TariffSectionId]) REFERENCES [TariffSections] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [TariffItems] (
    [Id] int NOT NULL IDENTITY,
    [TariffPackageId] int NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [DisplayOrder] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_TariffItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TariffItems_TariffPackages_TariffPackageId] FOREIGN KEY ([TariffPackageId]) REFERENCES [TariffPackages] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_Admins_Username] ON [Admins] ([Username]);

CREATE UNIQUE INDEX [IX_ArticleCategories_Slug] ON [ArticleCategories] ([Slug]);

CREATE INDEX [IX_Articles_ArticleCategoryId] ON [Articles] ([ArticleCategoryId]);

CREATE INDEX [IX_Articles_Slug] ON [Articles] ([Slug]);

CREATE INDEX [IX_Portfolios_ServiceId] ON [Portfolios] ([ServiceId]);

CREATE INDEX [IX_ProjectOrders_SiteUserId] ON [ProjectOrders] ([SiteUserId]);

CREATE INDEX [IX_ServiceFeatures_ServiceId] ON [ServiceFeatures] ([ServiceId]);

CREATE UNIQUE INDEX [IX_Services_Slug] ON [Services] ([Slug]);

CREATE INDEX [IX_ServiceSteps_ServiceId] ON [ServiceSteps] ([ServiceId]);

CREATE UNIQUE INDEX [IX_SiteSettings_Key] ON [SiteSettings] ([Key]);

CREATE UNIQUE INDEX [IX_SiteUsers_Mobile] ON [SiteUsers] ([Mobile]);

CREATE INDEX [IX_TariffItems_TariffPackageId] ON [TariffItems] ([TariffPackageId]);

CREATE INDEX [IX_TariffPackages_TariffSectionId] ON [TariffPackages] ([TariffSectionId]);

/* جدول لاگ خطاها (مایگریتم AddErrorLogTable) */
CREATE TABLE [ErrorLogs] (
    [Id] int NOT NULL IDENTITY,
    [Message] nvarchar(max) NOT NULL,
    [StackTrace] nvarchar(max) NULL,
    [Source] nvarchar(300) NULL,
    [Path] nvarchar(300) NULL,
    [HttpMethod] nvarchar(10) NULL,
    [IpAddress] nvarchar(50) NULL,
    [UserName] nvarchar(100) NULL,
    [Severity] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_ErrorLogs] PRIMARY KEY ([Id])
);

CREATE INDEX [IX_ErrorLogs_CreatedAt] ON [ErrorLogs] ([CreatedAt]);

CREATE INDEX [IX_ErrorLogs_Severity] ON [ErrorLogs] ([Severity]);

INSERT INTO [__SoransoftMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260915064050_InitialCreate', N'9.0.0'),
       (N'20260915064150_AddDependentQueryFilters', N'9.0.0'),
       (N'20260915073147_AddErrorLogTable', N'9.0.0');

COMMIT;

END; -- پایان بلاک اسکیما
GO

/* ============================================================================
   2) داده‌های اولیه — idempotent (فقط اگر خالی باشند)
      مقادیر CreatedAt با GETDATE() پر می‌شوند (برنامه WHERE history ندارد)
   ============================================================================ */

/* ---------- ادمین پیش‌فرض: admin / Admin@123 ---------- */
IF NOT EXISTS (SELECT 1 FROM [Admins] WHERE [Username] = N'admin')
    INSERT INTO [Admins] ([Username], [PasswordHash], [FullName], [Email], [LastLoginAt], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (
        N'admin',
        N'AQAAAAIAAYagAAAAEB/WCH2At77PdI8ETmSyC+hVnv2vnKuC28oDMs7YsMoOGf2mXMC2pSsA6LdpCRlYXA==',
        N'مدیر سایت',
        N'admin@soransoft.ir',
        NULL, 1, GETDATE(), NULL, 0, NULL);
GO

/* ---------- تنظیمات سایت ---------- */
IF NOT EXISTS (SELECT 1 FROM [SiteSettings])
BEGIN
    INSERT INTO [SiteSettings] ([Key], [Value], [Title], [Group], [Type], [DisplayOrder], [CreatedAt])
    VALUES
      (N'SiteName',         N'سوران سافت',                                   N'نام سایت',            N'عمومی',              N'text',     1,  GETDATE()),
      (N'SiteTagline',      N'هر چه در فکر شماست، ما می‌سازیم',               N'شعار سایت',           N'عمومی',              N'text',     2,  GETDATE()),
      (N'SiteDescription',  N'طراحی سایت، سئو، اپلیکیشن موبایل و تولید محتوا', N'توضیح متا',          N'عمومی',              N'textarea', 3,  GETDATE()),
      (N'LogoText',         N'SORANSOFT',                                    N'متن لوگو',            N'عمومی',              N'text',     4,  GETDATE()),
      (N'Phone',            N'021-28 4 28 140',                              N'تلفن ثابت',           N'تماس',               N'text',     5,  GETDATE()),
      (N'Mobile',           N'09120000000',                                  N'موبایل',              N'تماس',               N'text',     6,  GETDATE()),
      (N'Address',          N'تهران، خیابان ولیعصر، مرکز نوآوری سوران',       N'آدرس',               N'تماس',               N'textarea', 7,  GETDATE()),
      (N'WorkingHours',     N'شنبه تا پنجشنبه ۹ تا ۱۸',                      N'ساعات کاری',          N'تماس',               N'text',     8,  GETDATE()),
      (N'Email',            N'info@soransoft.ir',                            N'ایمیل',               N'تماس',               N'text',     9,  GETDATE()),
      (N'InstagramUrl',     N'https://instagram.com/soransoft',              N'اینستاگرام',          N'شبکه‌های اجتماعی',    N'text',     10, GETDATE()),
      (N'TelegramUrl',      N'https://t.me/soransoft',                       N'تلگرام',              N'شبکه‌های اجتماعی',    N'text',     11, GETDATE()),
      (N'WhatsappUrl',      N'https://wa.me/989120000000',                   N'واتساپ',              N'شبکه‌های اجتماعی',    N'text',     12, GETDATE()),
      (N'AboutText',        N'تیم سوران سافت با بیش از یک دهه تجربه در طراحی وب، سئو و تولید محتوا، کنار شماست تا کسب‌وکارتان را آنلاین کنند.', N'متن درباره ما', N'درباره', N'textarea', 13, GETDATE()),
      (N'FooterNote',       N'Designed with ❤️ by Soransoft team',           N'متن فوتر',            N'عمومی',              N'text',     14, GETDATE()),
      (N'ConsultationNote', N'جهت دریافت مشاوره رایگان و قیمت دقیق اطلاعات زیر را پر کنید', N'یادداشت فرم مشاوره', N'عمومی', N'text',    15, GETDATE());
END
GO

/* ---------- منوها: Main=0، Mobile=1، Footer=2 (MenuPosition enum) ---------- */
IF NOT EXISTS (SELECT 1 FROM [MenuItems])
BEGIN
    INSERT INTO [MenuItems] ([Title], [Url], [Position], [DisplayOrder], [IsActive], [IsDeleted], [CreatedAt])
    VALUES
      (N'خانه',          N'/',             0, 1, 1, 0, GETDATE()),
      (N'خدمات',         N'/OurServices',  0, 2, 1, 0, GETDATE()),
      (N'نمونه کارها',   N'/Portfolio',    0, 3, 1, 0, GETDATE()),
      (N'سفارش پروژه',   N'/OrderProject', 0, 4, 1, 0, GETDATE()),
      (N'تعرفه',         N'/Tariff',       0, 5, 1, 0, GETDATE()),
      (N'مقالات',        N'/Articles',     0, 6, 1, 0, GETDATE()),
      (N'درباره ما',     N'/AboutUs',      0, 7, 1, 0, GETDATE()),
      (N'تماس با ما',    N'/ContactUs',    0, 8, 1, 0, GETDATE()),

      (N'خانه',          N'/',             1, 1, 1, 0, GETDATE()),
      (N'خدمات',         N'/OurServices',  1, 2, 1, 0, GETDATE()),
      (N'نمونه کارها',   N'/Portfolio',    1, 3, 1, 0, GETDATE()),
      (N'سفارش پروژه',   N'/OrderProject', 1, 4, 1, 0, GETDATE()),
      (N'تعرفه',         N'/Tariff',       1, 5, 1, 0, GETDATE()),
      (N'مقالات',        N'/Articles',     1, 6, 1, 0, GETDATE()),
      (N'درباره ما',     N'/AboutUs',      1, 7, 1, 0, GETDATE()),
      (N'تماس با ما',    N'/ContactUs',    1, 8, 1, 0, GETDATE()),

      (N'خدمات ما',      N'/OurServices',  2, 1, 1, 0, GETDATE()),
      (N'نمونه کارها',   N'/Portfolio',    2, 2, 1, 0, GETDATE()),
      (N'مقالات',        N'/Articles',     2, 3, 1, 0, GETDATE()),
      (N'درباره ما',     N'/AboutUs',      2, 4, 1, 0, GETDATE()),
      (N'تماس با ما',    N'/ContactUs',    2, 5, 1, 0, GETDATE());
END
GO

/* ---------- سرویس‌ها ---------- */
IF NOT EXISTS (SELECT 1 FROM [Services])
BEGIN
    INSERT INTO [Services] ([Kind], [Title], [ShortDescription], [FullDescription], [Slug], [Icon], [Image], [ComingSoon], [DisplayOrder], [IsActive], [IsDeleted], [Slogan], [CreatedAt])
    VALUES
    (0, N'طراحی سایت',
       N'وردپرس یا کدنویسی اختصاصی؟ کدوم مورد نیاز شماست؟ ما شما رو راهنمایی می کنیم',
       N'<p>در دنیای دیجیتال امروزی، بسیار مهم است که با مشتریان و کاربران خود در ارتباط باشید تا از بازارهای جدید استفاده کنید و تجارت خود را گسترش دهید. آمارها نشان می‌دهد که تقریباً ۴.۵۷ میلیارد نفر در سراسر جهان کاربر فعال اینترنت هستند.</p><p>بدون حضور در دنیای وب، شما به‌طور خودکار فرصت برقراری ارتباط با مخاطبان هدف خود و محبوبیت برند خود را از دست می‌دهید. با طراحی یک وب‌سایت و صرف هزینه‌ای اندک، در دنیای اینترنت نیز حضور داشته باشید.</p><p>ما در سوران سافت خدمات طراحی وب‌سایت سازگار با سئو را ارائه می‌دهیم که هدف آن افزایش رتبه‌ی وب‌سایت شما در نتایج موتورهای جستجو، افزایش تعامل با مشتری‌ها و افزایش درآمد شما است.</p><h3>چرا باید طراحی وب‌سایت خود را به سوران سافت بسپارید؟</h3><ul><li>بهینه‌سازی نرخ تبدیل (CRO)</li><li>طراحی ریسپانسیو مطابق استانداردهای روز</li><li>افزایش رتبه در موتورهای جستجو</li><li>کاهش هزینه‌ی نگهداری</li><li>بهینه‌سازی تجربه‌ی کاربری</li><li>افزایش شهرت برند</li></ul><p>طراحی سایت اختصاصی با ASP.NET Core و کدنویسی اختصاصی، سبک‌تر و سریع‌تر از قالب‌های آماده است و گوگل عاشق سرعت است!</p>',
       N'site-project', N'bi-globe2', N'', 0, 1, 1, 0, N'برای اولین بودن باید بهترین بود', GETDATE()),
    (1, N'سئوی سایت',
       N'باورت میشه سایتت صفحه اول گوگل باشه؟! باور کن! با سوران سافت شدنیه',
       N'<p>اومدن صفحه اول گوگل شوخی نیست! ما هم نیومدیم شوخی کنیم! لغت دلخواهت رو به ما بده و تماشا کن چه گرد و خاکی توی گوگل به پا می‌کنیم.</p><p>بالای هشتاد درصد از مشتری‌های شما از گوگل میان پس سئو رو دست کم نگیر. برای رسیدن به صفحه اول گوگل هم کار فنی لازمه هم کار محتوایی که سوران سافت در هر دو زمینه تخصص و تجربه دارد.</p>',
       N'seo', N'bi-graph-up-arrow', N'', 0, 2, 1, 0, N'صفحه اول گوگل، دیگر رویا نیست', GETDATE()),
    (2, N'اپلیکیشن موبایل',
       N'اندروید، ای او اس، مک، ویندوز! دیگه کجا میخوای اجرا بشه؟ اپلیکیشنت همه جا هست',
       N'<p>اپلیکیشن موبایل، نزدیک‌ترین نقطه‌ی تماس برند شما با مشتری است. به‌زودی خدمات اپلیکیشن سوران سافت آغاز می‌شود!</p>',
       N'application', N'bi-phone', N'', 1, 3, 1, 0, N'برند شما در جیب مشتری', GETDATE()),
    (3, N'تولید محتوا',
       N'سایت داری محتوا نداره؟ ما محتواش رو می سازیم با یک تیم حرفه ای و ابزارهای به روز',
       N'<p>برای سایتت محتوای تخصصی نیاز داری یا محتوای عمومی؟ فرقی نداره، سوران سافت از پس جفتش مثل آب خوردن بر میاد. تیم تولید محتوا کاملاً منعطف است و شما حتی می‌توانی لحن دلخواهت را به تیم بدهی تا با آن لحن محتوا تولید شود.</p><p>محتوای تولیدشده تمام اصول سئو در آن رعایت می‌شود و این‌طوری با یک تیر دو نشان می‌زنی!</p>',
       N'content', N'bi-pencil-square', N'', 0, 4, 1, 0, N'محتوای سئو شده، محتوای منحصر به فرد', GETDATE());
END
GO

/* ---------- ویژگی‌های سرویس طراحی سایت ---------- */
IF NOT EXISTS (SELECT 1 FROM [ServiceFeatures])
BEGIN
    INSERT INTO [ServiceFeatures] ([ServiceId], [Title], [Description], [DisplayOrder], [CreatedAt])
    SELECT Id, V.[Title], V.[Description], V.[Ord], GETDATE()
    FROM [Services] S
    CROSS APPLY (VALUES
        (N'طراحی ریسپانسیو',   N'سازگار با موبایل، تبلت و دسکتاپ', 1),
        (N'کدنویسی اختصاصی',   N'بدون قالب آماده، کاملاً اختصاصی',  2),
        (N'سئو شده',           N'مطابق اصول سئوی گوگل',            3),
        (N'پنل مدیریت',        N'کنترل کامل محتوای سایت',          4)
    ) V([Title], [Description], [Ord])
    WHERE S.[Slug] = N'site-project';
END
GO

/* ---------- مراحل سرویس‌ها (تایم‌لاین صفحه اصلی از همین‌ها می‌خواند) ---------- */
IF NOT EXISTS (SELECT 1 FROM [ServiceSteps])
BEGIN
    INSERT INTO [ServiceSteps] ([ServiceId], [Title], [DisplayOrder], [CreatedAt])
    SELECT S.[Id], V.[Title], V.[Ord], GETDATE()
    FROM [Services] S
    CROSS APPLY (VALUES
        (N'ثبت سفارش',              1), (N'واریز پیش پرداخت', 2), (N'تایید طراحی', 3), (N'تحویل پروژه', 4), (N'آموزش کار با پروژه', 5)
    ) V([Title], [Ord])
    WHERE S.[Slug] IN (N'site-project', N'application')
    UNION ALL
    SELECT S.[Id], V.[Title], V.[Ord], GETDATE()
    FROM [Services] S
    CROSS APPLY (VALUES
        (N'ثبت سفارش', 1), (N'مشاوره', 2), (N'ارزیابی کلمات کلیدی', 3), (N'تدوین استراتژی', 4), (N'ارائه گزارش', 5)
    ) V([Title], [Ord])
    WHERE S.[Slug] = N'seo'
    UNION ALL
    SELECT S.[Id], V.[Title], V.[Ord], GETDATE()
    FROM [Services] S
    CROSS APPLY (VALUES
        (N'ثبت سفارش', 1), (N'دریافت موضوع', 2), (N'یافتن منابع', 3), (N'تولید محتوا', 4), (N'ارائه مقالات', 5)
    ) V([Title], [Ord])
    WHERE S.[Slug] = N'content';
END
GO

/* ---------- تعرفه‌ها ---------- */
IF NOT EXISTS (SELECT 1 FROM [TariffSections])
BEGIN
    INSERT INTO [TariffSections] ([Kind], [Title], [DisplayOrder], [IsActive], [IsDeleted], [CreatedAt])
    VALUES (0, N'طراحی سایت',  1, 1, 0, GETDATE()),
           (1, N'سئو',         2, 1, 0, GETDATE()),
           (2, N'اپلیکیشن',    3, 1, 0, GETDATE()),
           (3, N'تولید محتوا', 4, 1, 0, GETDATE());
END
GO

/* ---------- پکیج‌های تعرفه ---------- */
IF NOT EXISTS (SELECT 1 FROM [TariffPackages])
BEGIN
    DECLARE @site    INT = (SELECT Id FROM [TariffSections] WHERE [Kind] = 0),
            @seo     INT = (SELECT Id FROM [TariffSections] WHERE [Kind] = 1),
            @app     INT = (SELECT Id FROM [TariffSections] WHERE [Kind] = 2),
            @content INT = (SELECT Id FROM [TariffSections] WHERE [Kind] = 3);

    INSERT INTO [TariffPackages] ([TariffSectionId], [Title], [Price], [PriceNote], [PriceSuffix], [Duration], [IsInstallmentAvailable], [HasFreeSupport], [DisplayOrder], [IsActive], [IsPopular], [IsDeleted], [CreatedAt])
    VALUES
    (@site,    N'وب سایت شرکتی',     30000000, NULL, NULL, N'مدت زمان چهار الی شش هفته', 1, 1, 1, 1, 0, 0, GETDATE()),
    (@site,    N'وب سایت فروشگاهی',  35000000, NULL, NULL, N'مدت زمان شش الی هشت هفته',  1, 1, 2, 1, 0, 0, GETDATE()),
    (@site,    N'وب سایت مجله خبری', 35000000, NULL, NULL, N'مدت زمان شش الی هشت هفته',  1, 1, 3, 1, 0, 0, GETDATE()),
    (@site,    N'وب سایت شخصی',      25000000, NULL, NULL, N'مدت زمان چهار الی شش هفته', 1, 1, 4, 1, 0, 0, GETDATE()),
    (@seo,     N'پروژه سئو',         NULL,     N'برای قیمت تماس بگیرید', NULL, NULL, 1, 0, 1, 1, 0, 0, GETDATE()),
    (@app,     N'اپلیکیشن موبایل',   NULL,     N'به زودی',               NULL, N'مدت زمان یک الی سه ماه', 1, 1, 1, 1, 0, 0, GETDATE()),
    (@content, N'1 تا 30 مقاله',     NULL,     NULL, N'کلمه ای 120', NULL, 0, 0, 1, 1, 0, 0, GETDATE()),
    (@content, N'30 تا 60 مقاله',    NULL,     NULL, N'کلمه ای 110', NULL, 0, 0, 2, 1, 0, 0, GETDATE()),
    (@content, N'60 تا 90 مقاله',    NULL,     NULL, N'کلمه ای 100', NULL, 0, 0, 3, 1, 0, 0, GETDATE()),
    (@content, N'90 مقاله به بالا',  NULL,     NULL, N'کلمه ای 90',  NULL, 0, 0, 4, 1, 0, 0, GETDATE()),
    (@content, N'1 تا 30 مقاله',     NULL,     NULL, N'کلمه ای 150', NULL, 0, 0, 5, 1, 0, 0, GETDATE()),
    (@content, N'30 تا 60 مقاله',    NULL,     NULL, N'کلمه ای 135', NULL, 0, 0, 6, 1, 0, 0, GETDATE()),
    (@content, N'60 تا 90 مقاله',    NULL,     NULL, N'کلمه ای 120', NULL, 0, 0, 7, 1, 0, 0, GETDATE()),
    (@content, N'90 مقاله به بالا',  NULL,     NULL, N'کلمه ای 105', NULL, 0, 0, 8, 1, 0, 0, GETDATE());
END
GO

/* ---------- آیتم‌های پکیج‌های تعرفه ---------- */
IF NOT EXISTS (SELECT 1 FROM [TariffItems])
BEGIN
    INSERT INTO [TariffItems] ([TariffPackageId], [Title], [DisplayOrder], [CreatedAt])
    SELECT P.[Id], V.[Title], V.[Ord], GETDATE()
    FROM [TariffPackages] P
    CROSS APPLY (VALUES
        (N'طراحی اختصاصی', 1), (N'پنل مدیریت کامل', 2), (N'بهینه برای سئو', 3)
    ) V([Title], [Ord])
    WHERE P.[Title] = N'وب سایت شرکتی'
    UNION ALL
    SELECT P.[Id], V.[Title], V.[Ord], GETDATE()
    FROM [TariffPackages] P
    CROSS APPLY (VALUES
        (N'درگاه پرداخت', 1), (N'مدیریت سفارش‌ها', 2), (N'پنل مدیریت فروشگاه', 3)
    ) V([Title], [Ord])
    WHERE P.[Title] = N'وب سایت فروشگاهی'
    UNION ALL
    SELECT P.[Id], V.[Title], V.[Ord], GETDATE()
    FROM [TariffPackages] P
    CROSS APPLY (VALUES (N'آنالیز کلمه کلیدی', 1), (N'تدوین استراتژی', 2), (N'تولید محتوا', 3)) V([Title], [Ord])
    WHERE P.[Title] = N'پروژه سئو'
    UNION ALL
    SELECT P.[Id], V.[Title], V.[Ord], GETDATE()
    FROM [TariffPackages] P
    CROSS APPLY (VALUES (N'محتوای سئو شده', 1), (N'محتوای منحصر به فرد', 2), (N'تحویل سریع', 3)) V([Title], [Ord])
    WHERE P.[PriceSuffix] IS NOT NULL AND P.[PriceSuffix] <> N'کلمه ای 150' AND P.[PriceSuffix] <> N'کلمه ای 135'
          AND P.[PriceSuffix] <> N'کلمه ای 120' AND P.[PriceSuffix] <> N'کلمه ای 105'
    UNION ALL
    SELECT P.[Id], V.[Title], V.[Ord], GETDATE()
    FROM [TariffPackages] P
    CROSS APPLY (VALUES (N'محتوای اختصاصی', 1), (N'محتوای سئو شده', 2), (N'تحویل سریع', 3)) V([Title], [Ord])
    WHERE P.[PriceSuffix] IN (N'کلمه ای 150', N'کلمه ای 135', N'کلمه ای 120', N'کلمه ای 105');
END
GO

/* ---------- نمونه‌کارها ---------- */
IF NOT EXISTS (SELECT 1 FROM [Portfolios])
BEGIN
    INSERT INTO [Portfolios] ([Title], [Description], [Image], [Url], [ServiceId], [ComingSoon], [DisplayOrder], [IsActive], [IsDeleted], [CreatedAt])
    SELECT V.[Title], V.[Description], N'', V.[Url], S.[Id], V.[Soon], V.[Ord], 1, 0, GETDATE()
    FROM [Services] S
    CROSS APPLY (VALUES
        (N'سایت موسوی کباب',      N'سایت رستورانی موسوی کباب، فروش آنلاین غذا',                                            N'https://mosavikabab.com', 0, 1),
        (N'سایت همایار',          N'سایت شرکت همایار در حال تکمیل است و به زودی بالا می آید',                              NULL,                       1, 2),
        (N'سایت همافارمد',        N'شرکت تولید کننده دارو که در سال 2008 تاسیس شده است',                                   N'https://hamapharmed.com', 0, 3),
        (N'سایت پیلو',            N'تولید کننده قابلمه، ماهیتابه و گریل',                                                  N'https://pilo.ir',         0, 4),
        (N'سایت هیل فیت',         N'هیلفیت از طریق علم ورزشی، تغذیه و روانشناسی به جامعه کمک می‌کند تا سبک زندگی سالم ایجاد کنیم', N'https://hillfit.ir', 0, 5),
        (N'سایت ایران قسط',       N'فروش قسطی کالا و خدمات',                                                               N'https://iranqest.ir',     0, 6),
        (N'سایت آقای طرح پارچه',  N'فروش انلاین پارچه مبل با امکان مشاهده زنده پارچه انتخابی بر روی مبل',                   N'https://aghayetarh.ir',   0, 7),
        (N'سایت پرو ابزار',       N'مرجع فروش آنلاین ابزار آلات و قطعات صنعتی',                                            N'https://proabzar.com',    0, 8),
        (N'سایت گرین باکس',       N'سفارش پکیج های پذیرایی برای ایونت های مختلف در کوتاه ترین زمان و تحویل در محل',         N'https://greenbox.ir',     0, 9),
        (N'سایت مای شیائومی',     N'مرجع تخصصی فروش محصولات شیائومی در ایران به همراه مجله خبری',                           N'https://myxiaomi.ir',     0, 10),
        (N'سایت ژوابکس',          N'شرکت وارد کننده فیلرهای زیبایی ژوابکس سوییس',                                          N'https://juvax.com',       0, 11)
    ) V([Title], [Description], [Url], [Soon], [Ord])
    WHERE S.[Slug] = N'site-project';
END
GO

/* ---------- دسته‌بندی‌های مقاله ---------- */
IF NOT EXISTS (SELECT 1 FROM [ArticleCategories])
BEGIN
    INSERT INTO [ArticleCategories] ([Title], [Slug], [DisplayOrder], [IsActive], [IsDeleted], [CreatedAt])
    VALUES
    (N'مقاله‌ها',     N'articles',    1, 1, 0, GETDATE()),
    (N'اخبار',        N'news',        2, 1, 0, GETDATE()),
    (N'آموزش',        N'education',   3, 1, 0, GETDATE()),
    (N'طراحی سایت',   N'site-design', 4, 1, 0, GETDATE()),
    (N'افزونه',       N'plugins',     5, 1, 0, GETDATE());
END
GO

/* ---------- مقالات نمونه ---------- */
IF NOT EXISTS (SELECT 1 FROM [Articles])
BEGIN
    INSERT INTO [Articles] ([Title], [Summary], [Body], [Image], [AuthorName], [ArticleCategoryId], [PublishedAt], [Status], [VisitCount], [Slug], [IsFeatured], [IsDeleted], [CreatedAt])
    VALUES
    (N'نقشه سایت یا سایت مپ چیست؟',
     N'هر آنچه باید درباره نقشه سایت و نقش آن در ایندکس شدن سریع‌تر بدانید.',
     N'<p>نقشه سایت (Sitemap) فایلی است که ساختار صفحات سایت شما را به موتورهای جستجو معرفی می‌کند و ایندکس شدن سریع‌تر صفحات را ممکن می‌سازد. در این مقاله با انواع سایت مپ، ساخت و ثبت آن در سرچ کنسول آشنا می‌شوید.</p>',
     N'', N'تیم سوران سافت', (SELECT Id FROM [ArticleCategories] WHERE [Slug] = N'articles'),  DATEADD(DAY, -10, GETDATE()), 1, 42,  N'what-is-sitemap', 0, 0, GETDATE()),
    (N'چرا بازدید سایت من کم است؟',
     N'دلایل رایج کم بودن بازدید سایت و راه‌حل‌های عملی برای رفع آن‌ها.',
     N'<p>کم بودن بازدید سایت دلایل مختلفی دارد: ضعف سئوی فنی، محتوای بی‌کیفیت، سرعت پایین سایت و نبود بک‌لینک‌های معتبر. در این مقاله هر یک را بررسی می‌کنیم و راه‌حل عملی ارائه می‌دهیم.</p>',
     N'', N'تیم سوران سافت', (SELECT Id FROM [ArticleCategories] WHERE [Slug] = N'education'), DATEADD(DAY, -7, GETDATE()),  1, 87,  N'why-my-website-has-low-traffic', 0, 0, GETDATE()),
    (N'چرا نمی‌توان برای بالا رفتن رتبه سایت در گوگل زمان تعیین کرد؟',
     N'چرا هیچ‌کس نمی‌تواند زمان دقیق رسیدن به صفحه اول گوگل را تضمین کند؟',
     N'<p>الگوریتم گوگل صدها سیگنال را بررسی می‌کند و رقابت کلمات کلیدی متفاوت است؛ به همین دلیل هیچ‌کس نمی‌تواند زمان دقیقی برای رتبه گرفتن تعیین کند، اما با استراتژی درست مسیر رشد قابل پیش‌بینی می‌شود.</p>',
     N'', N'تیم سوران سافت', (SELECT Id FROM [ArticleCategories] WHERE [Slug] = N'articles'),  DATEADD(DAY, -3, GETDATE()),  1, 65,  N'google-ranking-time-expectations', 0, 0, GETDATE()),
    (N'چرا سرعت سایت‌های ASP.NET بیشتر از سایت‌های وردپرسی است؟',
     N'مقایسه فنی سرعت سایت‌های کدنویسی‌شده با ASP.NET و وردپرس.',
     N'<p>ASP.NET Core با کامپایل پیش‌دستانه، کش سمت سرور و معماری سبک، عملکرد به‌مراتب بهتری نسبت به وردپرس ارائه می‌دهد. سرعت سایت یکی از فاکتورهای مهم سئو است.</p>',
     N'', N'تیم سوران سافت', (SELECT Id FROM [ArticleCategories] WHERE [Slug] = N'site-design'), DATEADD(DAY, -1, GETDATE()), 1, 120, N'aspnet-vs-wordpress-speed', 0, 0, GETDATE());
END
GO

/* ---------- اعضای تیم ---------- */
IF NOT EXISTS (SELECT 1 FROM [TeamMembers])
BEGIN
    INSERT INTO [TeamMembers] ([FullName], [Role], [Image], [LinkedinUrl], [DisplayOrder], [IsActive], [IsDeleted], [CreatedAt])
    VALUES
    (N'علی رضایی',  N'مدیر بخش برنامه نویسی',  N'', NULL, 1, 1, 0, GETDATE()),
    (N'سارا محمدی', N'مدیر بخش طراحی و سئو',   N'', NULL, 2, 1, 0, GETDATE()),
    (N'امیر کریمی', N'مدیر بخش تولید محتوا',   N'', NULL, 3, 1, 0, GETDATE());
END
GO

/* ---------- اسلایدرهای صفحه اصلی ---------- */
IF NOT EXISTS (SELECT 1 FROM [Sliders])
BEGIN
    INSERT INTO [Sliders] ([Title], [SubTitle], [Image], [Link], [DisplayOrder], [IsActive], [IsDeleted], [CreatedAt])
    VALUES
    (N'هر چه در فکر شماست، ما می‌سازیم',        N'طراحی سایت حرفه‌ای با کدنویسی اختصاصی',        N'', N'/OrderProject',    1, 1, 0, GETDATE()),
    (N'صفحه اول گوگل، دیگر رویا نیست',          N'خدمات سئو با استراتژی محتوایی و فنی',          N'', N'/SeoProject',      2, 1, 0, GETDATE()),
    (N'محتوای سئو شده، محتوای منحصر به فرد',    N'تیم تولید محتوا با ابزارهای به‌روز',            N'', N'/ContentProject',  3, 1, 0, GETDATE());
END
GO

PRINT N'✅ SoransoftDb با موفقیت ساخته شد — اسکیمای کامل + داده‌های اولیه (ادمین: admin / Admin@123)';
GO

# Soransoft — تحلیل وب‌سایت مرجع (siteliner.ir) و معماری پروژه

این سند، نتیجه‌ی بررسی دقیق وب‌سایت siteliner.ir و نقشه‌ی پیاده‌سازی پروژه **soransoft** است.

## ۱. صفحات شناسایی‌شده در وب‌سایت مرجع

| صفحه | آدرس | محتوای کلیدی |
|---|---|---|
| خانه | `/` | هیرو با تگ‌لاین «هر چه در فکر شماست، ما می‌سازیم»، ۴ سرویس (طراحی سایت، سئو، اپلیکیشن موبایل-به‌زودی، تولید محتوا)، تب‌های «هوشمندانه انتخاب کن» با تایم‌لاین ۵ مرحله‌ای هر سرویس + تایمر شمارش‌معکوس، اسلایدر نمونه‌کارها (۱۱ آیتم)، تب‌های تعرفه ۴ سرویس، فرم مشاوره رایگان، ۲ مقاله آخر |
| خدمات | `/OurServices` | ۳ سرویس فعال با توضیح بلند و دکمه «بیشتر بدانید» |
| صفحه سرویس | `/SiteProject` | محتوای سئویی بلند (مقاله خدمات طراحی سایت) + CTA مشاوره |
| نمونه‌کارها | `/Portfolio` | گرید ۲۱ نمونه‌کار با عنوان/توضیح/دکمه مشاهده یا «به زودی» |
| مقالات | `/Articles` | لیست مقالات با دسته‌بندی (مقاله‌ها/اخبار/آموزش/طراحی سایت/افزونه) |
| جزئیات مقاله | `/Article/{id}` | عنوان، متن کامل، نویسنده، تاریخ انتشار |
| تعرفه | منوی سایت (تب‌ها) | ۴ سرویس، پکیج‌ها با ویژگی‌ها و قیمت (وب شرکتی ۳۰م، فروشگاهی ۳۵م، مجله ۳۵م، شخصی ۲۵م، سئو «تماس»، اپ «به‌زودی»، تولید محتوا پله‌ای کلمه‌ای ۹۰ تا ۱۵۰ تومان) + فرم مشاوره |
| سفارش پروژه | منوی سایت | فرم ثبت سفارش (نام، تماس، نوع پروژه، توضیحات) |
| درباره ما | `/AboutUs` | معرفی تیم، سابقه، ۳ نفر تیم با سمت |
| تماس با ما | `/ContactUs` | آدرس، تلفن ثابت/موبایل، فرم تماس، نقشه |
| ورود/عضویت | منو | احراز هویت کاربران |
| فوتِر | همه صفحات | «ما را دنبال کنید»، لینک‌ها، آدرس، تلفن، «Designed with ❤️ by Soransoft team» |

## ۲. معماری پروژه (Clean / Onion، سازگار با Visual Studio 2022، .NET 8)

```
Soransoft.sln
├─ Soransoft.Domain        (Entities, Enums,.Abstractions — بدون هیچ وابستگی)
├─ Soransoft.Application   (ViewModels, DTOs, Services/Interfaces, Mappers)
├─ Soransoft.Infrastructure (EF Core DbContext, Configurations, Repositories, Seed, FileStorage, Identity)
└─ Soransoft.Web           (ASP.NET Core MVC — Public Pages + Areas/Admin + RTL fa-UI)
```

- **Database:** SQL Server + EF Core 8 (Code-First + Migrations)
- **Auth:** ASP.NET Core Identity (Admin role + cookie)
- **UI:** Bootstrap 5 RTL، Vazirmatn فونت، CSS اختصاصی سبک siteliner
- **Admin Panel:** Area `Admin` با مدیریت کامل: داشبورد، سرویس‌ها، نمونه‌کارها، مقالات+دسته‌ها، تعرفه‌ها+پکیج‌ها، سفارش‌ها، پیام‌ها، کاربران، تنظیمات سایت، منو، اسلایدر/بنر، لاگ تماس‌ها

## ۳. جداول پایه

`SiteSetting`, `Menu`, `Service`, `ServiceFeature`, `ServiceStep`, `Portfolio`, `ArticleCategory`, `Article`, `TariffSection`, `TariffPackage`, `TariffItem`, `ProjectOrder`, `ContactMessage`, `ConsultationRequest`, `Slider`, `TeamMember`, `User(Identity)`

## ۴. جریان‌های کلیدی UX

- منوی دوتایی (دسکتاپ/موبایل) همانند مرجع + دکمه ورود
- تب‌های سرویس در خانه/تعرفه با تایم‌لاین و شمارش‌معکوس انیمیشنی
- فرم مشاوره (اسکرول عمودی شماره‌دار) → `ConsultationRequest`
- فرم سفارش پروژه → `ProjectOrder` (با انتخاب نوع سرویس)
- فرم تماس → `ContactMessage`
- همه‌ی فرم‌ها با AJAX + پیام موفقیت/خطا، `AntiForgeryToken`
- حالت «به زودی» برای سرویس/پکیج غیرفعال

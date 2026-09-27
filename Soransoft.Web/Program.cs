using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Soransoft.Application.DependencyInjection;
using Soransoft.Application.Interfaces;
using Soransoft.Infrastructure.Persistence;
using Soransoft.Infrastructure.DependencyInjection;
using Soransoft.Web.Infrastructure;
using Soransoft.Web.Middleware;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// ---------- Services ----------
// غیرفعال‌سازی Required ضمنی برای خاصیت‌های non-nullable موجودیت‌ها؛
// بدون این، هر فرمی که همه‌ی خاصیت‌های رشته‌ای/ناوبری موجودیت را ارسال نکند بی‌صدا رد می‌شد
builder.Services.AddControllersWithViews(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    // تاریخ‌های ارسالی از فیلدهای <input type="date"> همیشه میلادی‌اند؛ با تقویم شمسی فرهنگ fa-IR
    // اشتباه تفسیر می‌شدند و با اختلاف چند صد سال ذخیره می‌شدند.
    options.ModelBinderProviders.Insert(0, new GregorianDateModelBinderProvider());
});

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// ---------- احراز هویت کوکی‌محور: ادمین + کاربر عادی (دو اسکیم جدا) ----------
// اسکیم «Smart» بر اساس کوکی موجود، درخواست را به اسکیم مناسب فوروارد می‌کند
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Smart";
    options.DefaultAuthenticateScheme = "Smart";
    options.DefaultChallengeScheme = "Smart";
})
.AddPolicyScheme("Smart", "Smart", options =>
{
    options.ForwardDefaultSelector = context =>
        context.Request.Cookies.ContainsKey("Soransoft.Partner") ? "PartnerAuth"
        : context.Request.Cookies.ContainsKey("Soransoft.User") ? "UserAuth"
        : CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Admin/Account/Login";
    options.AccessDeniedPath = "/Admin/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
})
.AddCookie("UserAuth", options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
    options.Cookie.Name = "Soransoft.User";
})
.AddCookie("PartnerAuth", options =>
{
    options.LoginPath = "/Partner/Account/Login";
    options.AccessDeniedPath = "/Partner/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromHours(12);
    options.SlidingExpiration = true;
    options.Cookie.Name = "Soransoft.Partner";
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("PartnerOnly", policy => policy.RequireClaim("UserType", "Partner"));
});

// ---------- Culture (fa-IR, RTL) ----------
var faCulture = new CultureInfo("fa-IR")
{
    DateTimeFormat = { Calendar = new PersianCalendar() }
};
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("fa-IR");
    options.SupportedCultures = new[] { faCulture };
    options.SupportedUICultures = new[] { faCulture };
});

var app = builder.Build();

// ---------- Database ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SoransoftDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    try
    {
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db, hasher);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "خطا در اجرای Migration/Seed دیتابیس");
    }
}

// ---------- Pipeline ----------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseErrorLogging();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseRequestLocalization();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

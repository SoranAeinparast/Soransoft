using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Soransoft.Application.DependencyInjection;
using Soransoft.Application.Interfaces;
using Soransoft.Infrastructure.Persistence;
using Soransoft.Infrastructure.DependencyInjection;
using Soransoft.Infrastructure.Storage;
using Soransoft.Web.Infrastructure;
using Soransoft.Web.Middleware;
using System.Globalization;
using System.Net;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

var allowedHosts = builder.Configuration["AllowedHosts"];
if (!builder.Environment.IsDevelopment() &&
    (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
        .Any(host => host.Trim() == "*")))
    throw new InvalidOperationException("AllowedHosts must contain explicit trusted hosts outside Development.");

builder.Services.AddHostFiltering(options =>
{
    options.AllowedHosts.Clear();
    foreach (var host in (allowedHosts ?? string.Empty)
        .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        options.AllowedHosts.Add(host);
    }
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;

    var configuredProxies = builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").GetChildren()
        .Select(section => section.Value)
        .Concat((builder.Configuration["ForwardedHeaders:KnownProxies"] ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    foreach (var value in configuredProxies.Distinct(StringComparer.OrdinalIgnoreCase))
    {
        if (IPAddress.TryParse(value, out var address))
            options.KnownProxies.Add(address);
    }
});

async Task ValidateActiveCookieAsync(CookieValidatePrincipalContext context)
{
    if (!int.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var accountId))
    {
        context.RejectPrincipal();
        return;
    }

    try
    {
        var db = context.HttpContext.RequestServices.GetRequiredService<SoransoftDbContext>();
        var isActive = context.Scheme.Name switch
        {
            "PartnerAuth" => await db.Partners.AsNoTracking()
                .AnyAsync(p => p.Id == accountId && p.IsActive && !p.IsDeleted, context.HttpContext.RequestAborted),
            "UserAuth" => await db.SiteUsers.AsNoTracking()
                .AnyAsync(u => u.Id == accountId && u.IsActive && !u.IsDeleted, context.HttpContext.RequestAborted),
            _ => await db.Admins.AsNoTracking()
                .AnyAsync(a => a.Id == accountId && a.IsActive && !a.IsDeleted, context.HttpContext.RequestAborted),
        };

        if (!isActive)
            context.RejectPrincipal();
    }
    catch (OperationCanceledException) when (context.HttpContext.RequestAborted.IsCancellationRequested)
    {
        throw;
    }
    catch (Exception ex)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Soransoft.Authentication");
        logger.LogWarning(ex, "Unable to validate the authenticated account cookie.");
        context.RejectPrincipal();
    }
}

void ConfigureCookie(CookieAuthenticationOptions options, string loginPath, string accessDeniedPath, TimeSpan lifetime, string cookieName)
{
    options.LoginPath = loginPath;
    options.AccessDeniedPath = accessDeniedPath;
    options.ExpireTimeSpan = lifetime;
    options.SlidingExpiration = true;
    options.Cookie.Name = cookieName;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Events.OnValidatePrincipal = ValidateActiveCookieAsync;
}

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
        context.Request.Path.StartsWithSegments("/Admin") ? CookieAuthenticationDefaults.AuthenticationScheme
        : context.Request.Path.StartsWithSegments("/Partner") ? "PartnerAuth"
        : context.Request.Path.StartsWithSegments("/Account") || context.Request.Path.StartsWithSegments("/Panel") ? "UserAuth"
        : context.Request.Cookies.ContainsKey("Soransoft.Partner") ? "PartnerAuth"
        : context.Request.Cookies.ContainsKey("Soransoft.User") ? "UserAuth"
        : CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    ConfigureCookie(options, "/Admin/Account/Login", "/Admin/Account/ChangePassword", TimeSpan.FromHours(8), "Soransoft.Admin");
})
.AddCookie("UserAuth", options =>
{
    ConfigureCookie(options, "/Account/Login", "/Account/Login", TimeSpan.FromDays(14), "Soransoft.User");
})
.AddCookie("PartnerAuth", options =>
{
    ConfigureCookie(options, "/Partner/Account/Login", "/Partner/Account/Login", TimeSpan.FromHours(12), "Soransoft.Partner");
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PartnerOnly", policy => policy.RequireClaim("UserType", "Partner"));
    options.AddPolicy("AdminOnly", policy => policy
        .RequireRole("Admin")
        .RequireAssertion(context => !context.User.HasClaim("MustChangePassword", "1")));
    options.AddPolicy("AdminPasswordSetup", policy => policy
        .RequireRole("Admin")
        .RequireClaim("MustChangePassword", "1"));
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
var persistentMediaRoot = PersistentPublicMediaStorage.ResolveRoot(app.Environment, builder.Configuration);
Directory.CreateDirectory(persistentMediaRoot);

// ---------- Database ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SoransoftDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Soransoft.Startup");
    try
    {
        await db.Database.MigrateAsync();
        await PublicMediaMigration.RunAsync(app.Environment, builder.Configuration, startupLogger);
        await PrivateDocumentMigration.RunAsync(
            db,
            app.Environment.ContentRootPath,
            app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"),
            PersistentProfileStorage.ResolveRoot(app.Environment, app.Configuration),
            startupLogger);
        await DbSeeder.SeedAsync(db, hasher, builder.Configuration);
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "خطا در اجرای Migration/Seed دیتابیس");
        // ادامه‌ی اجرای برنامه با schema قدیمی، خطای گمراه‌کننده‌ای مثل نبودن ستون در Login ایجاد می‌کند.
        throw;
    }
}

// ---------- Pipeline ----------
app.UseForwardedHeaders();
app.UseHostFiltering();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseErrorLogging();

app.UseHttpsRedirection();
var legacyPrivateDocumentPaths = new[]
{
    "/uploads/partners/contracts",
    "/uploads/partners/agreements",
    "/uploads/partners/wallet-docs",
    "/uploads/partners/contract-stages",
    "/uploads/partners/sellable-projects",
};
app.UseWhen(
    context => !legacyPrivateDocumentPaths.Any(path => context.Request.Path.StartsWithSegments(path))
        || context.Request.Path.StartsWithSegments("/uploads/partners/sellable-projects/images"),
    branch => branch.UseStaticFiles());
app.UseWhen(
    context => !legacyPrivateDocumentPaths.Any(path => context.Request.Path.StartsWithSegments(path))
        || context.Request.Path.StartsWithSegments("/uploads/partners/sellable-projects/images"),
    branch => branch.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(persistentMediaRoot),
        RequestPath = "/uploads",
    }));

app.UseRouting();
app.UseRequestLocalization();

app.UseAuthentication();

app.Use(async (context, next) =>
{
    var feature = context.RequestServices.GetRequiredService<ISiteFeatureService>();
    var path = context.Request.Path;
    var key = path.StartsWithSegments("/Partner")
        ? SiteFeatures.PartnerPortal
        : path.StartsWithSegments("/Account") || path.StartsWithSegments("/Panel")
            ? SiteFeatures.SiteAccount
            : path.StartsWithSegments("/verify/card")
                ? SiteFeatures.CardVerification
                : path.StartsWithSegments("/documents")
                    ? SiteFeatures.PartnerPortal
                    : null;

    if (key is not null && !context.User.IsInRole("Admin") && !await feature.IsEnabledAsync(key, context.RequestAborted))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next();
});

app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

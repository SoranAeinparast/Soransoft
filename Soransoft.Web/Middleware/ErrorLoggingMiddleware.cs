using Microsoft.EntityFrameworkCore;
using Soransoft.Domain.Entities;
using Soransoft.Infrastructure.Persistence;
using System.Diagnostics;

namespace Soransoft.Web.Middleware
{
    /// <summary>
    /// ثبت خودکار خطاهای مدیریت‌نشده‌ی برنامه در جدول ErrorLogs برای نمایش در پنل ادمین.
    /// پس از ثبت، درخواست با Status Code 500 خاتمه می‌ یابد.
    /// </summary>
    public class ErrorLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorLoggingMiddleware> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public ErrorLoggingMiddleware(
            RequestDelegate next,
            ILogger<ErrorLoggingMiddleware> logger,
            IServiceScopeFactory scopeFactory)
        {
            _next = next;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await LogAsync(context, ex, "Error");
                _logger.LogError(ex, "خطای ثبت‌نشده در {Path}", context.Request.Path);

                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    await context.Response.WriteAsync("خطایی در پردازش درخواست رخ داد. لطفاً بعداً تلاش کنید.");
                }
            }
        }

        private async Task LogAsync(HttpContext context, Exception ex, string severity)
        {
            try
            {
                //scope جدید؛ DbContext میان‌افزار طول عمر Scoped دارد
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<SoransoftDbContext>();

                db.ErrorLogs.Add(new ErrorLog
                {
                    Message = Truncate(ex.Message, 4000),
                    StackTrace = Truncate(ex.StackTrace, 8000),
                    Source = Truncate(ex.Source, 300),
                    Path = Truncate(context.Request.Path.ToString(), 300),
                    HttpMethod = Truncate(context.Request.Method, 10),
                    IpAddress = Truncate(context.Connection.RemoteIpAddress?.ToString(), 50),
                    UserName = Truncate(context.User.Identity?.Name, 100),
                    Severity = severity,
                });
                await db.SaveChangesAsync();
            }
            catch (Exception logEx)
            {
                // اگر خودِ لاگ‌گیری خطا خورد، نباید درخواست را خراب کند
                _logger.LogError(logEx, "خطا در ثبت ErrorLog");
            }
        }

        private static string Truncate(string? value, int maxLength) =>
            string.IsNullOrEmpty(value) ? string.Empty : value.Length <= maxLength ? value : value[..maxLength];
    }

    public static class ErrorLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseErrorLogging(this IApplicationBuilder app) =>
            app.UseMiddleware<ErrorLoggingMiddleware>();
    }
}

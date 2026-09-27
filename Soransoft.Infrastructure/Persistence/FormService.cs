using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using Soransoft.Application.ViewModels;
using Soransoft.Domain.Entities;
using Soransoft.Domain.Enums;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace Soransoft.Infrastructure.Persistence
{
    /// <summary>سرویس فرم‌های عمومی سایت: سفارش پروژه، تماس با ما و درخواست مشاوره</summary>
    public class FormService : IFormService
    {
        private readonly SoransoftDbContext _db;
        private readonly IHttpContextAccessor _http;
        private readonly ILogger<FormService> _logger;

        public FormService(SoransoftDbContext db, IHttpContextAccessor http, ILogger<FormService> logger)
        {
            _db = db;
            _http = http;
            _logger = logger;
        }

        /// <summary>شناسه کاربر عضو سایت در صورت لاگین بودن (از کوکی UserAuth)</summary>
        private int? CurrentSiteUserId
        {
            get
            {
                var user = _http.HttpContext?.User;
                if (user?.Identity?.IsAuthenticated != true) return null;
                if (!string.Equals(user.FindFirst("UserType")?.Value, "SiteUser", StringComparison.Ordinal)) return null;
                return int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
            }
        }

        /// <summary>ثبت سفارش پروژه — اگر کاربر عضو سایت لاگین باشد به حسابش متصل می‌شود</summary>
        public async Task<OperationResult> SubmitOrderAsync(OrderProjectViewModel model, CancellationToken ct = default)
        {
            try
            {
                var order = new ProjectOrder
                {
                    FullName = model.FullName.Trim(),
                    Mobile = model.Mobile.Trim(),
                    Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
                    ProjectType = model.ProjectType,
                    Budget = model.Budget,
                    Description = model.Description.Trim(),
                    Status = ProjectOrderStatus.New,
                };

                // اتصال سفارش به کاربر عضو سایت (در صورت لاگین بودن)
                var userId = CurrentSiteUserId;
                if (userId is not null)
                {
                    var exists = await _db.SiteUsers.AnyAsync(u => u.Id == userId.Value && !u.IsDeleted, ct);
                    if (exists)
                        order.SiteUserId = userId;
                }

                await _db.ProjectOrders.AddAsync(order, ct);
                await _db.SaveChangesAsync(ct);
                return OperationResult.Ok("سفارش شما با موفقیت ثبت شد. کارشناسان ما به‌زودی با شما تماس می‌گیرند.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در ثبت سفارش پروژه");
                return OperationResult.Fail("ثبت سفارش با خطا مواجه شد. دوباره تلاش کنید.");
            }
        }

        /// <summary>ثبت پیام فرم تماس با ما همراه با IP فرستنده</summary>
        public async Task<OperationResult> SubmitContactAsync(ContactUsViewModel model, string? ip, CancellationToken ct = default)
        {
            try
            {
                var message = new ContactMessage
                {
                    FullName = model.FullName.Trim(),
                    Mobile = model.Mobile.Trim(),
                    Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
                    Subject = model.Subject.Trim(),
                    Message = model.Message.Trim(),
                    Status = MessageStatus.Unread,
                    IpAddress = ip,
                };

                await _db.ContactMessages.AddAsync(message, ct);
                await _db.SaveChangesAsync(ct);
                return OperationResult.Ok("پیام شما با موفقیت ارسال شد. در اولین فرصت پاسخ خواهیم داد.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در ثبت پیام تماس");
                return OperationResult.Fail("ارسال پیام با خطا مواجه شد. دوباره تلاش کنید.");
            }
        }

        /// <summary>ثبت درخواست مشاوره رایگان</summary>
        public async Task<OperationResult> SubmitConsultationAsync(ConsultationRequestViewModel model, CancellationToken ct = default)
        {
            try
            {
                var request = new ConsultationRequest
                {
                    FullName = model.FullName.Trim(),
                    Mobile = model.Mobile.Trim(),
                    ServiceKind = model.ServiceKind,
                    Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                    Status = MessageStatus.Unread,
                };

                await _db.ConsultationRequests.AddAsync(request, ct);
                await _db.SaveChangesAsync(ct);
                return OperationResult.Ok("درخواست مشاوره شما ثبت شد. کارشناسان ما با شما تماس می‌گیرند.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در ثبت درخواست مشاوره");
                return OperationResult.Fail("ثبت درخواست مشاوره با خطا مواجه شد. دوباره تلاش کنید.");
            }
        }
    }
}

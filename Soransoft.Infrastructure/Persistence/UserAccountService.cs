using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Soransoft.Application.Interfaces;
using Soransoft.Application.ViewModels;
using Soransoft.Domain.Entities;

namespace Soransoft.Infrastructure.Persistence
{
    /// <summary>پیاده‌سازی سرویس حساب کاربری کاربران عادی</summary>
    public class UserAccountService : IUserAccountService
    {
        private readonly SoransoftDbContext _db;
        private readonly IPasswordHasher _hasher;
        private readonly ILogger<UserAccountService> _logger;

        public UserAccountService(
            SoransoftDbContext db,
            IPasswordHasher hasher,
            ILogger<UserAccountService> logger)
        {
            _db = db;
            _hasher = hasher;
            _logger = logger;
        }

        public async Task<AuthResult> RegisterAsync(RegisterViewModel model, CancellationToken ct = default)
        {
            try
            {
                var mobile = model.Mobile.Trim();
                if (await _db.SiteUsers.AnyAsync(u => u.Mobile == mobile, ct))
                    return AuthResult.Fail("این شماره موبایل قبلاً ثبت‌نام کرده است. وارد شوید یا شماره دیگری را امتحان کنید.");

                var user = new SiteUser
                {
                    FullName = model.FullName.Trim(),
                    Mobile = mobile,
                    Email = model.Email?.Trim() ?? string.Empty,
                    PasswordHash = _hasher.Hash(model.Password),
                    IsActive = true,
                    LastLoginAt = DateTime.Now,
                };

                _db.SiteUsers.Add(user);
                await _db.SaveChangesAsync(ct);
                return AuthResult.Ok(user, "ثبت‌نام با موفقیت انجام شد. خوش آمدید!");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در ثبت‌نام کاربر");
                return AuthResult.Fail("ثبت‌نام انجام نشد. لطفاً دوباره تلاش کنید.");
            }
        }

        public async Task<AuthResult> LoginAsync(UserLoginViewModel model, CancellationToken ct = default)
        {
            try
            {
                var mobile = model.Mobile.Trim();
                var user = await _db.SiteUsers.FirstOrDefaultAsync(u => u.Mobile == mobile, ct);

                if (user is null || !user.IsActive || user.IsDeleted || !_hasher.Verify(model.Password, user.PasswordHash))
                    return AuthResult.Fail("شماره موبایل یا رمز عبور اشتباه است");

                user.LastLoginAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
                return AuthResult.Ok(user, "ورود موفق. خوش آمدید!");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در ورود کاربر");
                return AuthResult.Fail("ورود انجام نشد. لطفاً دوباره تلاش کنید.");
            }
        }

        public Task<SiteUser?> GetByIdAsync(int userId, CancellationToken ct = default) =>
            _db.SiteUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);

        public async Task<AuthResult> UpdateProfileAsync(int userId, UserProfileViewModel model, CancellationToken ct = default)
        {
            var user = await _db.SiteUsers.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null) return AuthResult.Fail("کاربر یافت نشد");

            user.FullName = model.FullName.Trim();
            user.Email = model.Email?.Trim() ?? string.Empty;
            await _db.SaveChangesAsync(ct);
            return AuthResult.Ok(user, "پروفایل با موفقیت به‌روزرسانی شد");
        }

        public async Task<AuthResult> ChangePasswordAsync(int userId, ChangePasswordViewModel model, CancellationToken ct = default)
        {
            var user = await _db.SiteUsers.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null) return AuthResult.Fail("کاربر یافت نشد");

            if (!_hasher.Verify(model.CurrentPassword, user.PasswordHash))
                return AuthResult.Fail("رمز عبور فعلی اشتباه است");

            if (_hasher.Verify(model.NewPassword, user.PasswordHash))
                return AuthResult.Fail("رمز عبور جدید نباید با رمز فعلی یکسان باشد");

            user.PasswordHash = _hasher.Hash(model.NewPassword);
            await _db.SaveChangesAsync(ct);
            return AuthResult.Ok(user, "رمز عبور با موفقیت تغییر کرد");
        }

        public Task<List<ProjectOrder>> GetUserOrdersAsync(int userId, CancellationToken ct = default) =>
            _db.ProjectOrders.AsNoTracking()
                .Where(o => o.SiteUserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync(ct);
    }
}

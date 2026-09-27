using Soransoft.Application.ViewModels;
using Soransoft.Domain.Entities;

namespace Soransoft.Application.Interfaces
{
    /// <summary>نتایج عملیات حساب کاربری</summary>
    public class AuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public SiteUser? User { get; set; }

        public static AuthResult Ok(SiteUser user, string message = "با موفقیت انجام شد") =>
            new() { Success = true, Message = message, User = user };

        public static AuthResult Fail(string message) => new() { Success = false, Message = message };
    }

    /// <summary>سرویس حساب کاربری کاربران عادی سایت</summary>
    public interface IUserAccountService
    {
        Task<AuthResult> RegisterAsync(RegisterViewModel model, CancellationToken ct = default);
        Task<AuthResult> LoginAsync(UserLoginViewModel model, CancellationToken ct = default);
        Task<SiteUser?> GetByIdAsync(int userId, CancellationToken ct = default);
        Task<AuthResult> UpdateProfileAsync(int userId, UserProfileViewModel model, CancellationToken ct = default);
        Task<AuthResult> ChangePasswordAsync(int userId, ChangePasswordViewModel model, CancellationToken ct = default);
        Task<List<ProjectOrder>> GetUserOrdersAsync(int userId, CancellationToken ct = default);
    }
}

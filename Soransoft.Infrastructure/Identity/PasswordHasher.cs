using Microsoft.AspNetCore.Identity;
using Soransoft.Application.Interfaces;

namespace Soransoft.Infrastructure.Identity
{
    /// <summary>هش رمز عبور با ASP.NET Core Identity PasswordHasher</summary>
    public class PasswordHasher : IPasswordHasher
    {
        private readonly PasswordHasher<object> _hasher = new();

        public string Hash(string password) =>
            _hasher.HashPassword(new object(), password);

        public bool Verify(string password, string hash) =>
            _hasher.VerifyHashedPassword(new object(), hash, password) != PasswordVerificationResult.Failed;
    }
}

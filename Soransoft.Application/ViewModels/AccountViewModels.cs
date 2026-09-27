using System.ComponentModel.DataAnnotations;

namespace Soransoft.Application.ViewModels
{
    /// <summary>فرم ثبت‌نام کاربر عادی</summary>
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "نام و نام خانوادگی الزامی است")]
        [StringLength(150)]
        [Display(Name = "نام و نام خانوادگی")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "شماره موبایل الزامی است")]
        [StringLength(20)]
        [Display(Name = "شماره موبایل")]
        [RegularExpression(@"^09\d{9}$", ErrorMessage = "شماره موبایل باید با 09 شروع شود و 11 رقم باشد")]
        public string Mobile { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "ایمیل معتبر نیست")]
        [Display(Name = "ایمیل (اختیاری)")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "رمز عبور الزامی است")]
        [StringLength(100, MinimumLength = 12, ErrorMessage = "رمز عبور باید حداقل ۱۲ کاراکتر باشد")]
        [DataType(DataType.Password)]
        [Display(Name = "رمز عبور")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "تکرار رمز عبور الزامی است")]
        [Compare(nameof(Password), ErrorMessage = "رمز عبور و تکرار آن یکسان نیستند")]
        [DataType(DataType.Password)]
        [Display(Name = "تکرار رمز عبور")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>فرم ورود کاربر عادی</summary>
    public class UserLoginViewModel
    {
        [Required(ErrorMessage = "شماره موبایل الزامی است")]
        [Display(Name = "شماره موبایل")]
        public string Mobile { get; set; } = string.Empty;

        [Required(ErrorMessage = "رمز عبور الزامی است")]
        [DataType(DataType.Password)]
        [Display(Name = "رمز عبور")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "مرا به خاطر بسپار")]
        public bool RememberMe { get; set; }
    }

    /// <summary>ویرایش پروفایل کاربر</summary>
    public class UserProfileViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "نام و نام خانوادگی الزامی است")]
        [StringLength(150)]
        [Display(Name = "نام و نام خانوادگی")]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "ایمیل معتبر نیست")]
        [Display(Name = "ایمیل")]
        public string? Email { get; set; }

        [Display(Name = "شماره موبایل")]
        public string Mobile { get; set; } = string.Empty;

        public DateTime? LastLoginAt { get; set; }
        public DateTime RegisteredAt { get; set; }
    }

    /// <summary>تغییر رمز عبور کاربر</summary>
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "رمز عبور فعلی الزامی است")]
        [DataType(DataType.Password)]
        [Display(Name = "رمز عبور فعلی")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "رمز عبور جدید الزامی است")]
        [StringLength(100, MinimumLength = 12, ErrorMessage = "رمز عبور باید حداقل ۱۲ کاراکتر باشد")]
        [DataType(DataType.Password)]
        [Display(Name = "رمز عبور جدید")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "تکرار رمز عبور جدید الزامی است")]
        [Compare(nameof(NewPassword), ErrorMessage = "رمز عبور جدید و تکرار آن یکسان نیستند")]
        [DataType(DataType.Password)]
        [Display(Name = "تکرار رمز عبور جدید")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}

using System.Text;
using System.Text.RegularExpressions;

namespace Soransoft.Application.Services
{
    /// <summary>
    /// تولید URL تمیز (slug) از عنوان — با پشتیبانی از فارسی.
    /// حروف فارسی حفظ و فاصله‌ها به خط تیره تبدیل می‌شوند؛
    /// برای اسلاگ انگلیسی، حروف به ASCII نرمال می‌شوند.
    /// </summary>
    public static class SlugGenerator
    {
        public static string Generate(string title, bool transliterate = true)
        {
            if (string.IsNullOrWhiteSpace(title))
                return DateTime.Now.Ticks.ToString("x");

            var text = title.Trim().ToLowerInvariant();

            if (transliterate)
                text = Transliterate(text);

            // حذف کاراکترهای غیرمجاز
            text = Regex.Replace(text, @"[^\p{L}\p{Nd}\-]+", "-");
            text = Regex.Replace(text, @"-{2,}", "-");
            return text.Trim('-');
        }

        /// <summary>نگاشت حروف خاص به معادل لاتین</summary>
        private static string Transliterate(string input)
        {
            var sb = new StringBuilder(input.Length);
            foreach (var ch in input)
            {
                sb.Append(ch switch
                {
                    'آ' or 'أ' or 'إ' => "a",
                    'ا' => "a",
                    'ب' => "b",
                    'پ' => "p",
                    'ت' => "t",
                    'ث' => "s",
                    'ج' => "j",
                    'چ' => "ch",
                    'ح' => "h",
                    'خ' => "kh",
                    'د' => "d",
                    'ذ' => "z",
                    'ر' => "r",
                    'ز' => "z",
                    'ژ' => "zh",
                    'س' => "s",
                    'ش' => "sh",
                    'ص' => "s",
                    'ض' => "z",
                    'ط' => "t",
                    'ظ' => "z",
                    'ع' => "a",
                    'غ' => "gh",
                    'ف' => "f",
                    'ق' => "gh",
                    'ک' or 'ك' => "k",
                    'گ' => "g",
                    'ل' => "l",
                    'م' => "m",
                    'ن' => "n",
                    'و' or 'ؤ' => "v",
                    'ه' or 'ة' => "h",
                    'ی' or 'ي' or 'ى' => "i",
                    'ٔ' or 'ِ' or 'َ' or 'ُ' or 'ّ' or 'ْ' => "", // اعراب
                    >= '0' and <= '9' => ch.ToString(),
                    >= 'a' and <= 'z' => ch.ToString(), // حروف لاتین موجود حفظ شوند
                    '-' => "-",
                    _ => "",
                });
            }
            return sb.ToString();
        }
    }
}

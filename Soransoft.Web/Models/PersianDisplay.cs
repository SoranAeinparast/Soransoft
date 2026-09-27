using System.Globalization;
using System.Text;

namespace Soransoft.Web.Models;

public static class PersianDisplay
{
    public static string Date(DateTime value) => DateTimeText(value, false);

    public static string? Date(DateTime? value) => value.HasValue ? Date(value.Value) : null;

    public static string DateTimeText(DateTime value, bool seconds = false)
    {
        var calendar = new PersianCalendar();
        var date = string.Create(CultureInfo.InvariantCulture, $"{calendar.GetYear(value):0000}/{calendar.GetMonth(value):00}/{calendar.GetDayOfMonth(value):00}");
        var time = seconds ? value.ToString("HH:mm:ss", CultureInfo.InvariantCulture) : value.ToString("HH:mm", CultureInfo.InvariantCulture);
        return ToPersianDigits($"{date} {time}");
    }

    public static string? DateTimeText(DateTime? value, bool seconds = false) =>
        value.HasValue ? DateTimeText(value.Value, seconds) : null;

    public static string GregorianDate(DateTime? value) =>
        value.HasValue ? value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;

    public static string Number<T>(T value, string format = "N0") where T : IFormattable =>
        ToPersianDigits(value.ToString(format, CultureInfo.InvariantCulture) ?? string.Empty);

    public static string ToPersianDigits(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value ?? string.Empty;

        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(character switch
            {
                >= '0' and <= '9' => (char)('۰' + (character - '0')),
                '٠' => '۰',
                '١' => '۱',
                '٢' => '۲',
                '٣' => '۳',
                '٤' => '۴',
                '٥' => '۵',
                '٦' => '۶',
                '٧' => '۷',
                '٨' => '۸',
                '٩' => '۹',
                _ => character,
            });
        }

        return builder.ToString();
    }
}

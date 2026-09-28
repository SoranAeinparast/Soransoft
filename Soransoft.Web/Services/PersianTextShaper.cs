using System.Text;

namespace Soransoft.Web.Services;

/// <summary>Shapes the Persian/Arabic characters used on the identity card without a native package.</summary>
internal static class PersianTextShaper
{
    private readonly record struct Forms(char Isolated, char Final, char Initial, char Medial, bool JoinsPrevious, bool JoinsNext);

    private static readonly IReadOnlyDictionary<char, Forms> CharacterForms = new Dictionary<char, Forms>
    {
        ['ء'] = new('\uFE80', '\uFE80', '\uFE80', '\uFE80', false, false),
        ['آ'] = new('\uFE81', '\uFE82', '\uFE81', '\uFE82', true, false),
        ['أ'] = new('\uFE83', '\uFE84', '\uFE83', '\uFE84', true, false),
        ['ؤ'] = new('\uFE85', '\uFE86', '\uFE85', '\uFE86', true, false),
        ['إ'] = new('\uFE87', '\uFE88', '\uFE87', '\uFE88', true, false),
        ['ئ'] = new('\uFE89', '\uFE8A', '\uFE8B', '\uFE8C', true, true),
        ['ا'] = new('\uFE8D', '\uFE8E', '\uFE8D', '\uFE8E', true, false),
        ['ب'] = new('\uFE8F', '\uFE90', '\uFE91', '\uFE92', true, true),
        ['پ'] = new('\uFB56', '\uFB57', '\uFB58', '\uFB59', true, true),
        ['ت'] = new('\uFE95', '\uFE96', '\uFE97', '\uFE98', true, true),
        ['ث'] = new('\uFE99', '\uFE9A', '\uFE9B', '\uFE9C', true, true),
        ['ج'] = new('\uFE9D', '\uFE9E', '\uFE9F', '\uFEA0', true, true),
        ['چ'] = new('\uFB7A', '\uFB7B', '\uFB7C', '\uFB7D', true, true),
        ['ح'] = new('\uFEA1', '\uFEA2', '\uFEA3', '\uFEA4', true, true),
        ['خ'] = new('\uFEA5', '\uFEA6', '\uFEA7', '\uFEA8', true, true),
        ['د'] = new('\uFEA9', '\uFEAA', '\uFEA9', '\uFEAA', true, false),
        ['ذ'] = new('\uFEAB', '\uFEAC', '\uFEAB', '\uFEAC', true, false),
        ['ر'] = new('\uFEAD', '\uFEAE', '\uFEAD', '\uFEAE', true, false),
        ['ز'] = new('\uFEAF', '\uFEB0', '\uFEAF', '\uFEB0', true, false),
        ['ژ'] = new('\uFB8A', '\uFB8B', '\uFB8A', '\uFB8B', true, false),
        ['س'] = new('\uFEB1', '\uFEB2', '\uFEB3', '\uFEB4', true, true),
        ['ش'] = new('\uFEB5', '\uFEB6', '\uFEB7', '\uFEB8', true, true),
        ['ص'] = new('\uFEB9', '\uFEBA', '\uFEBB', '\uFEBC', true, true),
        ['ض'] = new('\uFEBD', '\uFEBE', '\uFEBF', '\uFEC0', true, true),
        ['ط'] = new('\uFEC1', '\uFEC2', '\uFEC3', '\uFEC4', true, true),
        ['ظ'] = new('\uFEC5', '\uFEC6', '\uFEC7', '\uFEC8', true, true),
        ['ع'] = new('\uFEC9', '\uFECA', '\uFECB', '\uFECC', true, true),
        ['غ'] = new('\uFECD', '\uFECE', '\uFECF', '\uFED0', true, true),
        ['ف'] = new('\uFED1', '\uFED2', '\uFED3', '\uFED4', true, true),
        ['ق'] = new('\uFED5', '\uFED6', '\uFED7', '\uFED8', true, true),
        ['ک'] = new('\uFED9', '\uFEDA', '\uFEDB', '\uFEDC', true, true),
        ['ك'] = new('\uFED9', '\uFEDA', '\uFEDB', '\uFEDC', true, true),
        ['گ'] = new('\uFB92', '\uFB93', '\uFB94', '\uFB95', true, true),
        ['ل'] = new('\uFEDD', '\uFEDE', '\uFEDF', '\uFEE0', true, true),
        ['م'] = new('\uFEE1', '\uFEE2', '\uFEE3', '\uFEE4', true, true),
        ['ن'] = new('\uFEE5', '\uFEE6', '\uFEE7', '\uFEE8', true, true),
        ['و'] = new('\uFEE9', '\uFEEA', '\uFEE9', '\uFEEA', true, false),
        ['ه'] = new('\uFEEB', '\uFEEC', '\uFEED', '\uFEEE', true, true),
        ['ی'] = new('\uFBFC', '\uFBFD', '\uFBFE', '\uFBFF', true, true),
        ['ي'] = new('\uFEF1', '\uFEF2', '\uFEF3', '\uFEF4', true, true),
        ['ى'] = new('\uFEEF', '\uFEF0', '\uFEEF', '\uFEF0', true, false),
        ['ة'] = new('\uFE93', '\uFE94', '\uFE93', '\uFE94', true, false),
    };

    public static string Shape(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var source = value.Normalize(NormalizationForm.FormC).Trim();
        var result = new StringBuilder(source.Length);
        for (var index = source.Length - 1; index >= 0; index--)
        {
            var current = source[index];
            if (!CharacterForms.TryGetValue(current, out var forms))
            {
                result.Append(current);
                continue;
            }

            var hasPreviousConnection = index > 0 && CharacterForms.TryGetValue(source[index - 1], out var previous)
                && previous.JoinsNext && forms.JoinsPrevious;
            var hasNextConnection = index < source.Length - 1 && CharacterForms.TryGetValue(source[index + 1], out var next)
                && forms.JoinsNext && next.JoinsPrevious;

            result.Append(hasPreviousConnection
                ? hasNextConnection ? forms.Medial : forms.Final
                : hasNextConnection ? forms.Initial : forms.Isolated);
        }

        return result.ToString();
    }
}

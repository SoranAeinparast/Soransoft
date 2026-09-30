using System.Text.RegularExpressions;

namespace Soransoft.Web.Models;

public static partial class SafeHtml
{
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "br", "strong", "b", "em", "i", "u", "s", "h2", "h3", "h4", "ul", "ol", "li", "blockquote", "a",
        "div", "span", "section", "article", "small", "hr", "figure", "figcaption", "img"
    };

    public static string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;

        var value = DangerousBlockRegex().Replace(html, string.Empty);
        value = EventAttributeRegex().Replace(value, string.Empty);
        value = DangerousUrlRegex().Replace(value, "$1=\"#\"");
        value = TagRegex().Replace(value, match =>
        {
            var name = match.Groups[1].Value;
            return AllowedTags.Contains(name) ? match.Value : string.Empty;
        });
        return value;
    }

    [GeneratedRegex(@"<\s*(script|iframe|object|embed|style|form|input|button|svg)[^>]*>.*?<\s*/\s*\1\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex DangerousBlockRegex();

    [GeneratedRegex("\\s+on[a-z]+\\s*=\\s*(\"[^\"]*\"|'[^']*'|[^\\s>]+)", RegexOptions.IgnoreCase)]
    private static partial Regex EventAttributeRegex();

    [GeneratedRegex("\\s+(href|src|action)\\s*=\\s*(\"|')\\s*(javascript:|data:)[^\"']*\\2", RegexOptions.IgnoreCase)]
    private static partial Regex DangerousUrlRegex();

    [GeneratedRegex(@"<\s*/?\s*([a-z][a-z0-9]*)\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex TagRegex();
}

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Soransoft.Infrastructure.Storage;

/// <summary>مسیر پایدار فایل‌های عمومی رسانه که با انتشار نسخه جدید حذف نمی‌شود.</summary>
public static class PersistentPublicMediaStorage
{
    public static string ResolveRoot(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var configured = configuration["Storage:PersistentMediaRoot"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var expanded = Environment.ExpandEnvironmentVariables(configured.Trim());
            return Path.GetFullPath(Path.IsPathRooted(expanded)
                ? expanded
                : Path.Combine(environment.ContentRootPath, expanded));
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
            return Path.Combine(localAppData, "Soransoft", "PublicMedia");

        var workspaceRoot = Directory.GetParent(environment.ContentRootPath)?.FullName ?? environment.ContentRootPath;
        return Path.Combine(workspaceRoot, "SoransoftData", "PublicMedia");
    }

    public static string ResolveUnderRoot(string root, string relativePath)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(fullRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("مسیر فایل رسانه مجاز نیست.");
        return fullPath;
    }

    public static bool IsPrivateLegacyPath(string relativePath)
    {
        var normalized = relativePath.Trim('/').Replace('\\', '/');
        if (normalized.StartsWith("partners/sellable-projects/images/", StringComparison.OrdinalIgnoreCase))
            return false;

        return normalized.StartsWith("partners/contracts/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("partners/agreements/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("partners/wallet-docs/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("partners/contract-stages/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("partners/sellable-projects/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("partners/profile/", StringComparison.OrdinalIgnoreCase);
    }
}

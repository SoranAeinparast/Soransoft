using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Soransoft.Infrastructure.Storage;

/// <summary>Resolves profile files to storage that survives rebuilds and deployments.</summary>
public static class PersistentProfileStorage
{
    public static string ResolveRoot(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var configured = configuration["Storage:PersistentProfileRoot"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var expanded = Environment.ExpandEnvironmentVariables(configured.Trim());
            return Path.GetFullPath(Path.IsPathRooted(expanded)
                ? expanded
                : Path.Combine(environment.ContentRootPath, expanded));
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
            return Path.Combine(localAppData, "Soransoft", "ProfileDocuments");

        var workspaceRoot = Directory.GetParent(environment.ContentRootPath)?.FullName ?? environment.ContentRootPath;
        return Path.Combine(workspaceRoot, "SoransoftData", "ProfileDocuments");
    }

    public static bool IsProfileFolder(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return false;
        var segments = relativePath.Replace('\\', '/').Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 3
            && segments[0].Equals("partners", StringComparison.OrdinalIgnoreCase)
            && segments[2].Equals("profile", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsProfilePath(string? relativePath) => IsProfileFolder(relativePath);
}
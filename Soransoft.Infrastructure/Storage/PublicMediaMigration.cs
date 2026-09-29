using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Soransoft.Infrastructure.Storage;

/// <summary>کپی یک‌باره رسانه‌های عمومی قدیمی به فضای پایدار.</summary>
public static class PublicMediaMigration
{
    public static async Task RunAsync(
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken ct = default)
    {
        var legacyRoot = Path.GetFullPath(Path.Combine(
            environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "uploads"));
        var persistentRoot = Path.GetFullPath(PersistentPublicMediaStorage.ResolveRoot(environment, configuration));
        if (!Directory.Exists(legacyRoot) || string.Equals(legacyRoot, persistentRoot, StringComparison.OrdinalIgnoreCase))
            return;

        foreach (var sourcePath in Directory.EnumerateFiles(legacyRoot, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            var relativePath = Path.GetRelativePath(legacyRoot, sourcePath).Replace('\\', '/');
            if (PersistentPublicMediaStorage.IsPrivateLegacyPath(relativePath)) continue;

            try
            {
                var targetPath = PersistentPublicMediaStorage.ResolveUnderRoot(persistentRoot, relativePath);
                if (File.Exists(targetPath)) continue;

                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                await using var source = File.OpenRead(sourcePath);
                await using var target = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await source.CopyToAsync(target, ct);
                logger.LogInformation("Public media copied to persistent storage: {Path}", relativePath);
            }
            catch (IOException ex)
            {
                logger.LogWarning(ex, "Unable to migrate public media file: {Path}", relativePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogWarning(ex, "Unable to access public media file: {Path}", relativePath);
            }
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Soransoft.Infrastructure.Storage;

namespace Soransoft.Infrastructure.Persistence;

/// <summary>انتقال یک‌باره اسناد قدیمی از wwwroot/uploads به فضای خصوصی برنامه.</summary>
public static class PrivateDocumentMigration
{
    public static async Task RunAsync(
        SoransoftDbContext db,
        string contentRoot,
        string webRoot,
        string persistentProfileRoot,
        ILogger logger,
        CancellationToken ct = default)
    {
        var privateRoot = Path.GetFullPath(Path.Combine(contentRoot, "App_Data", "PrivateDocuments"));
        Directory.CreateDirectory(privateRoot);
        var oldRoot = Path.GetFullPath(Path.Combine(webRoot, "uploads"));
        persistentProfileRoot = Path.GetFullPath(persistentProfileRoot);
        var changed = false;

        foreach (var contract in await db.PartnerContracts.IgnoreQueryFilters().ToListAsync(ct))
            changed |= await MoveAsync(contract.ContractFile, value => contract.ContractFile = value, oldRoot, privateRoot, logger, ct);

        foreach (var agreement in await db.CooperationAgreements.IgnoreQueryFilters().ToListAsync(ct))
            changed |= await MoveAsync(agreement.ContractFile, value => agreement.ContractFile = value, oldRoot, privateRoot, logger, ct);

        foreach (var stage in await db.ContractPaymentStages.IgnoreQueryFilters().ToListAsync(ct))
        {
            changed |= await MoveAsync(stage.ReceiptFile, value => stage.ReceiptFile = value, oldRoot, privateRoot, logger, ct);
            changed |= await MoveAsync(stage.DocumentFile, value => stage.DocumentFile = value, oldRoot, privateRoot, logger, ct);
        }

        foreach (var transaction in await db.WalletTransactions.IgnoreQueryFilters().ToListAsync(ct))
            changed |= await MoveAsync(transaction.DocumentFile, value => transaction.DocumentFile = value, oldRoot, privateRoot, logger, ct);

        foreach (var partner in await db.Partners.IgnoreQueryFilters().ToListAsync(ct))
        {
            changed |= await MoveAsync(partner.PersonalPhotoPath, value => partner.PersonalPhotoPath = value, oldRoot, privateRoot, logger, ct);
            changed |= await MoveAsync(partner.NationalCardFrontPath, value => partner.NationalCardFrontPath = value, oldRoot, privateRoot, logger, ct);
            changed |= await MoveAsync(partner.NationalCardBackPath, value => partner.NationalCardBackPath = value, oldRoot, privateRoot, logger, ct);
            changed |= await MoveAsync(partner.BirthCertificatePath, value => partner.BirthCertificatePath = value, oldRoot, privateRoot, logger, ct);
            changed |= await MoveAsync(partner.IdentityDocumentPath, value => partner.IdentityDocumentPath = value, oldRoot, privateRoot, logger, ct);
        }

        foreach (var document in await db.PartnerDocuments.IgnoreQueryFilters().ToListAsync(ct))
            changed |= await MoveAsync(document.StoredPath, value => document.StoredPath = value, oldRoot, privateRoot, logger, ct);

        foreach (var partner in await db.Partners.IgnoreQueryFilters().ToListAsync(ct))
        {
            await MoveProfileFileAsync(partner.PersonalPhotoPath, privateRoot, persistentProfileRoot, logger, ct);
            await MoveProfileFileAsync(partner.NationalCardFrontPath, privateRoot, persistentProfileRoot, logger, ct);
            await MoveProfileFileAsync(partner.NationalCardBackPath, privateRoot, persistentProfileRoot, logger, ct);
            await MoveProfileFileAsync(partner.BirthCertificatePath, privateRoot, persistentProfileRoot, logger, ct);
            await MoveProfileFileAsync(partner.IdentityDocumentPath, privateRoot, persistentProfileRoot, logger, ct);
        }

        foreach (var document in await db.PartnerDocuments.IgnoreQueryFilters().ToListAsync(ct))
            await MoveProfileFileAsync(document.StoredPath, privateRoot, persistentProfileRoot, logger, ct);

        if (changed) await db.SaveChangesAsync(ct);
    }

    private static async Task<bool> MoveAsync(
        string? storedPath,
        Action<string> update,
        string oldRoot,
        string privateRoot,
        ILogger logger,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(storedPath) ||
            !storedPath.StartsWith("/uploads/partners/", StringComparison.OrdinalIgnoreCase))
            return false;

        var relativePath = storedPath.TrimStart('/')["uploads/".Length..].Replace('\\', '/');
        if (relativePath.Split('/').Any(segment => segment is "." or ".." || segment.Contains(':')))
        {
            logger.LogWarning("Legacy private document path rejected: {Path}", storedPath);
            return false;
        }

        var oldPath = ResolveUnderRoot(oldRoot, relativePath);
        var newPath = ResolveUnderRoot(privateRoot, relativePath);
        if (!File.Exists(oldPath) && !File.Exists(newPath)) return false;

        Directory.CreateDirectory(Path.GetDirectoryName(newPath)!);
        if (File.Exists(oldPath) && !File.Exists(newPath))
            File.Move(oldPath, newPath);

        update($"/documents/download?path={Uri.EscapeDataString(relativePath)}");
        await Task.CompletedTask;
        ct.ThrowIfCancellationRequested();
        return true;
    }

    private static async Task MoveProfileFileAsync(
        string? storedPath,
        string legacyRoot,
        string persistentRoot,
        ILogger logger,
        CancellationToken ct)
    {
        const string marker = "?path=";
        if (string.IsNullOrWhiteSpace(storedPath) ||
            !storedPath.Contains(marker, StringComparison.OrdinalIgnoreCase))
            return;

        var markerIndex = storedPath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        var relativePath = Uri.UnescapeDataString(storedPath[(markerIndex + marker.Length)..].Split('&', 2)[0]).Replace('\\', '/').Trim('/');
        if (!PersistentProfileStorage.IsProfilePath(relativePath) ||
            relativePath.Split('/').Any(segment => segment is "." or ".." || segment.Contains(':') || segment.Contains('\0')))
            return;

        var oldPath = ResolveUnderRoot(legacyRoot, relativePath);
        var newPath = ResolveUnderRoot(persistentRoot, relativePath);
        if (!File.Exists(oldPath) || File.Exists(newPath)) return;

        Directory.CreateDirectory(Path.GetDirectoryName(newPath)!);
        File.Move(oldPath, newPath);
        logger.LogInformation("Profile file moved to persistent storage: {Path}", relativePath);
        await Task.CompletedTask;
        ct.ThrowIfCancellationRequested();
    }

    private static string ResolveUnderRoot(string root, string relativePath)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(fullRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("مسیر سند خارج از محدوده مجاز است.");
        return fullPath;
    }
}
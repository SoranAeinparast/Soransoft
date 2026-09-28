using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using Soransoft.Domain.Entities;
using Soransoft.Infrastructure.Persistence;
using Soransoft.Web.Models;

namespace Soransoft.Web.Areas.Admin.Controllers;

/// <summary>مدیریت پروژه‌های آماده ارائه به مشتری و مستندات فروش</summary>
[Area("Admin")]
[Route("Admin/PortalProjects/{action=Index}/{id?}")]
[Authorize(Policy = "AdminOnly")]
public sealed class PortalProjectsController : Controller
{
    private readonly SoransoftDbContext _db;
    private readonly IFileStorage _storage;
    private readonly ILogger<PortalProjectsController> _logger;

    public PortalProjectsController(SoransoftDbContext db, IFileStorage storage, ILogger<PortalProjectsController> logger)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var projects = await _db.SellableProjects
            .Include(p => p.Documents.Where(d => !d.IsDeleted).OrderBy(d => d.DisplayOrder).ThenBy(d => d.Title))
            .Include(p => p.Comments.Where(c => !c.IsDeleted).OrderByDescending(c => c.CreatedAt))
            .Include(p => p.PartnerAccess).ThenInclude(a => a.Partner)
            .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Title)
            .ToListAsync(ct);

        return View(projects);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveProject(int id, string title, string? description, int displayOrder, bool isActive, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["Error"] = "عنوان پروژه الزامی است.";
            return RedirectToAction(nameof(Index));
        }

        SellableProject? project = id == 0
            ? new SellableProject()
            : await _db.SellableProjects.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null)
        {
            TempData["Error"] = "پروژه یافت نشد.";
            return RedirectToAction(nameof(Index));
        }

        project.Title = title.Trim();
        project.Description = SafeHtml.Sanitize(description ?? string.Empty);
        project.DisplayOrder = Math.Max(0, displayOrder);
        project.IsActive = isActive;
        if (id == 0) _db.SellableProjects.Add(project);
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = id == 0 ? "پروژه قابل ارائه ایجاد شد." : "پروژه قابل ارائه ویرایش شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProject(int id, CancellationToken ct)
    {
        var project = await _db.SellableProjects
            .Include(p => p.Documents)
            .Include(p => p.PartnerAccess)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null)
        {
            TempData["Error"] = "پروژه یافت نشد.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var document in project.Documents)
        {
            document.IsDeleted = true;
            document.DeletedAt = DateTime.Now;
            await _storage.DeleteAsync(document.StoredPath, ct);
        }
        project.IsDeleted = true;
        project.DeletedAt = DateTime.Now;
        _db.SellableProjectPartners.RemoveRange(project.PartnerAccess);
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = "پروژه و دسترسی‌های آن حذف شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDocument(
        int projectId,
        int id,
        string title,
        string category,
        SellableDocumentKind kind,
        string? externalUrl,
        int displayOrder,
        bool isActive,
        IFormFile? file,
        CancellationToken ct)
    {
        var project = await _db.SellableProjects.FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (project is null)
        {
            TempData["Error"] = "پروژه یافت نشد.";
            return RedirectToAction(nameof(Index));
        }
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(category))
        {
            TempData["Error"] = "عنوان و دسته مستند الزامی است.";
            return RedirectToAction(nameof(Index));
        }
        if (!Enum.IsDefined(kind))
        {
            TempData["Error"] = "نوع مستند معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var document = id == 0
            ? new SellableProjectDocument { SellableProjectId = projectId }
            : await _db.SellableProjectDocuments.FirstOrDefaultAsync(d => d.Id == id && d.SellableProjectId == projectId, ct);
        if (document is null)
        {
            TempData["Error"] = "مستند یافت نشد.";
            return RedirectToAction(nameof(Index));
        }

        var previousPath = document.StoredPath;
        string? newFilePath = null;
        try
        {
            document.Title = title.Trim();
            document.Category = category.Trim();
            document.Kind = kind;
            document.DisplayOrder = Math.Max(0, displayOrder);
            document.IsActive = isActive;

            if (kind == SellableDocumentKind.Link)
            {
                if (!IsSafeExternalUrl(externalUrl))
                {
                    TempData["Error"] = "لینک مستند باید با http یا https شروع شود.";
                    return RedirectToAction(nameof(Index));
                }
                document.ExternalUrl = externalUrl!.Trim();
                document.StoredPath = null;
                document.OriginalFileName = null;
                document.ContentType = null;
                document.SizeBytes = null;
            }
            else
            {
                if (file is not null && file.Length > 0)
                {
                    document.StoredPath = await _storage.SaveDocumentAsync(file, "partners/sellable-projects", ct);
                    newFilePath = document.StoredPath;
                    document.OriginalFileName = Path.GetFileName(file.FileName);
                    document.ContentType = file.ContentType;
                    document.SizeBytes = file.Length;
                }
                if (string.IsNullOrWhiteSpace(document.StoredPath))
                {
                    TempData["Error"] = "برای مستند آپلودی یک فایل انتخاب کنید.";
                    return RedirectToAction(nameof(Index));
                }
                document.ExternalUrl = null;
            }

            if (id == 0) _db.SellableProjectDocuments.Add(document);
            await _db.SaveChangesAsync(ct);
        }
        catch (ArgumentException e)
        {
            TempData["Error"] = e.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException e)
        {
            TempData["Error"] = e.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (IOException e)
        {
            _logger.LogError(e, "خطا در ذخیره فایل مستند پروژه {ProjectId}", projectId);
            TempData["Error"] = "ذخیره فایل انجام نشد؛ دسترسی پوشه ذخیره‌سازی را بررسی کنید.";
            return RedirectToAction(nameof(Index));
        }
        catch (UnauthorizedAccessException e)
        {
            _logger.LogError(e, "عدم دسترسی به مسیر فایل مستند پروژه {ProjectId}", projectId);
            TempData["Error"] = "دسترسی به پوشه ذخیره‌سازی فایل مجاز نیست.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException e)
        {
            _logger.LogError(e, "خطای دیتابیس هنگام ذخیره مستند پروژه {ProjectId}", projectId);
            if (!string.IsNullOrWhiteSpace(newFilePath))
                await _storage.DeleteAsync(newFilePath, CancellationToken.None);
            TempData["Error"] = "اطلاعات مستند در دیتابیس ذخیره نشد.";
            return RedirectToAction(nameof(Index));
        }

        if (!string.IsNullOrWhiteSpace(previousPath) && !string.Equals(previousPath, document.StoredPath, StringComparison.Ordinal))
            await _storage.DeleteAsync(previousPath, ct);

        TempData["Success"] = id == 0 ? "مستند پروژه اضافه شد." : "مستند پروژه ویرایش شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDocument(int id, CancellationToken ct)
    {
        var document = await _db.SellableProjectDocuments.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (document is null)
        {
            TempData["Error"] = "مستند یافت نشد.";
            return RedirectToAction(nameof(Index));
        }

        document.IsDeleted = true;
        document.DeletedAt = DateTime.Now;
        await _db.SaveChangesAsync(ct);
        await _storage.DeleteAsync(document.StoredPath, ct);
        TempData["Success"] = "مستند حذف شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveComment(int projectId, int id, string body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            TempData["Error"] = "متن دستورالعمل یا کامنت الزامی است.";
            return RedirectToAction(nameof(Index));
        }

        var projectExists = await _db.SellableProjects.AnyAsync(p => p.Id == projectId, ct);
        if (!projectExists)
        {
            TempData["Error"] = "پروژه یافت نشد.";
            return RedirectToAction(nameof(Index));
        }

        SellableProjectComment? comment = id == 0
            ? new SellableProjectComment { SellableProjectId = projectId }
            : await _db.SellableProjectComments.FirstOrDefaultAsync(c => c.Id == id && c.SellableProjectId == projectId, ct);
        if (comment is null)
        {
            TempData["Error"] = "کامنت یافت نشد.";
            return RedirectToAction(nameof(Index));
        }

        comment.Body = body.Trim();
        if (id == 0) _db.SellableProjectComments.Add(comment);
        await _db.SaveChangesAsync(ct);
        TempData["Success"] = id == 0 ? "دستورالعمل ثبت شد." : "دستورالعمل ویرایش شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int id, CancellationToken ct)
    {
        var comment = await _db.SellableProjectComments.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (comment is null)
        {
            TempData["Error"] = "کامنت یافت نشد.";
            return RedirectToAction(nameof(Index));
        }

        comment.IsDeleted = true;
        comment.DeletedAt = DateTime.Now;
        await _db.SaveChangesAsync(ct);
        TempData["Success"] = "دستورالعمل حذف شد.";
        return RedirectToAction(nameof(Index));
    }

    private static bool IsSafeExternalUrl(string? value) =>
        Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

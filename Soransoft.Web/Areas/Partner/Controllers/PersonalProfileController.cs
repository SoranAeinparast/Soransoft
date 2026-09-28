using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using Soransoft.Domain.Entities;
using Soransoft.Infrastructure.Persistence;
using System.ComponentModel.DataAnnotations;

namespace Soransoft.Web.Areas.Partner.Controllers
{
    public sealed class PersonalProfileViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "نام و نام خانوادگی الزامی است")]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "شماره موبایل الزامی است")]
        public string Mobile { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "ایمیل معتبر نیست")]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? NationalId { get; set; }
        public string? FatherName { get; set; }
        public string? BirthCertificateNumber { get; set; }
        public string? BirthPlace { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? Landline { get; set; }
        public string? PostalCode { get; set; }
        public string? Address { get; set; }

        public string? PersonalPhotoPath { get; set; }
        public string? NationalCardFrontPath { get; set; }
        public string? NationalCardBackPath { get; set; }
        public string? BirthCertificatePath { get; set; }
        public string? IdentityDocumentPath { get; set; }

        public IFormFile? PersonalPhoto { get; set; }
        public IFormFile? NationalCardFront { get; set; }
        public IFormFile? NationalCardBack { get; set; }
        public IFormFile? BirthCertificate { get; set; }
        public IFormFile? IdentityDocument { get; set; }
        public IFormFile? OtherDocument { get; set; }
        public string? OtherDocumentTitle { get; set; }

        public List<PartnerDocument> Documents { get; set; } = new();
    }

    public sealed class PersonalProfileController : PartnerBaseController
    {
        private readonly SoransoftDbContext _db;
        private readonly IFileStorage _storage;

        public PersonalProfileController(SoransoftDbContext db, IFileStorage storage)
        {
            _db = db;
            _storage = storage;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var partner = await _db.Partners.AsNoTracking().FirstOrDefaultAsync(p => p.Id == PartnerId, ct);
            if (partner is null) return Forbidden();

            return View(ToViewModel(partner, await DocumentsAsync(ct)));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(PersonalProfileViewModel model, CancellationToken ct)
        {
            var partner = await _db.Partners.FirstOrDefaultAsync(p => p.Id == PartnerId, ct);
            if (partner is null) return Forbidden();

            model.Id = partner.Id;
            model.Documents = await DocumentsAsync(ct);
            if (!ModelState.IsValid) return View("Index", model);

            partner.FullName = model.FullName.Trim();
            partner.Mobile = Clean(model.Mobile) ?? string.Empty;
            partner.Email = Clean(model.Email);
            partner.NationalId = Clean(model.NationalId);
            partner.FatherName = Clean(model.FatherName);
            partner.BirthCertificateNumber = Clean(model.BirthCertificateNumber);
            partner.BirthPlace = Clean(model.BirthPlace);
            partner.BirthDate = model.BirthDate;
            partner.Landline = Clean(model.Landline);
            partner.PostalCode = Clean(model.PostalCode);
            partner.Address = Clean(model.Address);

            await ReplaceFileAsync(model.PersonalPhoto, value => partner.PersonalPhotoPath = value, () => partner.PersonalPhotoPath, true, ct);
            await ReplaceFileAsync(model.NationalCardFront, value => partner.NationalCardFrontPath = value, () => partner.NationalCardFrontPath, false, ct);
            await ReplaceFileAsync(model.NationalCardBack, value => partner.NationalCardBackPath = value, () => partner.NationalCardBackPath, false, ct);
            await ReplaceFileAsync(model.BirthCertificate, value => partner.BirthCertificatePath = value, () => partner.BirthCertificatePath, false, ct);
            await ReplaceFileAsync(model.IdentityDocument, value => partner.IdentityDocumentPath = value, () => partner.IdentityDocumentPath, false, ct);

            if (model.OtherDocument is { Length: > 0 })
            {
                if (string.IsNullOrWhiteSpace(model.OtherDocumentTitle))
                    ModelState.AddModelError(nameof(model.OtherDocumentTitle), "توضیح مدرک سایر مدارک الزامی است.");
                else
                {
                    var path = await SaveFileAsync(model.OtherDocument, ct);
                    if (path is not null)
                    {
                        _db.PartnerDocuments.Add(new PartnerDocument
                        {
                            PartnerId = partner.Id,
                            Title = model.OtherDocumentTitle.Trim(),
                            StoredPath = path,
                        });
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                model.Documents = await DocumentsAsync(ct);
                return View("Index", model);
            }

            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "پروفایل شخصی و مدارک شما با موفقیت ذخیره شد.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int id, CancellationToken ct)
        {
            var document = await _db.PartnerDocuments.FirstOrDefaultAsync(d => d.Id == id && d.PartnerId == PartnerId, ct);
            if (document is null) return NotFound();

            await _storage.DeleteAsync(document.StoredPath, ct);
            document.IsDeleted = true;
            document.DeletedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "مدرک حذف شد.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<List<PartnerDocument>> DocumentsAsync(CancellationToken ct) => await _db.PartnerDocuments
            .AsNoTracking()
            .Where(d => d.PartnerId == PartnerId && !d.IsDeleted)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

        private async Task ReplaceFileAsync(
            IFormFile? file,
            Action<string> assign,
            Func<string?> current,
            bool imageOnly,
            CancellationToken ct)
        {
            if (file is null || file.Length == 0) return;
            if (imageOnly && !IsImage(file.FileName))
            {
                ModelState.AddModelError(string.Empty, "تصویر پرسنلی باید JPG، PNG یا WEBP باشد.");
                return;
            }

            var previous = current();
            var path = await SaveFileAsync(file, ct);
            if (path is null) return;
            assign(path);
            if (!string.IsNullOrWhiteSpace(previous)) await _storage.DeleteAsync(previous, ct);
        }

        private async Task<string?> SaveFileAsync(IFormFile file, CancellationToken ct)
        {
            try
            {
                return await _storage.SaveDocumentAsync(file, $"partners/{PartnerId}/profile", ct);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return null;
            }
        }

        private static bool IsImage(string fileName) => new[] { ".jpg", ".jpeg", ".png", ".webp" }
            .Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase);

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static PersonalProfileViewModel ToViewModel(Partner partner, List<PartnerDocument> documents) => new()
        {
            Id = partner.Id,
            FullName = partner.FullName,
            Mobile = partner.Mobile ?? string.Empty,
            Email = partner.Email,
            NationalId = partner.NationalId,
            FatherName = partner.FatherName,
            BirthCertificateNumber = partner.BirthCertificateNumber,
            BirthPlace = partner.BirthPlace,
            BirthDate = partner.BirthDate,
            Landline = partner.Landline,
            PostalCode = partner.PostalCode,
            Address = partner.Address,
            PersonalPhotoPath = partner.PersonalPhotoPath,
            NationalCardFrontPath = partner.NationalCardFrontPath,
            NationalCardBackPath = partner.NationalCardBackPath,
            BirthCertificatePath = partner.BirthCertificatePath,
            IdentityDocumentPath = partner.IdentityDocumentPath,
            Documents = documents,
        };
    }
}

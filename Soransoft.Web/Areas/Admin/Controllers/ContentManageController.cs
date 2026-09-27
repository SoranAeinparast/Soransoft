using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using Soransoft.Application.Services;
using Soransoft.Infrastructure.Persistence;

namespace Soransoft.Web.Areas.Admin.Controllers
{
    /// <summary>مدیریت سرویس‌ها</summary>
    public class ServicesController : AdminBaseController
    {
        private readonly SoransoftDbContext _db;
        public ServicesController(SoransoftDbContext db) => _db = db;

        public async Task<IActionResult> Index(CancellationToken ct) =>
            View(await _db.Services.OrderBy(s => s.DisplayOrder).ToListAsync(ct));

        public IActionResult Create() => View(new Soransoft.Domain.Entities.Service());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Soransoft.Domain.Entities.Service model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ذخیره نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return View(model);
            }
            model.CreatedAt = DateTime.Now;
            _db.Services.Add(model);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "سرویس با موفقیت ایجاد شد";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            var item = await _db.Services.FindAsync(new object[] { id }, ct);
            if (item is null) return NotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Soransoft.Domain.Entities.Service model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ذخیره نشد: " + string.Join(" | ", ModelState.Keys.Where(k => ModelState[k]!.Errors.Count > 0).Select(k => $"{k}: {string.Join(", ", ModelState[k]!.Errors.Select(e => e.ErrorMessage))}"));
                return View(model);
            }

            // فقط فیلدهای فرم کپی می‌شود؛ Image و تاریخ‌های سیستمی حفظ می‌شوند
            var item = await _db.Services.FirstOrDefaultAsync(s => s.Id == model.Id, ct);
            if (item is null) return NotFound();

            item.Kind = model.Kind;
            item.Title = model.Title;
            item.ShortDescription = model.ShortDescription;
            item.FullDescription = model.FullDescription;
            item.Slug = model.Slug;
            item.Icon = model.Icon;
            item.Slogan = model.Slogan;
            item.ComingSoon = model.ComingSoon;
            item.IsActive = model.IsActive;
            item.DisplayOrder = model.DisplayOrder;

            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "سرویس با موفقیت ویرایش شد";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var item = await _db.Services.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.IsDeleted = true;
                item.DeletedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = "سرویس حذف شد";
            }
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>مدیریت نمونه‌کارها</summary>
    public class PortfoliosController : AdminBaseController
    {
        private readonly SoransoftDbContext _db;
        private readonly IFileStorage _storage;
        public PortfoliosController(SoransoftDbContext db, IFileStorage storage) { _db = db; _storage = storage; }

        public async Task<IActionResult> Index(CancellationToken ct) =>
            View(await _db.Portfolios.OrderBy(p => p.DisplayOrder).ToListAsync(ct));

        public async Task<IActionResult> Create(CancellationToken ct)
        {
            await FillServicesAsync(ct);
            return View(new Soransoft.Domain.Entities.Portfolio());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Soransoft.Domain.Entities.Portfolio model, IFormFile? imageFile, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ذخیره نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                await FillServicesAsync(ct);
                return View(model);
            }
            if (imageFile is not null && imageFile.Length > 0)
                model.Image = await _storage.SaveImageAsync(imageFile, "portfolios", ct);
            _db.Portfolios.Add(model);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "نمونه‌کار با موفقیت ایجاد شد";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            var item = await _db.Portfolios.FindAsync(new object[] { id }, ct);
            if (item is null) return NotFound();
            await FillServicesAsync(ct);
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Soransoft.Domain.Entities.Portfolio model, IFormFile? imageFile, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ذخیره نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                await FillServicesAsync(ct);
                return View(model);
            }

            var item = await _db.Portfolios.FirstOrDefaultAsync(p => p.Id == model.Id, ct);
            if (item is null) return NotFound();

            item.Title = model.Title;
            item.Description = model.Description;
            item.Url = model.Url;
            item.ServiceId = model.ServiceId;
            item.ComingSoon = model.ComingSoon;
            item.IsActive = model.IsActive;
            item.DisplayOrder = model.DisplayOrder;

            if (imageFile is not null && imageFile.Length > 0)
            {
                await _storage.DeleteAsync(item.Image, ct);
                item.Image = await _storage.SaveImageAsync(imageFile, "portfolios", ct);
            }
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "نمونه‌کار با موفقیت ویرایش شد";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var item = await _db.Portfolios.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                await _storage.DeleteAsync(item.Image, ct);
                item.IsDeleted = true;
                item.DeletedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = "نمونه‌کار حذف شد";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task FillServicesAsync(CancellationToken ct) =>
            ViewBag.Services = await _db.Services.Where(s => !s.IsDeleted).ToListAsync(ct);
    }

    /// <summary>مدیریت مقالات</summary>
    public class ArticlesController : AdminBaseController
    {
        private readonly SoransoftDbContext _db;
        private readonly IFileStorage _storage;
        public ArticlesController(SoransoftDbContext db, IFileStorage storage) { _db = db; _storage = storage; }

        public async Task<IActionResult> Index(CancellationToken ct) =>
            View(await _db.Articles.Include(a => a.ArticleCategory).OrderByDescending(a => a.CreatedAt).ToListAsync(ct));

        public async Task<IActionResult> Create(CancellationToken ct)
        {
            await FillCategoriesAsync(ct);
            return View(new Soransoft.Domain.Entities.Article { PublishedAt = DateTime.Now });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Soransoft.Domain.Entities.Article model, IFormFile? imageFile, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ذخیره نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                await FillCategoriesAsync(ct);
                return View(model);
            }

            // اسلاگ خودکار از عنوان؛ تضمین یکتایی
            if (string.IsNullOrWhiteSpace(model.Slug))
                model.Slug = SlugGenerator.Generate(model.Title);
            model.Slug = await EnsureUniqueSlugAsync(model.Slug, null, ct);

            if (imageFile is not null && imageFile.Length > 0)
                model.Image = await _storage.SaveImageAsync(imageFile, "articles", ct);
            _db.Articles.Add(model);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "مقاله با موفقیت ایجاد شد";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            var item = await _db.Articles.FindAsync(new object[] { id }, ct);
            if (item is null) return NotFound();
            await FillCategoriesAsync(ct);
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Soransoft.Domain.Entities.Article model, IFormFile? imageFile, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ذخیره نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                await FillCategoriesAsync(ct);
                return View(model);
            }

            // الگوی صحیح آپدیت: موجودیت از دیتابیس بار و فقط فیلدهای فرم روی آن کپی می‌شود
            // تا فیلدهای خارج از فرم (تصویر، شمارنده بازدید، تاریخ ایجاد) حفظ شوند
            var item = await _db.Articles.FirstOrDefaultAsync(a => a.Id == model.Id, ct);
            if (item is null) return NotFound();

            item.Title = model.Title;
            item.Summary = model.Summary;
            item.Body = model.Body;
            item.AuthorName = model.AuthorName;
            item.ArticleCategoryId = model.ArticleCategoryId;
            item.PublishedAt = model.PublishedAt;
            item.Status = model.Status;
            item.IsFeatured = model.IsFeatured;

            // اسلاگ خودکار از عنوان؛ تضمین یکتایی
            var slug = string.IsNullOrWhiteSpace(model.Slug) ? SlugGenerator.Generate(model.Title) : model.Slug;
            item.Slug = await EnsureUniqueSlugAsync(slug, model.Id, ct);

            if (imageFile is not null && imageFile.Length > 0)
            {
                await _storage.DeleteAsync(item.Image, ct);
                item.Image = await _storage.SaveImageAsync(imageFile, "articles", ct);
            }
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "مقاله با موفقیت ویرایش شد";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var item = await _db.Articles.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                await _storage.DeleteAsync(item.Image, ct);
                item.IsDeleted = true;
                item.DeletedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = "مقاله حذف شد";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task FillCategoriesAsync(CancellationToken ct) =>
            ViewBag.Categories = await _db.ArticleCategories.Where(c => !c.IsDeleted).ToListAsync(ct);

        /// <summary>تضمین یکتایی اسلاگ مقاله — در صورت تکرار، پسوند عددی اضافه می‌شود</summary>
        private async Task<string> EnsureUniqueSlugAsync(string slug, int? excludeId, CancellationToken ct)
        {
            var baseSlug = slug;
            var n = 2;
            while (await _db.Articles.AnyAsync(a => a.Slug == slug && (excludeId == null || a.Id != excludeId), ct))
                slug = $"{baseSlug}-{n++}";
            return slug;
        }
    }

    /// <summary>مدیریت دسته‌بندی مقالات</summary>
    public class ArticleCategoriesController : AdminBaseController
    {
        private readonly SoransoftDbContext _db;
        public ArticleCategoriesController(SoransoftDbContext db) => _db = db;

        public async Task<IActionResult> Index(CancellationToken ct) =>
            View(await _db.ArticleCategories.OrderBy(c => c.DisplayOrder).ToListAsync(ct));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Soransoft.Domain.Entities.ArticleCategory model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ذخیره نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Index));
            }
            // اسلاگ خودکار از عنوان در صورت خالی بودن
            if (string.IsNullOrWhiteSpace(model.Slug))
                model.Slug = SlugGenerator.Generate(model.Title);
            model.CreatedAt = DateTime.Now;
            _db.ArticleCategories.Add(model);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = $"دسته‌بندی «{model.Title}» ایجاد شد";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Soransoft.Domain.Entities.ArticleCategory model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ذخیره نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Index));
            }
            // الگوی صحیح: بارگذاری از دیتابیس تا CreatedAt و IsDeleted حفظ شود
            var item = await _db.ArticleCategories.FirstOrDefaultAsync(c => c.Id == model.Id, ct);
            if (item is null) return NotFound();
            item.Title = model.Title;
            item.Slug = string.IsNullOrWhiteSpace(model.Slug) ? SlugGenerator.Generate(model.Title) : model.Slug;
            item.DisplayOrder = model.DisplayOrder;
            item.IsActive = model.IsActive;
            item.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "دسته‌بندی ویرایش شد";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var item = await _db.ArticleCategories.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.IsDeleted = true;
                item.DeletedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = "دسته‌بندی حذف شد";
            }
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>مدیریت تعرفه‌ها (بخش‌ها + پکیج‌ها)</summary>
    public class TariffsController : AdminBaseController
    {
        private readonly SoransoftDbContext _db;
        public TariffsController(SoransoftDbContext db) => _db = db;

        public async Task<IActionResult> Index(CancellationToken ct) =>
            View(await _db.TariffSections
                .Include(t => t.Packages.Where(p => !p.IsDeleted))
                .OrderBy(t => t.DisplayOrder).ToListAsync(ct));

        // ---------------- بخش‌های تعرفه ----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSection(Soransoft.Domain.Entities.TariffSection model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ایجاد بخش نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Index));
            }
            _db.TariffSections.Add(model);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = $"بخش تعرفه «{model.Title}» ایجاد شد";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSection(Soransoft.Domain.Entities.TariffSection model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ویرایش بخش نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Index));
            }
            // الگوی صحیح: بارگذاری از دیتابیس و کپی فیلدهای فرم تا CreatedAt و پکیج‌ها حفظ شوند
            var item = await _db.TariffSections.FirstOrDefaultAsync(t => t.Id == model.Id, ct);
            if (item is null) return NotFound();
            item.Kind = model.Kind;
            item.Title = model.Title;
            item.DisplayOrder = model.DisplayOrder;
            item.IsActive = model.IsActive;
            item.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "بخش تعرفه ویرایش شد";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>فعال/غیرفعال کردن سریع بخش (بدون تغییر بقیه فیلدها)</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSection(int id, CancellationToken ct)
        {
            var item = await _db.TariffSections.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.IsActive = !item.IsActive;
                item.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = $"بخش «{item.Title}» {(item.IsActive ? "فعال" : "غیرفعال")} شد";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSection(int id, CancellationToken ct)
        {
            var item = await _db.TariffSections.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.IsDeleted = true;
                item.DeletedAt = DateTime.Now;
                item.IsActive = false;
                // پکیج‌های زیرمجموعه هم غیرفعال می‌شوند تا از سایت حذف شوند
                var packages = await _db.TariffPackages.Where(p => p.TariffSectionId == id && !p.IsDeleted).ToListAsync(ct);
                foreach (var p in packages) { p.IsActive = false; p.UpdatedAt = DateTime.Now; }
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = $"بخش تعرفه «{item.Title}» حذف شد";
            }
            return RedirectToAction(nameof(Index));
        }

        // ---------------- پکیج‌های تعرفه ----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePackage(Soransoft.Domain.Entities.TariffPackage model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ایجاد پکیج نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Index));
            }
            _db.TariffPackages.Add(model);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = $"پکیج «{model.Title}» ایجاد شد";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPackage(Soransoft.Domain.Entities.TariffPackage model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ویرایش پکیج نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Index));
            }
            // الگوی صحیح: بارگذاری از دیتابیس و کپی فیلدهای فرم تا CreatedAt حفظ شود
            var item = await _db.TariffPackages.FirstOrDefaultAsync(p => p.Id == model.Id, ct);
            if (item is null) return NotFound();
            item.Title = model.Title;
            item.Price = model.Price;
            item.PriceNote = model.PriceNote;
            item.PriceSuffix = model.PriceSuffix;
            item.Duration = model.Duration;
            item.IsInstallmentAvailable = model.IsInstallmentAvailable;
            item.HasFreeSupport = model.HasFreeSupport;
            item.DisplayOrder = model.DisplayOrder;
            item.IsActive = model.IsActive;
            item.IsPopular = model.IsPopular;
            item.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "پکیج ویرایش شد";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>فعال/غیرفعال کردن سریع پکیج</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePackage(int id, CancellationToken ct)
        {
            var item = await _db.TariffPackages.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.IsActive = !item.IsActive;
                item.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = $"پکیج «{item.Title}» {(item.IsActive ? "فعال" : "غیرفعال")} شد";
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>برجسته‌کردن پکیج (نشان «محبوب‌ترین»)</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePopular(int id, CancellationToken ct)
        {
            var item = await _db.TariffPackages.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.IsPopular = !item.IsPopular;
                item.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = $"پکیج «{item.Title}» {(item.IsPopular ? "به عنوان محبوب‌ترین برجسته" : "از حالت محبوب خارج")} شد";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePackage(int id, CancellationToken ct)
        {
            var item = await _db.TariffPackages.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.IsDeleted = true;
                item.DeletedAt = DateTime.Now;
                item.IsActive = false;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = $"پکیج «{item.Title}» حذف شد";
            }
            return RedirectToAction(nameof(Index));
        }

        // ---------------- ردیف‌های ویژگی پکیج ----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(int packageId, string title, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["Error"] = "عنوان ردیف نمی‌تواند خالی باشد";
                return RedirectToAction(nameof(Index));
            }
            var pkg = await _db.TariffPackages.FirstOrDefaultAsync(p => p.Id == packageId, ct);
            if (pkg is null) return NotFound();
            var order = await _db.TariffItems.Where(i => i.TariffPackageId == packageId).MaxAsync(i => (int?)i.DisplayOrder, ct) ?? 0;
            _db.TariffItems.Add(new Soransoft.Domain.Entities.TariffItem
            {
                TariffPackageId = packageId,
                Title = title.Trim(),
                DisplayOrder = order + 1,
            });
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "ردیف ویژگی اضافه شد";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditItem(int id, string title, CancellationToken ct)
        {
            var item = await _db.TariffItems.FindAsync(new object[] { id }, ct);
            if (item is null) return NotFound();
            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["Error"] = "عنوان ردیف نمی‌تواند خالی باشد";
                return RedirectToAction(nameof(Index));
            }
            item.Title = title.Trim();
            item.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "ردیف ویرایش شد";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteItem(int id, CancellationToken ct)
        {
            var item = await _db.TariffItems.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                _db.TariffItems.Remove(item);
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = "ردیف حذف شد";
            }
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>مدیریت اعضای تیم</summary>
    public class TeamMembersController : AdminBaseController
    {
        private readonly SoransoftDbContext _db;
        private readonly IFileStorage _storage;
        public TeamMembersController(SoransoftDbContext db, IFileStorage storage) { _db = db; _storage = storage; }

        public async Task<IActionResult> Index(CancellationToken ct) =>
            View(await _db.TeamMembers.OrderBy(t => t.DisplayOrder).ToListAsync(ct));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Soransoft.Domain.Entities.TeamMember model, IFormFile? imageFile, CancellationToken ct)
        {
            if (ModelState.IsValid)
            {
                if (imageFile is not null && imageFile.Length > 0)
                    model.Image = await _storage.SaveImageAsync(imageFile, "team", ct);
                _db.TeamMembers.Add(model);
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = "عضو تیم ایجاد شد";
            }
            else
            {
                TempData["Error"] = "ذخیره نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Soransoft.Domain.Entities.TeamMember model, IFormFile? imageFile, CancellationToken ct)
        {
            if (ModelState.IsValid)
            {
                var item = await _db.TeamMembers.FirstOrDefaultAsync(t => t.Id == model.Id, ct);
                if (item is not null)
                {
                    item.FullName = model.FullName;
                    item.Role = model.Role;
                    item.LinkedinUrl = model.LinkedinUrl;
                    item.IsActive = model.IsActive;
                    item.DisplayOrder = model.DisplayOrder;

                    if (imageFile is not null && imageFile.Length > 0)
                    {
                        await _storage.DeleteAsync(item.Image, ct);
                        item.Image = await _storage.SaveImageAsync(imageFile, "team", ct);
                    }
                    await _db.SaveChangesAsync(ct);
                    TempData["Success"] = "عضو تیم ویرایش شد";
                }
            }
            else
            {
                TempData["Error"] = "ذخیره نشد: " + string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>فعال/غیرفعال کردن سریع عضو تیم</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id, CancellationToken ct)
        {
            var item = await _db.TeamMembers.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.IsActive = !item.IsActive;
                item.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = $"عضو «{item.FullName}» {(item.IsActive ? "فعال" : "غیرفعال")} شد";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var item = await _db.TeamMembers.FindAsync(new object[] { id }, ct);
            if (item is not null)
            {
                item.IsDeleted = true;
                item.DeletedAt = DateTime.Now;
                item.IsActive = false;
                await _db.SaveChangesAsync(ct);
                TempData["Success"] = $"عضو تیم «{item.FullName}» حذف شد";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

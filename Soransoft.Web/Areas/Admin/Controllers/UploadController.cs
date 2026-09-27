using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Soransoft.Application.Interfaces;
using Soransoft.Web.Areas.Admin.Controllers;

namespace Soransoft.Web.Areas.Admin.Controllers.Api
{
    /// <summary>آپلود تصویر برای ویرایشگر متن پیشرفته (TinyMCE)</summary>
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class UploadController : AdminBaseController
    {
        private readonly IFileStorage _storage;

        public UploadController(IFileStorage storage) => _storage = storage;

        /// <summary>خروجی مورد انتظار TinyMCE images_upload_handler</summary>
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Image(IFormFile? file, CancellationToken ct)
        {
            if (file is null || file.Length == 0)
                return BadRequest(new { message = "فایلی ارسال نشده است." });

            try
            {
                var path = await _storage.SaveImageAsync(file, "editor", ct);
                // مسیر نسبی سایت — برای پورت‌های محیط توسعه امن است
                return Ok(new { location = path });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

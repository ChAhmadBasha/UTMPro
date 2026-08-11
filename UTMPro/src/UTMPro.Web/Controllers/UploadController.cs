using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UTMPro.Web.Controllers;

[Authorize]
public class UploadController : Controller
{
    private readonly IWebHostEnvironment _env;

    public UploadController(IWebHostEnvironment env) => _env = env;

    // Generic image upload (link preview, logo, avatar)
    [HttpPost("{workspaceSlug}/api/upload-image")]
    public async Task<IActionResult> UploadImage(string workspaceSlug, IFormFile file)
    {
        return await ProcessUpload(file, "images");
    }

    // Settings logo upload
    [HttpPost("/admin/upload/logo")]
    public async Task<IActionResult> UploadLogo(IFormFile file)
    {
        return await ProcessUpload(file, "logos");
    }

    // Settings favicon upload
    [HttpPost("/admin/upload/favicon")]
    public async Task<IActionResult> UploadFavicon(IFormFile file)
    {
        return await ProcessUpload(file, "favicons");
    }

    // General upload for any authenticated user
    [HttpPost("/api/upload")]
    public async Task<IActionResult> Upload(IFormFile file, string? type = "images")
    {
        return await ProcessUpload(file, type ?? "images");
    }

    private async Task<IActionResult> ProcessUpload(IFormFile? file, string subFolder)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        // Max 2MB
        if (file.Length > 2 * 1024 * 1024)
            return BadRequest(new { error = "File too large. Max 2MB." });

        // Allowed types
        var ext = Path.GetExtension(file.FileName).ToLower();
        if (ext is not (".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" or ".svg" or ".ico"))
            return BadRequest(new { error = "Invalid file type. Allowed: png, jpg, jpeg, gif, webp, svg, ico" });

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", subFolder);
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var url = $"/uploads/{subFolder}/{fileName}";
        return Ok(new { url, fileName });
    }
}

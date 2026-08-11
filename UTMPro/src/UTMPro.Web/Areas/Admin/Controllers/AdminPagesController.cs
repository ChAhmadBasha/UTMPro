using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin")]
[Route("admin/pages")]
public class AdminPagesController : Controller
{
    private readonly ISystemSettingsRepository _settings;
    private readonly IWebHostEnvironment _env;

    public AdminPagesController(ISystemSettingsRepository settings, IWebHostEnvironment env)
    {
        _settings = settings; _env = env;
    }

    private long AdminId => long.Parse(User.FindFirst("UserId")!.Value);

    // ── Pages overview ──
    [HttpGet("")]
    public IActionResult Index() => View("~/Areas/Admin/Views/Pages/Index.cshtml");

    // ── About Us ──
    [HttpGet("about")]
    public async Task<IActionResult> About()
    {
        ViewBag.Title = await _settings.GetValueAsync("AboutUsTitle") ?? "About UTMPro";
        ViewBag.Content = await _settings.GetValueAsync("AboutUsContent") ?? "";
        ViewBag.Mission = await _settings.GetValueAsync("AboutUsMission") ?? "";
        ViewBag.Vision = await _settings.GetValueAsync("AboutUsVision") ?? "";
        ViewBag.TeamJson = await _settings.GetValueAsync("AboutUsTeamJson") ?? "[]";
        return View("~/Areas/Admin/Views/Pages/About.cshtml");
    }

    [HttpPost("about")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAbout(string title, string content, string mission, string vision, string teamJson)
    {
        await _settings.SetValueAsync("AboutUsTitle", title ?? "", AdminId);
        await _settings.SetValueAsync("AboutUsContent", content ?? "", AdminId);
        await _settings.SetValueAsync("AboutUsMission", mission ?? "", AdminId);
        await _settings.SetValueAsync("AboutUsVision", vision ?? "", AdminId);
        await _settings.SetValueAsync("AboutUsTeamJson", teamJson ?? "[]", AdminId);
        TempData["Success"] = "About Us page updated";
        return Redirect("/admin/pages/about");
    }

    // ── Contact Us ──
    [HttpGet("contact")]
    public async Task<IActionResult> Contact()
    {
        ViewBag.Title = await _settings.GetValueAsync("ContactUsTitle") ?? "Contact Us";
        ViewBag.Subtitle = await _settings.GetValueAsync("ContactUsSubtitle") ?? "";
        ViewBag.FormEmail = await _settings.GetValueAsync("ContactUsFormEmail") ?? "";
        ViewBag.Address = await _settings.GetValueAsync("ContactUsAddress") ?? "";
        ViewBag.Phone = await _settings.GetValueAsync("ContactUsPhone") ?? "";
        ViewBag.MapEmbed = await _settings.GetValueAsync("ContactUsMapEmbed") ?? "";
        return View("~/Areas/Admin/Views/Pages/Contact.cshtml");
    }

    [HttpPost("contact")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveContact(string title, string subtitle, string formEmail, string address, string phone, string mapEmbed)
    {
        await _settings.SetValueAsync("ContactUsTitle", title ?? "", AdminId);
        await _settings.SetValueAsync("ContactUsSubtitle", subtitle ?? "", AdminId);
        await _settings.SetValueAsync("ContactUsFormEmail", formEmail ?? "", AdminId);
        await _settings.SetValueAsync("ContactUsAddress", address ?? "", AdminId);
        await _settings.SetValueAsync("ContactUsPhone", phone ?? "", AdminId);
        await _settings.SetValueAsync("ContactUsMapEmbed", mapEmbed ?? "", AdminId);
        TempData["Success"] = "Contact Us page updated";
        return Redirect("/admin/pages/contact");
    }

    // ── Privacy Policy ──
    [HttpGet("privacy")]
    public async Task<IActionResult> Privacy()
    {
        ViewBag.Content = await _settings.GetValueAsync("PrivacyPolicyHtml") ?? "";
        return View("~/Areas/Admin/Views/Pages/Privacy.cshtml");
    }

    [HttpPost("privacy")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePrivacy(string content)
    {
        await _settings.SetValueAsync("PrivacyPolicyHtml", content ?? "", AdminId);
        TempData["Success"] = "Privacy Policy updated";
        return Redirect("/admin/pages/privacy");
    }

    // ── Terms of Service ──
    [HttpGet("terms")]
    public async Task<IActionResult> Terms()
    {
        ViewBag.Content = await _settings.GetValueAsync("TermsOfServiceHtml") ?? "";
        return View("~/Areas/Admin/Views/Pages/Terms.cshtml");
    }

    [HttpPost("terms")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTerms(string content)
    {
        await _settings.SetValueAsync("TermsOfServiceHtml", content ?? "", AdminId);
        TempData["Success"] = "Terms of Service updated";
        return Redirect("/admin/pages/terms");
    }

    // ── Site Branding ──
    [HttpGet("branding")]
    public async Task<IActionResult> Branding()
    {
        ViewBag.LogoUrl = await _settings.GetValueAsync("SiteLogoUrl") ?? "";
        ViewBag.FaviconUrl = await _settings.GetValueAsync("SiteFaviconUrl") ?? "";
        ViewBag.ContactEmail = await _settings.GetValueAsync("SiteContactEmail") ?? "";
        ViewBag.ContactPhone = await _settings.GetValueAsync("SiteContactPhone") ?? "";
        ViewBag.FooterText = await _settings.GetValueAsync("SiteFooterText") ?? "";
        ViewBag.Twitter = await _settings.GetValueAsync("SiteSocialTwitter") ?? "";
        ViewBag.LinkedIn = await _settings.GetValueAsync("SiteSocialLinkedIn") ?? "";
        ViewBag.Github = await _settings.GetValueAsync("SiteSocialGithub") ?? "";
        return View("~/Areas/Admin/Views/Pages/Branding.cshtml");
    }

    [HttpPost("branding")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBranding(string logoUrl, string faviconUrl, string contactEmail,
        string contactPhone, string footerText, string twitter, string linkedin, string github)
    {
        await _settings.SetValueAsync("SiteLogoUrl", logoUrl ?? "", AdminId);
        await _settings.SetValueAsync("SiteFaviconUrl", faviconUrl ?? "", AdminId);
        await _settings.SetValueAsync("SiteContactEmail", contactEmail ?? "", AdminId);
        await _settings.SetValueAsync("SiteContactPhone", contactPhone ?? "", AdminId);
        await _settings.SetValueAsync("SiteFooterText", footerText ?? "", AdminId);
        await _settings.SetValueAsync("SiteSocialTwitter", twitter ?? "", AdminId);
        await _settings.SetValueAsync("SiteSocialLinkedIn", linkedin ?? "", AdminId);
        await _settings.SetValueAsync("SiteSocialGithub", github ?? "", AdminId);
        TempData["Success"] = "Site branding updated";
        return Redirect("/admin/pages/branding");
    }

    // ── Email Templates ──
    [HttpGet("emails")]
    public async Task<IActionResult> Emails()
    {
        ViewBag.Verification = await _settings.GetValueAsync("EmailTemplateVerification") ?? "";
        ViewBag.Welcome = await _settings.GetValueAsync("EmailTemplateWelcome") ?? "";
        ViewBag.PasswordReset = await _settings.GetValueAsync("EmailTemplatePasswordReset") ?? "";
        ViewBag.Invitation = await _settings.GetValueAsync("EmailTemplateInvitation") ?? "";
        ViewBag.EnableWelcome = await _settings.GetValueAsync("EnableWelcomeEmail") ?? "true";
        return View("~/Areas/Admin/Views/Pages/Emails.cshtml");
    }

    [HttpPost("emails")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveEmails(string verification, string welcome, string passwordReset, string invitation, string enableWelcome)
    {
        await _settings.SetValueAsync("EmailTemplateVerification", verification ?? "", AdminId);
        await _settings.SetValueAsync("EmailTemplateWelcome", welcome ?? "", AdminId);
        await _settings.SetValueAsync("EmailTemplatePasswordReset", passwordReset ?? "", AdminId);
        await _settings.SetValueAsync("EmailTemplateInvitation", invitation ?? "", AdminId);
        await _settings.SetValueAsync("EnableWelcomeEmail", enableWelcome ?? "true", AdminId);
        TempData["Success"] = "Email templates updated";
        return Redirect("/admin/pages/emails");
    }

    // ── Logo Upload ──
    [HttpPost("branding/upload-logo")]
    public async Task<IActionResult> UploadLogo(IFormFile file)
    {
        if (file == null || file.Length == 0 || file.Length > 2 * 1024 * 1024)
            return BadRequest(new { error = "File required, max 2MB" });

        var ext = Path.GetExtension(file.FileName).ToLower();
        if (ext is not (".png" or ".jpg" or ".jpeg" or ".svg" or ".ico" or ".webp"))
            return BadRequest(new { error = "Invalid file type" });

        var dir = Path.Combine(_env.WebRootPath, "uploads", "logos");
        Directory.CreateDirectory(dir);
        var fileName = $"logo{ext}";
        var filePath = Path.Combine(dir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        var url = $"/uploads/logos/{fileName}";
        await _settings.SetValueAsync("SiteLogoUrl", url, AdminId);
        return Ok(new { url });
    }

    [HttpPost("branding/upload-favicon")]
    public async Task<IActionResult> UploadFavicon(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest(new { error = "File required" });

        var ext = Path.GetExtension(file.FileName).ToLower();
        var dir = Path.Combine(_env.WebRootPath, "uploads", "favicons");
        Directory.CreateDirectory(dir);
        var fileName = $"favicon{ext}";
        var filePath = Path.Combine(dir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        var url = $"/uploads/favicons/{fileName}";
        await _settings.SetValueAsync("SiteFaviconUrl", url, AdminId);
        return Ok(new { url });
    }
}

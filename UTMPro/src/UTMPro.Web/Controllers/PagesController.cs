using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Repositories;
using UTMPro.Web.Services;

namespace UTMPro.Web.Controllers;

public class PagesController : Controller
{
    private readonly ISystemSettingsRepository _settings;
    private readonly IEmailService _emailService;

    public PagesController(ISystemSettingsRepository settings, IEmailService emailService)
    {
        _settings = settings;
        _emailService = emailService;
    }

    private async Task LoadSiteDataAsync()
    {
        ViewBag.SiteLogo = await _settings.GetValueAsync("SiteLogoUrl");
        ViewBag.FooterText = await _settings.GetValueAsync("SiteFooterText");
        ViewBag.ContactEmail = await _settings.GetValueAsync("SiteContactEmail");
        ViewBag.ContactPhone = await _settings.GetValueAsync("SiteContactPhone");
    }

    // ── About Us ────────────────────────────────
    [HttpGet("/about")]
    public async Task<IActionResult> About()
    {
        await LoadSiteDataAsync();
        ViewBag.Title = await _settings.GetValueAsync("AboutUsTitle") ?? "About UTMPro";
        ViewBag.Content = await _settings.GetValueAsync("AboutUsContent") ?? "";
        ViewBag.Mission = await _settings.GetValueAsync("AboutUsMission") ?? "";
        ViewBag.Vision = await _settings.GetValueAsync("AboutUsVision") ?? "";
        ViewBag.TeamJson = await _settings.GetValueAsync("AboutUsTeamJson") ?? "[]";
        return View("~/Views/Pages/About.cshtml");
    }

    // ── Contact Us ──────────────────────────────
    [HttpGet("/contact")]
    public async Task<IActionResult> Contact()
    {
        await LoadSiteDataAsync();
        ViewBag.Title = await _settings.GetValueAsync("ContactUsTitle") ?? "Contact Us";
        ViewBag.Subtitle = await _settings.GetValueAsync("ContactUsSubtitle") ?? "";
        ViewBag.Address = await _settings.GetValueAsync("ContactUsAddress") ?? "";
        ViewBag.Phone = await _settings.GetValueAsync("ContactUsPhone") ?? "";
        ViewBag.MapEmbed = await _settings.GetValueAsync("ContactUsMapEmbed") ?? "";
        return View("~/Views/Pages/Contact.cshtml");
    }

    [HttpPost("/contact")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ContactSubmit(string name, string email, string subject, string message)
    {
        var toEmail = await _settings.GetValueAsync("ContactUsFormEmail") ?? "hello@utmpro.link";

        try
        {
            await _emailService.SendEmailAsync(toEmail, $"[UTMPro Contact] {subject}",
                $"<h3>New Contact Form Submission</h3><p><strong>From:</strong> {name} ({email})</p><p><strong>Subject:</strong> {subject}</p><hr><p>{message}</p>");
            TempData["Success"] = "Your message has been sent! We'll get back to you soon.";
        }
        catch
        {
            TempData["Error"] = "Failed to send message. Please email us directly.";
        }

        return Redirect("/contact");
    }

    // ── Privacy Policy ──────────────────────────
    [HttpGet("/privacy")]
    public async Task<IActionResult> Privacy()
    {
        await LoadSiteDataAsync();
        ViewBag.Content = await _settings.GetValueAsync("PrivacyPolicyHtml") ?? "<p>Privacy policy coming soon.</p>";
        return View("~/Views/Pages/Privacy.cshtml");
    }

    // ── Terms of Service ────────────────────────
    [HttpGet("/terms")]
    public async Task<IActionResult> Terms()
    {
        await LoadSiteDataAsync();
        ViewBag.Content = await _settings.GetValueAsync("TermsOfServiceHtml") ?? "<p>Terms of service coming soon.</p>";
        return View("~/Views/Pages/Terms.cshtml");
    }
}

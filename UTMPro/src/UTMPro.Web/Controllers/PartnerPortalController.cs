using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Repositories;
using UTMPro.Web.Services;

namespace UTMPro.Web.Controllers;

[Route("partners")]
public class PartnerPortalController : Controller
{
    private readonly IPartnerRepository _partnerRepo;
    private readonly IPartnerService _partnerService;

    public PartnerPortalController(IPartnerRepository partnerRepo, IPartnerService partnerService)
    {
        _partnerRepo = partnerRepo; _partnerService = partnerService;
    }

    [HttpGet("{programSlug}")]
    public async Task<IActionResult> Apply(string programSlug)
    {
        var program = await _partnerRepo.GetProgramBySlugAsync(programSlug);
        if (program == null || !program.IsPublic) return NotFound();
        return View("~/Views/PartnerPortal/Apply.cshtml", program);
    }

    [HttpPost("{programSlug}/apply")]
    public async Task<IActionResult> SubmitApplication(string programSlug, string name, string email, string? country)
    {
        var program = await _partnerRepo.GetProgramBySlugAsync(programSlug);
        if (program == null) return NotFound();

        var result = await _partnerService.RegisterPartnerAsync(program.Id, new RegisterPartnerRequest
        {
            Name = name, Email = email, Country = country
        });

        if (!result.Success)
        {
            ViewBag.Error = result.Error;
            return View("~/Views/PartnerPortal/Apply.cshtml", program);
        }

        ViewBag.Partner = result.Data;
        return View("~/Views/PartnerPortal/Welcome.cshtml", program);
    }

    [HttpGet("{programSlug}/login")]
    public async Task<IActionResult> Login(string programSlug)
    {
        var program = await _partnerRepo.GetProgramBySlugAsync(programSlug);
        if (program == null) return NotFound();
        return View("~/Views/PartnerPortal/Login.cshtml", program);
    }

    [HttpPost("{programSlug}/login")]
    public async Task<IActionResult> ProcessLogin(string programSlug, string email)
    {
        var program = await _partnerRepo.GetProgramBySlugAsync(programSlug);
        if (program == null) return NotFound();

        var partner = await _partnerRepo.GetPartnerByEmailAndProgramAsync(email, program.Id);
        if (partner == null)
        {
            ViewBag.Error = "Email not found in this program";
            return View("~/Views/PartnerPortal/Login.cshtml", program);
        }

        // Simple session via cookie (partner portal separate from main app)
        Response.Cookies.Append("partner_id", partner.Id.ToString(), new CookieOptions
        {
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });
        Response.Cookies.Append("program_slug", programSlug, new CookieOptions
        {
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });

        return Redirect("/partners/dashboard");
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var partner = await GetCurrentPartnerAsync();
        if (partner == null) return Redirect("/");
        var sales = await _partnerRepo.GetSalesByPartnerAsync(partner.Id, 1, 10);
        var payouts = await _partnerRepo.GetPayoutsByPartnerAsync(partner.Id, 1, 5);
        ViewBag.Sales = sales; ViewBag.Payouts = payouts;
        return View("~/Views/PartnerPortal/Dashboard.cshtml", partner);
    }

    [HttpGet("links")]
    public async Task<IActionResult> Links()
    {
        var partner = await GetCurrentPartnerAsync();
        if (partner == null) return Redirect("/");
        return View("~/Views/PartnerPortal/Links.cshtml", partner);
    }

    [HttpGet("sales")]
    public async Task<IActionResult> Sales(int page = 1)
    {
        var partner = await GetCurrentPartnerAsync();
        if (partner == null) return Redirect("/");
        var sales = await _partnerRepo.GetSalesByPartnerAsync(partner.Id, page, 25);
        ViewBag.CurrentPage = page;
        return View("~/Views/PartnerPortal/Sales.cshtml", sales);
    }

    [HttpGet("payouts")]
    public async Task<IActionResult> Payouts(int page = 1)
    {
        var partner = await GetCurrentPartnerAsync();
        if (partner == null) return Redirect("/");
        var payouts = await _partnerRepo.GetPayoutsByPartnerAsync(partner.Id, page, 25);
        return View("~/Views/PartnerPortal/Payouts.cshtml", payouts);
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("partner_id");
        Response.Cookies.Delete("program_slug");
        return Redirect("/");
    }

    private async Task<Data.Models.Partner?> GetCurrentPartnerAsync()
    {
        if (!Request.Cookies.TryGetValue("partner_id", out var idStr) || !long.TryParse(idStr, out var id))
            return null;
        return await _partnerRepo.GetPartnerByIdAsync(id);
    }
}

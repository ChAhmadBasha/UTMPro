using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Controllers;

public class HomeController : Controller
{
    private readonly IBlogRepository _blogRepo;
    private readonly ISystemSettingsRepository _settingsRepo;
    private readonly IPlanRepository _planRepo;

    public HomeController(IBlogRepository blogRepo, ISystemSettingsRepository settingsRepo, IPlanRepository planRepo)
    {
        _blogRepo = blogRepo; _settingsRepo = settingsRepo; _planRepo = planRepo;
    }

    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect("/onboarding/workspace");

        var latestPosts = await _blogRepo.GetLatestAsync(5);
        ViewBag.LatestPosts = latestPosts;
        ViewBag.SiteLogo = await _settingsRepo.GetValueAsync("SiteLogoUrl");
        ViewBag.FooterText = await _settingsRepo.GetValueAsync("SiteFooterText");
        ViewBag.ContactEmail = await _settingsRepo.GetValueAsync("SiteContactEmail");
        return View("~/Views/Home/Index.cshtml");
    }

    [HttpGet("/pricing")]
    public async Task<IActionResult> Pricing()
    {
        ViewBag.SiteLogo = await _settingsRepo.GetValueAsync("SiteLogoUrl");
        var plans = await _planRepo.GetAllActiveAsync();
        ViewBag.Plans = plans;
        return View("~/Views/Home/Pricing.cshtml");
    }
}

using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Controllers;

[Route("stats")]
public class PublicStatsController : Controller
{
    private readonly ILinkRepository _linkRepo;
    private readonly IAnalyticsRepository _analyticsRepo;

    public PublicStatsController(ILinkRepository linkRepo, IAnalyticsRepository analyticsRepo)
    {
        _linkRepo = linkRepo; _analyticsRepo = analyticsRepo;
    }

    [HttpGet("{externalId}")]
    public async Task<IActionResult> Index(string externalId)
    {
        var link = await _linkRepo.GetByExternalIdAsync(externalId);
        if (link == null || !link.IsActive) return NotFound();

        // Check if public stats enabled (IsPublicStats field)
        // For now, show basic public view
        var end = DateTime.UtcNow;
        var start = end.AddDays(-30);
        var analytics = await _analyticsRepo.GetSummaryAsync(link.WorkspaceId, start, end, link.Id);

        ViewBag.Link = link;
        return View("~/Views/PublicStats/Index.cshtml", analytics);
    }
}

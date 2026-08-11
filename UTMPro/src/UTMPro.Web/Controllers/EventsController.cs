using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Controllers;

[Route("{workspaceSlug}/events")]
public class EventsController : BaseWorkspaceController
{
    private readonly IAnalyticsRepository _analyticsRepo;
    private readonly IWorkspaceRepository _wsRepo;

    public EventsController(IAnalyticsRepository analyticsRepo, IWorkspaceRepository wsRepo)
    {
        _analyticsRepo = analyticsRepo;
        _wsRepo = wsRepo;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string workspaceSlug, int page = 1, long? linkId = null)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();

        var events = await _analyticsRepo.GetEventsAsync(CurrentWorkspace!.Id, page, 50, linkId, IsSuperAdmin);
        var total = await _analyticsRepo.GetEventsCountAsync(CurrentWorkspace.Id, linkId, IsSuperAdmin);

        ViewBag.TotalCount = total;
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = 50;
        return View("~/Views/Events/Index.cshtml", events);
    }
}

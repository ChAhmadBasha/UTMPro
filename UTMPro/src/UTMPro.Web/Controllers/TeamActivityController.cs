using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Controllers;

[Route("{workspaceSlug}/activity")]
public class TeamActivityController : BaseWorkspaceController
{
    private readonly ITeamActivityRepository _activityRepo;
    private readonly IWorkspaceRepository _wsRepo;

    public TeamActivityController(ITeamActivityRepository activityRepo, IWorkspaceRepository wsRepo)
    {
        _activityRepo = activityRepo; _wsRepo = wsRepo;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string workspaceSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        var recent = await _activityRepo.GetRecentAsync(CurrentWorkspace!.Id, 50);
        var counts = await _activityRepo.GetMemberActivityCountsAsync(CurrentWorkspace.Id, DateTime.UtcNow.AddDays(-7));
        var members = await _wsRepo.GetMembersAsync(CurrentWorkspace.Id);
        ViewBag.ActivityCounts = counts;
        ViewBag.Members = members;
        return View("~/Views/Activity/Index.cshtml", recent);
    }
}

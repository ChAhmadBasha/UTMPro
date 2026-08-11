using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Helpers;
using UTMPro.Data.Models;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Controllers;

[Authorize]
public class OnboardingController : Controller
{
    private readonly IWorkspaceRepository _wsRepo;
    private readonly IDomainRepository _domainRepo;
    private readonly IPlanRepository _planRepo;
    private readonly ISystemSettingsRepository _settings;

    public OnboardingController(IWorkspaceRepository wsRepo, IDomainRepository domainRepo,
        IPlanRepository planRepo, ISystemSettingsRepository settings)
    {
        _wsRepo = wsRepo;
        _domainRepo = domainRepo;
        _planRepo = planRepo;
        _settings = settings;
    }

    private long UserId => long.Parse(User.FindFirst("UserId")!.Value);

    // ── First-time onboarding (redirects if workspace exists) ──
    [HttpGet("/onboarding/workspace")]
    public async Task<IActionResult> Workspace()
    {
        var existing = await _wsRepo.GetByUserIdAsync(UserId);

        if (existing.Count == 1)
            return Redirect($"/{existing[0].Slug}/links");

        if (existing.Count > 1)
            return Redirect("/workspaces");

        // No workspaces — show creation form
        var defaultPlan = await _planRepo.GetDefaultPlanAsync();
        ViewBag.DefaultPlan = defaultPlan;
        ViewBag.IsNewWorkspace = true;
        return View("~/Views/Onboarding/Workspace.cshtml");
    }

    // ── Create additional workspace (does NOT redirect away) ──
    [HttpGet("/workspaces/new")]
    public async Task<IActionResult> NewWorkspace()
    {
        // Check max workspace limit
        var maxWsStr = await _settings.GetValueAsync("MaxWorkspacesPerUser") ?? "5";
        var maxWs = int.TryParse(maxWsStr, out var m) ? m : 5;
        var count = await _wsRepo.GetUserWorkspaceCountAsync(UserId);

        if (count >= maxWs)
        {
            TempData["Error"] = $"You've reached the maximum of {maxWs} workspaces. Contact support to increase your limit.";
            var existing = await _wsRepo.GetByUserIdAsync(UserId);
            if (existing.Count > 0) return Redirect($"/{existing[0].Slug}/links");
            return Redirect("/");
        }

        var defaultPlan = await _planRepo.GetDefaultPlanAsync();
        ViewBag.DefaultPlan = defaultPlan;
        ViewBag.IsNewWorkspace = false; // Not first-time onboarding
        ViewBag.WorkspaceCount = count;
        ViewBag.MaxWorkspaces = maxWs;
        return View("~/Views/Onboarding/Workspace.cshtml");
    }

    // ── Handle workspace creation (works for both first-time and additional) ──
    [HttpPost("/onboarding/workspace")]
    [HttpPost("/workspaces/new")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateWorkspace(string name, string slug)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(slug))
        {
            ViewBag.Error = "Name and slug are required";
            ViewBag.DefaultPlan = await _planRepo.GetDefaultPlanAsync();
            return View("~/Views/Onboarding/Workspace.cshtml");
        }

        slug = slug.ToLower().Trim().Replace(" ", "-");

        // Check max workspace limit
        var maxWsStr = await _settings.GetValueAsync("MaxWorkspacesPerUser") ?? "5";
        var maxWs = int.TryParse(maxWsStr, out var m) ? m : 5;
        var count = await _wsRepo.GetUserWorkspaceCountAsync(UserId);

        if (count >= maxWs)
        {
            ViewBag.Error = $"You've reached the maximum of {maxWs} workspaces.";
            ViewBag.DefaultPlan = await _planRepo.GetDefaultPlanAsync();
            return View("~/Views/Onboarding/Workspace.cshtml");
        }

        if (await _wsRepo.SlugExistsAsync(slug))
        {
            ViewBag.Error = "This workspace URL is already taken";
            ViewBag.DefaultPlan = await _planRepo.GetDefaultPlanAsync();
            return View("~/Views/Onboarding/Workspace.cshtml");
        }

        // Resolve the default plan
        var defaultPlan = await _planRepo.GetDefaultPlanAsync();
        int planId;
        DateTime? planEndDate = null;

        if (defaultPlan != null)
        {
            planId = defaultPlan.Id;
            if (defaultPlan.TrialDays > 0)
            {
                planEndDate = DateTime.UtcNow.AddDays(defaultPlan.TrialDays);
            }
        }
        else
        {
            var defaultPlanIdStr = await _settings.GetValueAsync("DefaultPlanId") ?? "1";
            planId = int.TryParse(defaultPlanIdStr, out var pid) ? pid : 1;
        }

        var ws = new Workspace
        {
            ExternalId = IdGenerator.NewExternalId("ws_"),
            Name = name,
            Slug = slug,
            OwnerId = UserId,
            PlanId = planId
        };

        var wsId = await _wsRepo.CreateAsync(ws);
        await _wsRepo.AddMemberAsync(wsId, UserId, "Owner", null);

        if (planEndDate.HasValue)
        {
            await _wsRepo.AssignPlanAsync(wsId, planId, DateTime.UtcNow, planEndDate,
                $"Auto-assigned {defaultPlan!.Name} plan with {defaultPlan.TrialDays}-day free trial", UserId);
        }

        return Redirect($"/{slug}/links");
    }

    // ── Workspace list page (for switching) ──
    [HttpGet("/workspaces")]
    public async Task<IActionResult> ListWorkspaces()
    {
        var workspaces = await _wsRepo.GetByUserIdAsync(UserId);
        var maxWsStr = await _settings.GetValueAsync("MaxWorkspacesPerUser") ?? "5";
        ViewBag.MaxWorkspaces = int.TryParse(maxWsStr, out var m) ? m : 5;
        return View("~/Views/Onboarding/WorkspaceList.cshtml", workspaces);
    }

    [HttpGet("/api/workspaces/check-slug")]
    public async Task<IActionResult> CheckSlug(string slug)
    {
        if (string.IsNullOrEmpty(slug))
            return Ok(new { available = false });
        var exists = await _wsRepo.SlugExistsAsync(slug.ToLower().Trim());
        return Ok(new { available = !exists });
    }
}

# PART 8: ADMIN PORTAL CONTROLLERS

## 8.1 Admin Traffic Rules (ADDON 1 - Core Feature)

```csharp
// File: UTMPro.Web/Areas/Admin/Controllers/
//       TrafficRulesController.cs
[Area("Admin")]
[Authorize(Roles = "SuperAdmin")]
[Route("traffic-rules")]
public class TrafficRulesController : Controller
{
    private readonly IAdminTrafficRepository _repo;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var rules = await _repo.GetAllRulesAsync();
        return View(rules);
    }

    [HttpGet("create")]
    public IActionResult Create() => View();

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateTrafficRuleRequest request)
    {
        if (!ModelState.IsValid) return View(request);

        // Validate: user URL weights sum > 0
        if (request.Urls.Count == 0)
        {
            ModelState.AddModelError("",
                "At least one admin URL is required");
            return View(request);
        }

        await _repo.CreateRuleAsync(request, 
            long.Parse(User.FindFirst("UserId")!.Value));

        TempData["Success"] = "Traffic rule created";
        return RedirectToAction("Index");
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Edit(long id)
    {
        var rule = await _repo.GetRuleByIdAsync(id);
        if (rule == null) return NotFound();
        return View(rule);
    }

    [HttpPost("{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        long id, UpdateTrafficRuleRequest request)
    {
        if (!ModelState.IsValid) return View(request);
        await _repo.UpdateRuleAsync(id, request);
        TempData["Success"] = "Traffic rule updated";
        return RedirectToAction("Index");
    }

    [HttpPost("{id}/toggle")]
    public async Task<IActionResult> Toggle(long id)
    {
        await _repo.ToggleRuleAsync(id);
        return Ok();
    }

    [HttpGet("{id}/stats")]
    public async Task<IActionResult> Stats(
        long id, string interval = "7d")
    {
        var stats = await _repo.GetRuleStatsAsync(id, interval);
        return Ok(stats);
    }
}

public class CreateTrafficRuleRequest
{
    [Required]
    [MaxLength(100)]
    public string RuleName { get; set; } = string.Empty;
    
    public bool IsGlobal { get; set; }
    public long? WorkspaceId { get; set; }
    
    [Range(0.01, 100)]
    public decimal TrafficPercent { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    [Required]
    [MinLength(1)]
    public List<AdminUrlRequest> Urls { get; set; } = new();
}

public class AdminUrlRequest
{
    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;
    
    [Range(1, 10000)]
    public int Weight { get; set; } = 100;
    
    [MaxLength(100)]
    public string? Label { get; set; }
}
```

## 8.2 Admin Workspaces Controller

```csharp
// File: UTMPro.Web/Areas/Admin/Controllers/
//       WorkspacesController.cs
[Area("Admin")]
[Authorize(Roles = "SuperAdmin")]
[Route("workspaces")]
public class WorkspacesController : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? search, int? planId,
        int page = 1, int pageSize = 25)
    {
        var workspaces = await _repo.GetAllAsync(
            search, planId, page, pageSize);
        return View(workspaces);
    }

    [HttpPost("{id}/assign-plan")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignPlan(
        long id, AssignPlanRequest request)
    {
        var workspace = await _repo.GetByIdAsync(id);
        if (workspace == null) return NotFound();

        await _repo.AssignPlanAsync(
            id, request.PlanId,
            request.StartDate, request.EndDate,
            request.Notes,
            long.Parse(User.FindFirst("UserId")!.Value));

        TempData["Success"] = 
            $"Plan assigned to {workspace.Name}";
        return RedirectToAction("Index");
    }

    [HttpPost("{id}/suspend")]
    public async Task<IActionResult> Suspend(long id)
    {
        await _repo.SuspendAsync(id);
        return Ok();
    }
}
```

---

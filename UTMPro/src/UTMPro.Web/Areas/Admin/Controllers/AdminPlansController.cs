using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Models;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin")]
[Route("admin/plans")]
public class AdminPlansController : Controller
{
    private readonly IPlanRepository _planRepo;
    public AdminPlansController(IPlanRepository planRepo) => _planRepo = planRepo;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var plans = await _planRepo.GetAllAsync();
        return View("~/Areas/Admin/Views/Plans/Index.cshtml", plans);
    }

    [HttpGet("create")]
    public IActionResult Create() => View("~/Areas/Admin/Views/Plans/Create.cshtml");

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePlan(IFormCollection form)
    {
        try
        {
            var plan = BindPlanFromForm(form);
            if (string.IsNullOrWhiteSpace(plan.Name)) { TempData["Error"] = "Plan name is required"; return Redirect("/admin/plans/create"); }
            await _planRepo.CreateAsync(plan);
            TempData["Success"] = $"Plan '{plan.Name}' created";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to create plan: {ex.Message}";
        }
        return Redirect("/admin/plans");
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var plan = await _planRepo.GetByIdAsync(id);
        if (plan == null) return NotFound();
        return View("~/Areas/Admin/Views/Plans/Edit.cshtml", plan);
    }

    [HttpPost("{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPlan(int id, IFormCollection form)
    {
        try
        {
            var plan = BindPlanFromForm(form);
            plan.Id = id;
            await _planRepo.UpdateAsync(plan);
            TempData["Success"] = $"Plan '{plan.Name}' updated";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to update plan: {ex.Message}";
        }
        return Redirect("/admin/plans");
    }

    [HttpPost("{id:int}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        try
        {
            await _planRepo.ToggleActiveAsync(id);
            TempData["Success"] = "Plan status toggled";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to toggle plan: {ex.Message}";
        }
        return Redirect("/admin/plans");
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _planRepo.DeleteAsync(id);
            TempData["Success"] = "Plan disabled";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to disable plan: {ex.Message}";
        }
        return Redirect("/admin/plans");
    }

    // ── Manual form binding to avoid computed-property / nullable issues ──
    private static Plan BindPlanFromForm(IFormCollection form)
    {
        return new Plan
        {
            Name = form["Name"].ToString().Trim(),
            Price = decimal.TryParse(form["Price"], out var price) ? price : 0,
            BillingCycle = form["BillingCycle"].ToString() is { Length: > 0 } bc ? bc : "Monthly",
            MaxLinksPerMonth = int.TryParse(form["MaxLinksPerMonth"], out var ml) ? ml : 25,
            MaxEventsPerMonth = int.TryParse(form["MaxEventsPerMonth"], out var me) ? me : 1000,
            AnalyticsRetentionDays = int.TryParse(form["AnalyticsRetentionDays"], out var ar) ? ar : 30,
            MaxDomains = int.TryParse(form["MaxDomains"], out var md) ? md : 1,
            MaxMembers = int.TryParse(form["MaxMembers"], out var mm) ? mm : 1,
            MaxFolders = int.TryParse(form["MaxFolders"], out var mf) ? mf : 5,
            MaxTagsPerLink = int.TryParse(form["MaxTagsPerLink"], out var mt) ? mt : 3,
            MaxDestinationsPerLink = int.TryParse(form["MaxDestinationsPerLink"], out var mds) ? mds : 1,
            SortOrder = int.TryParse(form["SortOrder"], out var so) ? so : 0,
            // Boolean checkboxes — present = "true", absent = false
            IsActive = form["IsActive"].ToString() == "true",
            IsDefault = form["IsDefault"].ToString() == "true",
            HasPasswordProtection = form["HasPasswordProtection"].ToString() == "true",
            HasLinkExpiration = form["HasLinkExpiration"].ToString() == "true",
            HasGeoTargeting = form["HasGeoTargeting"].ToString() == "true",
            HasDeviceTargeting = form["HasDeviceTargeting"].ToString() == "true",
            HasLinkCloaking = form["HasLinkCloaking"].ToString() == "true",
            HasABTesting = form["HasABTesting"].ToString() == "true",
            HasCustomerInsights = form["HasCustomerInsights"].ToString() == "true",
            HasEventWebhooks = form["HasEventWebhooks"].ToString() == "true",
            HasAPIAccess = form["HasAPIAccess"].ToString() == "true",
            HasWeightedURLs = form["HasWeightedURLs"].ToString() == "true",
            // Discount & Trial
            DiscountPercent = int.TryParse(form["DiscountPercent"], out var dp) ? dp : 0,
            DiscountLabel = form["DiscountLabel"].ToString() is { Length: > 0 } dl ? dl : null,
            DiscountBadge = form["DiscountBadge"].ToString() is { Length: > 0 } db ? db : null,
            TrialDays = int.TryParse(form["TrialDays"], out var td) ? td : 0,
            FallbackPlanId = int.TryParse(form["FallbackPlanId"], out var fp) && fp > 0 ? fp : null,
        };
    }
}

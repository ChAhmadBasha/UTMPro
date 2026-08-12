using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Models;
using UTMPro.Data.Repositories;
using UTMPro.Web.Services;

namespace UTMPro.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin")]
[Route("admin/traffic-rules")]
public class TrafficRulesController : Controller
{
    private readonly IAdminTrafficRepository _repo;
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly ISystemSettingsRepository _settingsRepo;
    private readonly IRedirectCacheInvalidationService _cacheInvalidation;

    public TrafficRulesController(
        IAdminTrafficRepository repo,
        IWorkspaceRepository workspaceRepo,
        ISystemSettingsRepository settingsRepo,
        IRedirectCacheInvalidationService cacheInvalidation)
    {
        _repo = repo;
        _workspaceRepo = workspaceRepo;
        _settingsRepo = settingsRepo;
        _cacheInvalidation = cacheInvalidation;
    }

    private long UserId => long.Parse(User.FindFirst("UserId")!.Value);

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var rules = await _repo.GetAllRulesAsync();
        var raw = await _settingsRepo.GetValueAsync(SystemSettingKeys.AdminTrafficMinClicks);
        ViewBag.AdminTrafficMinClicks = SystemSettingKeys.ParseAdminTrafficMinClicks(raw);
        return View("~/Areas/Admin/Views/TrafficRules/Index.cshtml", rules);
    }

    [HttpPost("min-clicks")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateMinClicks(int minClicks)
    {
        if (minClicks is < 0 or > SystemSettingKeys.AdminTrafficMinClicksMax)
        {
            TempData["Error"] =
                $"Warm-up clicks must be between 0 and {SystemSettingKeys.AdminTrafficMinClicksMax}.";
            return Redirect("/admin/traffic-rules");
        }

        await _settingsRepo.SetValueAsync(
            SystemSettingKeys.AdminTrafficMinClicks,
            minClicks.ToString(),
            UserId,
            SystemSettingKeys.AdminTrafficMinClicksDescription);
        await _cacheInvalidation.InvalidateAllAsync();

        TempData["Success"] = minClicks == 0
            ? "Admin traffic now starts immediately on new links."
            : $"Admin traffic now starts after {minClicks} original click(s) on each link.";
        return Redirect("/admin/traffic-rules");
    }

    [HttpGet("report")]
    public async Task<IActionResult> Report(int days = 30)
    {
        days = Math.Clamp(days, 1, 365);
        var report = await _repo.GetReportAsync(days);
        return View("~/Areas/Admin/Views/TrafficRules/Report.cshtml", report);
    }

    [HttpGet("{id:long}/test")]
    public async Task<IActionResult> Test(long id, long? urlId = null)
    {
        var rule = await _repo.GetRuleByIdAsync(id);
        if (rule == null)
            return NotFound();

        var activeUrls = rule.Urls.Where(url => url.IsActive).ToList();
        if (activeUrls.Count == 0)
        {
            TempData["Error"] = "This rule has no active admin URL to test.";
            return Redirect($"/admin/traffic-rules/{id}");
        }

        AdminTrafficUrl destination;
        if (urlId.HasValue)
        {
            var requestedUrl = activeUrls.FirstOrDefault(url => url.Id == urlId.Value);
            if (requestedUrl == null)
                return NotFound();
            destination = requestedUrl;
        }
        else
        {
            destination = PickWeightedUrl(activeUrls);
        }

        // This deliberately bypasses the configured percentage and always
        // opens an admin destination. Test clicks are not analytics events.
        Response.Headers["Cache-Control"] = "no-store";
        return Redirect(destination.Url);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        await LoadWorkspacesAsync();
        return View("~/Areas/Admin/Views/TrafficRules/Create.cshtml");
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string ruleName,
        decimal trafficPercent,
        bool isGlobal,
        long? workspaceId,
        string[]? urls,
        int[]? weights,
        string[]? labels)
    {
        var error = await ValidateRuleAsync(
            ruleName, trafficPercent, isGlobal, workspaceId, urls, weights, labels);
        if (error != null)
        {
            TempData["Error"] = error;
            await LoadWorkspacesAsync();
            return View("~/Areas/Admin/Views/TrafficRules/Create.cshtml");
        }

        var ruleId = await _repo.CreateRuleAsync(new AdminTrafficRule
        {
            RuleName = ruleName.Trim(),
            TrafficPercent = trafficPercent,
            IsGlobal = isGlobal,
            WorkspaceId = isGlobal ? null : workspaceId,
            IsActive = true,
            CreatedBy = UserId
        });

        await AddUrlsAsync(ruleId, urls!, weights, labels);
        await _cacheInvalidation.InvalidateAllAsync();

        TempData["Success"] = "Traffic rule created";
        return Redirect("/admin/traffic-rules");
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Edit(long id)
    {
        var rule = await _repo.GetRuleByIdAsync(id);
        if (rule == null)
            return NotFound();

        await LoadWorkspacesAsync();
        return View("~/Areas/Admin/Views/TrafficRules/Edit.cshtml", rule);
    }

    [HttpPost("{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        long id,
        string ruleName,
        decimal trafficPercent,
        bool isGlobal,
        long? workspaceId,
        bool isActive,
        long[]? urlIds,
        string[]? urls,
        int[]? weights,
        string[]? labels)
    {
        var rule = await _repo.GetRuleByIdAsync(id);
        if (rule == null)
            return NotFound();

        var error = await ValidateRuleAsync(
            ruleName, trafficPercent, isGlobal, workspaceId, urls, weights, labels);
        if (error != null)
        {
            TempData["Error"] = error;
            await LoadWorkspacesAsync();
            return View("~/Areas/Admin/Views/TrafficRules/Edit.cshtml", rule);
        }

        rule.RuleName = ruleName.Trim();
        rule.TrafficPercent = trafficPercent;
        rule.IsGlobal = isGlobal;
        rule.WorkspaceId = isGlobal ? null : workspaceId;
        rule.IsActive = isActive;
        await _repo.UpdateRuleAsync(rule);

        await _repo.SyncUrlsAsync(
            id,
            BuildUrls(id, urlIds, urls!, weights, labels));
        await _cacheInvalidation.InvalidateAllAsync();

        TempData["Success"] = "Traffic rule updated";
        return Redirect("/admin/traffic-rules");
    }

    [HttpPost("{id}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(long id)
    {
        await _repo.ToggleRuleAsync(id);
        await _cacheInvalidation.InvalidateAllAsync();
        return Redirect("/admin/traffic-rules");
    }

    private static AdminTrafficUrl PickWeightedUrl(IReadOnlyList<AdminTrafficUrl> urls)
    {
        if (urls.Count == 1)
            return urls[0];

        var totalWeight = urls.Sum(url => (long)Math.Max(0, url.Weight));
        if (totalWeight <= 0)
            return urls[0];

        var roll = Random.Shared.NextInt64(1, totalWeight + 1);
        long cumulative = 0;
        foreach (var url in urls)
        {
            cumulative += Math.Max(0, url.Weight);
            if (roll <= cumulative)
                return url;
        }

        return urls[^1];
    }

    private async Task<string?> ValidateRuleAsync(
        string ruleName,
        decimal trafficPercent,
        bool isGlobal,
        long? workspaceId,
        string[]? urls,
        int[]? weights,
        string[]? labels)
    {
        if (string.IsNullOrWhiteSpace(ruleName) || ruleName.Trim().Length > 100)
            return "Rule name is required and cannot exceed 100 characters.";

        if (trafficPercent is <= 0 or > 100)
            return "Traffic percentage must be greater than 0 and no more than 100.";

        if (!isGlobal)
        {
            if (!workspaceId.HasValue)
                return "Select a workspace for a workspace-scoped rule.";

            if (await _workspaceRepo.GetByIdAsync(workspaceId.Value) == null)
                return "The selected workspace does not exist.";
        }

        var activeUrls = (urls ?? Array.Empty<string>())
            .Select((url, index) => new { Url = url?.Trim(), Index = index })
            .Where(item => !string.IsNullOrWhiteSpace(item.Url))
            .ToList();

        if (activeUrls.Count == 0)
            return "At least one admin URL is required.";

        foreach (var item in activeUrls)
        {
            if (!Uri.TryCreate(item.Url, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return $"Admin URL #{item.Index + 1} must be a valid HTTP or HTTPS URL.";
            }

            var weight = item.Index < (weights?.Length ?? 0) ? weights![item.Index] : 100;
            if (weight is < 1 or > 10_000)
                return $"Weight for admin URL #{item.Index + 1} must be between 1 and 10,000.";

            if (item.Index < (labels?.Length ?? 0) && labels![item.Index]?.Length > 100)
                return $"Label for admin URL #{item.Index + 1} cannot exceed 100 characters.";
        }

        return null;
    }

    private static List<AdminTrafficUrl> BuildUrls(
        long ruleId,
        long[]? urlIds,
        string[] urls,
        int[]? weights,
        string[]? labels)
    {
        var result = new List<AdminTrafficUrl>();
        for (var i = 0; i < urls.Length; i++)
        {
            var url = urls[i]?.Trim();
            if (string.IsNullOrWhiteSpace(url))
                continue;

            result.Add(new AdminTrafficUrl
            {
                Id = i < (urlIds?.Length ?? 0) ? urlIds![i] : 0,
                RuleId = ruleId,
                Url = url,
                Weight = i < (weights?.Length ?? 0) ? weights![i] : 100,
                Label = i < (labels?.Length ?? 0) && !string.IsNullOrWhiteSpace(labels![i])
                    ? labels[i].Trim()
                    : null,
                IsActive = true
            });
        }

        return result;
    }

    private async Task AddUrlsAsync(
        long ruleId,
        string[] urls,
        int[]? weights,
        string[]? labels)
    {
        for (var i = 0; i < urls.Length; i++)
        {
            var url = urls[i]?.Trim();
            if (string.IsNullOrWhiteSpace(url))
                continue;

            await _repo.AddUrlAsync(new AdminTrafficUrl
            {
                RuleId = ruleId,
                Url = url,
                Weight = i < (weights?.Length ?? 0) ? weights![i] : 100,
                Label = i < (labels?.Length ?? 0) && !string.IsNullOrWhiteSpace(labels![i])
                    ? labels[i].Trim()
                    : null
            });
        }
    }

    private async Task LoadWorkspacesAsync()
    {
        ViewBag.Workspaces = await _workspaceRepo.GetAllAsync(
            search: null,
            planId: null,
            page: 1,
            pageSize: 1000);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Models;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin")]
[Route("admin/traffic-rules")]
public class TrafficRulesController : Controller
{
    private readonly IAdminTrafficRepository _repo;
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TrafficRulesController> _logger;

    public TrafficRulesController(
        IAdminTrafficRepository repo,
        IWorkspaceRepository workspaceRepo,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<TrafficRulesController> logger)
    {
        _repo = repo;
        _workspaceRepo = workspaceRepo;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    private long UserId => long.Parse(User.FindFirst("UserId")!.Value);

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var rules = await _repo.GetAllRulesAsync();
        return View("~/Areas/Admin/Views/TrafficRules/Index.cshtml", rules);
    }

    [HttpGet("report")]
    public async Task<IActionResult> Report(int days = 30)
    {
        days = Math.Clamp(days, 1, 365);
        var report = await _repo.GetReportAsync(days);
        return View("~/Areas/Admin/Views/TrafficRules/Report.cshtml", report);
    }

    [HttpGet("{id}/test")]
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
        await InvalidateRedirectCacheAsync();

        TempData["Success"] = "Traffic rule created";
        return Redirect("/admin/traffic-rules");
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Edit(long id)
    {
        var rule = await _repo.GetRuleByIdAsync(id);
        if (rule == null)
            return NotFound();

        await LoadWorkspacesAsync();
        return View("~/Areas/Admin/Views/TrafficRules/Edit.cshtml", rule);
    }

    [HttpPost("{id}")]
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
        await InvalidateRedirectCacheAsync();

        TempData["Success"] = "Traffic rule updated";
        return Redirect("/admin/traffic-rules");
    }

    [HttpPost("{id}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(long id)
    {
        await _repo.ToggleRuleAsync(id);
        await InvalidateRedirectCacheAsync();
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

    private async Task InvalidateRedirectCacheAsync()
    {
        try
        {
            var redirectEngineUrl = _configuration["App:RedirectEngineUrl"]
                ?? "https://go.utmpro.link";
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(3);

            var internalApiKey = _configuration["InternalApiKey"];
            if (!string.IsNullOrWhiteSpace(internalApiKey))
                client.DefaultRequestHeaders.Add("X-Internal-Key", internalApiKey);

            using var response = await client.PostAsync(
                redirectEngineUrl.TrimEnd('/') + "/cache/invalidate-all",
                content: null);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Redirect cache invalidation returned HTTP {StatusCode}; cached links will refresh by TTL",
                    (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            // The redirect engine also has a short TTL, so an invalidation
            // outage must not make the admin rule update itself fail.
            _logger.LogWarning(ex, "Could not invalidate redirect cache after traffic-rule change");
        }
    }
}

using System.ComponentModel.DataAnnotations;
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

    public TrafficRulesController(IAdminTrafficRepository repo) => _repo = repo;

    private long UserId => long.Parse(User.FindFirst("UserId")!.Value);

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var rules = await _repo.GetAllRulesAsync();
        return View("~/Areas/Admin/Views/TrafficRules/Index.cshtml", rules);
    }

    [HttpGet("create")]
    public IActionResult Create() => View("~/Areas/Admin/Views/TrafficRules/Create.cshtml");

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string ruleName, decimal trafficPercent, bool isGlobal,
        long? workspaceId, string[] urls, int[] weights, string[] labels)
    {
        if (urls == null || urls.Length == 0)
        {
            TempData["Error"] = "At least one admin URL is required";
            return View("~/Areas/Admin/Views/TrafficRules/Create.cshtml");
        }

        var ruleId = await _repo.CreateRuleAsync(new AdminTrafficRule
        {
            RuleName = ruleName,
            TrafficPercent = trafficPercent,
            IsGlobal = isGlobal,
            WorkspaceId = isGlobal ? null : workspaceId,
            IsActive = true,
            CreatedBy = UserId
        });

        for (int i = 0; i < urls.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(urls[i]))
            {
                await _repo.AddUrlAsync(new AdminTrafficUrl
                {
                    RuleId = ruleId,
                    Url = urls[i],
                    Weight = i < weights.Length ? weights[i] : 100,
                    Label = i < labels.Length ? labels[i] : null
                });
            }
        }

        TempData["Success"] = "Traffic rule created";
        return Redirect("/admin/traffic-rules");
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Edit(long id)
    {
        var rule = await _repo.GetRuleByIdAsync(id);
        if (rule == null) return NotFound();
        return View("~/Areas/Admin/Views/TrafficRules/Edit.cshtml", rule);
    }

    [HttpPost("{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, string ruleName, decimal trafficPercent,
        bool isGlobal, bool isActive, string[] urls, int[] weights, string[] labels)
    {
        var rule = await _repo.GetRuleByIdAsync(id);
        if (rule == null) return NotFound();

        rule.RuleName = ruleName;
        rule.TrafficPercent = trafficPercent;
        rule.IsGlobal = isGlobal;
        rule.IsActive = isActive;
        await _repo.UpdateRuleAsync(rule);

        // Rebuild URLs
        await _repo.DeleteUrlsByRuleIdAsync(id);
        for (int i = 0; i < (urls?.Length ?? 0); i++)
        {
            if (!string.IsNullOrWhiteSpace(urls![i]))
            {
                await _repo.AddUrlAsync(new AdminTrafficUrl
                {
                    RuleId = id,
                    Url = urls[i],
                    Weight = i < weights!.Length ? weights[i] : 100,
                    Label = i < labels!.Length ? labels[i] : null
                });
            }
        }

        TempData["Success"] = "Traffic rule updated";
        return Redirect("/admin/traffic-rules");
    }

    [HttpPost("{id}/toggle")]
    public async Task<IActionResult> Toggle(long id)
    {
        await _repo.ToggleRuleAsync(id);
        return Ok(new { success = true });
    }
}

using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Models;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Controllers;

[Route("{workspaceSlug}/links/domains")]
public class DomainsPageController : BaseWorkspaceController
{
    private readonly IDomainRepository _domainRepo;
    private readonly IWorkspaceRepository _wsRepo;
    private readonly IPlanRepository _planRepo;
    private readonly ISystemSettingsRepository _settingsRepo;

    public DomainsPageController(IDomainRepository domainRepo, IWorkspaceRepository wsRepo,
        IPlanRepository planRepo, ISystemSettingsRepository settingsRepo)
    {
        _domainRepo = domainRepo; _wsRepo = wsRepo; _planRepo = planRepo; _settingsRepo = settingsRepo;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string workspaceSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        var domains = await _domainRepo.GetByWorkspaceIdAsync(CurrentWorkspace!.Id);
        var plan = await _planRepo.GetByIdAsync(CurrentWorkspace.PlanId);
        ViewBag.MaxDomains = plan?.MaxDomains ?? 1;
        ViewBag.CustomDomainCount = await _domainRepo.GetWorkspaceDomainCountAsync(CurrentWorkspace.Id);
        ViewBag.ServerIP = await _settingsRepo.GetValueAsync("ServerIP") ?? "76.76.21.21";
        return View("~/Views/DomainsPage/Index.cshtml", domains);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string workspaceSlug, string domain)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanAdmin()) return Forbidden();

        var plan = await _planRepo.GetByIdAsync(CurrentWorkspace!.PlanId);
        var count = await _domainRepo.GetWorkspaceDomainCountAsync(CurrentWorkspace.Id);
        if (count >= (plan?.MaxDomains ?? 1))
        {
            TempData["Error"] = "Domain limit reached for your plan";
            return Redirect($"/{workspaceSlug}/links/domains");
        }

        var existing = await _domainRepo.GetByDomainNameAsync(domain);
        if (existing != null)
        {
            TempData["Error"] = "This domain is already registered";
            return Redirect($"/{workspaceSlug}/links/domains");
        }

        // Get server IP from settings (not hardcoded!)
        var serverIP = await _settingsRepo.GetValueAsync("ServerIP") ?? "76.76.21.21";

        await _domainRepo.CreateAsync(new Domain
        {
            WorkspaceId = CurrentWorkspace.Id,
            DomainName = domain.ToLower().Trim(),
            IsSystemDomain = false,
            IsVerified = false,
            DNSValue = serverIP,
            CreatedBy = CurrentUserId
        });

        TempData["Success"] = $"Domain added. Set your DNS A record to point to {serverIP}.";
        return Redirect($"/{workspaceSlug}/links/domains");
    }

    [HttpPost("{id}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string workspaceSlug, long id, string? defaultRedirectUrl, string? description)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanEdit()) return Forbidden();

        var domain = await _domainRepo.GetByIdAsync(id);
        if (domain == null) return NotFound();

        // Users can only edit their own workspace's domains
        if (!IsSuperAdmin && domain.WorkspaceId != CurrentWorkspace!.Id)
        {
            TempData["Error"] = "You can only edit your own domains";
            return Redirect($"/{workspaceSlug}/links/domains");
        }

        domain.DefaultRedirectUrl = defaultRedirectUrl;
        domain.Description = description;
        await _domainRepo.UpdateAsync(domain);

        TempData["Success"] = "Domain updated";
        return Redirect($"/{workspaceSlug}/links/domains");
    }

    [HttpPost("{id}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string workspaceSlug, long id)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanEdit()) return Forbidden();

        var domain = await _domainRepo.GetByIdAsync(id);
        if (domain == null) return NotFound();

        if (domain.IsSystemDomain)
        {
            TempData["Error"] = "Cannot delete system domains";
            return Redirect($"/{workspaceSlug}/links/domains");
        }

        // Users can only delete their workspace's domains
        if (!IsSuperAdmin && domain.WorkspaceId != CurrentWorkspace!.Id)
        {
            TempData["Error"] = "You can only delete your own domains";
            return Redirect($"/{workspaceSlug}/links/domains");
        }

        await _domainRepo.DeleteAsync(id);
        TempData["Success"] = "Domain removed";
        return Redirect($"/{workspaceSlug}/links/domains");
    }

    [HttpPost("verify/{id}")]
    public async Task<IActionResult> Verify(string workspaceSlug, long id)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        return Ok(new { message = "Verification check queued" });
    }
}

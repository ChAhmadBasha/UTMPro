using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Controllers;

[Route("{workspaceSlug}/settings/integrations")]
public class IntegrationsController : BaseWorkspaceController
{
    private readonly IWorkspaceRepository _wsRepo;
    private readonly IIntegrationRepository _intRepo;

    public IntegrationsController(IWorkspaceRepository wsRepo, IIntegrationRepository intRepo)
    {
        _wsRepo = wsRepo; _intRepo = intRepo;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string workspaceSlug, string? category = null)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        var allIntegrations = await _intRepo.GetAllAsync();
        var connected = await _intRepo.GetWorkspaceIntegrationsAsync(CurrentWorkspace!.Id);
        var connectedIds = connected.Select(c => c.IntegrationId).ToHashSet();
        ViewBag.AllIntegrations = allIntegrations;
        ViewBag.ConnectedIds = connectedIds;
        ViewBag.Category = category;
        return View("~/Views/Settings/IntegrationsMarketplace.cshtml");
    }

    [HttpGet("{integrationSlug}/connect")]
    public async Task<IActionResult> Connect(string workspaceSlug, string integrationSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanAdmin()) return Forbidden();
        var integration = await _intRepo.GetBySlugAsync(integrationSlug);
        if (integration == null) return NotFound();
        ViewBag.Integration = integration;
        return View("~/Views/Settings/IntegrationConnect.cshtml");
    }

    [HttpPost("{integrationSlug}/connect")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveConnect(string workspaceSlug, string integrationSlug, string? config)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanAdmin()) return Forbidden();
        var integration = await _intRepo.GetBySlugAsync(integrationSlug);
        if (integration == null) return NotFound();
        await _intRepo.ConnectAsync(CurrentWorkspace!.Id, integration.Id, config, CurrentUserId);
        TempData["Success"] = $"{integration.Name} connected";
        return Redirect($"/{workspaceSlug}/settings/integrations");
    }

    [HttpPost("{integrationSlug}/disconnect")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disconnect(string workspaceSlug, string integrationSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanAdmin()) return Forbidden();
        var integration = await _intRepo.GetBySlugAsync(integrationSlug);
        if (integration == null) return NotFound();
        await _intRepo.DisconnectAsync(CurrentWorkspace!.Id, integration.Id);
        TempData["Success"] = $"{integration.Name} disconnected";
        return Redirect($"/{workspaceSlug}/settings/integrations");
    }
}

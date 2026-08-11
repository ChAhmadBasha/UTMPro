using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Models;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Controllers;

[Route("{workspaceSlug}/utm-templates")]
public class UTMTemplatesController : BaseWorkspaceController
{
    private readonly IUTMTemplateRepository _templateRepo;
    private readonly IWorkspaceRepository _wsRepo;

    public UTMTemplatesController(IUTMTemplateRepository templateRepo, IWorkspaceRepository wsRepo)
    {
        _templateRepo = templateRepo; _wsRepo = wsRepo;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string workspaceSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        var templates = await _templateRepo.GetByWorkspaceAsync(CurrentWorkspace!.Id);
        return View("~/Views/UTMTemplates/Index.cshtml", templates);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string workspaceSlug, string name, string? utmSource,
        string? utmMedium, string? utmCampaign, string? utmTerm, string? utmContent, bool isDefault = false)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanEdit()) return Forbidden();

        await _templateRepo.CreateAsync(new UTMTemplate
        {
            WorkspaceId = CurrentWorkspace!.Id, Name = name, UTMSource = utmSource,
            UTMMedium = utmMedium, UTMCampaign = utmCampaign, UTMTerm = utmTerm, UTMContent = utmContent, IsDefault = isDefault
        });
        TempData["Success"] = "UTM template saved";
        return Redirect($"/{workspaceSlug}/utm-templates");
    }

    [HttpPost("{id}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string workspaceSlug, long id)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        await _templateRepo.DeleteAsync(id);
        TempData["Success"] = "Template deleted";
        return Redirect($"/{workspaceSlug}/utm-templates");
    }

    // AJAX: Get templates for link creation modal
    [HttpGet("api/list")]
    public async Task<IActionResult> GetTemplates(string workspaceSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        var templates = await _templateRepo.GetByWorkspaceAsync(CurrentWorkspace!.Id);
        return Ok(templates.Select(t => new { t.Id, t.Name, t.UTMSource, t.UTMMedium, t.UTMCampaign, t.UTMTerm, t.UTMContent, t.IsDefault }));
    }
}

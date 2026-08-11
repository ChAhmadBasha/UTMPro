using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Controllers;

[Route("{workspaceSlug}/links/bulk-action")]
public class BulkActionsController : BaseWorkspaceController
{
    private readonly ILinkRepository _linkRepo;
    private readonly IWorkspaceRepository _wsRepo;

    public BulkActionsController(ILinkRepository linkRepo, IWorkspaceRepository wsRepo)
    {
        _linkRepo = linkRepo; _wsRepo = wsRepo;
    }

    [HttpPost("archive")]
    public async Task<IActionResult> BulkArchive(string workspaceSlug, [FromBody] BulkActionRequest request)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanEdit()) return Forbidden();

        int count = 0;
        foreach (var id in request.LinkIds ?? Array.Empty<long>())
        {
            var link = await _linkRepo.GetByIdAsync(id, CurrentWorkspace!.Id);
            if (link != null) { await _linkRepo.ArchiveAsync(id); count++; }
        }
        return Ok(new { success = true, affected = count });
    }

    [HttpPost("delete")]
    public async Task<IActionResult> BulkDelete(string workspaceSlug, [FromBody] BulkActionRequest request)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanAdmin()) return Forbidden();

        int count = 0;
        foreach (var id in request.LinkIds ?? Array.Empty<long>())
        {
            var link = await _linkRepo.GetByIdAsync(id, CurrentWorkspace!.Id);
            if (link != null) { await _linkRepo.DeleteAsync(id); count++; }
        }
        return Ok(new { success = true, affected = count });
    }

    [HttpPost("toggle")]
    public async Task<IActionResult> BulkToggle(string workspaceSlug, [FromBody] BulkActionRequest request)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanEdit()) return Forbidden();

        int count = 0;
        foreach (var id in request.LinkIds ?? Array.Empty<long>())
        {
            var link = await _linkRepo.GetByIdAsync(id, CurrentWorkspace!.Id);
            if (link != null) { link.IsActive = !link.IsActive; await _linkRepo.UpdateAsync(link); count++; }
        }
        return Ok(new { success = true, affected = count });
    }
}

public class BulkActionRequest
{
    public long[]? LinkIds { get; set; }
}

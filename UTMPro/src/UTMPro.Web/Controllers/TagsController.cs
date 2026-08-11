using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Models;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Controllers;

[Route("{workspaceSlug}/links/tags")]
public class TagsController : BaseWorkspaceController
{
    private readonly ITagRepository _tagRepo;
    private readonly IWorkspaceRepository _wsRepo;

    public TagsController(ITagRepository tagRepo, IWorkspaceRepository wsRepo)
    {
        _tagRepo = tagRepo;
        _wsRepo = wsRepo;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string workspaceSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        var tags = await _tagRepo.GetByWorkspaceIdAsync(CurrentWorkspace!.Id);
        return View("~/Views/Tags/Index.cshtml", tags);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string workspaceSlug, string name, string color = "#22c55e")
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanEdit()) return Forbidden();

        await _tagRepo.CreateAsync(new Tag
        {
            WorkspaceId = CurrentWorkspace!.Id,
            Name = name,
            Color = color
        });

        TempData["Success"] = "Tag created";
        return Redirect($"/{workspaceSlug}/links/tags");
    }

    [HttpPost("{id}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string workspaceSlug, long id)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanAdmin()) return Forbidden();

        await _tagRepo.DeleteAsync(id);
        TempData["Success"] = "Tag deleted";
        return Redirect($"/{workspaceSlug}/links/tags");
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(string workspaceSlug, string q)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        var tags = await _tagRepo.SearchAsync(CurrentWorkspace!.Id, q ?? "");
        return Ok(tags.Select(t => new { t.Id, t.Name, t.Color }));
    }
}

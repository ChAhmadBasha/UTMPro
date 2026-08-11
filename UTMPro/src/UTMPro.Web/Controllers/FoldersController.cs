using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Models;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Controllers;

[Route("{workspaceSlug}/links/folders")]
public class FoldersController : BaseWorkspaceController
{
    private readonly IFolderRepository _folderRepo;
    private readonly IWorkspaceRepository _wsRepo;
    private readonly IPlanRepository _planRepo;

    public FoldersController(IFolderRepository folderRepo, IWorkspaceRepository wsRepo, IPlanRepository planRepo)
    {
        _folderRepo = folderRepo;
        _wsRepo = wsRepo;
        _planRepo = planRepo;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string workspaceSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        var folders = await _folderRepo.GetByWorkspaceIdAsync(CurrentWorkspace!.Id);
        var plan = await _planRepo.GetByIdAsync(CurrentWorkspace.PlanId);
        ViewBag.MaxFolders = plan?.MaxFolders ?? 5;
        return View("~/Views/Folders/Index.cshtml", folders);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string workspaceSlug, string name, string color = "#22c55e")
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanEdit()) return Forbidden();

        var plan = await _planRepo.GetByIdAsync(CurrentWorkspace!.PlanId);
        var count = await _folderRepo.GetWorkspaceFolderCountAsync(CurrentWorkspace.Id);
        if (count >= (plan?.MaxFolders ?? 5))
        {
            TempData["Error"] = "Folder limit reached for your plan";
            return Redirect($"/{workspaceSlug}/links/folders");
        }

        await _folderRepo.CreateAsync(new Folder
        {
            WorkspaceId = CurrentWorkspace.Id,
            Name = name,
            Color = color
        });

        TempData["Success"] = "Folder created";
        return Redirect($"/{workspaceSlug}/links/folders");
    }

    [HttpPost("{id}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string workspaceSlug, long id)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanAdmin()) return Forbidden();

        var folder = await _folderRepo.GetByIdAsync(id, CurrentWorkspace!.Id);
        if (folder == null) return NotFound();

        await _folderRepo.DeleteAsync(id);
        TempData["Success"] = "Folder deleted";
        return Redirect($"/{workspaceSlug}/links/folders");
    }
}

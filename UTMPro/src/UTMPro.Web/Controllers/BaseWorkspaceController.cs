using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Models;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Controllers;

[Authorize]
public abstract class BaseWorkspaceController : Controller
{
    protected long CurrentUserId =>
        long.Parse(User.FindFirst("UserId")!.Value);

    protected string CurrentUserName =>
        User.FindFirst("Name")?.Value ?? "";

    protected bool IsSuperAdmin =>
        User.IsInRole("SuperAdmin");

    protected Workspace? CurrentWorkspace { get; private set; }
    protected string CurrentRole { get; private set; } = "";

    protected async Task<bool> LoadWorkspaceAsync(string slug, IWorkspaceRepository wsRepo)
    {
        CurrentWorkspace = await wsRepo.GetBySlugAsync(slug);
        if (CurrentWorkspace == null) return false;

        var member = await wsRepo.GetMemberAsync(CurrentWorkspace.Id, CurrentUserId);
        if (member == null && !IsSuperAdmin) return false;

        CurrentRole = member?.Role ?? (IsSuperAdmin ? "Admin" : "");

        ViewBag.Workspace = CurrentWorkspace;
        ViewBag.CurrentRole = CurrentRole;
        ViewBag.UserId = CurrentUserId;
        ViewBag.UserName = CurrentUserName;

        // Load all user's workspaces for the sidebar switcher
        var userWorkspaces = await wsRepo.GetByUserIdAsync(CurrentUserId);
        ViewBag.UserWorkspaces = userWorkspaces;

        return true;
    }

    protected bool CanEdit() => CurrentRole is "Owner" or "Admin" or "Member";
    protected bool CanAdmin() => CurrentRole is "Owner" or "Admin";
    protected bool IsOwner() => CurrentRole == "Owner";
    protected IActionResult Forbidden() => StatusCode(403, "Access denied");
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Controllers;

public class InviteController : Controller
{
    private readonly IWorkspaceRepository _wsRepo;
    private readonly IUserRepository _userRepo;

    public InviteController(IWorkspaceRepository wsRepo, IUserRepository userRepo)
    {
        _wsRepo = wsRepo;
        _userRepo = userRepo;
    }

    [HttpGet("/invite/{token}")]
    public async Task<IActionResult> Accept(string token)
    {
        var invitation = await _wsRepo.GetInvitationByTokenAsync(token);
        if (invitation == null)
        {
            ViewBag.Error = "Invalid or expired invitation";
            return View("~/Views/Invite/Invalid.cshtml");
        }

        ViewBag.Invitation = invitation;

        if (User.Identity?.IsAuthenticated != true)
            return Redirect($"/login?returnUrl=/invite/{token}");

        var userId = long.Parse(User.FindFirst("UserId")!.Value);
        var existing = await _wsRepo.GetMemberAsync(invitation.WorkspaceId, userId);
        if (existing != null)
        {
            return Redirect($"/{(await _wsRepo.GetByIdAsync(invitation.WorkspaceId))?.Slug}/links");
        }

        await _wsRepo.AddMemberAsync(invitation.WorkspaceId, userId, invitation.Role, invitation.InvitedBy);
        await _wsRepo.AcceptInvitationAsync(invitation.Id);

        var ws = await _wsRepo.GetByIdAsync(invitation.WorkspaceId);
        TempData["Success"] = $"Welcome to {ws?.Name}!";
        return Redirect($"/{ws?.Slug}/links");
    }
}

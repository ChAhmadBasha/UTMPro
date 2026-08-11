using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Hubs;

[Authorize]
public class EventsHub : Hub
{
    private readonly IWorkspaceRepository _wsRepo;

    public EventsHub(IWorkspaceRepository wsRepo) => _wsRepo = wsRepo;

    public async Task JoinWorkspace(string workspaceSlug)
    {
        var userId = long.Parse(Context.User!.FindFirst("UserId")!.Value);
        var workspace = await _wsRepo.GetBySlugAsync(workspaceSlug);
        if (workspace == null) return;

        var member = await _wsRepo.GetMemberAsync(workspace.Id, userId);
        if (member == null) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, $"workspace:{workspace.Id}");
    }

    public async Task LeaveWorkspace(string workspaceSlug)
    {
        var workspace = await _wsRepo.GetBySlugAsync(workspaceSlug);
        if (workspace == null) return;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"workspace:{workspace.Id}");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}

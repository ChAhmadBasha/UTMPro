# PART 7: REAL-TIME EVENTS (SIGNALR)

```csharp
// ============================================================
// File: UTMPro.Web/Hubs/EventsHub.cs
// ============================================================
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace UTMPro.Web.Hubs;

[Authorize]
public class EventsHub : Hub
{
    private readonly IWorkspaceRepository _wsRepo;

    public EventsHub(IWorkspaceRepository wsRepo)
    {
        _wsRepo = wsRepo;
    }

    public async Task JoinWorkspace(string workspaceSlug)
    {
        var userId = long.Parse(
            Context.User!.FindFirst("UserId")!.Value);

        var workspace = await _wsRepo.GetBySlugAsync(workspaceSlug);
        if (workspace == null) return;

        var member = await _wsRepo.GetMemberAsync(
            workspace.Id, userId);
        if (member == null) return;

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            $"workspace:{workspace.Id}");
    }

    public async Task LeaveWorkspace(string workspaceSlug)
    {
        var workspace = await _wsRepo.GetBySlugAsync(workspaceSlug);
        if (workspace == null) return;

        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            $"workspace:{workspace.Id}");
    }

    public override async Task OnDisconnectedAsync(
        Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}

// ============================================================
// File: UTMPro.Web/Services/Phase2/RealTimeEventService.cs
// ============================================================
public interface IRealTimeEventService
{
    Task BroadcastClickAsync(long workspaceId, ClickEventDto click);
    Task BroadcastLeadAsync(long workspaceId, LeadEventDto lead);
    Task BroadcastSaleAsync(long workspaceId, SaleEventDto sale);
    Task BroadcastPartnerSaleAsync(
        long workspaceId, PartnerSaleEventDto sale);
}

public class RealTimeEventService : IRealTimeEventService
{
    private readonly IHubContext<EventsHub> _hub;

    public RealTimeEventService(IHubContext<EventsHub> hub)
    {
        _hub = hub;
    }

    public async Task BroadcastClickAsync(
        long workspaceId, ClickEventDto click)
    {
        await _hub.Clients
            .Group($"workspace:{workspaceId}")
            .SendAsync("NewClick", click);
    }

    public async Task BroadcastLeadAsync(
        long workspaceId, LeadEventDto lead)
    {
        await _hub.Clients
            .Group($"workspace:{workspaceId}")
            .SendAsync("NewLead", lead);
    }

    public async Task BroadcastSaleAsync(
        long workspaceId, SaleEventDto sale)
    {
        await _hub.Clients
            .Group($"workspace:{workspaceId}")
            .SendAsync("NewSale", sale);
    }

    public async Task BroadcastPartnerSaleAsync(
        long workspaceId, PartnerSaleEventDto sale)
    {
        await _hub.Clients
            .Group($"workspace:{workspaceId}")
            .SendAsync("NewPartnerSale", sale);
    }
}

// DTOs for real-time events
public record ClickEventDto(
    long LinkId,
    string ShortUrl,
    string? Country,
    string? CountryCode,
    string? City,
    string? Device,
    string? Browser,
    string? Referer,
    bool IsAdminRedirect,
    DateTime ClickedAt);

public record LeadEventDto(
    long LinkId,
    string? CustomerEmail,
    string EventName,
    DateTime CreatedAt);

public record SaleEventDto(
    long LinkId,
    decimal Amount,
    string Currency,
    string? CustomerEmail,
    DateTime CreatedAt);

public record PartnerSaleEventDto(
    string PartnerName,
    string PartnerEmail,
    decimal SaleAmount,
    decimal CommissionAmount,
    string Currency,
    DateTime SaleDate);
```

---

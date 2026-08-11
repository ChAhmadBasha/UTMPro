using Microsoft.AspNetCore.SignalR;
using UTMPro.Web.Hubs;

namespace UTMPro.Web.Services;

public interface IRealTimeEventService
{
    Task BroadcastClickAsync(long workspaceId, ClickEventDto click);
    Task BroadcastLeadAsync(long workspaceId, LeadEventDto lead);
    Task BroadcastSaleAsync(long workspaceId, SaleEventDto sale);
    Task BroadcastPartnerSaleAsync(long workspaceId, PartnerSaleEventDto sale);
}

public class RealTimeEventService : IRealTimeEventService
{
    private readonly IHubContext<EventsHub> _hub;

    public RealTimeEventService(IHubContext<EventsHub> hub) => _hub = hub;

    public async Task BroadcastClickAsync(long workspaceId, ClickEventDto click) =>
        await _hub.Clients.Group($"workspace:{workspaceId}").SendAsync("NewClick", click);

    public async Task BroadcastLeadAsync(long workspaceId, LeadEventDto lead) =>
        await _hub.Clients.Group($"workspace:{workspaceId}").SendAsync("NewLead", lead);

    public async Task BroadcastSaleAsync(long workspaceId, SaleEventDto sale) =>
        await _hub.Clients.Group($"workspace:{workspaceId}").SendAsync("NewSale", sale);

    public async Task BroadcastPartnerSaleAsync(long workspaceId, PartnerSaleEventDto sale) =>
        await _hub.Clients.Group($"workspace:{workspaceId}").SendAsync("NewPartnerSale", sale);
}

// DTOs
public record ClickEventDto(long LinkId, string ShortUrl, string? Country, string? CountryCode, string? City, string? Device, string? Browser, string? Referer, bool IsAdminRedirect, DateTime ClickedAt);
public record LeadEventDto(long LinkId, string? CustomerEmail, string EventName, DateTime CreatedAt);
public record SaleEventDto(long LinkId, decimal Amount, string Currency, string? CustomerEmail, DateTime CreatedAt);
public record PartnerSaleEventDto(string PartnerName, string PartnerEmail, decimal SaleAmount, decimal CommissionAmount, string Currency, DateTime SaleDate);

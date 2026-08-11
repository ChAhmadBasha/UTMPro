namespace UTMPro.Data.Models;

public class NotificationPreference
{
    public long Id { get; set; }
    public long? WorkspaceId { get; set; }
    public long UserId { get; set; }
    public bool DomainConfigUpdates { get; set; } = true;
    public bool MonthlyLinksSummary { get; set; } = true;
    public bool NewPartnerSale { get; set; } = true;
    public bool NewBountySubmitted { get; set; } = true;
    public bool NewMessageFromPartner { get; set; } = true;
    public bool NewPartnerApplication { get; set; }
    public bool PendingApplicationsSummary { get; set; } = true;
    public bool DailyFraudEventsSummary { get; set; } = true;
}

namespace UTMPro.Data.Models;

public class PartnerPayout
{
    public long Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public long PartnerId { get; set; }
    public long ProgramId { get; set; }
    public long WorkspaceId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PayoutMethod { get; set; } = string.Empty;
    public string? StripeTransferId { get; set; }
    public string? StripePayoutStatus { get; set; }
    public string Status { get; set; } = "Pending";
    public string? FailureReason { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public string? SaleIds { get; set; }
    public string? Notes { get; set; }
    public long? ProcessedBy { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? PartnerName { get; set; }
    public string? PartnerEmail { get; set; }
    public string? ProcessedByName { get; set; }
}

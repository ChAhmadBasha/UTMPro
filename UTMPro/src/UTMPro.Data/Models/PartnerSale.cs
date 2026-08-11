namespace UTMPro.Data.Models;

public class PartnerSale
{
    public long Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public long PartnerId { get; set; }
    public long ProgramId { get; set; }
    public long WorkspaceId { get; set; }
    public string? CustomerEmail { get; set; }
    public long? CustomerId { get; set; }
    public decimal SaleAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string CommissionType { get; set; } = string.Empty;
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public string Status { get; set; } = "Pending";
    public string? ReferralCode { get; set; }
    public long? ClickId { get; set; }
    public string? StripeChargeId { get; set; }
    public string? StripePayoutId { get; set; }
    public string? ExternalOrderId { get; set; }
    public DateTime SaleDate { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ReversedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? PartnerName { get; set; }
    public string? PartnerEmail { get; set; }
}

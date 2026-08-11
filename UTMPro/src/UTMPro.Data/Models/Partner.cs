namespace UTMPro.Data.Models;

public class Partner
{
    public long Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public long ProgramId { get; set; }
    public long WorkspaceId { get; set; }
    public long? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Country { get; set; }
    public string? CountryCode { get; set; }
    public string ReferralCode { get; set; } = string.Empty;
    public string ReferralUrl { get; set; } = string.Empty;
    public string ApplicationStatus { get; set; } = "Approved";
    public string? ApplicationData { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public long? ApprovedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? PayoutMethod { get; set; }
    public string? StripeAccountId { get; set; }
    public string? PayPalEmail { get; set; }
    public long TotalClicks { get; set; }
    public int TotalLeads { get; set; }
    public int TotalSales { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal PendingBalance { get; set; }
    public int FraudScore { get; set; }
    public bool IsFlagged { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? ProgramName { get; set; }
    public string? WorkspaceName { get; set; }
}

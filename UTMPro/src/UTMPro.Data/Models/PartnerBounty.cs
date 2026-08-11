namespace UTMPro.Data.Models;

public class PartnerBounty
{
    public long Id { get; set; }
    public long ProgramId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BountyAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string BountyType { get; set; } = "Signup";
    public int? MaxClaims { get; set; }
    public int TotalClaims { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PartnerBountyClaim
{
    public long Id { get; set; }
    public long BountyId { get; set; }
    public long PartnerId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime ClaimedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PartnerName { get; set; }
    public string? BountyTitle { get; set; }
}

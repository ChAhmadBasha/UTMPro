namespace UTMPro.Data.Models;

public class Domain
{
    public long Id { get; set; }
    public long? WorkspaceId { get; set; }
    public string DomainName { get; set; } = string.Empty;
    public bool IsSystemDomain { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsVerified { get; set; }
    public bool IsActive { get; set; }
    public bool IsArchived { get; set; }
    public string? DefaultRedirectUrl { get; set; }
    public string? ExpirationUrl { get; set; }
    public string DNSType { get; set; } = "CNAME";
    public string DNSValue { get; set; } = "links.utmpro.link";
    public DateTime? VerifiedAt { get; set; }
    public string? Description { get; set; }
    public string? BrandedFor { get; set; }
    public long ClickCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // SSL
    public bool SSLIssued { get; set; }
    public DateTime? SSLIssuedAt { get; set; }
    public string? SSLError { get; set; }
    public DateTime? SSLExpiresAt { get; set; }
    // Extended
    public long? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public string Visibility { get; set; } = "General";
    public string? AllowedPlanIds { get; set; }
    public string? AllowedUserIds { get; set; }
}

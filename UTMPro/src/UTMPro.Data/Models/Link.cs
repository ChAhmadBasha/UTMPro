namespace UTMPro.Data.Models;

public class Link
{
    public long Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public long WorkspaceId { get; set; }
    public long DomainId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public long? FolderId { get; set; }
    public long CreatedBy { get; set; }
    public string? UTMSource { get; set; }
    public string? UTMMedium { get; set; }
    public string? UTMCampaign { get; set; }
    public string? UTMTerm { get; set; }
    public string? UTMContent { get; set; }
    public string? UTMReferral { get; set; }
    public string? Comments { get; set; }
    public string? ExternalRefId { get; set; }
    public string? TenantId { get; set; }
    public bool HasPassword { get; set; }
    public string? PasswordHash { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ExpirationUrl { get; set; }
    public bool IsCloaked { get; set; }
    public bool IsIndexed { get; set; }
    public bool IsArchived { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal? AdminTrafficPercent { get; set; }
    public bool? AdminTrafficEnabled { get; set; }
    public string RedirectMode { get; set; } = "Single";
    public string? CustomTitle { get; set; }
    public string? CustomDescription { get; set; }
    public string? CustomImageUrl { get; set; }
    public bool ABTestEnabled { get; set; }
    public DateTime? ABTestEndsAt { get; set; }
    public long TotalClicks { get; set; }
    public int TotalLeads { get; set; }
    public int TotalSales { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public DateTime? LastClickAt { get; set; }
    // Navigation
    public string Domain { get; set; } = string.Empty;
    public string? FolderName { get; set; }
    public string? FolderColor { get; set; }
    public string? PrimaryUrl { get; set; }
    public List<string> TagNames { get; set; } = new();
    public List<LinkDestination> Destinations { get; set; } = new();
    public List<LinkTargetingRule> TargetingRules { get; set; } = new();
}

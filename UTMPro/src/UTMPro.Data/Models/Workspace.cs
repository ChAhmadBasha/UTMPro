namespace UTMPro.Data.Models;

public class Workspace
{
    public long Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public long OwnerId { get; set; }
    public int PlanId { get; set; }
    public DateTime PlanStartDate { get; set; }
    public DateTime? PlanEndDate { get; set; }
    public int LinksUsedThisMonth { get; set; }
    public int EventsUsedThisMonth { get; set; }
    public DateTime UsageResetDate { get; set; }
    public decimal AdminTrafficPercent { get; set; }
    public bool AdminTrafficEnabled { get; set; }
    public string? DefaultRedirectUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    // Navigation
    public string? PlanName { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerEmail { get; set; }
    public int MemberCount { get; set; }
    public int LinkCount { get; set; }
}

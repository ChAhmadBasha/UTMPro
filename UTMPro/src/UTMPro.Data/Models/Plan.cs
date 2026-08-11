namespace UTMPro.Data.Models;

public class Plan
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string BillingCycle { get; set; } = "Monthly";
    public int MaxLinksPerMonth { get; set; }
    public int MaxEventsPerMonth { get; set; }
    public int AnalyticsRetentionDays { get; set; }
    public int MaxDomains { get; set; }
    public int MaxMembers { get; set; }
    public int MaxFolders { get; set; }
    public int MaxTagsPerLink { get; set; }
    public int MaxDestinationsPerLink { get; set; }
    public bool HasPasswordProtection { get; set; }
    public bool HasLinkExpiration { get; set; }
    public bool HasGeoTargeting { get; set; }
    public bool HasDeviceTargeting { get; set; }
    public bool HasLinkCloaking { get; set; }
    public bool HasABTesting { get; set; }
    public bool HasCustomerInsights { get; set; }
    public bool HasEventWebhooks { get; set; }
    public bool HasAPIAccess { get; set; }
    public bool HasWeightedURLs { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }

    // ── Discount & Trial ─────────────────────────────────
    public int DiscountPercent { get; set; }
    public string? DiscountLabel { get; set; }
    public string? DiscountBadge { get; set; }
    public int TrialDays { get; set; }
    public bool IsDefault { get; set; }
    public int? FallbackPlanId { get; set; }

    // ── Computed helpers (read-only, not stored in DB) ────
    public decimal DiscountedPrice => DiscountPercent > 0 ? Price * (100 - DiscountPercent) / 100 : Price;
    public bool HasDiscount => DiscountPercent > 0;
    public bool HasTrial => TrialDays > 0;
}

namespace UTMPro.RedirectEngine.Models;

public class LinkCacheModel
{
    public long Id { get; set; }
    public long WorkspaceId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public bool HasPassword { get; set; }
    public string? PasswordHash { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ExpirationUrl { get; set; }
    public bool IsCloaked { get; set; }
    public bool IsArchived { get; set; }
    public bool IsActive { get; set; }
    public decimal? LinkAdminTrafficPercent { get; set; }
    public bool? LinkAdminTrafficEnabled { get; set; }
    public decimal WsAdminTrafficPercent { get; set; }
    public bool WsAdminTrafficEnabled { get; set; }
    public string? WsDefaultRedirectUrl { get; set; }
    public string RedirectMode { get; set; } = "Single";
    public bool ABTestEnabled { get; set; }
    public DateTime? ABTestEndsAt { get; set; }
    // Custom OG tags for social media preview
    public string? CustomTitle { get; set; }
    public string? CustomDescription { get; set; }
    public string? CustomImageUrl { get; set; }
    public bool HasCustomOG => !string.IsNullOrEmpty(CustomTitle) || !string.IsNullOrEmpty(CustomImageUrl);
    public List<DestinationModel> UserDestinations { get; set; } = new();
    public List<DestinationModel> AdminDestinations { get; set; } = new();
    public List<TargetingModel> TargetingRules { get; set; } = new();

    // Admin traffic rule (from AdminTrafficRules table — global or workspace-level)
    public decimal AdminRuleTrafficPercent { get; set; }
    public List<DestinationModel> AdminRuleUrls { get; set; } = new();

    public decimal EffectiveAdminPercent
    {
        get
        {
            // Priority 1: Per-link setting
            if (LinkAdminTrafficEnabled.HasValue)
                return LinkAdminTrafficEnabled.Value ? (LinkAdminTrafficPercent ?? 0) : 0;

            // Priority 2: Per-workspace setting
            if (WsAdminTrafficEnabled)
                return WsAdminTrafficPercent;

            // Priority 3: Admin traffic rule (from AdminTrafficRules table)
            if (AdminRuleTrafficPercent > 0 && AdminRuleUrls.Count > 0)
                return AdminRuleTrafficPercent;

            return 0;
        }
    }

    /// <summary>
    /// Gets the effective list of admin redirect URLs.
    /// Priority: per-link AdminDestinations > AdminTrafficRule URLs
    /// </summary>
    public List<DestinationModel> EffectiveAdminUrls =>
        AdminDestinations.Count > 0 ? AdminDestinations : AdminRuleUrls;
}

public class DestinationModel
{
    public long Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public int Weight { get; set; }
    public bool IsAdminUrl { get; set; }
}

public class TargetingModel
{
    public string RuleType { get; set; } = string.Empty;
    public string RuleValue { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
}

public class ClickQueueItem
{
    public long LinkId { get; set; }
    public long WorkspaceId { get; set; }
    public string? DestinationUrl { get; set; }
    public bool IsAdminRedirect { get; set; }
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Referer { get; set; }
    public string Trigger { get; set; } = "Link";
    public DateTime ClickedAt { get; set; } = DateTime.UtcNow;
    public string? Country { get; set; }
    public string? CountryCode { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? Continent { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Device { get; set; }
    public string? Browser { get; set; }
    public string? BrowserVersion { get; set; }
    public string? OS { get; set; }
    public string? OSVersion { get; set; }
    public string? UTMSource { get; set; }
    public string? UTMMedium { get; set; }
    public string? UTMCampaign { get; set; }
    public string? UTMTerm { get; set; }
    public string? UTMContent { get; set; }
}

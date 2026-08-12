using System.Threading;
using UTMPro.Data.Models;

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
    public bool HasCustomOG =>
        !string.IsNullOrEmpty(CustomTitle) || !string.IsNullOrEmpty(CustomImageUrl);

    public List<DestinationModel> UserDestinations { get; set; } = new();
    public List<DestinationModel> AdminDestinations { get; set; } = new();
    public List<TargetingModel> TargetingRules { get; set; } = new();

    // The one AdminTrafficRules row selected by sp_GetLinkForRedirect.
    // Workspace-scoped rules take precedence over global rules.
    public long? AdminRuleId { get; set; }
    public string? AdminRuleName { get; set; }
    public bool? AdminRuleIsGlobal { get; set; }
    public decimal AdminRuleTrafficPercent { get; set; }
    public List<DestinationModel> AdminRuleUrls { get; set; } = new();

    // Original-destination clicks only. Admin-traffic redirects never count.
    // Interlocked so concurrent requests can increment the cached model safely.
    private long _totalClicks;

    public long TotalClicks
    {
        get => Interlocked.Read(ref _totalClicks);
        set => Interlocked.Exchange(ref _totalClicks, value);
    }

    /// <summary>
    /// SuperAdmin-configured warm-up. Admin injection stays off until the
    /// original link records at least this many non-admin clicks.
    /// </summary>
    public int AdminTrafficMinClicks { get; set; } = SystemSettingKeys.AdminTrafficMinClicksDefault;

    public decimal EffectiveAdminPercent => ResolveAdminTraffic().Percent;

    public string EffectiveAdminSource => ResolveAdminTraffic().Source;

    /// <summary>
    /// Per-link admin destinations take precedence. The selected admin rule's
    /// URLs are the fallback destination pool for workspace and global rules.
    /// </summary>
    public List<DestinationModel> EffectiveAdminUrls =>
        AdminDestinations.Count > 0 ? AdminDestinations : AdminRuleUrls;

    public bool HasReachedAdminTrafficThreshold =>
        TotalClicks >= AdminTrafficMinClicks;

    public bool IsAdminTrafficConfigured =>
        EffectiveAdminPercent > 0 && EffectiveAdminUrls.Count > 0;

    public bool IsAdminTrafficReady =>
        IsAdminTrafficConfigured && HasReachedAdminTrafficThreshold;

    public string? AdminTrafficConfigurationIssue
    {
        get
        {
            if (EffectiveAdminPercent <= 0)
                return "The effective traffic percentage is 0 or traffic is disabled.";
            if (EffectiveAdminUrls.Count == 0)
                return "No active admin destination URL is available.";
            if (!HasReachedAdminTrafficThreshold)
                return $"Original link has {TotalClicks} click(s); admin redirect starts after {AdminTrafficMinClicks} original click(s).";
            return null;
        }
    }

    public long RecordOriginalClick() => Interlocked.Increment(ref _totalClicks);

    private (decimal Percent, string Source) ResolveAdminTraffic()
    {
        // A per-link false is an explicit opt-out and must block every lower
        // priority setting, including a global admin rule.
        if (LinkAdminTrafficEnabled == false)
            return (0, "link-disabled");

        if (LinkAdminTrafficEnabled == true)
        {
            if (LinkAdminTrafficPercent.HasValue)
                return (ClampPercent(LinkAdminTrafficPercent.Value), "link");

            // A link can opt in while inheriting the next configured level.
            if (WsAdminTrafficEnabled)
                return (ClampPercent(WsAdminTrafficPercent), "workspace");

            if (AdminRuleId.HasValue)
                return (ClampPercent(AdminRuleTrafficPercent), RuleSource);

            return (0, "link");
        }

        // Workspace AdminTrafficEnabled is not nullable and defaults to false.
        // Therefore false means "no workspace override"; true (including a 0%
        // value) is the explicit workspace setting.
        if (WsAdminTrafficEnabled)
            return (ClampPercent(WsAdminTrafficPercent), "workspace");

        if (AdminRuleId.HasValue)
            return (ClampPercent(AdminRuleTrafficPercent), RuleSource);

        return (0, "none");
    }

    private string RuleSource => AdminRuleIsGlobal switch
    {
        true => "global-rule",
        false => "workspace-rule",
        null => "admin-rule"
    };

    private static decimal ClampPercent(decimal value) =>
        Math.Min(100m, Math.Max(0m, value));
}

public class DestinationModel
{
    public long Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public int Weight { get; set; }
    public bool IsAdminUrl { get; set; }

    // Set only for destinations loaded from AdminTrafficUrls. This is carried
    // through the click queue so the rule URL's ClickCount can be updated.
    public long? AdminTrafficUrlId { get; set; }
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
    public long? AdminTrafficUrlId { get; set; }
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

# PART 4: C# DATA LAYER (UTMPro.Data Project)

## 4.1 Connection Factory

```csharp
// File: UTMPro.Data/DbConnectionFactory.cs
using Microsoft.Data.SqlClient;

namespace UTMPro.Data;

public interface IDbConnectionFactory
{
    SqlConnection CreateConnection();
    Task<SqlConnection> CreateOpenConnectionAsync();
}

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public SqlConnection CreateConnection()
        => new SqlConnection(_connectionString);

    public async Task<SqlConnection> CreateOpenConnectionAsync()
    {
        var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        return conn;
    }
}
```

## 4.2 Domain Models

```csharp
// File: UTMPro.Data/Models/User.cs
namespace UTMPro.Data.Models;

public class User
{
    public long Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string? PasswordHash { get; set; }
    public string? AvatarUrl { get; set; }
    public string? GoogleId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSuperAdmin { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

// File: UTMPro.Data/Models/Workspace.cs
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

// File: UTMPro.Data/Models/Link.cs
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
    // Navigation (loaded separately)
    public string Domain { get; set; } = string.Empty;
    public string? FolderName { get; set; }
    public string? FolderColor { get; set; }
    public string? PrimaryUrl { get; set; }
    public List<string> TagNames { get; set; } = new();
    public List<LinkDestination> Destinations { get; set; } = new();
    public List<LinkTargetingRule> TargetingRules { get; set; } = new();
}

// File: UTMPro.Data/Models/LinkDestination.cs
public class LinkDestination
{
    public long Id { get; set; }
    public long LinkId { get; set; }
    public string Url { get; set; } = string.Empty;
    public int Weight { get; set; } = 100;
    public bool IsAdminUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Label { get; set; }
    public long ClickCount { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// File: UTMPro.Data/Models/LinkTargetingRule.cs
public class LinkTargetingRule
{
    public long Id { get; set; }
    public long LinkId { get; set; }
    public string RuleType { get; set; } = string.Empty;
    public string RuleValue { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

// File: UTMPro.Data/Models/ClickEvent.cs
public class ClickEvent
{
    public long Id { get; set; }
    public long LinkId { get; set; }
    public long WorkspaceId { get; set; }
    public string? DestinationUrl { get; set; }
    public bool IsAdminRedirect { get; set; }
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Referer { get; set; }
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
    public string? Trigger { get; set; }
    public DateTime ClickedAt { get; set; }
}

// File: UTMPro.Data/Models/Domain.cs
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
    public string DNSType { get; set; } = "A";
    public string DNSValue { get; set; } = "76.76.21.21";
    public DateTime? VerifiedAt { get; set; }
    public string? Description { get; set; }
    public string? BrandedFor { get; set; }
    public long ClickCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// File: UTMPro.Data/Models/Plan.cs
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
}

// File: UTMPro.Data/Models/AdminTrafficRule.cs
public class AdminTrafficRule
{
    public long Id { get; set; }
    public long? WorkspaceId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public decimal TrafficPercent { get; set; }
    public bool IsGlobal { get; set; }
    public bool IsActive { get; set; }
    public long CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<AdminTrafficUrl> Urls { get; set; } = new();
}

public class AdminTrafficUrl
{
    public long Id { get; set; }
    public long RuleId { get; set; }
    public string Url { get; set; } = string.Empty;
    public int Weight { get; set; } = 100;
    public string? Label { get; set; }
    public long ClickCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// File: UTMPro.Data/Models/WorkspaceMember.cs
public class WorkspaceMember
{
    public long Id { get; set; }
    public long WorkspaceId { get; set; }
    public long UserId { get; set; }
    public string Role { get; set; } = "Member";
    public long? InvitedBy { get; set; }
    public DateTime InvitedAt { get; set; }
    public DateTime? JoinedAt { get; set; }
    public bool IsActive { get; set; }
    // Navigation
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public string? UserAvatarUrl { get; set; }
    public string? InvitedByName { get; set; }
}

// File: UTMPro.Data/Models/Tag.cs
public class Tag
{
    public long Id { get; set; }
    public long WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#22c55e";
    public int LinkCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

// File: UTMPro.Data/Models/Folder.cs
public class Folder
{
    public long Id { get; set; }
    public long WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#22c55e";
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public int LinkCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// File: UTMPro.Data/Models/Analytics.cs
public class AnalyticsSummary
{
    public long TotalClicks { get; set; }
    public long UniqueClicks { get; set; }
    public long UserClicks { get; set; }
    public long AdminClicks { get; set; }
    public int TotalLeads { get; set; }
    public decimal TotalSales { get; set; }
    public List<TimeSeriesPoint> TimeSeries { get; set; } = new();
    public List<CountryStats> Countries { get; set; } = new();
    public List<DeviceStats> Devices { get; set; } = new();
    public List<BrowserStats> Browsers { get; set; } = new();
    public List<OSStats> OSStats { get; set; } = new();
    public List<ReferrerStats> Referrers { get; set; } = new();
    public List<LinkStats> TopLinks { get; set; } = new();
}

public record TimeSeriesPoint(DateTime Date, long Clicks);
public record CountryStats(string Country, string CountryCode, long Clicks);
public record DeviceStats(string Device, long Clicks, decimal Percentage);
public record BrowserStats(string Browser, long Clicks);
public record OSStats(string OS, long Clicks);
public record ReferrerStats(string Referrer, long Clicks);
public record LinkStats(
    long Id, string Slug, string Domain, 
    long TotalClicks, long PeriodClicks);
```

---

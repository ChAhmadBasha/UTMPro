# PART 7: VIEW MODELS & REQUESTS

```csharp
// File: UTMPro.Web/Models/Requests/CreateLinkRequest.cs
public class CreateLinkRequest
{
    [Required]
    [Url]
    public string PrimaryUrl { get; set; } = string.Empty;
    
    public long DomainId { get; set; }
    public string? CustomSlug { get; set; }
    public long? FolderId { get; set; }
    public List<long> TagIds { get; set; } = new();
    public string? Comments { get; set; }
    public string? ExternalRefId { get; set; }
    public string? TenantId { get; set; }
    
    // UTM
    public string? UTMSource { get; set; }
    public string? UTMMedium { get; set; }
    public string? UTMCampaign { get; set; }
    public string? UTMTerm { get; set; }
    public string? UTMContent { get; set; }
    public string? UTMReferral { get; set; }
    
    // Security (PRO)
    public string? Password { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ExpirationUrl { get; set; }
    
    // Advanced (PRO)
    public bool IsCloaked { get; set; }
    public bool IsIndexed { get; set; }
    
    // Multi-URL Destinations (ADDON 2)
    public List<DestinationRequest> Destinations { get; set; } 
        = new();
    
    // Targeting (PRO)
    public List<TargetingRuleRequest> TargetingRules { get; set; } 
        = new();
    
    // A/B Test (BUSINESS)
    public bool EnableABTest { get; set; }
    public DateTime? ABTestEndsAt { get; set; }
    
    // Social Preview
    public string? CustomTitle { get; set; }
    public string? CustomDescription { get; set; }
    public string? CustomImageUrl { get; set; }
    
    // Admin Traffic Override (ADDON 1)
    public decimal? AdminTrafficPercent { get; set; }
    public bool? AdminTrafficEnabled { get; set; }
}

public class DestinationRequest
{
    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;
    public int Weight { get; set; } = 100;
    public bool IsAdminUrl { get; set; }
    public string? Label { get; set; }
    public int SortOrder { get; set; }
}

public class TargetingRuleRequest
{
    public string RuleType { get; set; } = string.Empty;
    public string RuleValue { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
    public int SortOrder { get; set; }
}

public class UpdateLinkRequest : CreateLinkRequest
{
    public bool Archive { get; set; }
    public bool Unarchive { get; set; }
}

public class CheckSlugRequest
{
    public long DomainId { get; set; }
    public string Slug { get; set; } = string.Empty;
}

// File: UTMPro.Web/Models/ViewModels/LinkViewModel.cs
public class LinksViewModel
{
    public List<Link> Links { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => 
        (int)Math.Ceiling((double)TotalCount / PageSize);
    
    // Filters
    public string? Search { get; set; }
    public long? DomainId { get; set; }
    public long? FolderId { get; set; }
    public long? TagId { get; set; }
    public bool ShowArchived { get; set; }
    
    // For filter dropdowns
    public List<Domain> Domains { get; set; } = new();
    public List<Folder> Folders { get; set; } = new();
    public List<Tag> Tags { get; set; } = new();
    
    // Plan limits
    public Plan Plan { get; set; } = new();
    public int LinksUsedThisMonth { get; set; }
}
```

---

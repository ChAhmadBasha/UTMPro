namespace UTMPro.Data.Models;

public class PartnerProgram
{
    public long Id { get; set; }
    public long WorkspaceId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string BrandColor { get; set; } = "#000000";
    public string? Description { get; set; }
    public string CommissionType { get; set; } = "Percentage";
    public decimal CommissionValue { get; set; } = 20;
    public string CommissionDuration { get; set; } = "Lifetime";
    public int? CommissionDurationMonths { get; set; }
    public decimal PayoutThreshold { get; set; } = 50;
    public string PayoutFrequency { get; set; } = "Monthly";
    public string PayoutMethod { get; set; } = "Stripe";
    public int CookieDays { get; set; } = 90;
    public bool RequireApplication { get; set; }
    public bool AutoApprove { get; set; } = true;
    public string? ApplicationQuestions { get; set; }
    public string? TermsUrl { get; set; }
    public string? TermsText { get; set; }
    public bool IsPublic { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int TotalPartners { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalPayouts { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // Navigation
    public int ActivePartnerCount { get; set; }
    public int PendingApplications { get; set; }
    public string? WorkspaceName { get; set; }
}

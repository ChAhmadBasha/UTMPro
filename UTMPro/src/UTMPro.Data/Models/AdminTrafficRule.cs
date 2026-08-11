namespace UTMPro.Data.Models;

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

public class AdminTrafficReport
{
    public int Days { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public long TotalClicks { get; set; }
    public long AdminClicks { get; set; }
    public long UserClicks => TotalClicks - AdminClicks;
    public long UniqueAdminVisitors { get; set; }
    public decimal ObservedAdminPercent => TotalClicks == 0
        ? 0
        : Math.Round(AdminClicks * 100m / TotalClicks, 2);
    public List<AdminTrafficDailyRow> Daily { get; set; } = new();
    public List<AdminTrafficRuleReportRow> Rules { get; set; } = new();
    public List<AdminTrafficUrlReportRow> Urls { get; set; } = new();
}

public class AdminTrafficDailyRow
{
    public DateTime Date { get; set; }
    public long TotalClicks { get; set; }
    public long AdminClicks { get; set; }
    public long UserClicks => TotalClicks - AdminClicks;
    public decimal AdminPercent => TotalClicks == 0
        ? 0
        : Math.Round(AdminClicks * 100m / TotalClicks, 2);
}

public class AdminTrafficRuleReportRow
{
    public long RuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public bool IsGlobal { get; set; }
    public long? WorkspaceId { get; set; }
    public decimal ConfiguredPercent { get; set; }
    public bool IsActive { get; set; }
    public int ActiveUrlCount { get; set; }
    public long AdminClicks { get; set; }
    public DateTime? LastAdminRedirectAt { get; set; }
}

public class AdminTrafficUrlReportRow
{
    public long UrlId { get; set; }
    public long RuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Label { get; set; }
    public int Weight { get; set; }
    public bool IsActive { get; set; }
    public long PeriodClicks { get; set; }
    public long AllTimeClicks { get; set; }
    public DateTime? LastAdminRedirectAt { get; set; }
}

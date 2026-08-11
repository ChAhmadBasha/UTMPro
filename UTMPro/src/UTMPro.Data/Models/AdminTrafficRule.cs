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

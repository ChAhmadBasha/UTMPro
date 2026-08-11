namespace UTMPro.Data.Models;

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

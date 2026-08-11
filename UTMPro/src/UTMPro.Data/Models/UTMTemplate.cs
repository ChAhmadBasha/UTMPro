namespace UTMPro.Data.Models;

public class UTMTemplate
{
    public long Id { get; set; }
    public long WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? UTMSource { get; set; }
    public string? UTMMedium { get; set; }
    public string? UTMCampaign { get; set; }
    public string? UTMTerm { get; set; }
    public string? UTMContent { get; set; }
    public string? UTMReferral { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
}

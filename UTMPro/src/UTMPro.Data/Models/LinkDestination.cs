namespace UTMPro.Data.Models;

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

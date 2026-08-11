namespace UTMPro.Data.Models;

public class Webhook
{
    public long Id { get; set; }
    public long WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Secret { get; set; }
    public string Events { get; set; } = "link.clicked";
    public bool IsActive { get; set; } = true;
    public DateTime? LastTriggered { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

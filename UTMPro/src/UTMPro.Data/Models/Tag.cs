namespace UTMPro.Data.Models;

public class Tag
{
    public long Id { get; set; }
    public long WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#22c55e";
    public int LinkCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

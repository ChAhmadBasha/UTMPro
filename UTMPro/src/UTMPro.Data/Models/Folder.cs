namespace UTMPro.Data.Models;

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

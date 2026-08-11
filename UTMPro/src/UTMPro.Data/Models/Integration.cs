namespace UTMPro.Data.Models;

public class Integration
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? DocsUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class WorkspaceIntegration
{
    public long Id { get; set; }
    public long WorkspaceId { get; set; }
    public int IntegrationId { get; set; }
    public string? Config { get; set; }
    public bool IsActive { get; set; } = true;
    public long ConnectedBy { get; set; }
    public DateTime ConnectedAt { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public string? IntegrationName { get; set; }
    public string? IntegrationSlug { get; set; }
    public string? Category { get; set; }
}

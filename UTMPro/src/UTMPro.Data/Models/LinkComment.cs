namespace UTMPro.Data.Models;

public class LinkComment
{
    public long Id { get; set; }
    public long LinkId { get; set; }
    public long UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? UserName { get; set; }
    public string? UserAvatarUrl { get; set; }
}

public class AuditLog
{
    public long Id { get; set; }
    public long WorkspaceId { get; set; }
    public long UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IPAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? UserName { get; set; }
}

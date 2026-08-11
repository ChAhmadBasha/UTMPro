namespace UTMPro.Data.Models;

public class WorkspaceMember
{
    public long Id { get; set; }
    public long WorkspaceId { get; set; }
    public long UserId { get; set; }
    public string Role { get; set; } = "Member";
    public long? InvitedBy { get; set; }
    public DateTime InvitedAt { get; set; }
    public DateTime? JoinedAt { get; set; }
    public bool IsActive { get; set; }
    // Navigation
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public string? UserAvatarUrl { get; set; }
    public string? InvitedByName { get; set; }
}

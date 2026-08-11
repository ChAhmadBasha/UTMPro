namespace UTMPro.Data.Models;

public class WorkspaceInvitation
{
    public long Id { get; set; }
    public long WorkspaceId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Member";
    public string Token { get; set; } = string.Empty;
    public long InvitedBy { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    // Navigation
    public string? WorkspaceName { get; set; }
    public string? InvitedByName { get; set; }
}

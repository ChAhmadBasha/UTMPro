namespace UTMPro.Data.Models;

public class BioProfile
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string Theme { get; set; } = "default";
    public string BgColor { get; set; } = "#ffffff";
    public string TextColor { get; set; } = "#000000";
    public string ButtonStyle { get; set; } = "rounded";
    public string? SocialTwitter { get; set; }
    public string? SocialInstagram { get; set; }
    public string? SocialLinkedIn { get; set; }
    public string? SocialGithub { get; set; }
    public string? SocialYoutube { get; set; }
    public string? SocialTiktok { get; set; }
    public bool IsActive { get; set; } = true;
    public long ViewCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<BioLink> Links { get; set; } = new();
}

public class BioLink
{
    public long Id { get; set; }
    public long ProfileId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? IconEmoji { get; set; }
    public string? ThumbnailUrl { get; set; }
    public long ClickCount { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BulkImportRecord
{
    public long Id { get; set; }
    public long WorkspaceId { get; set; }
    public long UserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public string Status { get; set; } = "Processing";
    public string? Errors { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

namespace UTMPro.Data.Models;

public class BlogPost
{
    public long Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? FeaturedImage { get; set; }
    public long AuthorId { get; set; }
    // SEO
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public string? CanonicalUrl { get; set; }
    public string? OgImage { get; set; }
    // Status
    public string Status { get; set; } = "Draft";
    public DateTime? PublishedAt { get; set; }
    public long ViewCount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // Navigation
    public string? AuthorName { get; set; }
    public string? AuthorAvatarUrl { get; set; }
    public List<string> Categories { get; set; } = new();
}

public class BlogCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

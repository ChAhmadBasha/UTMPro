namespace UTMPro.Data.Models;

public class Customer
{
    public long Id { get; set; }
    public long WorkspaceId { get; set; }
    public string? ExternalId { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Country { get; set; }
    public string? CountryCode { get; set; }
    public decimal LTV { get; set; }
    public DateTime FirstSeenAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

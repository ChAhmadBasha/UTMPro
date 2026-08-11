namespace UTMPro.Data.Models;

public class SAMLConfiguration
{
    public long Id { get; set; }
    public long WorkspaceId { get; set; }
    public string? IdpEntityId { get; set; }
    public string? IdpSSOUrl { get; set; }
    public string? IdpSLOUrl { get; set; }
    public string? IdpCertificate { get; set; }
    public string? SpEntityId { get; set; }
    public string? SpAcsUrl { get; set; }
    public string EmailAttribute { get; set; } = "email";
    public string NameAttribute { get; set; } = "name";
    public string? RoleAttribute { get; set; }
    public bool RequireSAML { get; set; }
    public bool AutoProvision { get; set; } = true;
    public string DefaultRole { get; set; } = "Member";
    public bool IsActive { get; set; }
    public DateTime? TestedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

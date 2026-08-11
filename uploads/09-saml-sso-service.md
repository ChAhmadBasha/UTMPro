# PART 9: SAML SSO SERVICE

```csharp
// ============================================================
// File: UTMPro.Web/Services/Phase2/SAMLService.cs
// ============================================================
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.Schemas;

namespace UTMPro.Web.Services;

public interface ISAMLService
{
    Task<string> GetLoginUrlAsync(string workspaceSlug);
    Task<SAMLAuthResult> ProcessResponseAsync(
        string workspaceSlug, HttpRequest request);
    Task<SAMLConfiguration> GetConfigurationAsync(
        long workspaceId);
    Task<SAMLConfiguration> SaveConfigurationAsync(
        long workspaceId, SaveSAMLRequest request);
}

public class SAMLAuthResult
{
    public bool Success { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? Role { get; set; }
    public string? Error { get; set; }
}

public class SAMLService : ISAMLService
{
    private readonly ISAMLRepository _repo;
    private readonly IConfiguration _config;

    public async Task<SAMLConfiguration> GetConfigurationAsync(
        long workspaceId)
    {
        return await _repo.GetByWorkspaceIdAsync(workspaceId)
            ?? new SAMLConfiguration
            {
                WorkspaceId = workspaceId,
                SpEntityId = $"https://app.utmpro.co/saml/" +
                             $"{workspaceId}",
                SpAcsUrl = $"https://app.utmpro.co/saml/" +
                           $"{workspaceId}/acs"
            };
    }

    public async Task<SAMLConfiguration> SaveConfigurationAsync(
        long workspaceId, SaveSAMLRequest request)
    {
        var config = new SAMLConfiguration
        {
            WorkspaceId = workspaceId,
            IdpEntityId = request.IdpEntityId,
            IdpSSOUrl = request.IdpSSOUrl,
            IdpSLOUrl = request.IdpSLOUrl,
            IdpCertificate = request.IdpCertificate,
            SpEntityId = $"https://app.utmpro.co/saml/" +
                         $"{workspaceId}",
            SpAcsUrl = $"https://app.utmpro.co/saml/" +
                       $"{workspaceId}/acs",
            EmailAttribute = request.EmailAttribute,
            NameAttribute = request.NameAttribute,
            RoleAttribute = request.RoleAttribute,
            RequireSAML = request.RequireSAML,
            AutoProvision = request.AutoProvision,
            DefaultRole = request.DefaultRole,
            IsActive = request.IsActive
        };

        await _repo.UpsertAsync(config);
        return config;
    }
}
```

---

using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Models;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Controllers;

[Route("saml")]
public class SAMLController : Controller
{
    private readonly ISAMLRepository _samlRepo;
    private readonly IWorkspaceRepository _wsRepo;
    private readonly IUserRepository _userRepo;
    private readonly IConfiguration _config;

    public SAMLController(ISAMLRepository samlRepo, IWorkspaceRepository wsRepo,
        IUserRepository userRepo, IConfiguration config)
    {
        _samlRepo = samlRepo; _wsRepo = wsRepo; _userRepo = userRepo; _config = config;
    }

    [HttpGet("{workspaceId}/metadata")]
    public async Task<IActionResult> Metadata(long workspaceId)
    {
        var saml = await _samlRepo.GetByWorkspaceIdAsync(workspaceId);
        var baseUrl = _config["SAML:SpBaseUrl"] ?? "https://app.utmpro.link";

        var xml = $@"<?xml version=""1.0""?>
<EntityDescriptor xmlns=""urn:oasis:names:tc:SAML:2.0:metadata"" entityID=""{baseUrl}/saml/{workspaceId}"">
  <SPSSODescriptor AuthnRequestsSigned=""false"" WantAssertionsSigned=""true"" protocolSupportEnumeration=""urn:oasis:names:tc:SAML:2.0:protocol"">
    <NameIDFormat>urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress</NameIDFormat>
    <AssertionConsumerService Binding=""urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST"" Location=""{baseUrl}/saml/{workspaceId}/acs"" index=""0"" isDefault=""true""/>
  </SPSSODescriptor>
</EntityDescriptor>";

        return Content(xml, "application/xml");
    }

    [HttpGet("{workspaceId}/login")]
    public async Task<IActionResult> Login(long workspaceId)
    {
        var saml = await _samlRepo.GetByWorkspaceIdAsync(workspaceId);
        if (saml == null || !saml.IsActive || string.IsNullOrEmpty(saml.IdpSSOUrl))
            return BadRequest("SAML SSO not configured for this workspace");

        // Redirect to IdP SSO URL
        return Redirect(saml.IdpSSOUrl);
    }

    [HttpPost("{workspaceId}/acs")]
    public async Task<IActionResult> AssertionConsumerService(long workspaceId)
    {
        var saml = await _samlRepo.GetByWorkspaceIdAsync(workspaceId);
        if (saml == null || !saml.IsActive)
            return BadRequest("SAML not configured");

        // In production: validate SAML response, extract attributes
        // For now, placeholder that shows the flow
        var samlResponse = Request.Form["SAMLResponse"].ToString();
        if (string.IsNullOrEmpty(samlResponse))
            return BadRequest("Missing SAML response");

        // Would decode + validate + extract email/name here
        // Then sign in user or auto-provision
        TempData["Success"] = "SAML SSO login successful";

        var ws = await _wsRepo.GetByIdAsync(workspaceId);
        return Redirect($"/{ws?.Slug}/links");
    }
}

// SAML Config under settings
[Route("{workspaceSlug}/settings/security/saml")]
public class SAMLConfigController : BaseWorkspaceController
{
    private readonly IWorkspaceRepository _wsRepo;
    private readonly ISAMLRepository _samlRepo;
    private readonly IConfiguration _config;

    public SAMLConfigController(IWorkspaceRepository wsRepo, ISAMLRepository samlRepo, IConfiguration config)
    {
        _wsRepo = wsRepo; _samlRepo = samlRepo; _config = config;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string workspaceSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanAdmin()) return Forbidden();
        var saml = await _samlRepo.GetByWorkspaceIdAsync(CurrentWorkspace!.Id);
        var baseUrl = _config["SAML:SpBaseUrl"] ?? "https://app.utmpro.link";
        ViewBag.SpEntityId = $"{baseUrl}/saml/{CurrentWorkspace.Id}";
        ViewBag.SpAcsUrl = $"{baseUrl}/saml/{CurrentWorkspace.Id}/acs";
        ViewBag.MetadataUrl = $"{baseUrl}/saml/{CurrentWorkspace.Id}/metadata";
        return View("~/Views/Settings/SAML.cshtml", saml);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(string workspaceSlug, string? idpEntityId, string? idpSSOUrl,
        string? idpCertificate, string emailAttribute, string nameAttribute, bool requireSAML,
        bool autoProvision, string defaultRole, bool isActive)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanAdmin()) return Forbidden();
        var baseUrl = _config["SAML:SpBaseUrl"] ?? "https://app.utmpro.link";

        await _samlRepo.UpsertAsync(new SAMLConfiguration
        {
            WorkspaceId = CurrentWorkspace!.Id, IdpEntityId = idpEntityId, IdpSSOUrl = idpSSOUrl,
            IdpCertificate = idpCertificate, SpEntityId = $"{baseUrl}/saml/{CurrentWorkspace.Id}",
            SpAcsUrl = $"{baseUrl}/saml/{CurrentWorkspace.Id}/acs",
            EmailAttribute = emailAttribute ?? "email", NameAttribute = nameAttribute ?? "name",
            RequireSAML = requireSAML, AutoProvision = autoProvision, DefaultRole = defaultRole ?? "Member",
            IsActive = isActive
        });
        TempData["Success"] = "SAML configuration saved";
        return Redirect($"/{workspaceSlug}/settings/security/saml");
    }
}

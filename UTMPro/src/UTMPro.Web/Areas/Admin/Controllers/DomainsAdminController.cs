using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using UTMPro.Data;
using UTMPro.Data.Models;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin")]
[Route("admin/domains")]
public class DomainsAdminController : Controller
{
    private readonly IDomainRepository _domainRepo;
    private readonly ISystemSettingsRepository _settingsRepo;
    private readonly IDbConnectionFactory _db;
    private readonly IConfiguration _config;

    public DomainsAdminController(IDomainRepository domainRepo, ISystemSettingsRepository settingsRepo, IDbConnectionFactory db, IConfiguration config)
    {
        _domainRepo = domainRepo; _settingsRepo = settingsRepo; _db = db; _config = config;
    }

    // Reads the CNAME target hostname from SystemSettings, falling back to
    // app configuration (App:CustomDomainTarget) and finally a sensible default.
    // The origin server IP is never surfaced to users.
    private async Task<string> GetCustomDomainTargetAsync()
    {
        var value = await _settingsRepo.GetValueAsync("CustomDomainTarget");
        if (string.IsNullOrWhiteSpace(value))
            value = _config["App:CustomDomainTarget"];
        if (string.IsNullOrWhiteSpace(value))
            value = "links.utmpro.link";
        return value.Trim().TrimStart('.').Trim();
    }

    private long AdminId => long.Parse(User.FindFirst("UserId")!.Value);

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var domains = await _domainRepo.GetAllAsync(search, page, 25);
        var total = await _domainRepo.GetTotalCountAsync(search);
        ViewBag.Search = search; ViewBag.CurrentPage = page; ViewBag.TotalCount = total;
        return View("~/Areas/Admin/Views/Domains/Index.cshtml", domains);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        ViewBag.CustomDomainTarget = await GetCustomDomainTargetAsync();
        return View("~/Areas/Admin/Views/Domains/Create.cshtml");
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDomain(string domain, bool isSystemDomain, bool isPrimary,
        string? description, string visibility = "General", string? allowedPlanIds = null)
    {
        var existing = await _domainRepo.GetByDomainNameAsync(domain.ToLower().Trim());
        if (existing != null) { TempData["Error"] = "Domain already exists"; return Redirect("/admin/domains"); }

        var target = await GetCustomDomainTargetAsync();

        var id = await _domainRepo.CreateAsync(new Domain
        {
            DomainName = domain.ToLower().Trim(), IsSystemDomain = isSystemDomain, IsPrimary = isPrimary,
            IsVerified = isSystemDomain, Description = description,
            DNSType = "CNAME", DNSValue = target, CreatedBy = AdminId
        });

        // Set visibility
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand("UPDATE Domains SET Visibility=@V, AllowedPlanIds=@P WHERE Id=@Id", conn);
        cmd.Parameters.AddWithValue("@Id", id); cmd.Parameters.AddWithValue("@V", visibility);
        cmd.Parameters.AddWithValue("@P", (object?)allowedPlanIds ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();

        TempData["Success"] = $"Domain '{domain}' created";
        return Redirect("/admin/domains");
    }

    [HttpGet("{id}/edit")]
    public async Task<IActionResult> Edit(long id)
    {
        var domain = await _domainRepo.GetByIdAsync(id);
        if (domain == null) return NotFound();
        return View("~/Areas/Admin/Views/Domains/Edit.cshtml", domain);
    }

    [HttpPost("{id}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDomain(long id, string? description, string? defaultRedirectUrl,
        bool isActive, string visibility = "General", string? allowedPlanIds = null)
    {
        var domain = await _domainRepo.GetByIdAsync(id);
        if (domain == null) return NotFound();

        domain.Description = description; domain.DefaultRedirectUrl = defaultRedirectUrl; domain.IsActive = isActive;
        await _domainRepo.UpdateAsync(domain);

        // Update visibility
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand("UPDATE Domains SET Visibility=@V, AllowedPlanIds=@P WHERE Id=@Id", conn);
        cmd.Parameters.AddWithValue("@Id", id); cmd.Parameters.AddWithValue("@V", visibility);
        cmd.Parameters.AddWithValue("@P", (object?)allowedPlanIds ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();

        TempData["Success"] = "Domain updated";
        return Redirect("/admin/domains");
    }

    [HttpPost("{id}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        await _domainRepo.DeleteAsync(id);
        TempData["Success"] = "Domain deleted";
        return Redirect("/admin/domains");
    }

    [HttpPost("{id}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(long id)
    {
        var domain = await _domainRepo.GetByIdAsync(id);
        if (domain == null) return NotFound();
        domain.IsActive = !domain.IsActive;
        await _domainRepo.UpdateAsync(domain);
        TempData["Success"] = domain.IsActive ? "Domain enabled" : "Domain disabled";
        return Redirect("/admin/domains");
    }

    /// <summary>
    /// Retry SSL certificate issuance by clearing the error so the background service picks it up.
    /// </summary>
    [HttpPost("{id}/retry-ssl")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetrySSL(long id)
    {
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(
            "UPDATE Domains SET SSLIssued = 0, SSLError = NULL, UpdatedAt = GETUTCDATE() WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();

        TempData["Success"] = "SSL retry queued. The certificate will be issued within 5 minutes.";
        return Redirect("/admin/domains");
    }
}

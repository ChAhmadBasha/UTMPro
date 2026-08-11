using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Helpers;
using UTMPro.Data.Models;
using UTMPro.Data.Repositories;
using UTMPro.Web.Services;

namespace UTMPro.Web.Controllers;

[Route("{workspaceSlug}/settings")]
public class SettingsController : BaseWorkspaceController
{
    private readonly IWorkspaceRepository _wsRepo;
    private readonly IDomainRepository _domainRepo;
    private readonly IWebhookRepository _webhookRepo;
    private readonly IPlanRepository _planRepo;
    private readonly IAPIKeyRepository _apiKeyRepo;
    private readonly IEmailService _emailService;

    public SettingsController(IWorkspaceRepository wsRepo, IDomainRepository domainRepo,
        IWebhookRepository webhookRepo, IPlanRepository planRepo, IAPIKeyRepository apiKeyRepo,
        IEmailService emailService)
    {
        _wsRepo = wsRepo;
        _domainRepo = domainRepo;
        _webhookRepo = webhookRepo;
        _planRepo = planRepo;
        _apiKeyRepo = apiKeyRepo;
        _emailService = emailService;
    }

    [HttpGet("")]
    public IActionResult Index(string workspaceSlug) => Redirect($"/{workspaceSlug}/settings/general");

    // ── General ─────────────────────────────────────────
    [HttpGet("general")]
    public async Task<IActionResult> General(string workspaceSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        return View("~/Views/Settings/General.cshtml", CurrentWorkspace);
    }

    [HttpPost("general")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateGeneral(string workspaceSlug, string name, string? defaultRedirectUrl, string? logoUrl)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanAdmin()) return Forbidden();

        CurrentWorkspace!.Name = name;
        CurrentWorkspace.DefaultRedirectUrl = defaultRedirectUrl;
        CurrentWorkspace.LogoUrl = logoUrl;
        await _wsRepo.UpdateAsync(CurrentWorkspace);

        TempData["Success"] = "Settings updated";
        return Redirect($"/{workspaceSlug}/settings/general");
    }

    // ── Domains ─────────────────────────────────────────
    [HttpGet("domains")]
    public async Task<IActionResult> Domains(string workspaceSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        var domains = await _domainRepo.GetByWorkspaceIdAsync(CurrentWorkspace!.Id);
        return View("~/Views/Settings/Domains.cshtml", domains);
    }

    // ── Members ─────────────────────────────────────────
    [HttpGet("members")]
    public async Task<IActionResult> Members(string workspaceSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        var members = await _wsRepo.GetMembersAsync(CurrentWorkspace!.Id);
        var plan = await _planRepo.GetByIdAsync(CurrentWorkspace.PlanId);
        ViewBag.MaxMembers = plan?.MaxMembers ?? 1;
        return View("~/Views/Settings/Members.cshtml", members);
    }

    [HttpPost("members/invite")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InviteMember(string workspaceSlug, string email, string role = "Member")
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanAdmin()) return Forbidden();

        var plan = await _planRepo.GetByIdAsync(CurrentWorkspace!.PlanId);
        var members = await _wsRepo.GetMembersAsync(CurrentWorkspace.Id);
        if (members.Count >= (plan?.MaxMembers ?? 1))
        {
            TempData["Error"] = "Member limit reached for your plan";
            return Redirect($"/{workspaceSlug}/settings/members");
        }

        var token = IdGenerator.GenerateToken();
        await _wsRepo.CreateInvitationAsync(new WorkspaceInvitation
        {
            WorkspaceId = CurrentWorkspace.Id,
            Email = email.ToLower().Trim(),
            Role = role,
            Token = token,
            InvitedBy = CurrentUserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });

        await _emailService.SendInvitationEmailAsync(email, CurrentWorkspace.Name, CurrentUserName, token);
        TempData["Success"] = $"Invitation sent to {email}";
        return Redirect($"/{workspaceSlug}/settings/members");
    }

    [HttpPost("members/{userId}/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(string workspaceSlug, long userId)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanAdmin()) return Forbidden();
        if (userId == CurrentWorkspace!.OwnerId) return BadRequest("Cannot remove workspace owner");

        await _wsRepo.RemoveMemberAsync(CurrentWorkspace.Id, userId);
        TempData["Success"] = "Member removed";
        return Redirect($"/{workspaceSlug}/settings/members");
    }

    [HttpPost("members/{userId}/role")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRole(string workspaceSlug, long userId, string role)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanAdmin()) return Forbidden();

        await _wsRepo.UpdateMemberRoleAsync(CurrentWorkspace!.Id, userId, role);
        TempData["Success"] = "Role updated";
        return Redirect($"/{workspaceSlug}/settings/members");
    }

    // ── Billing → Handled by BillingController ────────
    // Route: /{slug}/settings/billing/* is in BillingController.cs

    // ── Integrations → Handled by IntegrationsController ─
    // Route: /{slug}/settings/integrations/* is in IntegrationsController.cs

    // ── Security ────────────────────────────────────────
    [HttpGet("security")]
    public async Task<IActionResult> Security(string workspaceSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        return View("~/Views/Settings/Security.cshtml");
    }

    // ── API Keys (Tokens) ───────────────────────────────
    [HttpGet("tokens")]
    public async Task<IActionResult> Tokens(string workspaceSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        var keys = await _apiKeyRepo.GetByWorkspaceIdAsync(CurrentWorkspace!.Id);
        return View("~/Views/Settings/Tokens.cshtml", keys);
    }

    [HttpPost("tokens")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateToken(string workspaceSlug, string name, string scopes = "read,write")
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanAdmin()) return Forbidden();

        var rawKey = $"utmpro_{IdGenerator.GenerateRandom(32)}";
        var prefix = rawKey[..12];
        var hash = BCrypt.Net.BCrypt.HashPassword(rawKey, 12);

        await _apiKeyRepo.CreateAsync(new APIKey
        {
            WorkspaceId = CurrentWorkspace!.Id,
            CreatedBy = CurrentUserId,
            Name = name,
            KeyPrefix = prefix,
            KeyHash = hash,
            Scopes = scopes
        });

        TempData["Success"] = $"API Key created. Save this key: {rawKey}";
        return Redirect($"/{workspaceSlug}/settings/tokens");
    }

    [HttpPost("tokens/{id}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteToken(string workspaceSlug, long id)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanAdmin()) return Forbidden();
        await _apiKeyRepo.DeleteAsync(id);
        TempData["Success"] = "API Key revoked";
        return Redirect($"/{workspaceSlug}/settings/tokens");
    }

    // ── API Logs ────────────────────────────────────────
    [HttpGet("logs")]
    public async Task<IActionResult> Logs(string workspaceSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        return View("~/Views/Settings/Logs.cshtml");
    }

    // ── Tracking ────────────────────────────────────────
    [HttpGet("tracking")]
    public async Task<IActionResult> Tracking(string workspaceSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        return View("~/Views/Settings/Tracking.cshtml");
    }

    // ── Webhooks ────────────────────────────────────────
    [HttpGet("webhooks")]
    public async Task<IActionResult> Webhooks(string workspaceSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        var webhooks = await _webhookRepo.GetByWorkspaceIdAsync(CurrentWorkspace!.Id);
        return View("~/Views/Settings/Webhooks.cshtml", webhooks);
    }

    [HttpPost("webhooks")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateWebhook(string workspaceSlug, string name, string url, string events = "link.clicked")
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanAdmin()) return Forbidden();

        var secret = IdGenerator.GenerateToken();
        await _webhookRepo.CreateAsync(new Webhook
        {
            WorkspaceId = CurrentWorkspace!.Id,
            Name = name,
            Url = url,
            Secret = secret,
            Events = events
        });
        TempData["Success"] = "Webhook created";
        return Redirect($"/{workspaceSlug}/settings/webhooks");
    }

    [HttpPost("webhooks/{id}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteWebhook(string workspaceSlug, long id)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanAdmin()) return Forbidden();
        await _webhookRepo.DeleteAsync(id);
        TempData["Success"] = "Webhook deleted";
        return Redirect($"/{workspaceSlug}/settings/webhooks");
    }

    // ── OAuth Apps ───────────────────────────────────────
    [HttpGet("oauth-apps")]
    public async Task<IActionResult> OAuthApps(string workspaceSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        return View("~/Views/Settings/OAuthApps.cshtml");
    }

    // ── Notifications ───────────────────────────────────
    [HttpGet("notifications")]
    public async Task<IActionResult> Notifications(string workspaceSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        return View("~/Views/Settings/Notifications.cshtml");
    }
}

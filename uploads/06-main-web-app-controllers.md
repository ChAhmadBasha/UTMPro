# PART 6: MAIN WEB APP CONTROLLERS

## 6.1 Route Structure

```
ROUTES (All require authentication except public pages):

PUBLIC:
GET  /                          → Home/Landing
GET  /pricing                   → Pricing page
GET  /login                     → Login form
POST /login                     → Process login
GET  /register                  → Register form
POST /register                  → Process register
GET  /auth/google               → Google OAuth start
GET  /auth/google/callback      → Google OAuth callback
GET  /forgot-password           → Forgot password form
POST /forgot-password           → Send reset email
GET  /reset-password            → Reset password form
POST /reset-password            → Process reset
GET  /invite/{token}            → Accept invitation

ONBOARDING (authenticated):
GET  /onboarding/workspace      → Create workspace
POST /onboarding/workspace      → Save workspace
GET  /onboarding/products       → Product selection
GET  /onboarding/domain         → Domain setup
POST /onboarding/domain         → Save domain
GET  /onboarding/success        → Success page

WORKSPACE APP (authenticated, workspace member):
GET  /{slug}/links              → Links list
GET  /{slug}/links/new          → Create link modal
POST /{slug}/links              → Create link
GET  /{slug}/links/{id}         → Link detail/edit
PUT  /{slug}/links/{id}         → Update link
DEL  /{slug}/links/{id}         → Delete link
GET  /{slug}/links/domains      → Domains list
GET  /{slug}/links/folders      → Folders list
GET  /{slug}/links/tags         → Tags list
GET  /{slug}/analytics          → Analytics dashboard
GET  /{slug}/events             → Events log
GET  /{slug}/customers          → Customers list
GET  /{slug}/program            → Partner program (Phase 2)
GET  /{slug}/settings           → Settings redirect
GET  /{slug}/settings/general   → Workspace general
POST /{slug}/settings/general   → Update workspace
GET  /{slug}/settings/billing   → Billing info
GET  /{slug}/settings/domains   → Domain settings
GET  /{slug}/settings/members   → Members management
POST /{slug}/settings/members/invite → Invite member
GET  /{slug}/settings/integrations  → Integrations
GET  /{slug}/settings/security  → Security settings
GET  /{slug}/settings/tokens    → API Keys
GET  /{slug}/settings/logs      → API Logs
GET  /{slug}/settings/tracking  → Tracking setup
GET  /{slug}/settings/webhooks  → Webhooks
GET  /{slug}/settings/oauth-apps → OAuth Apps
GET  /{slug}/settings/notifications → Notifications

ACCOUNT:
GET  /account/settings          → Account settings
GET  /account/settings/security → Security
GET  /account/settings/referrals → Referrals
POST /account/settings          → Update account

AJAX ENDPOINTS:
POST /{slug}/api/links/check-slug       → Check slug available
GET  /{slug}/api/links/{id}/qr          → Get QR data
GET  /{slug}/api/analytics/data         → Analytics JSON
GET  /{slug}/api/tags/search            → Search tags
POST /{slug}/api/domains/verify/{id}    → Verify domain
GET  /api/workspaces/check-slug         → Check workspace slug

ADMIN (admin.utmpro.co):
GET  /                          → Admin dashboard
GET  /users                     → Users list
GET  /users/{id}                → User detail
GET  /workspaces                → Workspaces list
GET  /workspaces/{id}           → Workspace detail
POST /workspaces/{id}/plan      → Assign plan
GET  /traffic-rules             → Traffic injection rules
POST /traffic-rules             → Create rule
GET  /traffic-rules/{id}        → Edit rule
PUT  /traffic-rules/{id}        → Update rule
GET  /domains                   → All domains
GET  /analytics                 → Global analytics
GET  /logs                      → System logs
GET  /settings                  → System settings
POST /settings                  → Update settings
```

## 6.2 Base Controller

```csharp
// File: UTMPro.Web/Controllers/BaseWorkspaceController.cs
[Authorize]
public abstract class BaseWorkspaceController : Controller
{
    protected long CurrentUserId =>
        long.Parse(User.FindFirst("UserId")!.Value);

    protected string CurrentUserName =>
        User.FindFirst("Name")?.Value ?? "";

    protected bool IsSuperAdmin =>
        User.IsInRole("SuperAdmin");

    protected Workspace? CurrentWorkspace { get; private set; }
    protected string CurrentRole { get; private set; } = "";

    protected async Task<bool> LoadWorkspaceAsync(
        string slug,
        IWorkspaceRepository wsRepo)
    {
        CurrentWorkspace = await wsRepo
            .GetBySlugAsync(slug);

        if (CurrentWorkspace == null) return false;

        var member = await wsRepo.GetMemberAsync(
            CurrentWorkspace.Id, CurrentUserId);

        if (member == null) return false;

        CurrentRole = member.Role;

        ViewBag.Workspace = CurrentWorkspace;
        ViewBag.CurrentRole = CurrentRole;
        ViewBag.UserId = CurrentUserId;

        return true;
    }

    protected bool CanEdit() =>
        CurrentRole is "Owner" or "Admin" or "Member";

    protected bool CanAdmin() =>
        CurrentRole is "Owner" or "Admin";

    protected bool IsOwner() =>
        CurrentRole == "Owner";

    protected IActionResult Forbidden() =>
        StatusCode(403, "Access denied");
}
```

## 6.3 Links Controller

```csharp
// File: UTMPro.Web/Controllers/LinksController.cs
[Route("{workspaceSlug}/links")]
public class LinksController : BaseWorkspaceController
{
    private readonly ILinkRepository _linkRepo;
    private readonly IWorkspaceRepository _wsRepo;
    private readonly IDomainRepository _domainRepo;
    private readonly ITagRepository _tagRepo;
    private readonly IFolderRepository _folderRepo;
    private readonly ILinkService _linkService;
    private readonly LinkCacheService _cacheInvalidator;

    // GET /{slug}/links
    [HttpGet("")]
    public async Task<IActionResult> Index(
        string workspaceSlug,
        string? search,
        long? domainId,
        long? folderId,
        long? tagId,
        bool archived = false,
        int page = 1,
        int pageSize = 25,
        string sortBy = "CreatedAt",
        string sortDir = "DESC")
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo))
            return NotFound();

        var links = await _linkRepo.GetListAsync(
            CurrentWorkspace!.Id, search, domainId, folderId,
            tagId, archived, page, pageSize, sortBy, sortDir);

        var domains = await _domainRepo
            .GetByWorkspaceIdAsync(CurrentWorkspace.Id);
        var folders = await _folderRepo
            .GetByWorkspaceIdAsync(CurrentWorkspace.Id);
        var tags = await _tagRepo
            .GetByWorkspaceIdAsync(CurrentWorkspace.Id);

        ViewBag.Domains = domains;
        ViewBag.Folders = folders;
        ViewBag.Tags = tags;

        return View(links);
    }

    // POST /{slug}/links
    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string workspaceSlug,
        [FromBody] CreateLinkRequest request)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo))
            return NotFound();

        if (!CanEdit()) return Forbidden();

        var result = await _linkService.CreateAsync(
            CurrentWorkspace!, CurrentUserId, request);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            success = true,
            link = result.Link,
            shortUrl = $"https://{result.Link!.Domain}/" +
                       result.Link.Slug
        });
    }

    // PUT /{slug}/links/{id}
    [HttpPut("{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        string workspaceSlug,
        long id,
        [FromBody] UpdateLinkRequest request)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo))
            return NotFound();

        if (!CanEdit()) return Forbidden();

        var link = await _linkRepo.GetByIdAsync(
            id, CurrentWorkspace!.Id);
        if (link == null) return NotFound();

        var result = await _linkService.UpdateAsync(
            link, request);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        // Invalidate redirect cache
        _cacheInvalidator.Invalidate(
            link.Domain, link.Slug);

        return Ok(new { success = true });
    }

    // DELETE /{slug}/links/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(
        string workspaceSlug, long id)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo))
            return NotFound();

        if (!CanAdmin()) return Forbidden();

        var link = await _linkRepo.GetByIdAsync(
            id, CurrentWorkspace!.Id);
        if (link == null) return NotFound();

        await _linkRepo.DeleteAsync(id);
        _cacheInvalidator.Invalidate(link.Domain, link.Slug);

        return Ok(new { success = true });
    }

    // AJAX: Check slug availability
    [HttpPost("api/check-slug")]
    public async Task<IActionResult> CheckSlug(
        string workspaceSlug,
        [FromBody] CheckSlugRequest req)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo))
            return NotFound();

        var domain = await _domainRepo.GetByIdAsync(req.DomainId);
        if (domain == null) return BadRequest();

        var exists = await _linkRepo.SlugExistsAsync(
            domain.Id, req.Slug);

        return Ok(new { available = !exists });
    }
}
```

## 6.4 Analytics Controller

```csharp
// File: UTMPro.Web/Controllers/AnalyticsController.cs
[Route("{workspaceSlug}/analytics")]
public class AnalyticsController : BaseWorkspaceController
{
    private readonly IAnalyticsRepository _analyticsRepo;
    private readonly IWorkspaceRepository _wsRepo;

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string workspaceSlug,
        string? interval = "24h",
        long? linkId = null)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo))
            return NotFound();

        ViewBag.Interval = interval;
        ViewBag.LinkId = linkId;
        return View();
    }

    [HttpGet("data")]
    public async Task<IActionResult> GetData(
        string workspaceSlug,
        string interval = "24h",
        long? linkId = null)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo))
            return NotFound();

        var (start, end) = ParseInterval(interval);
        var plan = await GetPlanAsync(CurrentWorkspace!.PlanId);

        // Check retention policy
        var retentionStart = DateTime.UtcNow
            .AddDays(-plan.AnalyticsRetentionDays);
        if (start < retentionStart)
            start = retentionStart;

        var data = await _analyticsRepo.GetSummaryAsync(
            CurrentWorkspace.Id, start, end, linkId);

        return Ok(data);
    }

    private static (DateTime start, DateTime end) ParseInterval(
        string interval)
    {
        var end = DateTime.UtcNow;
        var start = interval switch
        {
            "1h"  => end.AddHours(-1),
            "24h" => end.AddHours(-24),
            "7d"  => end.AddDays(-7),
            "30d" => end.AddDays(-30),
            "90d" => end.AddDays(-90),
            "1y"  => end.AddDays(-365),
            _     => end.AddHours(-24)
        };
        return (start, end);
    }
}
```

---

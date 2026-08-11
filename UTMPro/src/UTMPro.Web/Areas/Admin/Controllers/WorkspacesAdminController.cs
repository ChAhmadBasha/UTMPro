using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using UTMPro.Data;
using UTMPro.Data.Models;
using UTMPro.Data.Repositories;
using UTMPro.Web.Services;

namespace UTMPro.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin")]
[Route("admin/workspaces")]
public class WorkspacesAdminController : Controller
{
    private readonly IWorkspaceRepository _wsRepo;
    private readonly IPlanRepository _planRepo;
    private readonly IBillingRepository _billingRepo;
    private readonly ILinkRepository _linkRepo;
    private readonly IDbConnectionFactory _db;

    public WorkspacesAdminController(IWorkspaceRepository wsRepo, IPlanRepository planRepo,
        IBillingRepository billingRepo, ILinkRepository linkRepo, IDbConnectionFactory db)
    {
        _wsRepo = wsRepo; _planRepo = planRepo; _billingRepo = billingRepo; _linkRepo = linkRepo; _db = db;
    }

    private long AdminId => long.Parse(User.FindFirst("UserId")!.Value);

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, int? planId, int page = 1)
    {
        var workspaces = await _wsRepo.GetAllAsync(search, planId, page, 25);
        var total = await _wsRepo.GetTotalCountAsync(search, planId);
        var plans = await _planRepo.GetAllActiveAsync();
        ViewBag.Search = search; ViewBag.PlanId = planId; ViewBag.Plans = plans;
        ViewBag.CurrentPage = page; ViewBag.TotalCount = total;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / 25);
        return View("~/Areas/Admin/Views/Workspaces/Index.cshtml", workspaces);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Detail(long id, int linkPage = 1)
    {
        var ws = await _wsRepo.GetByIdAsync(id);
        if (ws == null) return NotFound();
        var plans = await _planRepo.GetAllActiveAsync();
        var members = await _wsRepo.GetMembersAsync(ws.Id);
        var subscription = await _billingRepo.GetActiveSubscriptionAsync(ws.Id);
        var invoices = await _billingRepo.GetInvoicesAsync(ws.Id, 1, 10);

        // Fetch workspace links for the admin
        var (links, linkCount) = await _linkRepo.GetListAsync(ws.Id, null, null, null, null, false, linkPage, 20, "CreatedAt", "DESC");

        ViewBag.Plans = plans; ViewBag.Members = members;
        ViewBag.Subscription = subscription; ViewBag.Invoices = invoices;
        ViewBag.Links = links; ViewBag.LinkCount = linkCount;
        ViewBag.LinkPage = linkPage;
        ViewBag.LinkTotalPages = (int)Math.Ceiling((double)linkCount / 20);
        return View("~/Areas/Admin/Views/Workspaces/Detail.cshtml", ws);
    }

    // ── Assign Plan (manual, bypasses Stripe) ───────────
    [HttpPost("{id}/assign-plan")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignPlan(long id, int planId, string? notes)
    {
        var ws = await _wsRepo.GetByIdAsync(id);
        if (ws == null) return NotFound();

        await _wsRepo.AssignPlanAsync(id, planId, DateTime.UtcNow, null, notes ?? "Admin manual assignment", AdminId);
        
        TempData["Success"] = $"Plan changed to {(await _planRepo.GetByIdAsync(planId))?.Name} for {ws.Name}";
        return Redirect($"/admin/workspaces/{id}");
    }

    // ── Cancel Stripe Subscription (admin override) ─────
    [HttpPost("{id}/cancel-subscription")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelSubscription(long id)
    {
        var ws = await _wsRepo.GetByIdAsync(id);
        if (ws == null) return NotFound();

        var sub = await _billingRepo.GetActiveSubscriptionAsync(id);
        if (sub != null)
        {
            try
            {
                Stripe.StripeConfiguration.ApiKey = HttpContext.RequestServices
                    .GetRequiredService<IConfiguration>()["Stripe:SecretKey"];
                var service = new Stripe.SubscriptionService();
                await service.CancelAsync(sub.StripeSubscriptionId);
                
                await _billingRepo.UpdateSubscriptionAsync(sub.StripeSubscriptionId, "canceled",
                    sub.CurrentPeriodStart, sub.CurrentPeriodEnd, false, null);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Stripe cancel failed: {ex.Message}. Plan changed locally only.";
            }
        }

        await _wsRepo.AssignPlanAsync(id, 1, DateTime.UtcNow, null, "Admin canceled subscription", AdminId);
        TempData["Success"] = $"Subscription canceled and downgraded to Free for {ws.Name}";
        return Redirect($"/admin/workspaces/{id}");
    }

    // ── Force upgrade (admin creates subscription record without Stripe) ──
    [HttpPost("{id}/force-upgrade")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForceUpgrade(long id, int planId, int durationMonths = 12, string? notes = null)
    {
        var ws = await _wsRepo.GetByIdAsync(id);
        if (ws == null) return NotFound();

        var plan = await _planRepo.GetByIdAsync(planId);
        if (plan == null) return BadRequest("Invalid plan");

        var start = DateTime.UtcNow;
        var end = start.AddMonths(durationMonths);

        await _wsRepo.AssignPlanAsync(id, planId, start, end, 
            notes ?? $"Admin force upgrade: {plan.Name} for {durationMonths} months", AdminId);

        TempData["Success"] = $"Force upgraded {ws.Name} to {plan.Name} until {end:MMM d, yyyy}";
        return Redirect($"/admin/workspaces/{id}");
    }

    // ── Change member roles within workspace ────────────
    [HttpPost("{id}/members/{userId}/role")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeMemberRole(long id, long userId, string role)
    {
        if (role is not ("Owner" or "Admin" or "Member" or "Viewer"))
        {
            TempData["Error"] = "Invalid role";
            return Redirect($"/admin/workspaces/{id}");
        }

        await _wsRepo.UpdateMemberRoleAsync(id, userId, role);
        TempData["Success"] = $"Member role changed to {role}";
        return Redirect($"/admin/workspaces/{id}");
    }

    // ── Remove member from workspace ────────────────────
    [HttpPost("{id}/members/{userId}/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(long id, long userId)
    {
        await _wsRepo.RemoveMemberAsync(id, userId);
        TempData["Success"] = "Member removed from workspace";
        return Redirect($"/admin/workspaces/{id}");
    }

    // ── Add member to workspace ─────────────────────────
    [HttpPost("{id}/members/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMember(long id, string email, string role = "Member")
    {
        var ws = await _wsRepo.GetByIdAsync(id);
        if (ws == null) return NotFound();

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand("SELECT Id FROM Users WHERE Email = @Email AND DeletedAt IS NULL", conn);
        cmd.Parameters.AddWithValue("@Email", email.Trim().ToLower());
        var result = await cmd.ExecuteScalarAsync();
        
        if (result == null)
        {
            TempData["Error"] = $"User with email {email} not found";
            return Redirect($"/admin/workspaces/{id}");
        }

        var userId = (long)result;
        var existing = await _wsRepo.GetMemberAsync(id, userId);
        if (existing != null)
        {
            TempData["Error"] = "User is already a member";
            return Redirect($"/admin/workspaces/{id}");
        }

        await _wsRepo.AddMemberAsync(id, userId, role, null);
        TempData["Success"] = $"User {email} added as {role}";
        return Redirect($"/admin/workspaces/{id}");
    }

    // ── Suspend workspace ───────────────────────────────
    [HttpPost("{id}/suspend")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspend(long id)
    {
        await _wsRepo.SuspendAsync(id);
        TempData["Success"] = "Workspace suspended";
        return Redirect("/admin/workspaces");
    }

    // ── Reactivate workspace ────────────────────────────
    [HttpPost("{id}/reactivate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivate(long id)
    {
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand("UPDATE Workspaces SET IsActive = 1, UpdatedAt = GETUTCDATE() WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
        TempData["Success"] = "Workspace reactivated";
        return Redirect($"/admin/workspaces/{id}");
    }
}

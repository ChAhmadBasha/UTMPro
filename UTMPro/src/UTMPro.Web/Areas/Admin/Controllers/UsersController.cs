using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin")]
[Route("admin/users")]
public class UsersController : Controller
{
    private readonly IUserRepository _userRepo;
    private readonly IWorkspaceRepository _wsRepo;
    private readonly UTMPro.Data.IDbConnectionFactory _db;

    public UsersController(IUserRepository userRepo, IWorkspaceRepository wsRepo, UTMPro.Data.IDbConnectionFactory db)
    {
        _userRepo = userRepo; _wsRepo = wsRepo; _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var users = await _userRepo.GetAllAsync(search, page, 25);
        var total = await _userRepo.GetTotalCountAsync(search);
        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalCount = total;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / 25);
        return View("~/Areas/Admin/Views/Users/Index.cshtml", users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Detail(long id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) return NotFound();
        return View("~/Areas/Admin/Views/Users/Detail.cshtml", user);
    }

    [HttpPost("{id}/toggle-admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAdmin(long id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) return NotFound();
        await _userRepo.SetSuperAdminAsync(id, !user.IsSuperAdmin);
        TempData["Success"] = user.IsSuperAdmin ? "Admin rights removed" : "Admin rights granted";
        return Redirect($"/admin/users/{id}");
    }

    [HttpPost("{id}/suspend")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspend(long id)
    {
        await _userRepo.SoftDeleteAsync(id);
        TempData["Success"] = "User suspended";
        return Redirect("/admin/users");
    }

    [HttpGet("{id}/memberships")]
    public async Task<IActionResult> Memberships(long id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) return NotFound();

        // Get all workspace memberships for this user
        const string sql = @"SELECT wm.*, w.Name AS WorkspaceName, w.Slug AS WorkspaceSlug, p.Name AS PlanName
            FROM WorkspaceMembers wm
            INNER JOIN Workspaces w ON wm.WorkspaceId = w.Id
            INNER JOIN Plans p ON w.PlanId = p.Id
            WHERE wm.UserId = @UserId AND wm.IsActive = 1";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", id);
        var memberships = new List<dynamic>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            memberships.Add(new
            {
                Id = r.GetInt64(r.GetOrdinal("Id")),
                WorkspaceId = r.GetInt64(r.GetOrdinal("WorkspaceId")),
                WorkspaceName = r.GetString(r.GetOrdinal("WorkspaceName")),
                WorkspaceSlug = r.GetString(r.GetOrdinal("WorkspaceSlug")),
                Role = r.GetString(r.GetOrdinal("Role")),
                PlanName = r.GetString(r.GetOrdinal("PlanName")),
                JoinedAt = r.IsDBNull(r.GetOrdinal("JoinedAt")) ? (DateTime?)null : r.GetDateTime(r.GetOrdinal("JoinedAt"))
            });
        }
        ViewBag.Memberships = memberships;
        return View("~/Areas/Admin/Views/Users/Memberships.cshtml", user);
    }

    [HttpPost("{userId}/memberships/{workspaceId}/role")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(long userId, long workspaceId, string role)
    {
        if (role is not ("Owner" or "Admin" or "Member" or "Viewer"))
        {
            TempData["Error"] = "Invalid role";
            return Redirect($"/admin/users/{userId}/memberships");
        }
        await _wsRepo.UpdateMemberRoleAsync(workspaceId, userId, role);
        TempData["Success"] = $"Role changed to {role}";
        return Redirect($"/admin/users/{userId}/memberships");
    }

    [HttpPost("{userId}/memberships/{workspaceId}/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveFromWorkspace(long userId, long workspaceId)
    {
        await _wsRepo.RemoveMemberAsync(workspaceId, userId);
        TempData["Success"] = "User removed from workspace";
        return Redirect($"/admin/users/{userId}/memberships");
    }

    [HttpPost("{userId}/memberships/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToWorkspace(long userId, long workspaceId, string role = "Member")
    {
        var existing = await _wsRepo.GetMemberAsync(workspaceId, userId);
        if (existing != null)
        {
            TempData["Error"] = "User is already a member of this workspace";
            return Redirect($"/admin/users/{userId}/memberships");
        }
        await _wsRepo.AddMemberAsync(workspaceId, userId, role, null);
        TempData["Success"] = "User added to workspace";
        return Redirect($"/admin/users/{userId}/memberships");
    }
}

using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using UTMPro.Data;
using UTMPro.Web.Models.ViewModels;

namespace UTMPro.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin")]
[Route("admin")]
public class DashboardController : Controller
{
    private readonly IDbConnectionFactory _db;

    public DashboardController(IDbConnectionFactory db) => _db = db;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var vm = new AdminDashboardViewModel();

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand("sp_GetAdminDashboard", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandTimeout = 30;

        await using var r = await cmd.ExecuteReaderAsync();

        // ── RS 1: Summary Counters ──────────────────────
        if (await r.ReadAsync())
        {
            vm.TotalUsers = r.GetInt32(0);
            vm.NewUsersToday = r.GetInt32(1);
            vm.NewUsersWeek = r.GetInt32(2);
            vm.TotalWorkspaces = r.GetInt32(3);
            vm.ActiveWorkspaces = r.GetInt32(4);
            vm.TotalLinks = r.GetInt32(5);
            vm.LinksCreatedToday = r.GetInt32(6);
            vm.ClicksToday = r.GetInt32(7);
            vm.ClicksLastHour = r.GetInt32(8);
            vm.ClicksWeek = r.GetInt32(9);
            vm.ClicksMonth = r.GetInt32(10);
            vm.ClicksAllTime = r.GetInt32(11);
            vm.VerifiedDomains = r.GetInt32(12);
            vm.SystemDomains = r.GetInt32(13);
        }

        // ── RS 2: Clicks per day ────────────────────────
        if (await r.NextResultAsync())
            while (await r.ReadAsync())
                vm.ClicksByDay.Add(new DayCount { Date = r.GetDateTime(0), Count = r.GetInt32(1) });

        // ── RS 3: Signups per day ───────────────────────
        if (await r.NextResultAsync())
            while (await r.ReadAsync())
                vm.SignupsByDay.Add(new DayCount { Date = r.GetDateTime(0), Count = r.GetInt32(1) });

        // ── RS 4: Top countries ─────────────────────────
        if (await r.NextResultAsync())
            while (await r.ReadAsync())
                vm.TopCountries.Add(new NameCount { Name = r.GetString(0), Code = r.GetString(1), Count = r.GetInt32(2) });

        // ── RS 5: Top links ─────────────────────────────
        if (await r.NextResultAsync())
            while (await r.ReadAsync())
                vm.TopLinks.Add(new TopLinkItem
                {
                    LinkId = r.GetInt64(0), Domain = r.GetString(1), Slug = r.GetString(2),
                    PrimaryUrl = r.IsDBNull(3) ? null : r.GetString(3),
                    WorkspaceName = r.GetString(4), WorkspaceSlug = r.GetString(5),
                    Clicks30d = r.GetInt32(6), AllTimeClicks = r.GetInt64(7),
                    CreatedAt = r.GetDateTime(8)
                });

        // ── RS 6: Top workspaces ────────────────────────
        if (await r.NextResultAsync())
            while (await r.ReadAsync())
                vm.TopWorkspaces.Add(new TopWorkspaceItem
                {
                    WorkspaceId = r.GetInt64(0), Name = r.GetString(1), Slug = r.GetString(2),
                    PlanName = r.GetString(3), MemberCount = r.GetInt32(4), LinkCount = r.GetInt32(5),
                    Clicks30d = r.GetInt32(6), LinksUsed = r.GetInt32(7), EventsUsed = r.GetInt32(8),
                    CreatedAt = r.GetDateTime(9)
                });

        // ── RS 7: Device breakdown ──────────────────────
        if (await r.NextResultAsync())
            while (await r.ReadAsync())
                vm.DeviceBreakdown.Add(new NameCount { Name = r.GetString(0), Count = r.GetInt32(1) });

        // ── RS 8: Browser breakdown ─────────────────────
        if (await r.NextResultAsync())
            while (await r.ReadAsync())
                vm.BrowserBreakdown.Add(new NameCount { Name = r.GetString(0), Count = r.GetInt32(1) });

        // ── RS 9: OS breakdown ──────────────────────────
        if (await r.NextResultAsync())
            while (await r.ReadAsync())
                vm.OSBreakdown.Add(new NameCount { Name = r.GetString(0), Count = r.GetInt32(1) });

        // ── RS 10: Top referrers ────────────────────────
        if (await r.NextResultAsync())
            while (await r.ReadAsync())
                vm.TopReferrers.Add(new NameCount { Name = r.GetString(0), Count = r.GetInt32(1) });

        // ── RS 11: Plan distribution ────────────────────
        if (await r.NextResultAsync())
            while (await r.ReadAsync())
                vm.PlanDistribution.Add(new NameCount { Name = r.GetString(0), Count = r.GetInt32(1) });

        // ── RS 12: Hourly clicks today ──────────────────
        if (await r.NextResultAsync())
            while (await r.ReadAsync())
                vm.ClicksByHour.Add(new HourCount { Hour = r.GetInt32(0), Count = r.GetInt32(1) });

        return View("~/Areas/Admin/Views/Dashboard/Index.cshtml", vm);
    }
}

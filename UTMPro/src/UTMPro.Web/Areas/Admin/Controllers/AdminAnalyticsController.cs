using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using UTMPro.Data;
using UTMPro.Web.Models.ViewModels;

namespace UTMPro.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin")]
[Route("admin/analytics")]
public class AdminAnalyticsController : Controller
{
    private readonly IDbConnectionFactory _db;
    public AdminAnalyticsController(IDbConnectionFactory db) => _db = db;

    [HttpGet("")]
    public async Task<IActionResult> Index(int days = 30)
    {
        if (days < 1) days = 30;
        if (days > 365) days = 365;

        var vm = new AdminAnalyticsDetailViewModel { Days = days };
        await using var conn = await _db.CreateOpenConnectionAsync();

        // ── Total clicks in period ──
        await using (var cmd = new SqlCommand(@"
            SELECT COUNT(*) FROM ClickEvents WHERE ClickedAt >= DATEADD(DAY, -@D, GETUTCDATE())", conn))
        {
            cmd.Parameters.AddWithValue("@D", days);
            vm.TotalClicks = (int)(await cmd.ExecuteScalarAsync())!;
        }

        // ── Unique IPs ──
        await using (var cmd = new SqlCommand(@"
            SELECT COUNT(DISTINCT IPAddress) FROM ClickEvents WHERE ClickedAt >= DATEADD(DAY, -@D, GETUTCDATE()) AND IPAddress IS NOT NULL", conn))
        {
            cmd.Parameters.AddWithValue("@D", days);
            vm.UniqueVisitors = (int)(await cmd.ExecuteScalarAsync())!;
        }

        // ── Clicks per day ──
        await using (var cmd = new SqlCommand(@"
            SELECT CAST(ClickedAt AS DATE) AS D, COUNT(*) AS C
            FROM ClickEvents WHERE ClickedAt >= DATEADD(DAY, -@D, GETUTCDATE())
            GROUP BY CAST(ClickedAt AS DATE) ORDER BY D", conn))
        {
            cmd.Parameters.AddWithValue("@D", days);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                vm.ClicksByDay.Add(new DayCount { Date = r.GetDateTime(0), Count = r.GetInt32(1) });
        }

        // ── All countries ──
        await using (var cmd = new SqlCommand(@"
            SELECT ISNULL(Country,'Unknown') AS Country, ISNULL(CountryCode,'--') AS CC, COUNT(*) AS C
            FROM ClickEvents WHERE ClickedAt >= DATEADD(DAY, -@D, GETUTCDATE())
            GROUP BY Country, CountryCode ORDER BY C DESC", conn))
        {
            cmd.Parameters.AddWithValue("@D", days);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                vm.Countries.Add(new NameCount { Name = r.GetString(0), Code = r.GetString(1), Count = r.GetInt32(2) });
        }

        // ── Top 50 links (PrimaryUrl from LinkDestinations subquery) ──
        await using (var cmd = new SqlCommand(@"
            SELECT TOP 50
                l.Id, d.Domain, l.Slug,
                (SELECT TOP 1 ld.Url FROM LinkDestinations ld 
                 WHERE ld.LinkId = l.Id AND ld.IsAdminUrl = 0 AND ld.IsActive = 1 
                 ORDER BY ld.SortOrder) AS PrimaryUrl,
                w.Name, w.Slug,
                COUNT(c.Id) AS Clicks30d, l.TotalClicks, l.CreatedAt
            FROM ClickEvents c
            INNER JOIN Links l ON c.LinkId = l.Id
            INNER JOIN Domains d ON l.DomainId = d.Id
            INNER JOIN Workspaces w ON l.WorkspaceId = w.Id
            WHERE c.ClickedAt >= DATEADD(DAY, -@D, GETUTCDATE())
            GROUP BY l.Id, d.Domain, l.Slug, w.Name, w.Slug, l.TotalClicks, l.CreatedAt
            ORDER BY Clicks30d DESC", conn))
        {
            cmd.Parameters.AddWithValue("@D", days);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                vm.TopLinks.Add(new TopLinkItem
                {
                    LinkId = r.GetInt64(0), Domain = r.GetString(1), Slug = r.GetString(2),
                    PrimaryUrl = r.IsDBNull(3) ? null : r.GetString(3),
                    WorkspaceName = r.GetString(4), WorkspaceSlug = r.GetString(5),
                    Clicks30d = r.GetInt32(6), AllTimeClicks = r.GetInt64(7), CreatedAt = r.GetDateTime(8)
                });
        }

        // ── Top 25 workspaces (MemberCount + LinkCount via subqueries) ──
        await using (var cmd = new SqlCommand(@"
            SELECT TOP 25
                w.Id, w.Name, w.Slug, p.Name AS PlanName,
                (SELECT COUNT(*) FROM WorkspaceMembers wm WHERE wm.WorkspaceId = w.Id AND wm.IsActive = 1) AS MemberCount,
                (SELECT COUNT(*) FROM Links lk WHERE lk.WorkspaceId = w.Id AND lk.IsArchived = 0) AS LinkCount,
                COUNT(c.Id) AS Clicks30d,
                w.LinksUsedThisMonth, w.EventsUsedThisMonth, w.CreatedAt
            FROM ClickEvents c
            INNER JOIN Workspaces w ON c.WorkspaceId = w.Id
            INNER JOIN Plans p ON w.PlanId = p.Id
            WHERE c.ClickedAt >= DATEADD(DAY, -@D, GETUTCDATE()) AND w.DeletedAt IS NULL
            GROUP BY w.Id, w.Name, w.Slug, p.Name, w.LinksUsedThisMonth, w.EventsUsedThisMonth, w.CreatedAt
            ORDER BY Clicks30d DESC", conn))
        {
            cmd.Parameters.AddWithValue("@D", days);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                vm.TopWorkspaces.Add(new TopWorkspaceItem
                {
                    WorkspaceId = r.GetInt64(0), Name = r.GetString(1), Slug = r.GetString(2),
                    PlanName = r.GetString(3), MemberCount = r.GetInt32(4), LinkCount = r.GetInt32(5),
                    Clicks30d = r.GetInt32(6), LinksUsed = r.GetInt32(7), EventsUsed = r.GetInt32(8),
                    CreatedAt = r.GetDateTime(9)
                });
        }

        // ── Devices, Browsers, OS ──
        foreach (var (col, list) in new[] { ("Device", vm.Devices), ("Browser", vm.Browsers), ("OS", vm.OperatingSystems) })
        {
            await using var cmd = new SqlCommand($@"
                SELECT ISNULL([{col}],'Unknown'), COUNT(*) FROM ClickEvents
                WHERE ClickedAt >= DATEADD(DAY,-@D,GETUTCDATE())
                GROUP BY [{col}] ORDER BY COUNT(*) DESC", conn);
            cmd.Parameters.AddWithValue("@D", days);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                list.Add(new NameCount { Name = r.GetString(0), Count = r.GetInt32(1) });
        }

        // ── Top referrers ──
        await using (var cmd = new SqlCommand(@"
            SELECT TOP 30
                CASE WHEN Referer IS NULL OR Referer='' THEN 'Direct'
                     WHEN Referer LIKE '%google%' THEN 'Google'
                     WHEN Referer LIKE '%facebook%' OR Referer LIKE '%fb.%' THEN 'Facebook'
                     WHEN Referer LIKE '%twitter%' OR Referer LIKE '%t.co%' THEN 'Twitter/X'
                     WHEN Referer LIKE '%linkedin%' THEN 'LinkedIn'
                     WHEN Referer LIKE '%instagram%' THEN 'Instagram'
                     WHEN Referer LIKE '%youtube%' THEN 'YouTube'
                     WHEN Referer LIKE '%reddit%' THEN 'Reddit'
                     WHEN Referer LIKE '%tiktok%' THEN 'TikTok'
                     ELSE 'Other' END AS Ref,
                COUNT(*) AS C
            FROM ClickEvents WHERE ClickedAt >= DATEADD(DAY,-@D,GETUTCDATE())
            GROUP BY CASE WHEN Referer IS NULL OR Referer='' THEN 'Direct'
                     WHEN Referer LIKE '%google%' THEN 'Google'
                     WHEN Referer LIKE '%facebook%' OR Referer LIKE '%fb.%' THEN 'Facebook'
                     WHEN Referer LIKE '%twitter%' OR Referer LIKE '%t.co%' THEN 'Twitter/X'
                     WHEN Referer LIKE '%linkedin%' THEN 'LinkedIn'
                     WHEN Referer LIKE '%instagram%' THEN 'Instagram'
                     WHEN Referer LIKE '%youtube%' THEN 'YouTube'
                     WHEN Referer LIKE '%reddit%' THEN 'Reddit'
                     WHEN Referer LIKE '%tiktok%' THEN 'TikTok'
                     ELSE 'Other' END
            ORDER BY C DESC", conn))
        {
            cmd.Parameters.AddWithValue("@D", days);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                vm.Referrers.Add(new NameCount { Name = r.GetString(0), Count = r.GetInt32(1) });
        }

        ViewBag.Days = days;
        return View("~/Areas/Admin/Views/Analytics/Index.cshtml", vm);
    }
}

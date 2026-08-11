using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using UTMPro.Data;
using UTMPro.Data.Models;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Controllers;

[Route("{workspaceSlug}/analytics")]
public class AnalyticsController : BaseWorkspaceController
{
    private readonly IAnalyticsRepository _analyticsRepo;
    private readonly IWorkspaceRepository _wsRepo;
    private readonly IPlanRepository _planRepo;
    private readonly IDbConnectionFactory _db;

    public AnalyticsController(IAnalyticsRepository analyticsRepo, IWorkspaceRepository wsRepo,
        IPlanRepository planRepo, IDbConnectionFactory db)
    {
        _analyticsRepo = analyticsRepo; _wsRepo = wsRepo; _planRepo = planRepo; _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string workspaceSlug, string? interval = "24h", long? linkId = null)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        ViewBag.Interval = interval;
        ViewBag.LinkId = linkId;
        return View("~/Views/Analytics/Index.cshtml");
    }

    [HttpGet("data")]
    public async Task<IActionResult> GetData(string workspaceSlug, string interval = "24h",
        long? linkId = null, string? startDate = null, string? endDate = null,
        string? country = null, string? device = null, string? browser = null,
        string? os = null, string? trigger = null, string? referrer = null)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();

        DateTime start, end;

        // Custom date range takes priority over interval presets
        if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
        {
            start = DateTime.Parse(startDate);
            end = DateTime.Parse(endDate).AddDays(1).AddSeconds(-1); // End of day
        }
        else
        {
            (start, end) = ParseInterval(interval);
        }

        // Clamp to plan retention
        var plan = await _planRepo.GetByIdAsync(CurrentWorkspace!.PlanId);
        var retentionStart = DateTime.UtcNow.AddDays(-(plan?.AnalyticsRetentionDays ?? 30));
        if (start < retentionStart) start = retentionStart;

        // Check if any filters are applied
        bool hasFilters = country != null || device != null || browser != null || os != null || trigger != null || referrer != null;

        if (hasFilters)
        {
            // Use filtered SP
            return Ok(await GetFilteredAnalyticsAsync(CurrentWorkspace.Id, start, end, linkId, country, device, browser, os, trigger, referrer, IsSuperAdmin));
        }

        var data = await _analyticsRepo.GetSummaryAsync(CurrentWorkspace.Id, start, end, linkId, IsSuperAdmin);
        return Ok(data);
    }

    // Filter dropdown values
    [HttpGet("filters")]
    public async Task<IActionResult> GetFilterValues(string workspaceSlug, string interval = "30d")
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        var (start, end) = ParseInterval(interval);

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand("sp_GetAnalyticsFilterValues", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@WorkspaceId", CurrentWorkspace!.Id);
        cmd.Parameters.AddWithValue("@StartDate", start);
        cmd.Parameters.AddWithValue("@EndDate", end);
        cmd.Parameters.AddWithValue("@IncludeAdmin", IsSuperAdmin);

        var countries = new List<string>();
        var devices = new List<string>();
        var browsers = new List<string>();
        var osList = new List<string>();

        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) countries.Add(r.GetString(0));
        await r.NextResultAsync();
        while (await r.ReadAsync()) devices.Add(r.GetString(0));
        await r.NextResultAsync();
        while (await r.ReadAsync()) browsers.Add(r.GetString(0));
        await r.NextResultAsync();
        while (await r.ReadAsync()) osList.Add(r.GetString(0));

        return Ok(new { countries, devices, browsers, os = osList });
    }

    private async Task<AnalyticsSummary> GetFilteredAnalyticsAsync(long wsId, DateTime start, DateTime end,
        long? linkId, string? country, string? device, string? browser, string? os, string? trigger, string? referrer, bool includeAdmin)
    {
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand("sp_GetFilteredAnalytics", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandTimeout = 60;
        cmd.Parameters.AddWithValue("@WorkspaceId", wsId);
        cmd.Parameters.AddWithValue("@StartDate", start);
        cmd.Parameters.AddWithValue("@EndDate", end);
        cmd.Parameters.AddWithValue("@LinkId", (object?)linkId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IncludeAdmin", includeAdmin);
        cmd.Parameters.AddWithValue("@Country", (object?)country ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Device", (object?)device ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Browser", (object?)browser ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@OS", (object?)os ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Trigger", (object?)trigger ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Referrer", (object?)referrer ?? DBNull.Value);

        var summary = new AnalyticsSummary();
        await using var r = await cmd.ExecuteReaderAsync();

        if (await r.ReadAsync())
        {
            summary.TotalClicks = r.GetInt32(0);
            summary.UniqueClicks = r.GetInt32(1);
            summary.UserClicks = r.GetInt32(2);
            summary.AdminClicks = r.GetInt32(3);
        }

        await r.NextResultAsync();
        while (await r.ReadAsync()) summary.TimeSeries.Add(new TimeSeriesPoint(r.GetDateTime(0), r.GetInt32(1)));
        await r.NextResultAsync();
        while (await r.ReadAsync()) summary.Countries.Add(new CountryStats(r.GetString(0), r.GetString(1), r.GetInt32(2)));
        await r.NextResultAsync();
        while (await r.ReadAsync()) summary.Devices.Add(new DeviceStats(r.GetString(0), r.GetInt32(1), r.GetDecimal(2)));
        await r.NextResultAsync();
        while (await r.ReadAsync()) summary.Browsers.Add(new BrowserStats(r.GetString(0), r.GetInt32(1)));
        await r.NextResultAsync();
        while (await r.ReadAsync()) summary.OSStats.Add(new OSStats(r.GetString(0), r.GetInt32(1)));
        await r.NextResultAsync();
        while (await r.ReadAsync()) summary.Referrers.Add(new ReferrerStats(r.GetString(0), r.GetInt32(1)));
        await r.NextResultAsync();
        while (await r.ReadAsync()) summary.TopLinks.Add(new LinkStats(r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetInt64(3), r.GetInt32(4)));

        return summary;
    }

    private static (DateTime start, DateTime end) ParseInterval(string interval)
    {
        var end = DateTime.UtcNow;
        var start = interval switch
        {
            "1h" => end.AddHours(-1), "24h" => end.AddDays(-1), "7d" => end.AddDays(-7),
            "30d" => end.AddDays(-30), "90d" => end.AddDays(-90), "1y" => end.AddDays(-365),
            _ => end.AddDays(-1)
        };
        return (start, end);
    }
}

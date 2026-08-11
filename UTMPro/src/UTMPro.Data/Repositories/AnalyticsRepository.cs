using System.Data;
using Microsoft.Data.SqlClient;
using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly IDbConnectionFactory _db;
    public AnalyticsRepository(IDbConnectionFactory db) => _db = db;

    public async Task<AnalyticsSummary> GetSummaryAsync(long workspaceId, DateTime startDate, DateTime endDate, long? linkId = null)
    {
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand("sp_GetAnalyticsSummary", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandTimeout = 60;
        cmd.Parameters.AddWithValue("@WorkspaceId", workspaceId);
        cmd.Parameters.AddWithValue("@StartDate", startDate);
        cmd.Parameters.AddWithValue("@EndDate", endDate);
        cmd.Parameters.AddWithValue("@LinkId", (object?)linkId ?? DBNull.Value);

        var summary = new AnalyticsSummary();

        await using var reader = await cmd.ExecuteReaderAsync();

        // Summary metrics
        if (await reader.ReadAsync())
        {
            summary.TotalClicks = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
            summary.UniqueClicks = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            summary.UserClicks = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
            summary.AdminClicks = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
            summary.TotalLeads = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
            summary.TotalSales = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5);
        }

        // Time series
        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            summary.TimeSeries.Add(new TimeSeriesPoint(
                reader.GetDateTime(0), reader.GetInt32(1)));
        }

        // Countries
        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            summary.Countries.Add(new CountryStats(
                reader.GetString(0), reader.GetString(1), reader.GetInt32(2)));
        }

        // Devices
        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            summary.Devices.Add(new DeviceStats(
                reader.GetString(0), reader.GetInt32(1), reader.GetDecimal(2)));
        }

        // Browsers
        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            summary.Browsers.Add(new BrowserStats(
                reader.GetString(0), reader.GetInt32(1)));
        }

        // OS
        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            summary.OSStats.Add(new OSStats(
                reader.GetString(0), reader.GetInt32(1)));
        }

        // Referrers
        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            summary.Referrers.Add(new ReferrerStats(
                reader.GetString(0), reader.GetInt32(1)));
        }

        // Top Links
        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            summary.TopLinks.Add(new LinkStats(
                reader.GetInt64(0), reader.GetString(1), reader.GetString(2),
                reader.GetInt64(3), reader.GetInt32(4)));
        }

        return summary;
    }

    public async Task<List<ClickEvent>> GetEventsAsync(long workspaceId, int page, int pageSize, long? linkId = null)
    {
        const string sql = @"
            SELECT * FROM ClickEvents 
            WHERE WorkspaceId = @WorkspaceId
            AND (@LinkId IS NULL OR LinkId = @LinkId)
            ORDER BY ClickedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WorkspaceId", workspaceId);
        cmd.Parameters.AddWithValue("@LinkId", (object?)linkId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
        cmd.Parameters.AddWithValue("@PageSize", pageSize);

        var events = new List<ClickEvent>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            events.Add(new ClickEvent
            {
                Id = r.GetInt64(r.GetOrdinal("Id")),
                LinkId = r.GetInt64(r.GetOrdinal("LinkId")),
                WorkspaceId = r.GetInt64(r.GetOrdinal("WorkspaceId")),
                DestinationUrl = r.IsDBNull(r.GetOrdinal("DestinationUrl")) ? null : r.GetString(r.GetOrdinal("DestinationUrl")),
                IsAdminRedirect = r.GetBoolean(r.GetOrdinal("IsAdminRedirect")),
                IPAddress = r.IsDBNull(r.GetOrdinal("IPAddress")) ? null : r.GetString(r.GetOrdinal("IPAddress")),
                Country = r.IsDBNull(r.GetOrdinal("Country")) ? null : r.GetString(r.GetOrdinal("Country")),
                CountryCode = r.IsDBNull(r.GetOrdinal("CountryCode")) ? null : r.GetString(r.GetOrdinal("CountryCode")),
                Device = r.IsDBNull(r.GetOrdinal("Device")) ? null : r.GetString(r.GetOrdinal("Device")),
                Browser = r.IsDBNull(r.GetOrdinal("Browser")) ? null : r.GetString(r.GetOrdinal("Browser")),
                OS = r.IsDBNull(r.GetOrdinal("OS")) ? null : r.GetString(r.GetOrdinal("OS")),
                ClickedAt = r.GetDateTime(r.GetOrdinal("ClickedAt")),
            });
        }
        return events;
    }

    public async Task<int> GetEventsCountAsync(long workspaceId, long? linkId = null)
    {
        const string sql = @"
            SELECT COUNT(*) FROM ClickEvents 
            WHERE WorkspaceId = @WorkspaceId
            AND (@LinkId IS NULL OR LinkId = @LinkId)";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WorkspaceId", workspaceId);
        cmd.Parameters.AddWithValue("@LinkId", (object?)linkId ?? DBNull.Value);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }
}

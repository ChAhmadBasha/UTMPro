using Microsoft.Data.SqlClient;
using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public class AdminTrafficRepository : IAdminTrafficRepository
{
    private readonly IDbConnectionFactory _db;
    public AdminTrafficRepository(IDbConnectionFactory db) => _db = db;

    public async Task<List<AdminTrafficRule>> GetAllRulesAsync()
    {
        const string sql = "SELECT * FROM AdminTrafficRules ORDER BY CreatedAt DESC";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        var rules = new List<AdminTrafficRule>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) rules.Add(MapRule(r));
        await r.CloseAsync();
        foreach (var rule in rules)
            rule.Urls = await GetUrlsAsync(conn, rule.Id);
        return rules;
    }

    public async Task<AdminTrafficRule?> GetRuleByIdAsync(long id)
    {
        const string sql = "SELECT * FROM AdminTrafficRules WHERE Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        var rule = MapRule(r);
        await r.CloseAsync();
        rule.Urls = await GetUrlsAsync(conn, rule.Id);
        return rule;
    }

    public async Task<List<AdminTrafficRule>> GetActiveRulesForWorkspaceAsync(long? workspaceId)
    {
        const string sql = "SELECT * FROM AdminTrafficRules WHERE IsActive = 1 AND (IsGlobal = 1 OR WorkspaceId = @WsId)";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WsId", (object?)workspaceId ?? DBNull.Value);
        var rules = new List<AdminTrafficRule>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) rules.Add(MapRule(r));
        return rules;
    }

    public async Task<long> CreateRuleAsync(AdminTrafficRule rule)
    {
        const string sql = @"INSERT INTO AdminTrafficRules (WorkspaceId, RuleName, TrafficPercent, IsGlobal, IsActive, CreatedBy, CreatedAt, UpdatedAt)
            VALUES (@WsId, @Name, @Pct, @Global, @Active, @CreatedBy, GETUTCDATE(), GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WsId", (object?)rule.WorkspaceId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Name", rule.RuleName);
        cmd.Parameters.AddWithValue("@Pct", rule.TrafficPercent);
        cmd.Parameters.AddWithValue("@Global", rule.IsGlobal);
        cmd.Parameters.AddWithValue("@Active", rule.IsActive);
        cmd.Parameters.AddWithValue("@CreatedBy", rule.CreatedBy);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task UpdateRuleAsync(AdminTrafficRule rule)
    {
        const string sql = @"UPDATE AdminTrafficRules
            SET RuleName=@Name, TrafficPercent=@Pct, IsGlobal=@Global,
                WorkspaceId=@WsId, IsActive=@Active, UpdatedAt=GETUTCDATE()
            WHERE Id=@Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", rule.Id);
        cmd.Parameters.AddWithValue("@Name", rule.RuleName);
        cmd.Parameters.AddWithValue("@Pct", rule.TrafficPercent);
        cmd.Parameters.AddWithValue("@Global", rule.IsGlobal);
        cmd.Parameters.AddWithValue("@WsId", (object?)rule.WorkspaceId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Active", rule.IsActive);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ToggleRuleAsync(long id)
    {
        const string sql = "UPDATE AdminTrafficRules SET IsActive = CASE WHEN IsActive=1 THEN 0 ELSE 1 END, UpdatedAt=GETUTCDATE() WHERE Id=@Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteRuleAsync(long id)
    {
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(
            "UPDATE AdminTrafficRules SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE Id = @Id",
            conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<AdminTrafficReport> GetReportAsync(int days)
    {
        days = Math.Clamp(days, 1, 365);
        var endDate = DateTime.UtcNow.Date.AddDays(1);
        var startDate = endDate.AddDays(-days);
        var report = new AdminTrafficReport
        {
            Days = days,
            StartDate = startDate,
            EndDate = endDate
        };

        await using var conn = await _db.CreateOpenConnectionAsync();

        const string summarySql = @"
            SELECT
                COUNT_BIG(*) AS TotalClicks,
                COALESCE(SUM(CASE WHEN IsAdminRedirect = 1 THEN CONVERT(BIGINT, 1) ELSE CONVERT(BIGINT, 0) END), 0) AS AdminClicks,
                COUNT_BIG(DISTINCT CASE WHEN IsAdminRedirect = 1 THEN IPAddress END) AS UniqueAdminVisitors
            FROM ClickEvents
            WHERE ClickedAt >= @StartDate AND ClickedAt < @EndDate";
        await using (var cmd = new SqlCommand(summarySql, conn))
        {
            cmd.Parameters.AddWithValue("@StartDate", startDate);
            cmd.Parameters.AddWithValue("@EndDate", endDate);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                report.TotalClicks = reader.GetInt64(0);
                report.AdminClicks = reader.GetInt64(1);
                report.UniqueAdminVisitors = reader.GetInt64(2);
            }
        }

        const string dailySql = @"
            SELECT
                CAST(ClickedAt AS DATE) AS ClickDate,
                COUNT_BIG(*) AS TotalClicks,
                COALESCE(SUM(CASE WHEN IsAdminRedirect = 1 THEN CONVERT(BIGINT, 1) ELSE CONVERT(BIGINT, 0) END), 0) AS AdminClicks
            FROM ClickEvents
            WHERE ClickedAt >= @StartDate AND ClickedAt < @EndDate
            GROUP BY CAST(ClickedAt AS DATE)
            ORDER BY ClickDate";
        var dailyByDate = new Dictionary<DateTime, AdminTrafficDailyRow>();
        await using (var cmd = new SqlCommand(dailySql, conn))
        {
            cmd.Parameters.AddWithValue("@StartDate", startDate);
            cmd.Parameters.AddWithValue("@EndDate", endDate);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new AdminTrafficDailyRow
                {
                    Date = reader.GetDateTime(0),
                    TotalClicks = reader.GetInt64(1),
                    AdminClicks = reader.GetInt64(2)
                };
                dailyByDate[row.Date] = row;
            }
        }

        for (var date = startDate; date < endDate; date = date.AddDays(1))
        {
            report.Daily.Add(dailyByDate.TryGetValue(date, out var row)
                ? row
                : new AdminTrafficDailyRow { Date = date });
        }

        const string rulesSql = @"
            SELECT
                atr.Id,
                atr.RuleName,
                atr.IsGlobal,
                atr.WorkspaceId,
                atr.TrafficPercent,
                atr.IsActive,
                (SELECT COUNT(*) FROM AdminTrafficUrls activeUrl
                 WHERE activeUrl.RuleId = atr.Id AND activeUrl.IsActive = 1) AS ActiveUrlCount,
                COUNT_BIG(ce.Id) AS AdminClicks,
                MAX(ce.ClickedAt) AS LastAdminRedirectAt
            FROM AdminTrafficRules atr
            LEFT JOIN AdminTrafficUrls atu ON atu.RuleId = atr.Id
            LEFT JOIN ClickEvents ce
                ON ce.AdminTrafficUrlId = atu.Id
               AND ce.IsAdminRedirect = 1
               AND ce.ClickedAt >= @StartDate
               AND ce.ClickedAt < @EndDate
            GROUP BY atr.Id, atr.RuleName, atr.IsGlobal, atr.WorkspaceId,
                     atr.TrafficPercent, atr.IsActive
            ORDER BY AdminClicks DESC, MAX(atr.UpdatedAt) DESC";
        await using (var cmd = new SqlCommand(rulesSql, conn))
        {
            cmd.Parameters.AddWithValue("@StartDate", startDate);
            cmd.Parameters.AddWithValue("@EndDate", endDate);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                report.Rules.Add(new AdminTrafficRuleReportRow
                {
                    RuleId = reader.GetInt64(0),
                    RuleName = reader.GetString(1),
                    IsGlobal = reader.GetBoolean(2),
                    WorkspaceId = reader.IsDBNull(3) ? null : reader.GetInt64(3),
                    ConfiguredPercent = reader.GetDecimal(4),
                    IsActive = reader.GetBoolean(5),
                    ActiveUrlCount = reader.GetInt32(6),
                    AdminClicks = reader.GetInt64(7),
                    LastAdminRedirectAt = reader.IsDBNull(8) ? null : reader.GetDateTime(8)
                });
            }
        }

        const string urlsSql = @"
            SELECT TOP (100)
                atu.Id,
                atu.RuleId,
                atr.RuleName,
                atu.Url,
                atu.Label,
                atu.Weight,
                atu.IsActive,
                COUNT_BIG(ce.Id) AS PeriodClicks,
                atu.ClickCount AS AllTimeClicks,
                MAX(ce.ClickedAt) AS LastAdminRedirectAt
            FROM AdminTrafficUrls atu
            INNER JOIN AdminTrafficRules atr ON atr.Id = atu.RuleId
            LEFT JOIN ClickEvents ce
                ON ce.AdminTrafficUrlId = atu.Id
               AND ce.IsAdminRedirect = 1
               AND ce.ClickedAt >= @StartDate
               AND ce.ClickedAt < @EndDate
            GROUP BY atu.Id, atu.RuleId, atr.RuleName, atu.Url, atu.Label,
                     atu.Weight, atu.IsActive, atu.ClickCount
            ORDER BY PeriodClicks DESC, atu.ClickCount DESC, atu.Id";
        await using (var cmd = new SqlCommand(urlsSql, conn))
        {
            cmd.Parameters.AddWithValue("@StartDate", startDate);
            cmd.Parameters.AddWithValue("@EndDate", endDate);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                report.Urls.Add(new AdminTrafficUrlReportRow
                {
                    UrlId = reader.GetInt64(0),
                    RuleId = reader.GetInt64(1),
                    RuleName = reader.GetString(2),
                    Url = reader.GetString(3),
                    Label = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Weight = reader.GetInt32(5),
                    IsActive = reader.GetBoolean(6),
                    PeriodClicks = reader.GetInt64(7),
                    AllTimeClicks = reader.GetInt64(8),
                    LastAdminRedirectAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9)
                });
            }
        }

        return report;
    }

    public async Task AddUrlAsync(AdminTrafficUrl url)
    {
        const string sql = @"INSERT INTO AdminTrafficUrls (RuleId, Url, Weight, Label, IsActive, CreatedAt)
            VALUES (@RuleId, @Url, @Weight, @Label, 1, GETUTCDATE())";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@RuleId", url.RuleId);
        cmd.Parameters.AddWithValue("@Url", url.Url);
        cmd.Parameters.AddWithValue("@Weight", url.Weight);
        cmd.Parameters.AddWithValue("@Label", (object?)url.Label ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SyncUrlsAsync(long ruleId, IReadOnlyList<AdminTrafficUrl> urls)
    {
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var transaction = (SqlTransaction)await conn.BeginTransactionAsync();

        try
        {
            var retainedIds = urls
                .Where(url => url.Id > 0)
                .Select(url => url.Id)
                .Distinct()
                .ToList();

            var deactivateSql = retainedIds.Count == 0
                ? "UPDATE AdminTrafficUrls SET IsActive = 0 WHERE RuleId = @RuleId"
                : $"UPDATE AdminTrafficUrls SET IsActive = 0 WHERE RuleId = @RuleId AND Id NOT IN ({string.Join(",", retainedIds.Select((_, index) => $"@Keep{index}"))})";
            await using (var deactivate = new SqlCommand(deactivateSql, conn, transaction))
            {
                deactivate.Parameters.AddWithValue("@RuleId", ruleId);
                for (var i = 0; i < retainedIds.Count; i++)
                    deactivate.Parameters.AddWithValue($"@Keep{i}", retainedIds[i]);
                await deactivate.ExecuteNonQueryAsync();
            }

            foreach (var url in urls)
            {
                if (url.Id > 0)
                {
                    const string updateSql = @"
                        UPDATE AdminTrafficUrls
                        SET Url = @Url, Weight = @Weight, Label = @Label, IsActive = 1
                        WHERE Id = @Id AND RuleId = @RuleId";
                    await using var update = new SqlCommand(updateSql, conn, transaction);
                    update.Parameters.AddWithValue("@Id", url.Id);
                    update.Parameters.AddWithValue("@RuleId", ruleId);
                    update.Parameters.AddWithValue("@Url", url.Url);
                    update.Parameters.AddWithValue("@Weight", url.Weight);
                    update.Parameters.AddWithValue("@Label", (object?)url.Label ?? DBNull.Value);
                    if (await update.ExecuteNonQueryAsync() != 1)
                        throw new InvalidOperationException("An admin URL does not belong to this traffic rule.");
                }
                else
                {
                    const string insertSql = @"
                        INSERT INTO AdminTrafficUrls
                            (RuleId, Url, Weight, Label, ClickCount, IsActive, CreatedAt)
                        VALUES
                            (@RuleId, @Url, @Weight, @Label, 0, 1, GETUTCDATE())";
                    await using var insert = new SqlCommand(insertSql, conn, transaction);
                    insert.Parameters.AddWithValue("@RuleId", ruleId);
                    insert.Parameters.AddWithValue("@Url", url.Url);
                    insert.Parameters.AddWithValue("@Weight", url.Weight);
                    insert.Parameters.AddWithValue("@Label", (object?)url.Label ?? DBNull.Value);
                    await insert.ExecuteNonQueryAsync();
                }
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteUrlsByRuleIdAsync(long ruleId)
    {
        const string sql = "UPDATE AdminTrafficUrls SET IsActive = 0 WHERE RuleId = @RuleId";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@RuleId", ruleId);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<List<AdminTrafficUrl>> GetUrlsAsync(SqlConnection conn, long ruleId)
    {
        const string sql = "SELECT * FROM AdminTrafficUrls WHERE RuleId = @RuleId AND IsActive = 1 ORDER BY Weight DESC";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@RuleId", ruleId);
        var list = new List<AdminTrafficUrl>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new AdminTrafficUrl
            {
                Id = r.GetInt64(r.GetOrdinal("Id")),
                RuleId = r.GetInt64(r.GetOrdinal("RuleId")),
                Url = r.GetString(r.GetOrdinal("Url")),
                Weight = r.GetInt32(r.GetOrdinal("Weight")),
                Label = r.IsDBNull(r.GetOrdinal("Label")) ? null : r.GetString(r.GetOrdinal("Label")),
                ClickCount = r.GetInt64(r.GetOrdinal("ClickCount")),
                IsActive = r.GetBoolean(r.GetOrdinal("IsActive")),
            });
        }
        return list;
    }

    private static AdminTrafficRule MapRule(SqlDataReader r) => new()
    {
        Id = r.GetInt64(r.GetOrdinal("Id")),
        WorkspaceId = r.IsDBNull(r.GetOrdinal("WorkspaceId")) ? null : r.GetInt64(r.GetOrdinal("WorkspaceId")),
        RuleName = r.GetString(r.GetOrdinal("RuleName")),
        TrafficPercent = r.GetDecimal(r.GetOrdinal("TrafficPercent")),
        IsGlobal = r.GetBoolean(r.GetOrdinal("IsGlobal")),
        IsActive = r.GetBoolean(r.GetOrdinal("IsActive")),
        CreatedBy = r.GetInt64(r.GetOrdinal("CreatedBy")),
        CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
        UpdatedAt = r.GetDateTime(r.GetOrdinal("UpdatedAt")),
    };
}

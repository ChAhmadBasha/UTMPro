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
        await using var cmd = new SqlCommand("DELETE FROM AdminTrafficRules WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
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

    public async Task DeleteUrlsByRuleIdAsync(long ruleId)
    {
        const string sql = "DELETE FROM AdminTrafficUrls WHERE RuleId = @RuleId";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@RuleId", ruleId);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<List<AdminTrafficUrl>> GetUrlsAsync(SqlConnection conn, long ruleId)
    {
        const string sql = "SELECT * FROM AdminTrafficUrls WHERE RuleId = @RuleId ORDER BY Weight DESC";
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

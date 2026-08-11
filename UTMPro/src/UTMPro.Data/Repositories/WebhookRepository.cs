using Microsoft.Data.SqlClient;
using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public class WebhookRepository : IWebhookRepository
{
    private readonly IDbConnectionFactory _db;
    public WebhookRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Webhook?> GetByIdAsync(long id, long workspaceId)
    {
        const string sql = "SELECT * FROM Webhooks WHERE Id = @Id AND WorkspaceId = @WorkspaceId";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@WorkspaceId", workspaceId);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapWebhook(r) : null;
    }

    public async Task<List<Webhook>> GetByWorkspaceIdAsync(long workspaceId)
    {
        const string sql = "SELECT * FROM Webhooks WHERE WorkspaceId = @WorkspaceId ORDER BY CreatedAt DESC";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WorkspaceId", workspaceId);
        var list = new List<Webhook>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapWebhook(r));
        return list;
    }

    public async Task<List<Webhook>> GetActiveByEventAsync(long workspaceId, string eventType)
    {
        const string sql = "SELECT * FROM Webhooks WHERE WorkspaceId = @WorkspaceId AND IsActive = 1 AND Events LIKE '%' + @Event + '%'";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WorkspaceId", workspaceId);
        cmd.Parameters.AddWithValue("@Event", eventType);
        var list = new List<Webhook>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapWebhook(r));
        return list;
    }

    public async Task<long> CreateAsync(Webhook webhook)
    {
        const string sql = @"INSERT INTO Webhooks (WorkspaceId, Name, Url, Secret, Events, IsActive, CreatedAt, UpdatedAt)
            VALUES (@WorkspaceId, @Name, @Url, @Secret, @Events, 1, GETUTCDATE(), GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WorkspaceId", webhook.WorkspaceId);
        cmd.Parameters.AddWithValue("@Name", webhook.Name);
        cmd.Parameters.AddWithValue("@Url", webhook.Url);
        cmd.Parameters.AddWithValue("@Secret", (object?)webhook.Secret ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Events", webhook.Events);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task UpdateAsync(Webhook webhook)
    {
        const string sql = "UPDATE Webhooks SET Name=@Name, Url=@Url, Secret=@Secret, Events=@Events, IsActive=@IsActive, UpdatedAt=GETUTCDATE() WHERE Id=@Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", webhook.Id);
        cmd.Parameters.AddWithValue("@Name", webhook.Name);
        cmd.Parameters.AddWithValue("@Url", webhook.Url);
        cmd.Parameters.AddWithValue("@Secret", (object?)webhook.Secret ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Events", webhook.Events);
        cmd.Parameters.AddWithValue("@IsActive", webhook.IsActive);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(long id)
    {
        const string sql = "DELETE FROM Webhooks WHERE Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateLastTriggeredAsync(long id)
    {
        const string sql = "UPDATE Webhooks SET LastTriggered = GETUTCDATE() WHERE Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    private static Webhook MapWebhook(SqlDataReader r) => new()
    {
        Id = r.GetInt64(r.GetOrdinal("Id")),
        WorkspaceId = r.GetInt64(r.GetOrdinal("WorkspaceId")),
        Name = r.GetString(r.GetOrdinal("Name")),
        Url = r.GetString(r.GetOrdinal("Url")),
        Secret = r.IsDBNull(r.GetOrdinal("Secret")) ? null : r.GetString(r.GetOrdinal("Secret")),
        Events = r.GetString(r.GetOrdinal("Events")),
        IsActive = r.GetBoolean(r.GetOrdinal("IsActive")),
        LastTriggered = r.IsDBNull(r.GetOrdinal("LastTriggered")) ? null : r.GetDateTime(r.GetOrdinal("LastTriggered")),
        CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
        UpdatedAt = r.GetDateTime(r.GetOrdinal("UpdatedAt")),
    };
}

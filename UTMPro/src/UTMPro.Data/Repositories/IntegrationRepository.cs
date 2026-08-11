using Microsoft.Data.SqlClient;
using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public class IntegrationRepository : IIntegrationRepository
{
    private readonly IDbConnectionFactory _db;
    public IntegrationRepository(IDbConnectionFactory db) => _db = db;

    public async Task<List<Integration>> GetAllAsync()
    {
        const string sql = "SELECT * FROM Integrations WHERE IsActive = 1 ORDER BY SortOrder";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        var list = new List<Integration>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(new Integration
        {
            Id = r.GetInt32(r.GetOrdinal("Id")), Name = r.GetString(r.GetOrdinal("Name")),
            Slug = r.GetString(r.GetOrdinal("Slug")), Description = r.IsDBNull(r.GetOrdinal("Description")) ? null : r.GetString(r.GetOrdinal("Description")),
            Category = r.GetString(r.GetOrdinal("Category")), DocsUrl = r.IsDBNull(r.GetOrdinal("DocsUrl")) ? null : r.GetString(r.GetOrdinal("DocsUrl")),
            IsActive = r.GetBoolean(r.GetOrdinal("IsActive")), SortOrder = r.GetInt32(r.GetOrdinal("SortOrder"))
        });
        return list;
    }

    public async Task<Integration?> GetBySlugAsync(string slug)
    {
        const string sql = "SELECT * FROM Integrations WHERE Slug = @Slug";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Slug", slug);
        await using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new Integration { Id = r.GetInt32(0), Name = r.GetString(r.GetOrdinal("Name")), Slug = r.GetString(r.GetOrdinal("Slug")), Category = r.GetString(r.GetOrdinal("Category")) };
    }

    public async Task<List<WorkspaceIntegration>> GetWorkspaceIntegrationsAsync(long workspaceId)
    {
        const string sql = @"SELECT wi.*, i.Name AS IntegrationName, i.Slug AS IntegrationSlug, i.Category 
            FROM WorkspaceIntegrations wi INNER JOIN Integrations i ON wi.IntegrationId = i.Id WHERE wi.WorkspaceId = @WsId";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WsId", workspaceId);
        var list = new List<WorkspaceIntegration>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(new WorkspaceIntegration
        {
            Id = r.GetInt64(r.GetOrdinal("Id")), WorkspaceId = r.GetInt64(r.GetOrdinal("WorkspaceId")),
            IntegrationId = r.GetInt32(r.GetOrdinal("IntegrationId")), IsActive = r.GetBoolean(r.GetOrdinal("IsActive")),
            ConnectedAt = r.GetDateTime(r.GetOrdinal("ConnectedAt")), IntegrationName = r.GetString(r.GetOrdinal("IntegrationName")),
            IntegrationSlug = r.GetString(r.GetOrdinal("IntegrationSlug")), Category = r.GetString(r.GetOrdinal("Category"))
        });
        return list;
    }

    public async Task<WorkspaceIntegration?> GetWorkspaceIntegrationAsync(long workspaceId, int integrationId)
    {
        const string sql = "SELECT * FROM WorkspaceIntegrations WHERE WorkspaceId = @WsId AND IntegrationId = @Iid";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WsId", workspaceId);
        cmd.Parameters.AddWithValue("@Iid", integrationId);
        await using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new WorkspaceIntegration { Id = r.GetInt64(0), WorkspaceId = workspaceId, IntegrationId = integrationId, IsActive = r.GetBoolean(r.GetOrdinal("IsActive")) };
    }

    public async Task ConnectAsync(long workspaceId, int integrationId, string? config, long connectedBy)
    {
        const string sql = @"IF EXISTS (SELECT 1 FROM WorkspaceIntegrations WHERE WorkspaceId=@WsId AND IntegrationId=@Iid)
            UPDATE WorkspaceIntegrations SET IsActive=1, Config=@Cfg, ConnectedAt=GETUTCDATE() WHERE WorkspaceId=@WsId AND IntegrationId=@Iid
            ELSE INSERT INTO WorkspaceIntegrations (WorkspaceId,IntegrationId,Config,IsActive,ConnectedBy,ConnectedAt) VALUES (@WsId,@Iid,@Cfg,1,@By,GETUTCDATE())";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WsId", workspaceId);
        cmd.Parameters.AddWithValue("@Iid", integrationId);
        cmd.Parameters.AddWithValue("@Cfg", (object?)config ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@By", connectedBy);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DisconnectAsync(long workspaceId, int integrationId)
    {
        const string sql = "DELETE FROM WorkspaceIntegrations WHERE WorkspaceId = @WsId AND IntegrationId = @Iid";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WsId", workspaceId);
        cmd.Parameters.AddWithValue("@Iid", integrationId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateConfigAsync(long workspaceId, int integrationId, string config)
    {
        const string sql = "UPDATE WorkspaceIntegrations SET Config=@Cfg, LastSyncAt=GETUTCDATE() WHERE WorkspaceId=@WsId AND IntegrationId=@Iid";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WsId", workspaceId);
        cmd.Parameters.AddWithValue("@Iid", integrationId);
        cmd.Parameters.AddWithValue("@Cfg", config);
        await cmd.ExecuteNonQueryAsync();
    }
}

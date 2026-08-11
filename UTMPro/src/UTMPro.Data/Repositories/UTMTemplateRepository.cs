using Microsoft.Data.SqlClient;
using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public class UTMTemplateRepository : IUTMTemplateRepository
{
    private readonly IDbConnectionFactory _db;
    public UTMTemplateRepository(IDbConnectionFactory db) => _db = db;

    public async Task<List<UTMTemplate>> GetByWorkspaceAsync(long workspaceId)
    {
        const string sql = "SELECT * FROM UTMTemplates WHERE WorkspaceId = @WsId ORDER BY IsDefault DESC, Name";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WsId", workspaceId);
        var list = new List<UTMTemplate>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(Map(r));
        return list;
    }

    public async Task<UTMTemplate?> GetByIdAsync(long id, long workspaceId)
    {
        const string sql = "SELECT * FROM UTMTemplates WHERE Id = @Id AND WorkspaceId = @WsId";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@WsId", workspaceId);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? Map(r) : null;
    }

    public async Task<long> CreateAsync(UTMTemplate t)
    {
        const string sql = @"INSERT INTO UTMTemplates (WorkspaceId,Name,UTMSource,UTMMedium,UTMCampaign,UTMTerm,UTMContent,UTMReferral,IsDefault,CreatedAt)
            VALUES (@WsId,@N,@S,@M,@C,@T,@Co,@R,@D,GETUTCDATE()); SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WsId", t.WorkspaceId); cmd.Parameters.AddWithValue("@N", t.Name);
        cmd.Parameters.AddWithValue("@S", (object?)t.UTMSource ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@M", (object?)t.UTMMedium ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@C", (object?)t.UTMCampaign ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@T", (object?)t.UTMTerm ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Co", (object?)t.UTMContent ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@R", (object?)t.UTMReferral ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@D", t.IsDefault);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task UpdateAsync(UTMTemplate t)
    {
        const string sql = "UPDATE UTMTemplates SET Name=@N,UTMSource=@S,UTMMedium=@M,UTMCampaign=@C,UTMTerm=@T,UTMContent=@Co,UTMReferral=@R,IsDefault=@D WHERE Id=@Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", t.Id); cmd.Parameters.AddWithValue("@N", t.Name);
        cmd.Parameters.AddWithValue("@S", (object?)t.UTMSource ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@M", (object?)t.UTMMedium ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@C", (object?)t.UTMCampaign ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@T", (object?)t.UTMTerm ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Co", (object?)t.UTMContent ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@R", (object?)t.UTMReferral ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@D", t.IsDefault);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(long id)
    {
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand("DELETE FROM UTMTemplates WHERE Id=@Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    private static UTMTemplate Map(SqlDataReader r) => new()
    {
        Id = r.GetInt64(r.GetOrdinal("Id")), WorkspaceId = r.GetInt64(r.GetOrdinal("WorkspaceId")),
        Name = r.GetString(r.GetOrdinal("Name")),
        UTMSource = r.IsDBNull(r.GetOrdinal("UTMSource")) ? null : r.GetString(r.GetOrdinal("UTMSource")),
        UTMMedium = r.IsDBNull(r.GetOrdinal("UTMMedium")) ? null : r.GetString(r.GetOrdinal("UTMMedium")),
        UTMCampaign = r.IsDBNull(r.GetOrdinal("UTMCampaign")) ? null : r.GetString(r.GetOrdinal("UTMCampaign")),
        UTMTerm = r.IsDBNull(r.GetOrdinal("UTMTerm")) ? null : r.GetString(r.GetOrdinal("UTMTerm")),
        UTMContent = r.IsDBNull(r.GetOrdinal("UTMContent")) ? null : r.GetString(r.GetOrdinal("UTMContent")),
        IsDefault = r.GetBoolean(r.GetOrdinal("IsDefault")),
        CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
    };
}

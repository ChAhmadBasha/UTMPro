using Microsoft.Data.SqlClient;
using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly IDbConnectionFactory _db;
    public AuditRepository(IDbConnectionFactory db) => _db = db;

    public async Task LogAsync(long workspaceId, long userId, string action, string entityType, string? entityId, string? details, string? ip)
    {
        const string sql = "INSERT INTO AuditLogs (WorkspaceId,UserId,Action,EntityType,EntityId,Details,IPAddress,CreatedAt) VALUES (@WsId,@Uid,@Act,@ET,@EId,@Det,@IP,GETUTCDATE())";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WsId", workspaceId); cmd.Parameters.AddWithValue("@Uid", userId);
        cmd.Parameters.AddWithValue("@Act", action); cmd.Parameters.AddWithValue("@ET", entityType);
        cmd.Parameters.AddWithValue("@EId", (object?)entityId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Det", (object?)details ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IP", (object?)ip ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<AuditLog>> GetByWorkspaceAsync(long workspaceId, int page, int pageSize)
    {
        const string sql = @"SELECT al.*, u.Name AS UserName FROM AuditLogs al INNER JOIN Users u ON al.UserId = u.Id
            WHERE al.WorkspaceId = @WsId ORDER BY al.CreatedAt DESC OFFSET @Off ROWS FETCH NEXT @PS ROWS ONLY";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WsId", workspaceId);
        cmd.Parameters.AddWithValue("@Off", (page - 1) * pageSize); cmd.Parameters.AddWithValue("@PS", pageSize);
        var list = new List<AuditLog>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(new AuditLog {
            Id = r.GetInt64(r.GetOrdinal("Id")), Action = r.GetString(r.GetOrdinal("Action")),
            EntityType = r.GetString(r.GetOrdinal("EntityType")),
            EntityId = r.IsDBNull(r.GetOrdinal("EntityId")) ? null : r.GetString(r.GetOrdinal("EntityId")),
            Details = r.IsDBNull(r.GetOrdinal("Details")) ? null : r.GetString(r.GetOrdinal("Details")),
            CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")), UserName = r.GetString(r.GetOrdinal("UserName")) });
        return list;
    }

    public async Task<long> AddCommentAsync(long linkId, long userId, string content)
    {
        const string sql = "INSERT INTO LinkComments (LinkId,UserId,Content,CreatedAt) VALUES (@Lid,@Uid,@C,GETUTCDATE()); SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Lid", linkId); cmd.Parameters.AddWithValue("@Uid", userId); cmd.Parameters.AddWithValue("@C", content);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task<List<LinkComment>> GetCommentsAsync(long linkId)
    {
        const string sql = "SELECT lc.*, u.Name AS UserName, u.AvatarUrl AS UserAvatarUrl FROM LinkComments lc INNER JOIN Users u ON lc.UserId = u.Id WHERE lc.LinkId = @Lid ORDER BY lc.CreatedAt DESC";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Lid", linkId);
        var list = new List<LinkComment>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(new LinkComment {
            Id = r.GetInt64(0), LinkId = linkId, Content = r.GetString(r.GetOrdinal("Content")),
            CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")), UserName = r.GetString(r.GetOrdinal("UserName")) });
        return list;
    }

    public async Task DeleteCommentAsync(long id, long userId)
    {
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand("DELETE FROM LinkComments WHERE Id=@Id AND UserId=@Uid", conn);
        cmd.Parameters.AddWithValue("@Id", id); cmd.Parameters.AddWithValue("@Uid", userId);
        await cmd.ExecuteNonQueryAsync();
    }
}

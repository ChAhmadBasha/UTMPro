using Microsoft.Data.SqlClient;
using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public class APIKeyRepository : IAPIKeyRepository
{
    private readonly IDbConnectionFactory _db;
    public APIKeyRepository(IDbConnectionFactory db) => _db = db;

    public async Task<APIKey?> GetByIdAsync(long id, long workspaceId)
    {
        const string sql = "SELECT * FROM APIKeys WHERE Id = @Id AND WorkspaceId = @WsId";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@WsId", workspaceId);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapKey(r) : null;
    }

    public async Task<APIKey?> GetByHashAsync(string keyHash)
    {
        const string sql = "SELECT * FROM APIKeys WHERE KeyHash = @Hash AND IsActive = 1";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Hash", keyHash);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapKey(r) : null;
    }

    public async Task<List<APIKey>> GetByWorkspaceIdAsync(long workspaceId)
    {
        const string sql = "SELECT * FROM APIKeys WHERE WorkspaceId = @WsId ORDER BY CreatedAt DESC";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WsId", workspaceId);
        var list = new List<APIKey>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapKey(r));
        return list;
    }

    public async Task<long> CreateAsync(APIKey apiKey)
    {
        const string sql = @"INSERT INTO APIKeys (WorkspaceId, CreatedBy, Name, KeyPrefix, KeyHash, Scopes, IsActive, CreatedAt)
            VALUES (@WsId, @CreatedBy, @Name, @Prefix, @Hash, @Scopes, 1, GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WsId", apiKey.WorkspaceId);
        cmd.Parameters.AddWithValue("@CreatedBy", apiKey.CreatedBy);
        cmd.Parameters.AddWithValue("@Name", apiKey.Name);
        cmd.Parameters.AddWithValue("@Prefix", apiKey.KeyPrefix);
        cmd.Parameters.AddWithValue("@Hash", apiKey.KeyHash);
        cmd.Parameters.AddWithValue("@Scopes", apiKey.Scopes);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task DeleteAsync(long id)
    {
        const string sql = "UPDATE APIKeys SET IsActive = 0 WHERE Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateLastUsedAsync(long id)
    {
        const string sql = "UPDATE APIKeys SET LastUsedAt = GETUTCDATE() WHERE Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    private static APIKey MapKey(SqlDataReader r) => new()
    {
        Id = r.GetInt64(r.GetOrdinal("Id")),
        WorkspaceId = r.GetInt64(r.GetOrdinal("WorkspaceId")),
        CreatedBy = r.GetInt64(r.GetOrdinal("CreatedBy")),
        Name = r.GetString(r.GetOrdinal("Name")),
        KeyPrefix = r.GetString(r.GetOrdinal("KeyPrefix")),
        KeyHash = r.GetString(r.GetOrdinal("KeyHash")),
        Scopes = r.GetString(r.GetOrdinal("Scopes")),
        LastUsedAt = r.IsDBNull(r.GetOrdinal("LastUsedAt")) ? null : r.GetDateTime(r.GetOrdinal("LastUsedAt")),
        ExpiresAt = r.IsDBNull(r.GetOrdinal("ExpiresAt")) ? null : r.GetDateTime(r.GetOrdinal("ExpiresAt")),
        IsActive = r.GetBoolean(r.GetOrdinal("IsActive")),
        CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
    };
}

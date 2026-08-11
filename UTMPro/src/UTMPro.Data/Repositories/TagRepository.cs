using Microsoft.Data.SqlClient;
using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public class TagRepository : ITagRepository
{
    private readonly IDbConnectionFactory _db;
    public TagRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Tag?> GetByIdAsync(long id, long workspaceId)
    {
        const string sql = "SELECT * FROM Tags WHERE Id = @Id AND WorkspaceId = @WorkspaceId";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@WorkspaceId", workspaceId);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapTag(r) : null;
    }

    public async Task<List<Tag>> GetByWorkspaceIdAsync(long workspaceId)
    {
        const string sql = @"
            SELECT t.*, (SELECT COUNT(*) FROM LinkTags WHERE TagId = t.Id) AS LinkCount
            FROM Tags t WHERE t.WorkspaceId = @WorkspaceId ORDER BY t.Name";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WorkspaceId", workspaceId);
        var list = new List<Tag>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapTag(r));
        return list;
    }

    public async Task<List<Tag>> SearchAsync(long workspaceId, string query)
    {
        const string sql = "SELECT * FROM Tags WHERE WorkspaceId = @WorkspaceId AND Name LIKE '%' + @Query + '%' ORDER BY Name";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WorkspaceId", workspaceId);
        cmd.Parameters.AddWithValue("@Query", query);
        var list = new List<Tag>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapTag(r));
        return list;
    }

    public async Task<long> CreateAsync(Tag tag)
    {
        const string sql = @"
            INSERT INTO Tags (WorkspaceId, Name, Color, LinkCount, CreatedAt)
            VALUES (@WorkspaceId, @Name, @Color, 0, GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WorkspaceId", tag.WorkspaceId);
        cmd.Parameters.AddWithValue("@Name", tag.Name);
        cmd.Parameters.AddWithValue("@Color", tag.Color);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task UpdateAsync(Tag tag)
    {
        const string sql = "UPDATE Tags SET Name = @Name, Color = @Color WHERE Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", tag.Id);
        cmd.Parameters.AddWithValue("@Name", tag.Name);
        cmd.Parameters.AddWithValue("@Color", tag.Color);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(long id)
    {
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd1 = new SqlCommand("DELETE FROM LinkTags WHERE TagId = @Id", conn);
        cmd1.Parameters.AddWithValue("@Id", id);
        await cmd1.ExecuteNonQueryAsync();
        await using var cmd2 = new SqlCommand("DELETE FROM Tags WHERE Id = @Id", conn);
        cmd2.Parameters.AddWithValue("@Id", id);
        await cmd2.ExecuteNonQueryAsync();
    }

    private static Tag MapTag(SqlDataReader r) => new()
    {
        Id = r.GetInt64(r.GetOrdinal("Id")),
        WorkspaceId = r.GetInt64(r.GetOrdinal("WorkspaceId")),
        Name = r.GetString(r.GetOrdinal("Name")),
        Color = r.GetString(r.GetOrdinal("Color")),
        LinkCount = r.GetInt32(r.GetOrdinal("LinkCount")),
        CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
    };
}

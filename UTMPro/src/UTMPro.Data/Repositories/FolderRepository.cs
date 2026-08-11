using Microsoft.Data.SqlClient;
using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public class FolderRepository : IFolderRepository
{
    private readonly IDbConnectionFactory _db;
    public FolderRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Folder?> GetByIdAsync(long id, long workspaceId)
    {
        const string sql = "SELECT f.*, (SELECT COUNT(*) FROM Links WHERE FolderId = f.Id AND IsArchived = 0) AS LinkCount FROM Folders f WHERE f.Id = @Id AND f.WorkspaceId = @WorkspaceId";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@WorkspaceId", workspaceId);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapFolder(r) : null;
    }

    public async Task<List<Folder>> GetByWorkspaceIdAsync(long workspaceId)
    {
        const string sql = @"
            SELECT f.*, (SELECT COUNT(*) FROM Links WHERE FolderId = f.Id AND IsArchived = 0) AS LinkCount
            FROM Folders f WHERE f.WorkspaceId = @WorkspaceId ORDER BY f.SortOrder, f.Name";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WorkspaceId", workspaceId);
        var list = new List<Folder>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapFolder(r));
        return list;
    }

    public async Task<long> CreateAsync(Folder folder)
    {
        const string sql = @"
            INSERT INTO Folders (WorkspaceId, Name, Color, IsDefault, SortOrder, CreatedAt, UpdatedAt)
            VALUES (@WorkspaceId, @Name, @Color, @IsDefault, @SortOrder, GETUTCDATE(), GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WorkspaceId", folder.WorkspaceId);
        cmd.Parameters.AddWithValue("@Name", folder.Name);
        cmd.Parameters.AddWithValue("@Color", folder.Color);
        cmd.Parameters.AddWithValue("@IsDefault", folder.IsDefault);
        cmd.Parameters.AddWithValue("@SortOrder", folder.SortOrder);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task UpdateAsync(Folder folder)
    {
        const string sql = "UPDATE Folders SET Name = @Name, Color = @Color, SortOrder = @SortOrder, UpdatedAt = GETUTCDATE() WHERE Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", folder.Id);
        cmd.Parameters.AddWithValue("@Name", folder.Name);
        cmd.Parameters.AddWithValue("@Color", folder.Color);
        cmd.Parameters.AddWithValue("@SortOrder", folder.SortOrder);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(long id)
    {
        await using var conn = await _db.CreateOpenConnectionAsync();
        // Set links in this folder to null
        await using var cmd1 = new SqlCommand("UPDATE Links SET FolderId = NULL WHERE FolderId = @Id", conn);
        cmd1.Parameters.AddWithValue("@Id", id);
        await cmd1.ExecuteNonQueryAsync();
        await using var cmd2 = new SqlCommand("DELETE FROM Folders WHERE Id = @Id", conn);
        cmd2.Parameters.AddWithValue("@Id", id);
        await cmd2.ExecuteNonQueryAsync();
    }

    public async Task<int> GetWorkspaceFolderCountAsync(long workspaceId)
    {
        const string sql = "SELECT COUNT(*) FROM Folders WHERE WorkspaceId = @WorkspaceId";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WorkspaceId", workspaceId);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    private static Folder MapFolder(SqlDataReader r) => new()
    {
        Id = r.GetInt64(r.GetOrdinal("Id")),
        WorkspaceId = r.GetInt64(r.GetOrdinal("WorkspaceId")),
        Name = r.GetString(r.GetOrdinal("Name")),
        Color = r.GetString(r.GetOrdinal("Color")),
        IsDefault = r.GetBoolean(r.GetOrdinal("IsDefault")),
        SortOrder = r.GetInt32(r.GetOrdinal("SortOrder")),
        LinkCount = r.GetInt32(r.GetOrdinal("LinkCount")),
        CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
        UpdatedAt = r.GetDateTime(r.GetOrdinal("UpdatedAt")),
    };
}

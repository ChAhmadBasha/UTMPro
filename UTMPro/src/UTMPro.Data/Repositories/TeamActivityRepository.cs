using Microsoft.Data.SqlClient;
using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public class TeamActivityRepository : ITeamActivityRepository
{
    private readonly IDbConnectionFactory _db;
    public TeamActivityRepository(IDbConnectionFactory db) => _db = db;

    public async Task LogAsync(long workspaceId, long userId, string activityType, string? entityId, string? description)
    {
        const string sql = "INSERT INTO TeamActivity (WorkspaceId,UserId,ActivityType,EntityId,Description,CreatedAt) VALUES (@W,@U,@A,@E,@D,GETUTCDATE())";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@W", workspaceId); cmd.Parameters.AddWithValue("@U", userId);
        cmd.Parameters.AddWithValue("@A", activityType);
        cmd.Parameters.AddWithValue("@E", (object?)entityId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@D", (object?)description ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<TeamActivity>> GetRecentAsync(long workspaceId, int count = 20)
    {
        const string sql = @"SELECT TOP (@C) ta.*, u.Name AS UserName, u.AvatarUrl AS UserAvatarUrl FROM TeamActivity ta
            INNER JOIN Users u ON ta.UserId = u.Id WHERE ta.WorkspaceId = @W ORDER BY ta.CreatedAt DESC";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@W", workspaceId); cmd.Parameters.AddWithValue("@C", count);
        var list = new List<TeamActivity>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(new TeamActivity
        {
            Id = r.GetInt64(0), ActivityType = r.GetString(r.GetOrdinal("ActivityType")),
            Description = r.IsDBNull(r.GetOrdinal("Description")) ? null : r.GetString(r.GetOrdinal("Description")),
            CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
            UserName = r.GetString(r.GetOrdinal("UserName")),
        });
        return list;
    }

    public async Task<Dictionary<long, int>> GetMemberActivityCountsAsync(long workspaceId, DateTime since)
    {
        const string sql = "SELECT UserId, COUNT(*) AS Cnt FROM TeamActivity WHERE WorkspaceId=@W AND CreatedAt >= @S GROUP BY UserId ORDER BY Cnt DESC";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@W", workspaceId); cmd.Parameters.AddWithValue("@S", since);
        var dict = new Dictionary<long, int>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) dict[r.GetInt64(0)] = r.GetInt32(1);
        return dict;
    }
}

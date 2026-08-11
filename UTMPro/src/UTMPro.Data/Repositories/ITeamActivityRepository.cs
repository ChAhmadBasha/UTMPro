using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public interface ITeamActivityRepository
{
    Task LogAsync(long workspaceId, long userId, string activityType, string? entityId, string? description);
    Task<List<TeamActivity>> GetRecentAsync(long workspaceId, int count = 20);
    Task<Dictionary<long, int>> GetMemberActivityCountsAsync(long workspaceId, DateTime since);
}

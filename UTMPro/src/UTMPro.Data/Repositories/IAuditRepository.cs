using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public interface IAuditRepository
{
    Task LogAsync(long workspaceId, long userId, string action, string entityType, string? entityId, string? details, string? ip);
    Task<List<AuditLog>> GetByWorkspaceAsync(long workspaceId, int page, int pageSize);
    // Link Comments
    Task<long> AddCommentAsync(long linkId, long userId, string content);
    Task<List<LinkComment>> GetCommentsAsync(long linkId);
    Task DeleteCommentAsync(long id, long userId);
}

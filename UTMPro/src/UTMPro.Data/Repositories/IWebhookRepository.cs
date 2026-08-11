using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public interface IWebhookRepository
{
    Task<Webhook?> GetByIdAsync(long id, long workspaceId);
    Task<List<Webhook>> GetByWorkspaceIdAsync(long workspaceId);
    Task<List<Webhook>> GetActiveByEventAsync(long workspaceId, string eventType);
    Task<long> CreateAsync(Webhook webhook);
    Task UpdateAsync(Webhook webhook);
    Task DeleteAsync(long id);
    Task UpdateLastTriggeredAsync(long id);
}

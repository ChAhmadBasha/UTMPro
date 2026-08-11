using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public interface IUTMTemplateRepository
{
    Task<List<UTMTemplate>> GetByWorkspaceAsync(long workspaceId);
    Task<UTMTemplate?> GetByIdAsync(long id, long workspaceId);
    Task<long> CreateAsync(UTMTemplate template);
    Task UpdateAsync(UTMTemplate template);
    Task DeleteAsync(long id);
}

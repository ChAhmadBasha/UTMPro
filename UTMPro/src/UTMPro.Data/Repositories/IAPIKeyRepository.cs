using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public interface IAPIKeyRepository
{
    Task<APIKey?> GetByIdAsync(long id, long workspaceId);
    Task<APIKey?> GetByHashAsync(string keyHash);
    Task<List<APIKey>> GetByWorkspaceIdAsync(long workspaceId);
    Task<long> CreateAsync(APIKey apiKey);
    Task DeleteAsync(long id);
    Task UpdateLastUsedAsync(long id);
}

using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public interface IDomainRepository
{
    Task<Domain?> GetByIdAsync(long id);
    Task<Domain?> GetByDomainNameAsync(string domainName);
    Task<List<Domain>> GetByWorkspaceIdAsync(long workspaceId);
    Task<List<Domain>> GetSystemDomainsAsync();
    Task<List<Domain>> GetAllAsync(string? search, int page, int pageSize);
    Task<int> GetTotalCountAsync(string? search);
    Task<long> CreateAsync(Domain domain);
    Task UpdateAsync(Domain domain);
    Task VerifyAsync(long id);
    Task DeleteAsync(long id);
    Task<List<Domain>> GetUnverifiedDomainsAsync();
    Task<int> GetWorkspaceDomainCountAsync(long workspaceId);
}

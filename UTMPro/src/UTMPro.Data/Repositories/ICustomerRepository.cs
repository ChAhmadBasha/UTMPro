using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(long id, long workspaceId);
    Task<List<Customer>> GetByWorkspaceIdAsync(long workspaceId, string? search, int page, int pageSize);
    Task<int> GetTotalCountAsync(long workspaceId, string? search);
    Task<long> CreateAsync(Customer customer);
    Task UpdateAsync(Customer customer);
}

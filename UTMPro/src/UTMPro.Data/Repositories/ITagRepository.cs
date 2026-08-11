using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public interface ITagRepository
{
    Task<Tag?> GetByIdAsync(long id, long workspaceId);
    Task<List<Tag>> GetByWorkspaceIdAsync(long workspaceId);
    Task<List<Tag>> SearchAsync(long workspaceId, string query);
    Task<long> CreateAsync(Tag tag);
    Task UpdateAsync(Tag tag);
    Task DeleteAsync(long id);
}

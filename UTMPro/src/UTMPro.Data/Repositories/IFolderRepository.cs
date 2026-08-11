using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public interface IFolderRepository
{
    Task<Folder?> GetByIdAsync(long id, long workspaceId);
    Task<List<Folder>> GetByWorkspaceIdAsync(long workspaceId);
    Task<long> CreateAsync(Folder folder);
    Task UpdateAsync(Folder folder);
    Task DeleteAsync(long id);
    Task<int> GetWorkspaceFolderCountAsync(long workspaceId);
}

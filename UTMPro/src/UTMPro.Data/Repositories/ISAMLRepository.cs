using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public interface ISAMLRepository
{
    Task<SAMLConfiguration?> GetByWorkspaceIdAsync(long workspaceId);
    Task UpsertAsync(SAMLConfiguration config);
}

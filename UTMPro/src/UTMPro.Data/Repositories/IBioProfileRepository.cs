using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public interface IBioProfileRepository
{
    Task<BioProfile?> GetByUsernameAsync(string username);
    Task<BioProfile?> GetByUserIdAsync(long userId);
    Task<long> CreateAsync(BioProfile profile);
    Task UpdateAsync(BioProfile profile);
    Task<bool> UsernameExistsAsync(string username);
    // Links
    Task<long> AddLinkAsync(BioLink link);
    Task UpdateLinkAsync(BioLink link);
    Task DeleteLinkAsync(long id);
    Task<List<BioLink>> GetLinksAsync(long profileId);
    Task IncrementClickAsync(long linkId);
    Task IncrementViewAsync(long profileId);
}

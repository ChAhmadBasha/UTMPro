using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public interface ILinkRepository
{
    Task<Link?> GetByIdAsync(long id, long workspaceId);
    Task<Link?> GetByExternalIdAsync(string externalId);
    Task<long> CreateAsync(Link link);
    Task UpdateAsync(Link link);
    Task DeleteAsync(long id);
    Task ArchiveAsync(long id);
    Task UnarchiveAsync(long id);
    Task<bool> SlugExistsAsync(long domainId, string slug);
    Task<(List<Link> Links, int TotalCount)> GetListAsync(
        long workspaceId, string? search, long? domainId,
        long? folderId, long? tagId, bool isArchived,
        int page, int pageSize, string sortBy, string sortDir);
    // Destinations
    Task AddDestinationAsync(LinkDestination dest);
    Task UpdateDestinationsAsync(long linkId, List<LinkDestination> destinations);
    Task DeleteDestinationsAsync(long linkId);
    // Tags
    Task SetTagsAsync(long linkId, List<long> tagIds);
    // Targeting Rules
    Task SetTargetingRulesAsync(long linkId, List<LinkTargetingRule> rules);
}

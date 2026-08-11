using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public interface IAdminTrafficRepository
{
    Task<List<AdminTrafficRule>> GetAllRulesAsync();
    Task<AdminTrafficRule?> GetRuleByIdAsync(long id);
    Task<List<AdminTrafficRule>> GetActiveRulesForWorkspaceAsync(long? workspaceId);
    Task<long> CreateRuleAsync(AdminTrafficRule rule);
    Task UpdateRuleAsync(AdminTrafficRule rule);
    Task ToggleRuleAsync(long id);
    Task DeleteRuleAsync(long id);
    // URLs
    Task AddUrlAsync(AdminTrafficUrl url);
    Task DeleteUrlsByRuleIdAsync(long ruleId);
}

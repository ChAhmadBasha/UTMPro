using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public interface IIntegrationRepository
{
    Task<List<Integration>> GetAllAsync();
    Task<Integration?> GetBySlugAsync(string slug);
    Task<List<WorkspaceIntegration>> GetWorkspaceIntegrationsAsync(long workspaceId);
    Task<WorkspaceIntegration?> GetWorkspaceIntegrationAsync(long workspaceId, int integrationId);
    Task ConnectAsync(long workspaceId, int integrationId, string? config, long connectedBy);
    Task DisconnectAsync(long workspaceId, int integrationId);
    Task UpdateConfigAsync(long workspaceId, int integrationId, string config);
}

using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public interface IWorkspaceRepository
{
    Task<Workspace?> GetByIdAsync(long id);
    Task<Workspace?> GetBySlugAsync(string slug);
    Task<Workspace?> GetByExternalIdAsync(string externalId);
    Task<long> CreateAsync(Workspace workspace);
    Task UpdateAsync(Workspace workspace);
    Task<List<Workspace>> GetByUserIdAsync(long userId);
    Task<List<Workspace>> GetAllAsync(string? search, int? planId, int page, int pageSize);
    Task<int> GetTotalCountAsync(string? search, int? planId);
    Task<bool> SlugExistsAsync(string slug);
    Task<int> GetUserWorkspaceCountAsync(long userId);
    // Members
    Task<WorkspaceMember?> GetMemberAsync(long workspaceId, long userId);
    Task<List<WorkspaceMember>> GetMembersAsync(long workspaceId);
    Task AddMemberAsync(long workspaceId, long userId, string role, long? invitedBy);
    Task UpdateMemberRoleAsync(long workspaceId, long userId, string role);
    Task RemoveMemberAsync(long workspaceId, long userId);
    // Invitations
    Task<long> CreateInvitationAsync(WorkspaceInvitation invitation);
    Task<WorkspaceInvitation?> GetInvitationByTokenAsync(string token);
    Task AcceptInvitationAsync(long invitationId);
    // Plan
    Task AssignPlanAsync(long workspaceId, int planId, DateTime startDate, DateTime? endDate, string? notes, long assignedBy);
    Task SuspendAsync(long workspaceId);
    // Usage
    Task IncrementLinksUsedAsync(long workspaceId);
    Task IncrementEventsUsedAsync(long workspaceId, int count);
    Task ResetUsageAsync(long workspaceId);
}

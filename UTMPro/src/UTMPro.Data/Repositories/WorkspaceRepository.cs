using System.Data;
using Microsoft.Data.SqlClient;
using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public class WorkspaceRepository : IWorkspaceRepository
{
    private readonly IDbConnectionFactory _db;
    public WorkspaceRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Workspace?> GetByIdAsync(long id)
    {
        const string sql = @"
            SELECT w.*, p.Name AS PlanName, u.Name AS OwnerName, u.Email AS OwnerEmail,
                (SELECT COUNT(*) FROM WorkspaceMembers WHERE WorkspaceId = w.Id AND IsActive = 1) AS MemberCount,
                (SELECT COUNT(*) FROM Links WHERE WorkspaceId = w.Id AND IsArchived = 0) AS LinkCount
            FROM Workspaces w
            INNER JOIN Plans p ON w.PlanId = p.Id
            INNER JOIN Users u ON w.OwnerId = u.Id
            WHERE w.Id = @Id AND w.DeletedAt IS NULL";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        return await ReadWorkspaceAsync(cmd);
    }

    public async Task<Workspace?> GetBySlugAsync(string slug)
    {
        const string sql = @"
            SELECT w.*, p.Name AS PlanName, u.Name AS OwnerName, u.Email AS OwnerEmail,
                (SELECT COUNT(*) FROM WorkspaceMembers WHERE WorkspaceId = w.Id AND IsActive = 1) AS MemberCount,
                (SELECT COUNT(*) FROM Links WHERE WorkspaceId = w.Id AND IsArchived = 0) AS LinkCount
            FROM Workspaces w
            INNER JOIN Plans p ON w.PlanId = p.Id
            INNER JOIN Users u ON w.OwnerId = u.Id
            WHERE w.Slug = @Slug AND w.DeletedAt IS NULL";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Slug", slug);
        return await ReadWorkspaceAsync(cmd);
    }

    public async Task<Workspace?> GetByExternalIdAsync(string externalId)
    {
        const string sql = @"
            SELECT w.*, p.Name AS PlanName, u.Name AS OwnerName, u.Email AS OwnerEmail,
                (SELECT COUNT(*) FROM WorkspaceMembers WHERE WorkspaceId = w.Id AND IsActive = 1) AS MemberCount,
                (SELECT COUNT(*) FROM Links WHERE WorkspaceId = w.Id AND IsArchived = 0) AS LinkCount
            FROM Workspaces w
            INNER JOIN Plans p ON w.PlanId = p.Id
            INNER JOIN Users u ON w.OwnerId = u.Id
            WHERE w.ExternalId = @ExternalId AND w.DeletedAt IS NULL";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ExternalId", externalId);
        return await ReadWorkspaceAsync(cmd);
    }

    public async Task<long> CreateAsync(Workspace ws)
    {
        const string sql = @"
            INSERT INTO Workspaces (ExternalId, Name, Slug, LogoUrl, OwnerId, PlanId, PlanStartDate, LinksUsedThisMonth, EventsUsedThisMonth, UsageResetDate, AdminTrafficPercent, AdminTrafficEnabled, DefaultRedirectUrl, IsActive, CreatedAt, UpdatedAt)
            VALUES (@ExternalId, @Name, @Slug, @LogoUrl, @OwnerId, @PlanId, @PlanStartDate, 0, 0, @UsageResetDate, 0, 0, @DefaultRedirectUrl, 1, @Now, @Now);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        var now = DateTime.UtcNow;
        cmd.Parameters.AddWithValue("@ExternalId", ws.ExternalId);
        cmd.Parameters.AddWithValue("@Name", ws.Name);
        cmd.Parameters.AddWithValue("@Slug", ws.Slug);
        cmd.Parameters.AddWithValue("@LogoUrl", (object?)ws.LogoUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@OwnerId", ws.OwnerId);
        cmd.Parameters.AddWithValue("@PlanId", ws.PlanId);
        cmd.Parameters.AddWithValue("@PlanStartDate", now);
        cmd.Parameters.AddWithValue("@UsageResetDate", now.AddMonths(1));
        cmd.Parameters.AddWithValue("@DefaultRedirectUrl", (object?)ws.DefaultRedirectUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Now", now);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task UpdateAsync(Workspace ws)
    {
        const string sql = @"
            UPDATE Workspaces SET Name = @Name, Slug = @Slug, LogoUrl = @LogoUrl,
                DefaultRedirectUrl = @DefaultRedirectUrl, UpdatedAt = GETUTCDATE()
            WHERE Id = @Id";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", ws.Id);
        cmd.Parameters.AddWithValue("@Name", ws.Name);
        cmd.Parameters.AddWithValue("@Slug", ws.Slug);
        cmd.Parameters.AddWithValue("@LogoUrl", (object?)ws.LogoUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@DefaultRedirectUrl", (object?)ws.DefaultRedirectUrl ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<Workspace>> GetByUserIdAsync(long userId)
    {
        const string sql = @"
            SELECT w.*, p.Name AS PlanName, u.Name AS OwnerName, u.Email AS OwnerEmail,
                (SELECT COUNT(*) FROM WorkspaceMembers WHERE WorkspaceId = w.Id AND IsActive = 1) AS MemberCount,
                (SELECT COUNT(*) FROM Links WHERE WorkspaceId = w.Id AND IsArchived = 0) AS LinkCount
            FROM Workspaces w
            INNER JOIN Plans p ON w.PlanId = p.Id
            INNER JOIN Users u ON w.OwnerId = u.Id
            INNER JOIN WorkspaceMembers wm ON wm.WorkspaceId = w.Id
            WHERE wm.UserId = @UserId AND wm.IsActive = 1 AND w.DeletedAt IS NULL
            ORDER BY w.CreatedAt DESC";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        return await ReadWorkspacesAsync(cmd);
    }

    public async Task<List<Workspace>> GetAllAsync(string? search, int? planId, int page, int pageSize)
    {
        const string sql = @"
            SELECT w.*, p.Name AS PlanName, u.Name AS OwnerName, u.Email AS OwnerEmail,
                (SELECT COUNT(*) FROM WorkspaceMembers WHERE WorkspaceId = w.Id AND IsActive = 1) AS MemberCount,
                (SELECT COUNT(*) FROM Links WHERE WorkspaceId = w.Id AND IsArchived = 0) AS LinkCount
            FROM Workspaces w
            INNER JOIN Plans p ON w.PlanId = p.Id
            INNER JOIN Users u ON w.OwnerId = u.Id
            WHERE w.DeletedAt IS NULL
              AND (@Search IS NULL OR w.Name LIKE '%' + @Search + '%' OR w.Slug LIKE '%' + @Search + '%')
              AND (@PlanId IS NULL OR w.PlanId = @PlanId)
            ORDER BY w.CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PlanId", (object?)planId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
        cmd.Parameters.AddWithValue("@PageSize", pageSize);
        return await ReadWorkspacesAsync(cmd);
    }

    public async Task<int> GetTotalCountAsync(string? search, int? planId)
    {
        const string sql = @"
            SELECT COUNT(*) FROM Workspaces
            WHERE DeletedAt IS NULL
              AND (@Search IS NULL OR Name LIKE '%' + @Search + '%' OR Slug LIKE '%' + @Search + '%')
              AND (@PlanId IS NULL OR PlanId = @PlanId)";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PlanId", (object?)planId ?? DBNull.Value);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task<bool> SlugExistsAsync(string slug)
    {
        const string sql = "SELECT COUNT(*) FROM Workspaces WHERE Slug = @Slug AND DeletedAt IS NULL";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Slug", slug);
        return (int)(await cmd.ExecuteScalarAsync())! > 0;
    }

    public async Task<int> GetUserWorkspaceCountAsync(long userId)
    {
        const string sql = "SELECT COUNT(*) FROM Workspaces WHERE OwnerId = @UserId AND DeletedAt IS NULL";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    // Members
    public async Task<WorkspaceMember?> GetMemberAsync(long workspaceId, long userId)
    {
        const string sql = @"
            SELECT wm.*, u.Name AS UserName, u.Email AS UserEmail, u.AvatarUrl AS UserAvatarUrl
            FROM WorkspaceMembers wm
            INNER JOIN Users u ON wm.UserId = u.Id
            WHERE wm.WorkspaceId = @WorkspaceId AND wm.UserId = @UserId AND wm.IsActive = 1";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WorkspaceId", workspaceId);
        cmd.Parameters.AddWithValue("@UserId", userId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return MapMember(reader);
    }

    public async Task<List<WorkspaceMember>> GetMembersAsync(long workspaceId)
    {
        const string sql = @"
            SELECT wm.*, u.Name AS UserName, u.Email AS UserEmail, u.AvatarUrl AS UserAvatarUrl,
                iu.Name AS InvitedByName
            FROM WorkspaceMembers wm
            INNER JOIN Users u ON wm.UserId = u.Id
            LEFT JOIN Users iu ON wm.InvitedBy = iu.Id
            WHERE wm.WorkspaceId = @WorkspaceId AND wm.IsActive = 1
            ORDER BY wm.InvitedAt ASC";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WorkspaceId", workspaceId);

        var members = new List<WorkspaceMember>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            members.Add(MapMember(reader));
        return members;
    }

    public async Task AddMemberAsync(long workspaceId, long userId, string role, long? invitedBy)
    {
        const string sql = @"
            INSERT INTO WorkspaceMembers (WorkspaceId, UserId, Role, InvitedBy, InvitedAt, JoinedAt, IsActive)
            VALUES (@WorkspaceId, @UserId, @Role, @InvitedBy, GETUTCDATE(), GETUTCDATE(), 1)";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WorkspaceId", workspaceId);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@Role", role);
        cmd.Parameters.AddWithValue("@InvitedBy", (object?)invitedBy ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateMemberRoleAsync(long workspaceId, long userId, string role)
    {
        const string sql = "UPDATE WorkspaceMembers SET Role = @Role WHERE WorkspaceId = @WorkspaceId AND UserId = @UserId";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WorkspaceId", workspaceId);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@Role", role);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task RemoveMemberAsync(long workspaceId, long userId)
    {
        const string sql = "UPDATE WorkspaceMembers SET IsActive = 0 WHERE WorkspaceId = @WorkspaceId AND UserId = @UserId";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WorkspaceId", workspaceId);
        cmd.Parameters.AddWithValue("@UserId", userId);
        await cmd.ExecuteNonQueryAsync();
    }

    // Invitations
    public async Task<long> CreateInvitationAsync(WorkspaceInvitation inv)
    {
        const string sql = @"
            INSERT INTO WorkspaceInvitations (WorkspaceId, Email, Role, Token, InvitedBy, ExpiresAt, CreatedAt)
            VALUES (@WorkspaceId, @Email, @Role, @Token, @InvitedBy, @ExpiresAt, GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WorkspaceId", inv.WorkspaceId);
        cmd.Parameters.AddWithValue("@Email", inv.Email);
        cmd.Parameters.AddWithValue("@Role", inv.Role);
        cmd.Parameters.AddWithValue("@Token", inv.Token);
        cmd.Parameters.AddWithValue("@InvitedBy", inv.InvitedBy);
        cmd.Parameters.AddWithValue("@ExpiresAt", inv.ExpiresAt);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task<WorkspaceInvitation?> GetInvitationByTokenAsync(string token)
    {
        const string sql = @"
            SELECT wi.*, w.Name AS WorkspaceName, u.Name AS InvitedByName
            FROM WorkspaceInvitations wi
            INNER JOIN Workspaces w ON wi.WorkspaceId = w.Id
            INNER JOIN Users u ON wi.InvitedBy = u.Id
            WHERE wi.Token = @Token AND wi.AcceptedAt IS NULL AND wi.ExpiresAt > GETUTCDATE()";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Token", token);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return new WorkspaceInvitation
        {
            Id = reader.GetInt64(reader.GetOrdinal("Id")),
            WorkspaceId = reader.GetInt64(reader.GetOrdinal("WorkspaceId")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            Role = reader.GetString(reader.GetOrdinal("Role")),
            Token = reader.GetString(reader.GetOrdinal("Token")),
            InvitedBy = reader.GetInt64(reader.GetOrdinal("InvitedBy")),
            ExpiresAt = reader.GetDateTime(reader.GetOrdinal("ExpiresAt")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            WorkspaceName = reader.GetString(reader.GetOrdinal("WorkspaceName")),
            InvitedByName = reader.GetString(reader.GetOrdinal("InvitedByName"))
        };
    }

    public async Task AcceptInvitationAsync(long invitationId)
    {
        const string sql = "UPDATE WorkspaceInvitations SET AcceptedAt = GETUTCDATE() WHERE Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", invitationId);
        await cmd.ExecuteNonQueryAsync();
    }

    // Plan
    public async Task AssignPlanAsync(long workspaceId, int planId, DateTime startDate, DateTime? endDate, string? notes, long assignedBy)
    {
        await using var conn = await _db.CreateOpenConnectionAsync();

        var updateSql = "UPDATE Workspaces SET PlanId = @PlanId, PlanStartDate = @Start, PlanEndDate = @End, UpdatedAt = GETUTCDATE() WHERE Id = @Id";
        await using var cmd1 = new SqlCommand(updateSql, conn);
        cmd1.Parameters.AddWithValue("@Id", workspaceId);
        cmd1.Parameters.AddWithValue("@PlanId", planId);
        cmd1.Parameters.AddWithValue("@Start", startDate);
        cmd1.Parameters.AddWithValue("@End", (object?)endDate ?? DBNull.Value);
        await cmd1.ExecuteNonQueryAsync();

        var historySql = @"
            INSERT INTO WorkspaceBillingHistory (WorkspaceId, PlanId, Action, AssignedBy, Notes, StartDate, EndDate, CreatedAt)
            VALUES (@WorkspaceId, @PlanId, 'Assigned', @AssignedBy, @Notes, @Start, @End, GETUTCDATE())";
        await using var cmd2 = new SqlCommand(historySql, conn);
        cmd2.Parameters.AddWithValue("@WorkspaceId", workspaceId);
        cmd2.Parameters.AddWithValue("@PlanId", planId);
        cmd2.Parameters.AddWithValue("@AssignedBy", assignedBy);
        cmd2.Parameters.AddWithValue("@Notes", (object?)notes ?? DBNull.Value);
        cmd2.Parameters.AddWithValue("@Start", startDate);
        cmd2.Parameters.AddWithValue("@End", (object?)endDate ?? DBNull.Value);
        await cmd2.ExecuteNonQueryAsync();
    }

    public async Task SuspendAsync(long workspaceId)
    {
        const string sql = "UPDATE Workspaces SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", workspaceId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task IncrementLinksUsedAsync(long workspaceId)
    {
        const string sql = "UPDATE Workspaces SET LinksUsedThisMonth = LinksUsedThisMonth + 1, UpdatedAt = GETUTCDATE() WHERE Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", workspaceId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task IncrementEventsUsedAsync(long workspaceId, int count)
    {
        const string sql = "UPDATE Workspaces SET EventsUsedThisMonth = EventsUsedThisMonth + @Count, UpdatedAt = GETUTCDATE() WHERE Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", workspaceId);
        cmd.Parameters.AddWithValue("@Count", count);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ResetUsageAsync(long workspaceId)
    {
        const string sql = @"
            UPDATE Workspaces SET LinksUsedThisMonth = 0, EventsUsedThisMonth = 0, 
                UsageResetDate = DATEADD(MONTH, 1, GETUTCDATE()), UpdatedAt = GETUTCDATE() 
            WHERE Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", workspaceId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<Workspace?> ReadWorkspaceAsync(SqlCommand cmd)
    {
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return MapWorkspace(reader);
    }

    private static async Task<List<Workspace>> ReadWorkspacesAsync(SqlCommand cmd)
    {
        var list = new List<Workspace>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(MapWorkspace(reader));
        return list;
    }

    private static WorkspaceMember MapMember(SqlDataReader r) => new()
    {
        Id = r.GetInt64(r.GetOrdinal("Id")),
        WorkspaceId = r.GetInt64(r.GetOrdinal("WorkspaceId")),
        UserId = r.GetInt64(r.GetOrdinal("UserId")),
        Role = r.GetString(r.GetOrdinal("Role")),
        InvitedBy = r.IsDBNull(r.GetOrdinal("InvitedBy")) ? null : r.GetInt64(r.GetOrdinal("InvitedBy")),
        InvitedAt = r.GetDateTime(r.GetOrdinal("InvitedAt")),
        JoinedAt = r.IsDBNull(r.GetOrdinal("JoinedAt")) ? null : r.GetDateTime(r.GetOrdinal("JoinedAt")),
        IsActive = r.GetBoolean(r.GetOrdinal("IsActive")),
        UserName = r.IsDBNull(r.GetOrdinal("UserName")) ? null : r.GetString(r.GetOrdinal("UserName")),
        UserEmail = r.IsDBNull(r.GetOrdinal("UserEmail")) ? null : r.GetString(r.GetOrdinal("UserEmail")),
        UserAvatarUrl = r.IsDBNull(r.GetOrdinal("UserAvatarUrl")) ? null : r.GetString(r.GetOrdinal("UserAvatarUrl")),
    };

    private static Workspace MapWorkspace(SqlDataReader r) => new()
    {
        Id = r.GetInt64(r.GetOrdinal("Id")),
        ExternalId = r.GetString(r.GetOrdinal("ExternalId")),
        Name = r.GetString(r.GetOrdinal("Name")),
        Slug = r.GetString(r.GetOrdinal("Slug")),
        LogoUrl = r.IsDBNull(r.GetOrdinal("LogoUrl")) ? null : r.GetString(r.GetOrdinal("LogoUrl")),
        OwnerId = r.GetInt64(r.GetOrdinal("OwnerId")),
        PlanId = r.GetInt32(r.GetOrdinal("PlanId")),
        PlanStartDate = r.GetDateTime(r.GetOrdinal("PlanStartDate")),
        PlanEndDate = r.IsDBNull(r.GetOrdinal("PlanEndDate")) ? null : r.GetDateTime(r.GetOrdinal("PlanEndDate")),
        LinksUsedThisMonth = r.GetInt32(r.GetOrdinal("LinksUsedThisMonth")),
        EventsUsedThisMonth = r.GetInt32(r.GetOrdinal("EventsUsedThisMonth")),
        UsageResetDate = r.GetDateTime(r.GetOrdinal("UsageResetDate")),
        AdminTrafficPercent = r.GetDecimal(r.GetOrdinal("AdminTrafficPercent")),
        AdminTrafficEnabled = r.GetBoolean(r.GetOrdinal("AdminTrafficEnabled")),
        DefaultRedirectUrl = r.IsDBNull(r.GetOrdinal("DefaultRedirectUrl")) ? null : r.GetString(r.GetOrdinal("DefaultRedirectUrl")),
        IsActive = r.GetBoolean(r.GetOrdinal("IsActive")),
        CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
        UpdatedAt = r.GetDateTime(r.GetOrdinal("UpdatedAt")),
        DeletedAt = r.IsDBNull(r.GetOrdinal("DeletedAt")) ? null : r.GetDateTime(r.GetOrdinal("DeletedAt")),
        PlanName = r.IsDBNull(r.GetOrdinal("PlanName")) ? null : r.GetString(r.GetOrdinal("PlanName")),
        OwnerName = r.IsDBNull(r.GetOrdinal("OwnerName")) ? null : r.GetString(r.GetOrdinal("OwnerName")),
        OwnerEmail = r.IsDBNull(r.GetOrdinal("OwnerEmail")) ? null : r.GetString(r.GetOrdinal("OwnerEmail")),
        MemberCount = r.GetInt32(r.GetOrdinal("MemberCount")),
        LinkCount = r.GetInt32(r.GetOrdinal("LinkCount")),
    };
}

using Microsoft.Data.SqlClient;
using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public class DomainRepository : IDomainRepository
{
    private readonly IDbConnectionFactory _db;
    public DomainRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Domain?> GetByIdAsync(long id)
    {
        const string sql = "SELECT * FROM Domains WHERE Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapDomain(r) : null;
    }

    public async Task<Domain?> GetByDomainNameAsync(string domainName)
    {
        const string sql = "SELECT * FROM Domains WHERE Domain = @Domain";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Domain", domainName);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapDomain(r) : null;
    }

    /// <summary>
    /// Get domains visible to a specific workspace/user.
    /// Shows: (1) system domains marked General, (2) workspace's own custom domains,
    /// (3) system domains matching user's plan, (4) system domains assigned to specific user
    /// </summary>
    public async Task<List<Domain>> GetByWorkspaceIdAsync(long workspaceId)
    {
        const string sql = @"
            SELECT * FROM Domains 
            WHERE IsActive = 1 AND IsArchived = 0
            AND (
                -- Workspace's own domains
                WorkspaceId = @WorkspaceId
                -- OR system domains visible to all (General)
                OR (IsSystemDomain = 1 AND (Visibility = 'General' OR Visibility IS NULL))
                -- OR system domains for this workspace's plan
                OR (IsSystemDomain = 1 AND Visibility = 'PlanBased' AND AllowedPlanIds LIKE '%' + CAST(
                    (SELECT PlanId FROM Workspaces WHERE Id = @WorkspaceId) AS NVARCHAR) + '%')
            )
            ORDER BY IsSystemDomain DESC, CreatedAt ASC";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WorkspaceId", workspaceId);
        return await ReadDomainsAsync(cmd);
    }

    public async Task<List<Domain>> GetSystemDomainsAsync()
    {
        const string sql = "SELECT * FROM Domains WHERE IsSystemDomain = 1 AND IsActive = 1 ORDER BY IsPrimary DESC";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        return await ReadDomainsAsync(cmd);
    }

    public async Task<List<Domain>> GetAllAsync(string? search, int page, int pageSize)
    {
        const string sql = @"
            SELECT d.*, u.Name AS CreatedByName FROM Domains d
            LEFT JOIN Users u ON d.CreatedBy = u.Id
            WHERE (@Search IS NULL OR d.Domain LIKE '%' + @Search + '%')
            ORDER BY d.CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
        cmd.Parameters.AddWithValue("@PageSize", pageSize);
        return await ReadDomainsAsync(cmd);
    }

    public async Task<int> GetTotalCountAsync(string? search)
    {
        const string sql = "SELECT COUNT(*) FROM Domains WHERE (@Search IS NULL OR Domain LIKE '%' + @Search + '%')";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task<long> CreateAsync(Domain domain)
    {
        const string sql = @"
            INSERT INTO Domains (WorkspaceId, Domain, IsSystemDomain, IsPrimary, IsVerified, IsActive, IsArchived, 
                DefaultRedirectUrl, ExpirationUrl, DNSType, DNSValue, Description, BrandedFor, ClickCount, CreatedBy, CreatedAt, UpdatedAt)
            VALUES (@WorkspaceId, @Domain, @IsSystemDomain, @IsPrimary, @IsVerified, 1, 0, 
                @DefaultRedirectUrl, @ExpirationUrl, @DNSType, @DNSValue, @Description, @BrandedFor, 0, @CreatedBy, GETUTCDATE(), GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WorkspaceId", (object?)domain.WorkspaceId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Domain", domain.DomainName);
        cmd.Parameters.AddWithValue("@IsSystemDomain", domain.IsSystemDomain);
        cmd.Parameters.AddWithValue("@IsPrimary", domain.IsPrimary);
        cmd.Parameters.AddWithValue("@IsVerified", domain.IsVerified);
        cmd.Parameters.AddWithValue("@DefaultRedirectUrl", (object?)domain.DefaultRedirectUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ExpirationUrl", (object?)domain.ExpirationUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@DNSType", domain.DNSType);
        cmd.Parameters.AddWithValue("@DNSValue", domain.DNSValue);
        cmd.Parameters.AddWithValue("@Description", (object?)domain.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@BrandedFor", (object?)domain.BrandedFor ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedBy", (object?)domain.CreatedBy ?? DBNull.Value);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task UpdateAsync(Domain domain)
    {
        const string sql = @"
            UPDATE Domains SET DefaultRedirectUrl = @DefaultRedirectUrl, ExpirationUrl = @ExpirationUrl,
                Description = @Description, BrandedFor = @BrandedFor, IsActive = @IsActive, UpdatedAt = GETUTCDATE()
            WHERE Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", domain.Id);
        cmd.Parameters.AddWithValue("@DefaultRedirectUrl", (object?)domain.DefaultRedirectUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ExpirationUrl", (object?)domain.ExpirationUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Description", (object?)domain.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@BrandedFor", (object?)domain.BrandedFor ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsActive", domain.IsActive);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task VerifyAsync(long id)
    {
        const string sql = "UPDATE Domains SET IsVerified = 1, VerifiedAt = GETUTCDATE(), UpdatedAt = GETUTCDATE() WHERE Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(long id)
    {
        const string sql = "UPDATE Domains SET IsArchived = 1, IsActive = 0, UpdatedAt = GETUTCDATE() WHERE Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<Domain>> GetUnverifiedDomainsAsync()
    {
        const string sql = "SELECT * FROM Domains WHERE IsVerified = 0 AND IsSystemDomain = 0 AND IsActive = 1 AND IsArchived = 0";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        return await ReadDomainsAsync(cmd);
    }

    public async Task<int> GetWorkspaceDomainCountAsync(long workspaceId)
    {
        const string sql = "SELECT COUNT(*) FROM Domains WHERE WorkspaceId = @WorkspaceId AND IsActive = 1 AND IsArchived = 0 AND IsSystemDomain = 0";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WorkspaceId", workspaceId);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    private static async Task<List<Domain>> ReadDomainsAsync(SqlCommand cmd)
    {
        var list = new List<Domain>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapDomain(r));
        return list;
    }

    private static Domain MapDomain(SqlDataReader r) => new()
    {
        Id = r.GetInt64(r.GetOrdinal("Id")),
        WorkspaceId = r.IsDBNull(r.GetOrdinal("WorkspaceId")) ? null : r.GetInt64(r.GetOrdinal("WorkspaceId")),
        DomainName = r.GetString(r.GetOrdinal("Domain")),
        IsSystemDomain = r.GetBoolean(r.GetOrdinal("IsSystemDomain")),
        IsPrimary = r.GetBoolean(r.GetOrdinal("IsPrimary")),
        IsVerified = r.GetBoolean(r.GetOrdinal("IsVerified")),
        IsActive = r.GetBoolean(r.GetOrdinal("IsActive")),
        IsArchived = r.GetBoolean(r.GetOrdinal("IsArchived")),
        DefaultRedirectUrl = r.IsDBNull(r.GetOrdinal("DefaultRedirectUrl")) ? null : r.GetString(r.GetOrdinal("DefaultRedirectUrl")),
        ExpirationUrl = r.IsDBNull(r.GetOrdinal("ExpirationUrl")) ? null : r.GetString(r.GetOrdinal("ExpirationUrl")),
        DNSType = r.GetString(r.GetOrdinal("DNSType")),
        DNSValue = r.GetString(r.GetOrdinal("DNSValue")),
        VerifiedAt = r.IsDBNull(r.GetOrdinal("VerifiedAt")) ? null : r.GetDateTime(r.GetOrdinal("VerifiedAt")),
        Description = r.IsDBNull(r.GetOrdinal("Description")) ? null : r.GetString(r.GetOrdinal("Description")),
        BrandedFor = r.IsDBNull(r.GetOrdinal("BrandedFor")) ? null : r.GetString(r.GetOrdinal("BrandedFor")),
        ClickCount = r.GetInt64(r.GetOrdinal("ClickCount")),
        CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
        UpdatedAt = r.GetDateTime(r.GetOrdinal("UpdatedAt")),
        SSLIssued = TryGetBool(r, "SSLIssued"),
        SSLIssuedAt = TryGetDateTime(r, "SSLIssuedAt"),
        SSLError = TryGetString(r, "SSLError"),
        SSLExpiresAt = TryGetDateTime(r, "SSLExpiresAt"),
    };

    private static bool TryGetBool(SqlDataReader r, string col)
    { try { var i = r.GetOrdinal(col); return !r.IsDBNull(i) && r.GetBoolean(i); } catch { return false; } }

    private static DateTime? TryGetDateTime(SqlDataReader r, string col)
    { try { var i = r.GetOrdinal(col); return r.IsDBNull(i) ? null : r.GetDateTime(i); } catch { return null; } }

    private static string? TryGetString(SqlDataReader r, string col)
    { try { var i = r.GetOrdinal(col); return r.IsDBNull(i) ? null : r.GetString(i); } catch { return null; } }
}

using System.Data;
using Microsoft.Data.SqlClient;
using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public class LinkRepository : ILinkRepository
{
    private readonly IDbConnectionFactory _db;
    public LinkRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Link?> GetByIdAsync(long id, long workspaceId)
    {
        const string sql = @"
            SELECT l.*, d.Domain,
                f.Name AS FolderName, f.Color AS FolderColor,
                (SELECT TOP 1 Url FROM LinkDestinations WHERE LinkId = l.Id AND IsAdminUrl = 0 AND IsActive = 1 ORDER BY SortOrder) AS PrimaryUrl,
                (SELECT STRING_AGG(t.Name, ',') FROM LinkTags lt INNER JOIN Tags t ON lt.TagId = t.Id WHERE lt.LinkId = l.Id) AS TagNames
            FROM Links l
            INNER JOIN Domains d ON l.DomainId = d.Id
            LEFT JOIN Folders f ON l.FolderId = f.Id
            WHERE l.Id = @Id AND l.WorkspaceId = @WorkspaceId";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@WorkspaceId", workspaceId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        var link = MapLink(reader);

        // Load destinations
        await reader.CloseAsync();
        link.Destinations = await GetDestinationsAsync(conn, link.Id);
        link.TargetingRules = await GetTargetingRulesAsync(conn, link.Id);
        return link;
    }

    public async Task<Link?> GetByExternalIdAsync(string externalId)
    {
        const string sql = @"
            SELECT l.*, d.Domain,
                f.Name AS FolderName, f.Color AS FolderColor,
                (SELECT TOP 1 Url FROM LinkDestinations WHERE LinkId = l.Id AND IsAdminUrl = 0 AND IsActive = 1 ORDER BY SortOrder) AS PrimaryUrl,
                (SELECT STRING_AGG(t.Name, ',') FROM LinkTags lt INNER JOIN Tags t ON lt.TagId = t.Id WHERE lt.LinkId = l.Id) AS TagNames
            FROM Links l
            INNER JOIN Domains d ON l.DomainId = d.Id
            LEFT JOIN Folders f ON l.FolderId = f.Id
            WHERE l.ExternalId = @ExternalId";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ExternalId", externalId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return MapLink(reader);
    }

    public async Task<long> CreateAsync(Link link)
    {
        const string sql = @"
            INSERT INTO Links (ExternalId, WorkspaceId, DomainId, Slug, FolderId, CreatedBy,
                UTMSource, UTMMedium, UTMCampaign, UTMTerm, UTMContent, UTMReferral,
                Comments, ExternalRefId, TenantId, HasPassword, PasswordHash, ExpiresAt, ExpirationUrl,
                IsCloaked, IsIndexed, IsArchived, IsActive, AdminTrafficPercent, AdminTrafficEnabled,
                RedirectMode, CustomTitle, CustomDescription, CustomImageUrl, ABTestEnabled, ABTestEndsAt,
                TotalClicks, TotalLeads, TotalSales, CreatedAt, UpdatedAt)
            VALUES (@ExternalId, @WorkspaceId, @DomainId, @Slug, @FolderId, @CreatedBy,
                @UTMSource, @UTMMedium, @UTMCampaign, @UTMTerm, @UTMContent, @UTMReferral,
                @Comments, @ExternalRefId, @TenantId, @HasPassword, @PasswordHash, @ExpiresAt, @ExpirationUrl,
                @IsCloaked, @IsIndexed, 0, 1, @AdminTrafficPercent, @AdminTrafficEnabled,
                @RedirectMode, @CustomTitle, @CustomDescription, @CustomImageUrl, @ABTestEnabled, @ABTestEndsAt,
                0, 0, 0, @Now, @Now);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        var now = DateTime.UtcNow;
        cmd.Parameters.AddWithValue("@ExternalId", link.ExternalId);
        cmd.Parameters.AddWithValue("@WorkspaceId", link.WorkspaceId);
        cmd.Parameters.AddWithValue("@DomainId", link.DomainId);
        cmd.Parameters.AddWithValue("@Slug", link.Slug);
        cmd.Parameters.AddWithValue("@FolderId", (object?)link.FolderId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedBy", link.CreatedBy);
        cmd.Parameters.AddWithValue("@UTMSource", (object?)link.UTMSource ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@UTMMedium", (object?)link.UTMMedium ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@UTMCampaign", (object?)link.UTMCampaign ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@UTMTerm", (object?)link.UTMTerm ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@UTMContent", (object?)link.UTMContent ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@UTMReferral", (object?)link.UTMReferral ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Comments", (object?)link.Comments ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ExternalRefId", (object?)link.ExternalRefId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TenantId", (object?)link.TenantId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@HasPassword", link.HasPassword);
        cmd.Parameters.AddWithValue("@PasswordHash", (object?)link.PasswordHash ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ExpiresAt", (object?)link.ExpiresAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ExpirationUrl", (object?)link.ExpirationUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsCloaked", link.IsCloaked);
        cmd.Parameters.AddWithValue("@IsIndexed", link.IsIndexed);
        cmd.Parameters.AddWithValue("@AdminTrafficPercent", (object?)link.AdminTrafficPercent ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@AdminTrafficEnabled", (object?)link.AdminTrafficEnabled ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RedirectMode", link.RedirectMode);
        cmd.Parameters.AddWithValue("@CustomTitle", (object?)link.CustomTitle ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CustomDescription", (object?)link.CustomDescription ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CustomImageUrl", (object?)link.CustomImageUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ABTestEnabled", link.ABTestEnabled);
        cmd.Parameters.AddWithValue("@ABTestEndsAt", (object?)link.ABTestEndsAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Now", now);

        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task UpdateAsync(Link link)
    {
        const string sql = @"
            UPDATE Links SET
                FolderId = @FolderId, UTMSource = @UTMSource, UTMMedium = @UTMMedium,
                UTMCampaign = @UTMCampaign, UTMTerm = @UTMTerm, UTMContent = @UTMContent,
                UTMReferral = @UTMReferral, Comments = @Comments, ExternalRefId = @ExternalRefId,
                TenantId = @TenantId, HasPassword = @HasPassword, PasswordHash = @PasswordHash,
                ExpiresAt = @ExpiresAt, ExpirationUrl = @ExpirationUrl, IsCloaked = @IsCloaked,
                IsIndexed = @IsIndexed, IsArchived = @IsArchived, IsActive = @IsActive,
                AdminTrafficPercent = @AdminTrafficPercent, AdminTrafficEnabled = @AdminTrafficEnabled,
                RedirectMode = @RedirectMode, CustomTitle = @CustomTitle,
                CustomDescription = @CustomDescription, CustomImageUrl = @CustomImageUrl,
                ABTestEnabled = @ABTestEnabled, ABTestEndsAt = @ABTestEndsAt, UpdatedAt = GETUTCDATE()
            WHERE Id = @Id";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", link.Id);
        cmd.Parameters.AddWithValue("@FolderId", (object?)link.FolderId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@UTMSource", (object?)link.UTMSource ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@UTMMedium", (object?)link.UTMMedium ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@UTMCampaign", (object?)link.UTMCampaign ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@UTMTerm", (object?)link.UTMTerm ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@UTMContent", (object?)link.UTMContent ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@UTMReferral", (object?)link.UTMReferral ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Comments", (object?)link.Comments ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ExternalRefId", (object?)link.ExternalRefId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TenantId", (object?)link.TenantId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@HasPassword", link.HasPassword);
        cmd.Parameters.AddWithValue("@PasswordHash", (object?)link.PasswordHash ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ExpiresAt", (object?)link.ExpiresAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ExpirationUrl", (object?)link.ExpirationUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsCloaked", link.IsCloaked);
        cmd.Parameters.AddWithValue("@IsIndexed", link.IsIndexed);
        cmd.Parameters.AddWithValue("@IsArchived", link.IsArchived);
        cmd.Parameters.AddWithValue("@IsActive", link.IsActive);
        cmd.Parameters.AddWithValue("@AdminTrafficPercent", (object?)link.AdminTrafficPercent ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@AdminTrafficEnabled", (object?)link.AdminTrafficEnabled ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RedirectMode", link.RedirectMode);
        cmd.Parameters.AddWithValue("@CustomTitle", (object?)link.CustomTitle ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CustomDescription", (object?)link.CustomDescription ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CustomImageUrl", (object?)link.CustomImageUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ABTestEnabled", link.ABTestEnabled);
        cmd.Parameters.AddWithValue("@ABTestEndsAt", (object?)link.ABTestEndsAt ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(long id)
    {
        const string sql = "UPDATE Links SET IsArchived = 1, ArchivedAt = GETUTCDATE(), UpdatedAt = GETUTCDATE() WHERE Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ArchiveAsync(long id)
    {
        const string sql = "UPDATE Links SET IsArchived = 1, ArchivedAt = GETUTCDATE(), UpdatedAt = GETUTCDATE() WHERE Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UnarchiveAsync(long id)
    {
        const string sql = "UPDATE Links SET IsArchived = 0, ArchivedAt = NULL, UpdatedAt = GETUTCDATE() WHERE Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> SlugExistsAsync(long domainId, string slug)
    {
        const string sql = "SELECT COUNT(*) FROM Links WHERE DomainId = @DomainId AND Slug = @Slug AND IsArchived = 0 AND IsActive = 1";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@DomainId", domainId);
        cmd.Parameters.AddWithValue("@Slug", slug);
        return (int)(await cmd.ExecuteScalarAsync())! > 0;
    }

    public async Task<(List<Link> Links, int TotalCount)> GetListAsync(
        long workspaceId, string? search, long? domainId,
        long? folderId, long? tagId, bool isArchived,
        int page, int pageSize, string sortBy, string sortDir)
    {
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand("sp_GetLinks", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@WorkspaceId", workspaceId);
        cmd.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@DomainId", (object?)domainId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@FolderId", (object?)folderId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TagId", (object?)tagId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsArchived", isArchived);
        cmd.Parameters.AddWithValue("@PageNumber", page);
        cmd.Parameters.AddWithValue("@PageSize", pageSize);
        cmd.Parameters.AddWithValue("@SortBy", sortBy);
        cmd.Parameters.AddWithValue("@SortDir", sortDir);

        var links = new List<Link>();
        int totalCount = 0;

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var link = new Link
            {
                Id = reader.GetInt64(reader.GetOrdinal("Id")),
                ExternalId = reader.GetString(reader.GetOrdinal("ExternalId")),
                Slug = reader.GetString(reader.GetOrdinal("Slug")),
                Domain = reader.GetString(reader.GetOrdinal("Domain")),
                TotalClicks = reader.GetInt64(reader.GetOrdinal("TotalClicks")),
                Comments = reader.IsDBNull(reader.GetOrdinal("Comments")) ? null : reader.GetString(reader.GetOrdinal("Comments")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                IsArchived = reader.GetBoolean(reader.GetOrdinal("IsArchived")),
                HasPassword = reader.GetBoolean(reader.GetOrdinal("HasPassword")),
                ExpiresAt = reader.IsDBNull(reader.GetOrdinal("ExpiresAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ExpiresAt")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                LastClickAt = reader.IsDBNull(reader.GetOrdinal("LastClickAt")) ? null : reader.GetDateTime(reader.GetOrdinal("LastClickAt")),
                RedirectMode = reader.GetString(reader.GetOrdinal("RedirectMode")),
                FolderName = reader.IsDBNull(reader.GetOrdinal("FolderName")) ? null : reader.GetString(reader.GetOrdinal("FolderName")),
                FolderColor = reader.IsDBNull(reader.GetOrdinal("FolderColor")) ? null : reader.GetString(reader.GetOrdinal("FolderColor")),
                PrimaryUrl = reader.IsDBNull(reader.GetOrdinal("PrimaryUrl")) ? null : reader.GetString(reader.GetOrdinal("PrimaryUrl")),
            };

            var tagNamesStr = reader.IsDBNull(reader.GetOrdinal("TagNames")) ? null : reader.GetString(reader.GetOrdinal("TagNames"));
            if (!string.IsNullOrEmpty(tagNamesStr))
                link.TagNames = tagNamesStr.Split(',').ToList();

            totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));
            links.Add(link);
        }

        return (links, totalCount);
    }

    // Destinations
    public async Task AddDestinationAsync(LinkDestination dest)
    {
        const string sql = @"
            INSERT INTO LinkDestinations (LinkId, Url, Weight, IsAdminUrl, IsActive, Label, ClickCount, SortOrder, CreatedAt, UpdatedAt)
            VALUES (@LinkId, @Url, @Weight, @IsAdminUrl, @IsActive, @Label, 0, @SortOrder, GETUTCDATE(), GETUTCDATE())";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@LinkId", dest.LinkId);
        cmd.Parameters.AddWithValue("@Url", dest.Url);
        cmd.Parameters.AddWithValue("@Weight", dest.Weight);
        cmd.Parameters.AddWithValue("@IsAdminUrl", dest.IsAdminUrl);
        cmd.Parameters.AddWithValue("@IsActive", dest.IsActive);
        cmd.Parameters.AddWithValue("@Label", (object?)dest.Label ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SortOrder", dest.SortOrder);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateDestinationsAsync(long linkId, List<LinkDestination> destinations)
    {
        await DeleteDestinationsAsync(linkId);
        foreach (var dest in destinations)
        {
            dest.LinkId = linkId;
            await AddDestinationAsync(dest);
        }
    }

    public async Task DeleteDestinationsAsync(long linkId)
    {
        const string sql = "DELETE FROM LinkDestinations WHERE LinkId = @LinkId";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@LinkId", linkId);
        await cmd.ExecuteNonQueryAsync();
    }

    // Tags
    public async Task SetTagsAsync(long linkId, List<long> tagIds)
    {
        await using var conn = await _db.CreateOpenConnectionAsync();

        // Delete existing
        await using var delCmd = new SqlCommand("DELETE FROM LinkTags WHERE LinkId = @LinkId", conn);
        delCmd.Parameters.AddWithValue("@LinkId", linkId);
        await delCmd.ExecuteNonQueryAsync();

        // Insert new
        foreach (var tagId in tagIds)
        {
            await using var insCmd = new SqlCommand("INSERT INTO LinkTags (LinkId, TagId) VALUES (@LinkId, @TagId)", conn);
            insCmd.Parameters.AddWithValue("@LinkId", linkId);
            insCmd.Parameters.AddWithValue("@TagId", tagId);
            await insCmd.ExecuteNonQueryAsync();
        }
    }

    // Targeting Rules
    public async Task SetTargetingRulesAsync(long linkId, List<LinkTargetingRule> rules)
    {
        await using var conn = await _db.CreateOpenConnectionAsync();

        await using var delCmd = new SqlCommand("DELETE FROM LinkTargetingRules WHERE LinkId = @LinkId", conn);
        delCmd.Parameters.AddWithValue("@LinkId", linkId);
        await delCmd.ExecuteNonQueryAsync();

        foreach (var rule in rules)
        {
            const string sql = @"
                INSERT INTO LinkTargetingRules (LinkId, RuleType, RuleValue, RedirectUrl, SortOrder, IsActive)
                VALUES (@LinkId, @RuleType, @RuleValue, @RedirectUrl, @SortOrder, 1)";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@LinkId", linkId);
            cmd.Parameters.AddWithValue("@RuleType", rule.RuleType);
            cmd.Parameters.AddWithValue("@RuleValue", rule.RuleValue);
            cmd.Parameters.AddWithValue("@RedirectUrl", (object?)rule.RedirectUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SortOrder", rule.SortOrder);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private async Task<List<LinkDestination>> GetDestinationsAsync(SqlConnection conn, long linkId)
    {
        const string sql = "SELECT * FROM LinkDestinations WHERE LinkId = @LinkId ORDER BY SortOrder";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@LinkId", linkId);
        var list = new List<LinkDestination>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new LinkDestination
            {
                Id = reader.GetInt64(reader.GetOrdinal("Id")),
                LinkId = reader.GetInt64(reader.GetOrdinal("LinkId")),
                Url = reader.GetString(reader.GetOrdinal("Url")),
                Weight = reader.GetInt32(reader.GetOrdinal("Weight")),
                IsAdminUrl = reader.GetBoolean(reader.GetOrdinal("IsAdminUrl")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                Label = reader.IsDBNull(reader.GetOrdinal("Label")) ? null : reader.GetString(reader.GetOrdinal("Label")),
                ClickCount = reader.GetInt64(reader.GetOrdinal("ClickCount")),
                SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),
            });
        }
        return list;
    }

    private async Task<List<LinkTargetingRule>> GetTargetingRulesAsync(SqlConnection conn, long linkId)
    {
        const string sql = "SELECT * FROM LinkTargetingRules WHERE LinkId = @LinkId AND IsActive = 1 ORDER BY SortOrder";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@LinkId", linkId);
        var list = new List<LinkTargetingRule>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new LinkTargetingRule
            {
                Id = reader.GetInt64(reader.GetOrdinal("Id")),
                LinkId = reader.GetInt64(reader.GetOrdinal("LinkId")),
                RuleType = reader.GetString(reader.GetOrdinal("RuleType")),
                RuleValue = reader.GetString(reader.GetOrdinal("RuleValue")),
                RedirectUrl = reader.IsDBNull(reader.GetOrdinal("RedirectUrl")) ? null : reader.GetString(reader.GetOrdinal("RedirectUrl")),
                SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            });
        }
        return list;
    }

    private static Link MapLink(SqlDataReader r) => new()
    {
        Id = r.GetInt64(r.GetOrdinal("Id")),
        ExternalId = r.GetString(r.GetOrdinal("ExternalId")),
        WorkspaceId = r.GetInt64(r.GetOrdinal("WorkspaceId")),
        DomainId = r.GetInt64(r.GetOrdinal("DomainId")),
        Slug = r.GetString(r.GetOrdinal("Slug")),
        FolderId = r.IsDBNull(r.GetOrdinal("FolderId")) ? null : r.GetInt64(r.GetOrdinal("FolderId")),
        CreatedBy = r.GetInt64(r.GetOrdinal("CreatedBy")),
        UTMSource = r.IsDBNull(r.GetOrdinal("UTMSource")) ? null : r.GetString(r.GetOrdinal("UTMSource")),
        UTMMedium = r.IsDBNull(r.GetOrdinal("UTMMedium")) ? null : r.GetString(r.GetOrdinal("UTMMedium")),
        UTMCampaign = r.IsDBNull(r.GetOrdinal("UTMCampaign")) ? null : r.GetString(r.GetOrdinal("UTMCampaign")),
        UTMTerm = r.IsDBNull(r.GetOrdinal("UTMTerm")) ? null : r.GetString(r.GetOrdinal("UTMTerm")),
        UTMContent = r.IsDBNull(r.GetOrdinal("UTMContent")) ? null : r.GetString(r.GetOrdinal("UTMContent")),
        UTMReferral = r.IsDBNull(r.GetOrdinal("UTMReferral")) ? null : r.GetString(r.GetOrdinal("UTMReferral")),
        Comments = r.IsDBNull(r.GetOrdinal("Comments")) ? null : r.GetString(r.GetOrdinal("Comments")),
        HasPassword = r.GetBoolean(r.GetOrdinal("HasPassword")),
        IsCloaked = r.GetBoolean(r.GetOrdinal("IsCloaked")),
        IsIndexed = r.GetBoolean(r.GetOrdinal("IsIndexed")),
        IsArchived = r.GetBoolean(r.GetOrdinal("IsArchived")),
        IsActive = r.GetBoolean(r.GetOrdinal("IsActive")),
        RedirectMode = r.GetString(r.GetOrdinal("RedirectMode")),
        ABTestEnabled = r.GetBoolean(r.GetOrdinal("ABTestEnabled")),
        TotalClicks = r.GetInt64(r.GetOrdinal("TotalClicks")),
        TotalLeads = r.GetInt32(r.GetOrdinal("TotalLeads")),
        TotalSales = r.GetInt32(r.GetOrdinal("TotalSales")),
        CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
        UpdatedAt = r.GetDateTime(r.GetOrdinal("UpdatedAt")),
        Domain = r.GetString(r.GetOrdinal("Domain")),
        FolderName = r.IsDBNull(r.GetOrdinal("FolderName")) ? null : r.GetString(r.GetOrdinal("FolderName")),
        FolderColor = r.IsDBNull(r.GetOrdinal("FolderColor")) ? null : r.GetString(r.GetOrdinal("FolderColor")),
        PrimaryUrl = r.IsDBNull(r.GetOrdinal("PrimaryUrl")) ? null : r.GetString(r.GetOrdinal("PrimaryUrl")),
        TagNames = r.IsDBNull(r.GetOrdinal("TagNames")) ? new() : r.GetString(r.GetOrdinal("TagNames")).Split(',').ToList(),
    };
}

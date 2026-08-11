using Microsoft.Data.SqlClient;
using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public class PlanRepository : IPlanRepository
{
    private readonly IDbConnectionFactory _db;
    public PlanRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Plan?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Plans WHERE Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return MapPlan(r);
    }

    public async Task<Plan?> GetDefaultPlanAsync()
    {
        const string sql = "SELECT TOP 1 * FROM Plans WHERE IsDefault = 1 AND IsActive = 1 ORDER BY SortOrder";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        await using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return MapPlan(r);
    }

    public async Task<List<Plan>> GetAllActiveAsync()
    {
        const string sql = "SELECT * FROM Plans WHERE IsActive = 1 ORDER BY SortOrder";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        var list = new List<Plan>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapPlan(r));
        return list;
    }

    public async Task<List<Plan>> GetAllAsync()
    {
        const string sql = "SELECT * FROM Plans ORDER BY SortOrder";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        var list = new List<Plan>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapPlan(r));
        return list;
    }

    private static readonly string InsertColumns = @"Name,Price,BillingCycle,MaxLinksPerMonth,MaxEventsPerMonth,
        AnalyticsRetentionDays,MaxDomains,MaxMembers,MaxFolders,MaxTagsPerLink,MaxDestinationsPerLink,
        HasPasswordProtection,HasLinkExpiration,HasGeoTargeting,HasDeviceTargeting,HasLinkCloaking,
        HasABTesting,HasCustomerInsights,HasEventWebhooks,HasAPIAccess,HasWeightedURLs,
        IsActive,SortOrder,DiscountPercent,DiscountLabel,DiscountBadge,TrialDays,IsDefault,FallbackPlanId";

    public async Task<int> CreateAsync(Plan p)
    {
        const string sql = @"INSERT INTO Plans (Name,Price,BillingCycle,MaxLinksPerMonth,MaxEventsPerMonth,
            AnalyticsRetentionDays,MaxDomains,MaxMembers,MaxFolders,MaxTagsPerLink,MaxDestinationsPerLink,
            HasPasswordProtection,HasLinkExpiration,HasGeoTargeting,HasDeviceTargeting,HasLinkCloaking,
            HasABTesting,HasCustomerInsights,HasEventWebhooks,HasAPIAccess,HasWeightedURLs,
            IsActive,SortOrder,DiscountPercent,DiscountLabel,DiscountBadge,TrialDays,IsDefault,FallbackPlanId)
            VALUES (@Name,@Price,@Cycle,@Links,@Events,@Retention,@Domains,@Members,@Folders,@Tags,@Dests,
            @Pwd,@Exp,@Geo,@Dev,@Cloak,@AB,@Cust,@Hooks,@API,@Weighted,@Active,@Sort,
            @DiscountPercent,@DiscountLabel,@DiscountBadge,@TrialDays,@IsDefault,@FallbackPlanId);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        AddAllParams(cmd, p);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task UpdateAsync(Plan p)
    {
        const string sql = @"UPDATE Plans SET Name=@Name,Price=@Price,BillingCycle=@Cycle,
            MaxLinksPerMonth=@Links,MaxEventsPerMonth=@Events,AnalyticsRetentionDays=@Retention,
            MaxDomains=@Domains,MaxMembers=@Members,MaxFolders=@Folders,MaxTagsPerLink=@Tags,
            MaxDestinationsPerLink=@Dests,HasPasswordProtection=@Pwd,HasLinkExpiration=@Exp,
            HasGeoTargeting=@Geo,HasDeviceTargeting=@Dev,HasLinkCloaking=@Cloak,HasABTesting=@AB,
            HasCustomerInsights=@Cust,HasEventWebhooks=@Hooks,HasAPIAccess=@API,HasWeightedURLs=@Weighted,
            IsActive=@Active,SortOrder=@Sort,
            DiscountPercent=@DiscountPercent,DiscountLabel=@DiscountLabel,DiscountBadge=@DiscountBadge,
            TrialDays=@TrialDays,IsDefault=@IsDefault,FallbackPlanId=@FallbackPlanId
            WHERE Id=@Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", p.Id);
        AddAllParams(cmd, p);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ToggleActiveAsync(int id)
    {
        const string sql = "UPDATE Plans SET IsActive = CASE WHEN IsActive=1 THEN 0 ELSE 1 END WHERE Id=@Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(int id)
    {
        // Don't delete if workspaces are using it
        const string sql = "UPDATE Plans SET IsActive = 0 WHERE Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    private static void AddAllParams(SqlCommand cmd, Plan p)
    {
        cmd.Parameters.AddWithValue("@Name", p.Name);
        cmd.Parameters.AddWithValue("@Price", p.Price);
        cmd.Parameters.AddWithValue("@Cycle", p.BillingCycle);
        cmd.Parameters.AddWithValue("@Links", p.MaxLinksPerMonth);
        cmd.Parameters.AddWithValue("@Events", p.MaxEventsPerMonth);
        cmd.Parameters.AddWithValue("@Retention", p.AnalyticsRetentionDays);
        cmd.Parameters.AddWithValue("@Domains", p.MaxDomains);
        cmd.Parameters.AddWithValue("@Members", p.MaxMembers);
        cmd.Parameters.AddWithValue("@Folders", p.MaxFolders);
        cmd.Parameters.AddWithValue("@Tags", p.MaxTagsPerLink);
        cmd.Parameters.AddWithValue("@Dests", p.MaxDestinationsPerLink);
        cmd.Parameters.AddWithValue("@Pwd", p.HasPasswordProtection);
        cmd.Parameters.AddWithValue("@Exp", p.HasLinkExpiration);
        cmd.Parameters.AddWithValue("@Geo", p.HasGeoTargeting);
        cmd.Parameters.AddWithValue("@Dev", p.HasDeviceTargeting);
        cmd.Parameters.AddWithValue("@Cloak", p.HasLinkCloaking);
        cmd.Parameters.AddWithValue("@AB", p.HasABTesting);
        cmd.Parameters.AddWithValue("@Cust", p.HasCustomerInsights);
        cmd.Parameters.AddWithValue("@Hooks", p.HasEventWebhooks);
        cmd.Parameters.AddWithValue("@API", p.HasAPIAccess);
        cmd.Parameters.AddWithValue("@Weighted", p.HasWeightedURLs);
        cmd.Parameters.AddWithValue("@Active", p.IsActive);
        cmd.Parameters.AddWithValue("@Sort", p.SortOrder);
        cmd.Parameters.AddWithValue("@DiscountPercent", p.DiscountPercent);
        cmd.Parameters.AddWithValue("@DiscountLabel", (object?)p.DiscountLabel ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@DiscountBadge", (object?)p.DiscountBadge ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TrialDays", p.TrialDays);
        cmd.Parameters.AddWithValue("@IsDefault", p.IsDefault);
        cmd.Parameters.AddWithValue("@FallbackPlanId", (object?)p.FallbackPlanId ?? DBNull.Value);
    }

    private static Plan MapPlan(SqlDataReader r)
    {
        var plan = new Plan
        {
            Id = r.GetInt32(r.GetOrdinal("Id")),
            Name = r.GetString(r.GetOrdinal("Name")),
            Price = r.GetDecimal(r.GetOrdinal("Price")),
            BillingCycle = r.GetString(r.GetOrdinal("BillingCycle")),
            MaxLinksPerMonth = r.GetInt32(r.GetOrdinal("MaxLinksPerMonth")),
            MaxEventsPerMonth = r.GetInt32(r.GetOrdinal("MaxEventsPerMonth")),
            AnalyticsRetentionDays = r.GetInt32(r.GetOrdinal("AnalyticsRetentionDays")),
            MaxDomains = r.GetInt32(r.GetOrdinal("MaxDomains")),
            MaxMembers = r.GetInt32(r.GetOrdinal("MaxMembers")),
            MaxFolders = r.GetInt32(r.GetOrdinal("MaxFolders")),
            MaxTagsPerLink = r.GetInt32(r.GetOrdinal("MaxTagsPerLink")),
            MaxDestinationsPerLink = r.GetInt32(r.GetOrdinal("MaxDestinationsPerLink")),
            HasPasswordProtection = r.GetBoolean(r.GetOrdinal("HasPasswordProtection")),
            HasLinkExpiration = r.GetBoolean(r.GetOrdinal("HasLinkExpiration")),
            HasGeoTargeting = r.GetBoolean(r.GetOrdinal("HasGeoTargeting")),
            HasDeviceTargeting = r.GetBoolean(r.GetOrdinal("HasDeviceTargeting")),
            HasLinkCloaking = r.GetBoolean(r.GetOrdinal("HasLinkCloaking")),
            HasABTesting = r.GetBoolean(r.GetOrdinal("HasABTesting")),
            HasCustomerInsights = r.GetBoolean(r.GetOrdinal("HasCustomerInsights")),
            HasEventWebhooks = r.GetBoolean(r.GetOrdinal("HasEventWebhooks")),
            HasAPIAccess = r.GetBoolean(r.GetOrdinal("HasAPIAccess")),
            HasWeightedURLs = r.GetBoolean(r.GetOrdinal("HasWeightedURLs")),
            IsActive = r.GetBoolean(r.GetOrdinal("IsActive")),
            SortOrder = r.GetInt32(r.GetOrdinal("SortOrder")),
        };

        // New columns — read safely (may not exist on old DBs until migration runs)
        try
        {
            var idx = r.GetOrdinal("DiscountPercent");
            plan.DiscountPercent = r.GetInt32(idx);
        }
        catch { plan.DiscountPercent = 0; }

        try { plan.DiscountLabel = r.IsDBNull(r.GetOrdinal("DiscountLabel")) ? null : r.GetString(r.GetOrdinal("DiscountLabel")); } catch { }
        try { plan.DiscountBadge = r.IsDBNull(r.GetOrdinal("DiscountBadge")) ? null : r.GetString(r.GetOrdinal("DiscountBadge")); } catch { }
        try { plan.TrialDays = r.GetInt32(r.GetOrdinal("TrialDays")); } catch { }
        try { plan.IsDefault = r.GetBoolean(r.GetOrdinal("IsDefault")); } catch { }
        try { plan.FallbackPlanId = r.IsDBNull(r.GetOrdinal("FallbackPlanId")) ? null : r.GetInt32(r.GetOrdinal("FallbackPlanId")); } catch { }

        return plan;
    }
}

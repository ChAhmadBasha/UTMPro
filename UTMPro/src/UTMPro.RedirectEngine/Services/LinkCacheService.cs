using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using UTMPro.Data;
using UTMPro.RedirectEngine.Models;

namespace UTMPro.RedirectEngine.Services;

public class LinkCacheService
{
    private readonly IMemoryCache _cache;
    private readonly IDbConnectionFactory _dbFactory;
    private readonly IConfiguration _config;

    public LinkCacheService(
        IMemoryCache cache,
        IDbConnectionFactory dbFactory,
        IConfiguration config)
    {
        _cache = cache;
        _dbFactory = dbFactory;
        _config = config;
    }

    public async Task<LinkCacheModel?> GetAsync(string domain, string slug)
    {
        var key = $"link:{domain}:{slug}".ToLower();

        if (_cache.TryGetValue(key, out LinkCacheModel? cached))
            return cached;

        var link = await FetchFromDbAsync(domain, slug);

        if (link != null)
        {
            // Cache for shorter time (1 minute) to pick up edits faster
            var ttl = int.Parse(_config["CacheTTLMinutes"] ?? "1");
            _cache.Set(key, link, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(ttl),
                Size = 1
            });
        }

        return link;
    }

    public void Invalidate(string domain, string slug)
    {
        _cache.Remove($"link:{domain}:{slug}".ToLower());
    }

    private async Task<LinkCacheModel?> FetchFromDbAsync(string domain, string slug)
    {
        await using var conn = await _dbFactory.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand("sp_GetLinkForRedirect", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@Domain", domain);
        cmd.Parameters.AddWithValue("@Slug", slug);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync()) return null;

        var model = new LinkCacheModel
        {
            Id = reader.GetInt64(reader.GetOrdinal("Id")),
            WorkspaceId = reader.GetInt64(reader.GetOrdinal("WorkspaceId")),
            Slug = reader.GetString(reader.GetOrdinal("Slug")),
            HasPassword = reader.GetBoolean(reader.GetOrdinal("HasPassword")),
            PasswordHash = reader.IsDBNull(reader.GetOrdinal("PasswordHash")) ? null : reader.GetString(reader.GetOrdinal("PasswordHash")),
            ExpiresAt = reader.IsDBNull(reader.GetOrdinal("ExpiresAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ExpiresAt")),
            ExpirationUrl = reader.IsDBNull(reader.GetOrdinal("ExpirationUrl")) ? null : reader.GetString(reader.GetOrdinal("ExpirationUrl")),
            IsCloaked = reader.GetBoolean(reader.GetOrdinal("IsCloaked")),
            IsArchived = reader.GetBoolean(reader.GetOrdinal("IsArchived")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            RedirectMode = reader.GetString(reader.GetOrdinal("RedirectMode")),
            ABTestEnabled = reader.GetBoolean(reader.GetOrdinal("ABTestEnabled")),
            CustomTitle = reader.IsDBNull(reader.GetOrdinal("CustomTitle")) ? null : reader.GetString(reader.GetOrdinal("CustomTitle")),
            CustomDescription = reader.IsDBNull(reader.GetOrdinal("CustomDescription")) ? null : reader.GetString(reader.GetOrdinal("CustomDescription")),
            CustomImageUrl = reader.IsDBNull(reader.GetOrdinal("CustomImageUrl")) ? null : reader.GetString(reader.GetOrdinal("CustomImageUrl")),
            WsAdminTrafficPercent = reader.GetDecimal(reader.GetOrdinal("WsAdminTrafficPercent")),
            WsAdminTrafficEnabled = reader.GetBoolean(reader.GetOrdinal("WsAdminTrafficEnabled")),
            WsDefaultRedirectUrl = reader.IsDBNull(reader.GetOrdinal("WsDefaultRedirectUrl")) ? null : reader.GetString(reader.GetOrdinal("WsDefaultRedirectUrl")),
            LinkAdminTrafficPercent = reader.IsDBNull(reader.GetOrdinal("AdminTrafficPercent")) ? null : reader.GetDecimal(reader.GetOrdinal("AdminTrafficPercent")),
            LinkAdminTrafficEnabled = reader.IsDBNull(reader.GetOrdinal("AdminTrafficEnabled")) ? null : reader.GetBoolean(reader.GetOrdinal("AdminTrafficEnabled")),
        };

        // Destinations
        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            var dest = new DestinationModel
            {
                Id = reader.GetInt64(reader.GetOrdinal("Id")),
                Url = reader.GetString(reader.GetOrdinal("Url")),
                Weight = reader.GetInt32(reader.GetOrdinal("Weight")),
                IsAdminUrl = reader.GetBoolean(reader.GetOrdinal("IsAdminUrl")),
            };

            if (dest.IsAdminUrl)
                model.AdminDestinations.Add(dest);
            else
                model.UserDestinations.Add(dest);
        }

        // Targeting Rules (Result Set 3)
        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            model.TargetingRules.Add(new TargetingModel
            {
                RuleType = reader.GetString(reader.GetOrdinal("RuleType")),
                RuleValue = reader.GetString(reader.GetOrdinal("RuleValue")),
                RedirectUrl = reader.IsDBNull(reader.GetOrdinal("RedirectUrl")) ? null : reader.GetString(reader.GetOrdinal("RedirectUrl")),
            });
        }

        // Admin Traffic Rule URLs (Result Set 4)
        if (await reader.NextResultAsync())
        {
            while (await reader.ReadAsync())
            {
                // First row sets the traffic percent (all rows share the same rule percent)
                if (model.AdminRuleUrls.Count == 0)
                    model.AdminRuleTrafficPercent = reader.GetDecimal(reader.GetOrdinal("TrafficPercent"));

                model.AdminRuleUrls.Add(new DestinationModel
                {
                    Url = reader.GetString(reader.GetOrdinal("Url")),
                    Weight = reader.GetInt32(reader.GetOrdinal("Weight")),
                    IsAdminUrl = true,
                });
            }
        }

        return model;
    }
}

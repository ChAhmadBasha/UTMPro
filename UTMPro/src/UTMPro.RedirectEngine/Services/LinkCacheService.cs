using System.Collections.Concurrent;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using UTMPro.Data;
using UTMPro.Data.Models;
using UTMPro.RedirectEngine.Models;

namespace UTMPro.RedirectEngine.Services;

public class LinkCacheService
{
    private const string MinClicksCacheKey = "setting:AdminTrafficMinClicks";

    private readonly IMemoryCache _cache;
    private readonly IDbConnectionFactory _dbFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<LinkCacheService> _logger;
    private readonly ConcurrentDictionary<long, long> _originalClickHighWater = new();

    public LinkCacheService(
        IMemoryCache cache,
        IDbConnectionFactory dbFactory,
        IConfiguration config,
        ILogger<LinkCacheService> logger)
    {
        _cache = cache;
        _dbFactory = dbFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<LinkCacheModel?> GetAsync(string domain, string slug)
    {
        var key = $"link:{domain}:{slug}".ToLowerInvariant();

        if (!_cache.TryGetValue(key, out LinkCacheModel? link) || link == null)
        {
            link = await FetchFromDbAsync(domain, slug);

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
        }

        if (link != null)
        {
            link.AdminTrafficMinClicks = await GetAdminTrafficMinClicksAsync();
            ApplyObservedOriginalClicks(link);
        }

        return link;
    }

    /// <summary>
    /// Counts an original-destination click against the warm-up threshold
    /// immediately, without waiting for the click batch to flush to SQL.
    /// </summary>
    public void RecordOriginalClick(LinkCacheModel link)
    {
        var next = link.RecordOriginalClick();
        RememberOriginalClicks(link.Id, next);
    }

    public void Invalidate(string domain, string slug)
    {
        _cache.Remove($"link:{domain}:{slug}".ToLowerInvariant());
    }

    /// <summary>
    /// Traffic-rule changes can affect every cached link. Compacting the
    /// redirect-engine cache makes those changes visible immediately rather
    /// than waiting for every link's TTL to expire.
    /// </summary>
    public void InvalidateAll()
    {
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0);
            _logger.LogInformation("Invalidated the complete redirect link cache");
            return;
        }

        _logger.LogWarning("The configured IMemoryCache does not support full invalidation");
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

        var loadedTotalClicks = false;
        if (TryGetOrdinal(reader, "TotalClicks", out var totalClicksOrdinal)
            && !reader.IsDBNull(totalClicksOrdinal))
        {
            model.TotalClicks = reader.GetInt64(totalClicksOrdinal);
            loadedTotalClicks = true;
        }

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

        // Selected AdminTrafficRules row and URLs (Result Set 4)
        if (await reader.NextResultAsync())
        {
            var isFirstRow = true;
            while (await reader.ReadAsync())
            {
                if (isFirstRow)
                {
                    // Migration 021 supplies rule identity/scope. The fallback
                    // keeps a rolling deployment compatible with the older 019
                    // procedure until the database migration is applied.
                    model.AdminRuleId = TryGetOrdinal(reader, "RuleId", out var ruleIdOrdinal)
                        ? reader.GetInt64(ruleIdOrdinal)
                        : 0;
                    model.AdminRuleName = TryGetOrdinal(reader, "RuleName", out var ruleNameOrdinal)
                        && !reader.IsDBNull(ruleNameOrdinal)
                            ? reader.GetString(ruleNameOrdinal)
                            : null;
                    model.AdminRuleIsGlobal = TryGetOrdinal(reader, "IsGlobal", out var globalOrdinal)
                        && !reader.IsDBNull(globalOrdinal)
                            ? reader.GetBoolean(globalOrdinal)
                            : null;
                    model.AdminRuleTrafficPercent = reader.GetDecimal(reader.GetOrdinal("TrafficPercent"));
                    isFirstRow = false;
                }

                var urlOrdinal = reader.GetOrdinal("Url");
                if (reader.IsDBNull(urlOrdinal))
                    continue;

                long? adminTrafficUrlId = null;
                if (TryGetOrdinal(reader, "UrlId", out var urlIdOrdinal)
                    && !reader.IsDBNull(urlIdOrdinal))
                {
                    adminTrafficUrlId = reader.GetInt64(urlIdOrdinal);
                }

                model.AdminRuleUrls.Add(new DestinationModel
                {
                    Id = adminTrafficUrlId ?? 0,
                    AdminTrafficUrlId = adminTrafficUrlId,
                    Url = reader.GetString(urlOrdinal),
                    Weight = reader.GetInt32(reader.GetOrdinal("Weight")),
                    IsAdminUrl = true,
                });
            }
        }

        if (model.AdminRuleId.HasValue)
        {
            _logger.LogDebug(
                "Loaded admin traffic rule {RuleId} ({Source}) for link {LinkId}: {Percent}% to {UrlCount} URL(s)",
                model.AdminRuleId,
                model.AdminRuleIsGlobal == true ? "global" : "workspace",
                model.Id,
                model.AdminRuleTrafficPercent,
                model.AdminRuleUrls.Count);
        }

        await reader.DisposeAsync();
        if (!loadedTotalClicks)
            model.TotalClicks = await FetchTotalClicksFallbackAsync(model.Id);

        ApplyObservedOriginalClicks(model);

        if (model.EffectiveAdminPercent > 0 && model.EffectiveAdminUrls.Count == 0)
        {
            _logger.LogWarning(
                "Admin traffic is configured at {Percent}% for link {LinkId}, but no active admin URL is available",
                model.EffectiveAdminPercent,
                model.Id);
        }

        return model;
    }

    public async Task<int> GetAdminTrafficMinClicksAsync()
    {
        if (_cache.TryGetValue(MinClicksCacheKey, out int cached))
            return cached;

        var value = SystemSettingKeys.AdminTrafficMinClicksDefault;
        try
        {
            const string sql = "SELECT SettingValue FROM SystemSettings WHERE SettingKey = @Key";
            await using var conn = await _dbFactory.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Key", SystemSettingKeys.AdminTrafficMinClicks);
            var result = await cmd.ExecuteScalarAsync();
            value = SystemSettingKeys.ParseAdminTrafficMinClicks(result as string);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Could not load {SettingKey}; using default {Default}",
                SystemSettingKeys.AdminTrafficMinClicks,
                SystemSettingKeys.AdminTrafficMinClicksDefault);
        }

        _cache.Set(MinClicksCacheKey, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1),
            Size = 1
        });
        return value;
    }

    private void ApplyObservedOriginalClicks(LinkCacheModel link)
    {
        _originalClickHighWater.AddOrUpdate(
            link.Id,
            link.TotalClicks,
            (_, existing) =>
            {
                if (existing > link.TotalClicks)
                    link.TotalClicks = existing;
                return Math.Max(existing, link.TotalClicks);
            });
    }

    private void RememberOriginalClicks(long linkId, long clicks)
    {
        _originalClickHighWater.AddOrUpdate(
            linkId,
            clicks,
            (_, existing) => Math.Max(existing, clicks));
    }

    private async Task<long> FetchTotalClicksFallbackAsync(long linkId)
    {
        try
        {
            const string sql = "SELECT TotalClicks FROM Links WHERE Id = @Id";
            await using var conn = await _dbFactory.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", linkId);
            var result = await cmd.ExecuteScalarAsync();
            return result is long clicks ? clicks : Convert.ToInt64(result ?? 0L);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load TotalClicks for link {LinkId}", linkId);
            return 0;
        }
    }

    private static bool TryGetOrdinal(SqlDataReader reader, string columnName, out int ordinal)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
            {
                ordinal = i;
                return true;
            }
        }

        ordinal = -1;
        return false;
    }
}

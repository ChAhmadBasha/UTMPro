# PART 5: REDIRECT ENGINE
<!-- Sub-chunk of PART 5: 5.4 Services -->

## 5.4 Services

```csharp
// File: UTMPro.RedirectEngine/Services/WeightedUrlSelector.cs
namespace UTMPro.RedirectEngine.Services;

public class WeightedUrlSelector
{
    public string? Pick(List<DestinationModel> destinations)
    {
        if (destinations == null || destinations.Count == 0)
            return null;

        var active = destinations
            .Where(d => d.IsActive ?? true)
            .ToList();
            
        if (active.Count == 0) return null;
        if (active.Count == 1) return active[0].Url;

        int totalWeight = active.Sum(d => d.Weight);
        if (totalWeight <= 0) return active[0].Url;

        int roll = Random.Shared.Next(1, totalWeight + 1);
        int cumulative = 0;

        foreach (var dest in active)
        {
            cumulative += dest.Weight;
            if (roll <= cumulative)
                return dest.Url;
        }

        return active.Last().Url;
    }
}

// File: UTMPro.RedirectEngine/Services/LinkCacheService.cs
using Microsoft.Extensions.Caching.Memory;

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

    public async Task<LinkCacheModel?> GetAsync(
        string domain, string slug)
    {
        var key = $"link:{domain}:{slug}".ToLower();

        if (_cache.TryGetValue(key, out LinkCacheModel? cached))
            return cached;

        var link = await FetchFromDbAsync(domain, slug);

        if (link != null)
        {
            var ttl = int.Parse(
                _config["CacheTTLMinutes"] ?? "5");
            _cache.Set(key, link, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = 
                    TimeSpan.FromMinutes(ttl),
                SlidingExpiration = 
                    TimeSpan.FromMinutes(ttl / 2.0),
                Size = 1
            });
        }

        return link;
    }

    public void Invalidate(string domain, string slug)
    {
        _cache.Remove($"link:{domain}:{slug}".ToLower());
    }

    private async Task<LinkCacheModel?> FetchFromDbAsync(
        string domain, string slug)
    {
        using var conn = await _dbFactory
            .CreateOpenConnectionAsync();
        using var cmd = new SqlCommand(
            "sp_GetLinkForRedirect", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@Domain", domain);
        cmd.Parameters.AddWithValue("@Slug", slug);

        using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync()) return null;

        var model = new LinkCacheModel
        {
            Id = reader.GetInt64(reader.GetOrdinal("Id")),
            WorkspaceId = reader.GetInt64(
                reader.GetOrdinal("WorkspaceId")),
            Slug = reader.GetString(reader.GetOrdinal("Slug")),
            HasPassword = reader.GetBoolean(
                reader.GetOrdinal("HasPassword")),
            PasswordHash = reader.IsDBNull(
                reader.GetOrdinal("PasswordHash"))
                ? null
                : reader.GetString(
                    reader.GetOrdinal("PasswordHash")),
            ExpiresAt = reader.IsDBNull(
                reader.GetOrdinal("ExpiresAt"))
                ? null
                : reader.GetDateTime(
                    reader.GetOrdinal("ExpiresAt")),
            ExpirationUrl = reader.IsDBNull(
                reader.GetOrdinal("ExpirationUrl"))
                ? null
                : reader.GetString(
                    reader.GetOrdinal("ExpirationUrl")),
            IsCloaked = reader.GetBoolean(
                reader.GetOrdinal("IsCloaked")),
            IsArchived = reader.GetBoolean(
                reader.GetOrdinal("IsArchived")),
            IsActive = reader.GetBoolean(
                reader.GetOrdinal("IsActive")),
            RedirectMode = reader.GetString(
                reader.GetOrdinal("RedirectMode")),
            ABTestEnabled = reader.GetBoolean(
                reader.GetOrdinal("ABTestEnabled")),
            WsAdminTrafficPercent = reader.GetDecimal(
                reader.GetOrdinal("WsAdminTrafficPercent")),
            WsAdminTrafficEnabled = reader.GetBoolean(
                reader.GetOrdinal("WsAdminTrafficEnabled")),
            WsDefaultRedirectUrl = reader.IsDBNull(
                reader.GetOrdinal("WsDefaultRedirectUrl"))
                ? null
                : reader.GetString(
                    reader.GetOrdinal("WsDefaultRedirectUrl")),
        };

        // Destinations
        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            var dest = new DestinationModel
            {
                Id = reader.GetInt64(reader.GetOrdinal("Id")),
                Url = reader.GetString(reader.GetOrdinal("Url")),
                Weight = reader.GetInt32(
                    reader.GetOrdinal("Weight")),
                IsAdminUrl = reader.GetBoolean(
                    reader.GetOrdinal("IsAdminUrl")),
            };

            if (dest.IsAdminUrl)
                model.AdminDestinations.Add(dest);
            else
                model.UserDestinations.Add(dest);
        }

        // Targeting Rules
        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            model.TargetingRules.Add(new TargetingModel
            {
                RuleType = reader.GetString(
                    reader.GetOrdinal("RuleType")),
                RuleValue = reader.GetString(
                    reader.GetOrdinal("RuleValue")),
                RedirectUrl = reader.IsDBNull(
                    reader.GetOrdinal("RedirectUrl"))
                    ? null
                    : reader.GetString(
                        reader.GetOrdinal("RedirectUrl")),
            });
        }

        return model;
    }
}

// File: UTMPro.RedirectEngine/Services/ClickQueueService.cs
using System.Collections.Concurrent;

public class ClickQueueService
{
    private readonly ConcurrentQueue<ClickQueueItem> _queue 
        = new();

    public void Enqueue(ClickQueueItem item)
        => _queue.Enqueue(item);

    public IEnumerable<ClickQueueItem> DrainBatch(int maxSize)
    {
        var batch = new List<ClickQueueItem>(maxSize);
        while (batch.Count < maxSize && 
               _queue.TryDequeue(out var item))
            batch.Add(item);
        return batch;
    }

    public int Count => _queue.Count;
}

// File: UTMPro.RedirectEngine/Services/GeoIpService.cs
using MaxMind.GeoIP2;

public class GeoIpService : IDisposable
{
    private DatabaseReader? _reader;
    private readonly ILogger<GeoIpService> _logger;

    public GeoIpService(
        IConfiguration config, 
        ILogger<GeoIpService> logger)
    {
        _logger = logger;
        try
        {
            var path = config["GeoLite2DbPath"] 
                ?? "C:\\GeoLite2\\GeoLite2-City.mmdb";
            _reader = new DatabaseReader(path);
            _logger.LogInformation(
                "GeoIP database loaded: {path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "GeoIP database not found: {msg}", ex.Message);
        }
    }

    public GeoResult Lookup(string? ip)
    {
        if (_reader == null || string.IsNullOrEmpty(ip))
            return new GeoResult();

        try
        {
            var city = _reader.City(ip);
            return new GeoResult
            {
                Country = city.Country.Name,
                CountryCode = city.Country.IsoCode,
                City = city.City.Name,
                Region = city.MostSpecificSubdivision.Name,
                Continent = city.Continent.Name,
                Latitude = (decimal?)city.Location.Latitude,
                Longitude = (decimal?)city.Location.Longitude
            };
        }
        catch
        {
            return new GeoResult();
        }
    }

    public void Dispose() => _reader?.Dispose();
}

public class GeoResult
{
    public string? Country { get; set; }
    public string? CountryCode { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? Continent { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

// File: UTMPro.RedirectEngine/Services/DeviceDetectionService.cs
public class DeviceDetectionService
{
    public DeviceInfo Parse(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return new DeviceInfo();

        var ua = userAgent.ToLowerInvariant();

        return new DeviceInfo
        {
            Device = DetectDevice(ua),
            Browser = DetectBrowser(ua),
            BrowserVersion = DetectBrowserVersion(ua),
            OS = DetectOS(ua),
            OSVersion = DetectOSVersion(ua),
            IsIOS = ua.Contains("iphone") || ua.Contains("ipad"),
            IsAndroid = ua.Contains("android")
        };
    }

    private string DetectDevice(string ua)
    {
        if (ua.Contains("ipad") || 
            (ua.Contains("tablet") && !ua.Contains("mobile")))
            return "Tablet";
        if (ua.Contains("mobile") || ua.Contains("iphone") ||
            (ua.Contains("android") && 
             !ua.Contains("tablet")))
            return "Mobile";
        return "Desktop";
    }

    private string DetectBrowser(string ua)
    {
        if (ua.Contains("edg/")) return "Edge";
        if (ua.Contains("opr/") || ua.Contains("opera"))
            return "Opera";
        if (ua.Contains("chrome") && !ua.Contains("chromium"))
            return "Chrome";
        if (ua.Contains("firefox")) return "Firefox";
        if (ua.Contains("safari") && !ua.Contains("chrome"))
            return "Safari";
        if (ua.Contains("msie") || ua.Contains("trident"))
            return "IE";
        return "Other";
    }

    private string DetectBrowserVersion(string ua) => "";

    private string DetectOS(string ua)
    {
        if (ua.Contains("windows nt")) return "Windows";
        if (ua.Contains("mac os x") && 
            !ua.Contains("iphone") && !ua.Contains("ipad"))
            return "macOS";
        if (ua.Contains("iphone") || ua.Contains("ipad"))
            return "iOS";
        if (ua.Contains("android")) return "Android";
        if (ua.Contains("linux")) return "Linux";
        return "Other";
    }

    private string DetectOSVersion(string ua) => "";
}

public class DeviceInfo
{
    public string Device { get; set; } = "Unknown";
    public string Browser { get; set; } = "Unknown";
    public string BrowserVersion { get; set; } = "";
    public string OS { get; set; } = "Unknown";
    public string OSVersion { get; set; } = "";
    public bool IsIOS { get; set; }
    public bool IsAndroid { get; set; }
}
```

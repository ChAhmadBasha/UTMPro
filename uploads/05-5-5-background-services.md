# PART 5: REDIRECT ENGINE
<!-- Sub-chunk of PART 5: 5.5 Background Services -->

## 5.5 Background Services

```csharp
// File: UTMPro.RedirectEngine/BackgroundServices/ClickBatchProcessor.cs
using System.Text.Json;

public class ClickBatchProcessor : BackgroundService
{
    private readonly ClickQueueService _queue;
    private readonly GeoIpService _geo;
    private readonly DeviceDetectionService _device;
    private readonly IDbConnectionFactory _db;
    private readonly IConfiguration _config;
    private readonly ILogger<ClickBatchProcessor> _logger;

    public ClickBatchProcessor(
        ClickQueueService queue,
        GeoIpService geo,
        DeviceDetectionService device,
        IDbConnectionFactory db,
        IConfiguration config,
        ILogger<ClickBatchProcessor> logger)
    {
        _queue = queue;
        _geo = geo;
        _device = device;
        _db = db;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken ct)
    {
        var intervalSeconds = int.Parse(
            _config["BatchProcessorSeconds"] ?? "30");
        var batchSize = int.Parse(
            _config["BatchSizeLimit"] ?? "500");

        _logger.LogInformation(
            "ClickBatchProcessor started. " +
            "Interval: {s}s, BatchSize: {b}",
            intervalSeconds, batchSize);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Process immediately if queue is large
                if (_queue.Count >= batchSize)
                    await ProcessBatchAsync(batchSize);
                
                await Task.Delay(
                    TimeSpan.FromSeconds(intervalSeconds), ct);
                
                if (_queue.Count > 0)
                    await ProcessBatchAsync(batchSize);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "ClickBatchProcessor error");
            }
        }

        // Final drain on shutdown
        if (_queue.Count > 0)
            await ProcessBatchAsync(int.MaxValue);
    }

    private async Task ProcessBatchAsync(int maxSize)
    {
        var batch = _queue.DrainBatch(maxSize).ToList();
        if (batch.Count == 0) return;

        _logger.LogDebug(
            "Processing batch of {count} clicks", batch.Count);

        // Enrich with Geo + Device
        foreach (var item in batch)
        {
            var geo = _geo.Lookup(item.IPAddress);
            item.Country = geo.Country;
            item.CountryCode = geo.CountryCode;
            item.City = geo.City;
            item.Region = geo.Region;
            item.Continent = geo.Continent;
            item.Latitude = geo.Latitude;
            item.Longitude = geo.Longitude;

            var dev = _device.Parse(item.UserAgent);
            item.Device = dev.Device;
            item.Browser = dev.Browser;
            item.BrowserVersion = dev.BrowserVersion;
            item.OS = dev.OS;
            item.OSVersion = dev.OSVersion;
        }

        // Bulk insert via stored procedure with JSON
        try
        {
            var json = JsonSerializer.Serialize(batch);
            using var conn = await _db.CreateOpenConnectionAsync();
            using var cmd = new SqlCommand(
                "sp_BulkInsertClickEvents", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 60;
            cmd.Parameters.AddWithValue("@Events", json);
            await cmd.ExecuteNonQueryAsync();

            _logger.LogInformation(
                "Inserted {count} click events", batch.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to insert {count} click events", 
                batch.Count);
        }
    }
}

// File: UTMPro.RedirectEngine/BackgroundServices/CacheWarmupService.cs
public class CacheWarmupService : BackgroundService
{
    private readonly IDbConnectionFactory _db;
    private readonly LinkCacheService _cache;
    private readonly IConfiguration _config;
    private readonly ILogger<CacheWarmupService> _logger;

    public CacheWarmupService(
        IDbConnectionFactory db,
        LinkCacheService cache,
        IConfiguration config,
        ILogger<CacheWarmupService> logger)
    {
        _db = db;
        _cache = cache;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken ct)
    {
        // Initial warmup on startup
        await WarmupAsync(ct);

        // Refresh every 5 minutes
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), ct);
            await WarmupAsync(ct);
        }
    }

    private async Task WarmupAsync(CancellationToken ct)
    {
        try
        {
            var count = int.Parse(
                _config["CacheWarmupCount"] ?? "1000");

            const string sql = """
                SELECT TOP (@Count)
                    d.Domain, l.Slug
                FROM Links l
                INNER JOIN Domains d ON l.DomainId = d.Id
                WHERE l.IsActive = 1 AND l.IsArchived = 0
                ORDER BY l.TotalClicks DESC
                """;

            using var conn = await _db.CreateOpenConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Count", count);

            var pairs = new List<(string domain, string slug)>();
            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                pairs.Add((reader.GetString(0), 
                           reader.GetString(1)));

            int warmed = 0;
            foreach (var (domain, slug) in pairs)
            {
                await _cache.GetAsync(domain, slug);
                warmed++;
            }

            _logger.LogInformation(
                "Cache warmed: {count} links", warmed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache warmup failed");
        }
    }
}
```

---

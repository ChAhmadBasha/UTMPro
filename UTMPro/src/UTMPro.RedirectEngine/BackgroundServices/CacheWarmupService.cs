using Microsoft.Data.SqlClient;
using UTMPro.Data;
using UTMPro.RedirectEngine.Services;

namespace UTMPro.RedirectEngine.BackgroundServices;

public class CacheWarmupService : BackgroundService
{
    private readonly IDbConnectionFactory _db;
    private readonly LinkCacheService _cache;
    private readonly IConfiguration _config;
    private readonly ILogger<CacheWarmupService> _logger;

    public CacheWarmupService(IDbConnectionFactory db, LinkCacheService cache,
        IConfiguration config, ILogger<CacheWarmupService> logger)
    {
        _db = db;
        _cache = cache;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await WarmupAsync(ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5), ct);
                await WarmupAsync(ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache warmup failed");
            }
        }
    }

    private async Task WarmupAsync(CancellationToken ct)
    {
        try
        {
            var count = int.Parse(_config["CacheWarmupCount"] ?? "1000");

            const string sql = """
                SELECT TOP (@Count)
                    d.Domain, l.Slug
                FROM Links l
                INNER JOIN Domains d ON l.DomainId = d.Id
                WHERE l.IsActive = 1 AND l.IsArchived = 0
                ORDER BY l.TotalClicks DESC
                """;

            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Count", count);

            var pairs = new List<(string domain, string slug)>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                pairs.Add((reader.GetString(0), reader.GetString(1)));

            int warmed = 0;
            foreach (var (domain, slug) in pairs)
            {
                await _cache.GetAsync(domain, slug);
                warmed++;
            }

            _logger.LogInformation("Cache warmed: {count} links", warmed);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache warmup failed");
        }
    }
}

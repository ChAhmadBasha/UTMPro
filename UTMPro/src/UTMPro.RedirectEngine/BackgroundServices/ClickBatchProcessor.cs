using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using UTMPro.Data;
using UTMPro.RedirectEngine.Services;

namespace UTMPro.RedirectEngine.BackgroundServices;

public class ClickBatchProcessor : BackgroundService
{
    private readonly ClickQueueService _queue;
    private readonly GeoIpService _geo;
    private readonly DeviceDetectionService _device;
    private readonly IDbConnectionFactory _db;
    private readonly IConfiguration _config;
    private readonly ILogger<ClickBatchProcessor> _logger;

    public ClickBatchProcessor(
        ClickQueueService queue, GeoIpService geo,
        DeviceDetectionService device, IDbConnectionFactory db,
        IConfiguration config, ILogger<ClickBatchProcessor> logger)
    {
        _queue = queue; _geo = geo; _device = device;
        _db = db; _config = config; _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var intervalSeconds = int.Parse(_config["BatchProcessorSeconds"] ?? "10");
        var batchSize = int.Parse(_config["BatchSizeLimit"] ?? "500");

        _logger.LogInformation("ClickBatchProcessor started. Interval={s}s, BatchSize={b}", intervalSeconds, batchSize);

        // Initial delay to let app start
        await Task.Delay(2000, ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (_queue.Count > 0)
                {
                    await ProcessBatchAsync(batchSize);
                }

                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ClickBatchProcessor error. Queue size: {count}", _queue.Count);
                await Task.Delay(5000, ct); // Wait before retrying
            }
        }

        // Final drain on shutdown
        if (_queue.Count > 0)
        {
            _logger.LogInformation("Draining remaining {count} clicks on shutdown", _queue.Count);
            await ProcessBatchAsync(int.MaxValue);
        }
    }

    private async Task ProcessBatchAsync(int maxSize)
    {
        var batch = _queue.DrainBatch(maxSize).ToList();
        if (batch.Count == 0) return;

        _logger.LogInformation("Processing batch of {count} clicks", batch.Count);

        // Enrich with Geo + Device
        foreach (var item in batch)
        {
            try
            {
                var geo = _geo.Lookup(item.IPAddress);
                item.Country = geo.Country;
                item.CountryCode = geo.CountryCode;
                item.City = geo.City;
                item.Region = geo.Region;
                item.Continent = geo.Continent;
                item.Latitude = geo.Latitude;
                item.Longitude = geo.Longitude;
            }
            catch { /* GeoIP lookup failure is not critical */ }

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
            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand("sp_BulkInsertClickEvents", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 60;
            cmd.Parameters.AddWithValue("@Events", json);
            await cmd.ExecuteNonQueryAsync();

            _logger.LogInformation("Successfully inserted {count} click events into database", batch.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FAILED to insert {count} click events. Error: {msg}", batch.Count, ex.Message);
            
            // Try individual inserts as fallback
            foreach (var item in batch)
            {
                try
                {
                    await InsertSingleClickAsync(item);
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, "Failed to insert single click for link {linkId}", item.LinkId);
                }
            }
        }
    }

    private async Task InsertSingleClickAsync(UTMPro.RedirectEngine.Models.ClickQueueItem item)
    {
        const string sql = @"
            INSERT INTO ClickEvents (LinkId, WorkspaceId, DestinationUrl, IsAdminRedirect, AdminTrafficUrlId, IPAddress, UserAgent, Referer,
                Country, CountryCode, City, Region, Continent, Latitude, Longitude,
                Device, Browser, BrowserVersion, OS, OSVersion,
                UTMSource, UTMMedium, UTMCampaign, UTMTerm, UTMContent, [Trigger], ClickedAt)
            VALUES (@LinkId, @WsId, @Dest, @IsAdmin, @AdminTrafficUrlId, @IP, @UA, @Ref,
                @Country, @CC, @City, @Region, @Cont, @Lat, @Lng,
                @Device, @Browser, @BV, @OS, @OSV,
                @S, @M, @C, @T, @Co, @Tr, @At);
            UPDATE Links
            SET TotalClicks = TotalClicks + 1, LastClickAt = GETUTCDATE()
            WHERE Id = @LinkId;
            UPDATE AdminTrafficUrls
            SET ClickCount = ClickCount + 1
            WHERE Id = @AdminTrafficUrlId AND @IsAdmin = 1;";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@LinkId", item.LinkId);
        cmd.Parameters.AddWithValue("@WsId", item.WorkspaceId);
        cmd.Parameters.AddWithValue("@Dest", (object?)item.DestinationUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsAdmin", item.IsAdminRedirect);
        cmd.Parameters.AddWithValue("@AdminTrafficUrlId", (object?)item.AdminTrafficUrlId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IP", (object?)item.IPAddress ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@UA", (object?)item.UserAgent ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Ref", (object?)item.Referer ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Country", (object?)item.Country ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CC", (object?)item.CountryCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@City", (object?)item.City ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Region", (object?)item.Region ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Cont", (object?)item.Continent ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Lat", (object?)item.Latitude ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Lng", (object?)item.Longitude ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Device", (object?)item.Device ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Browser", (object?)item.Browser ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@BV", (object?)item.BrowserVersion ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@OS", (object?)item.OS ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@OSV", (object?)item.OSVersion ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@S", (object?)item.UTMSource ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@M", (object?)item.UTMMedium ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@C", (object?)item.UTMCampaign ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@T", (object?)item.UTMTerm ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Co", (object?)item.UTMContent ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Tr", (object?)item.Trigger ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@At", item.ClickedAt);
        await cmd.ExecuteNonQueryAsync();
    }
}

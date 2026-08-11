using Microsoft.Data.SqlClient;
using UTMPro.Data;

namespace UTMPro.Web.BackgroundServices;

public class MonthlyUsageResetService : BackgroundService
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<MonthlyUsageResetService> _logger;

    public MonthlyUsageResetService(IDbConnectionFactory db, ILogger<MonthlyUsageResetService> logger)
    {
        _db = db; _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(1), ct);
                await ResetExpiredUsageAsync();
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Monthly usage reset error"); }
        }
    }

    private async Task ResetExpiredUsageAsync()
    {
        const string sql = @"UPDATE Workspaces SET LinksUsedThisMonth = 0, EventsUsedThisMonth = 0,
            UsageResetDate = DATEADD(MONTH, 1, GETUTCDATE()), UpdatedAt = GETUTCDATE()
            WHERE UsageResetDate <= GETUTCDATE() AND IsActive = 1 AND DeletedAt IS NULL";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        var affected = await cmd.ExecuteNonQueryAsync();
        if (affected > 0) _logger.LogInformation("Reset usage for {count} workspaces", affected);
    }
}

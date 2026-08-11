using Microsoft.Data.SqlClient;
using UTMPro.Data;

namespace UTMPro.Web.BackgroundServices;

public class PartnerPayoutScheduler : BackgroundService
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<PartnerPayoutScheduler> _logger;

    public PartnerPayoutScheduler(IDbConnectionFactory db, ILogger<PartnerPayoutScheduler> logger)
    {
        _db = db; _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var nextRun = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(1);
                await Task.Delay(nextRun - now, ct);
                await ProcessScheduledPayoutsAsync();
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Partner payout scheduler error"); }
        }
    }

    private async Task ProcessScheduledPayoutsAsync()
    {
        // Find partners eligible for payout (PendingBalance >= PayoutThreshold of their program)
        const string sql = @"
            SELECT p.Id AS PartnerId, p.PendingBalance, p.WorkspaceId, p.ProgramId,
                   pp.PayoutThreshold, pp.PayoutFrequency
            FROM Partners p
            INNER JOIN PartnerPrograms pp ON p.ProgramId = pp.Id
            WHERE p.IsActive = 1 AND p.ApplicationStatus = 'Approved'
              AND p.PendingBalance >= pp.PayoutThreshold
              AND pp.PayoutFrequency = 'Monthly' AND pp.IsActive = 1";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        var eligible = new List<(long PartnerId, decimal Balance, long WorkspaceId, long ProgramId)>();
        while (await reader.ReadAsync())
        {
            eligible.Add((reader.GetInt64(0), reader.GetDecimal(1), reader.GetInt64(2), reader.GetInt64(3)));
        }
        await reader.CloseAsync();

        foreach (var (partnerId, balance, wsId, pgId) in eligible)
        {
            // Create payout record (status = Pending for manual review)
            var extId = $"po_{Guid.NewGuid():N}"[..23];
            var insertSql = @"INSERT INTO PartnerPayouts (ExternalId, PartnerId, ProgramId, WorkspaceId, Amount, Currency, PayoutMethod, Status, PeriodEnd, CreatedAt, UpdatedAt)
                VALUES (@Eid, @Pid, @Pgid, @Wid, @Amt, 'USD', 'Stripe', 'Pending', GETUTCDATE(), GETUTCDATE(), GETUTCDATE())";
            await using var insertCmd = new SqlCommand(insertSql, conn);
            insertCmd.Parameters.AddWithValue("@Eid", extId);
            insertCmd.Parameters.AddWithValue("@Pid", partnerId);
            insertCmd.Parameters.AddWithValue("@Pgid", pgId);
            insertCmd.Parameters.AddWithValue("@Wid", wsId);
            insertCmd.Parameters.AddWithValue("@Amt", balance);
            await insertCmd.ExecuteNonQueryAsync();

            _logger.LogInformation("Payout created for partner {id}: ${amt}", partnerId, balance);
        }

        if (eligible.Count > 0)
            _logger.LogInformation("Created {count} payout records", eligible.Count);
    }
}

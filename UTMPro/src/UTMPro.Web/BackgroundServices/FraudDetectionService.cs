using Microsoft.Data.SqlClient;
using UTMPro.Data;

namespace UTMPro.Web.BackgroundServices;

public class FraudDetectionService : BackgroundService
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<FraudDetectionService> _logger;

    public FraudDetectionService(IDbConnectionFactory db, ILogger<FraudDetectionService> logger)
    {
        _db = db; _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(15), ct);
                await RunFraudChecksAsync();
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Fraud detection error"); }
        }
    }

    private async Task RunFraudChecksAsync()
    {
        await using var conn = await _db.CreateOpenConnectionAsync();

        // 1. Self-referral detection
        const string selfRefSql = @"
            SELECT p.Id, p.ProgramId, p.Email FROM Partners p
            INNER JOIN PartnerSales ps ON ps.PartnerId = p.Id
            WHERE ps.CustomerEmail = p.Email AND ps.Status = 'Pending'
              AND ps.CreatedAt >= DATEADD(DAY, -1, GETUTCDATE())
            GROUP BY p.Id, p.ProgramId, p.Email HAVING COUNT(*) > 0";

        await using var cmd1 = new SqlCommand(selfRefSql, conn);
        await using var r1 = await cmd1.ExecuteReaderAsync();
        var selfRefs = new List<(long PartnerId, long ProgramId, string Email)>();
        while (await r1.ReadAsync()) selfRefs.Add((r1.GetInt64(0), r1.GetInt64(1), r1.GetString(2)));
        await r1.CloseAsync();

        foreach (var (pid, pgid, email) in selfRefs)
        {
            await using var insertCmd = new SqlCommand(
                @"IF NOT EXISTS (SELECT 1 FROM PartnerFraudEvents WHERE PartnerId=@Pid AND FraudType='SelfReferral' AND CreatedAt >= DATEADD(DAY,-1,GETUTCDATE()))
                  INSERT INTO PartnerFraudEvents (PartnerId,ProgramId,FraudType,Description,Severity,CreatedAt) VALUES (@Pid,@Pgid,'SelfReferral',@Desc,'High',GETUTCDATE())", conn);
            insertCmd.Parameters.AddWithValue("@Pid", pid);
            insertCmd.Parameters.AddWithValue("@Pgid", pgid);
            insertCmd.Parameters.AddWithValue("@Desc", $"Self-referral detected for {email}");
            await insertCmd.ExecuteNonQueryAsync();

            // Update fraud score
            await using var scoreCmd = new SqlCommand("UPDATE Partners SET FraudScore = FraudScore + 50, UpdatedAt = GETUTCDATE() WHERE Id = @Pid", conn);
            scoreCmd.Parameters.AddWithValue("@Pid", pid);
            await scoreCmd.ExecuteNonQueryAsync();
        }

        // 2. Auto-flag partners with high fraud score
        var threshold = 80;
        await using var flagCmd = new SqlCommand(
            "UPDATE Partners SET IsFlagged = 1, UpdatedAt = GETUTCDATE() WHERE FraudScore >= @Threshold AND IsFlagged = 0", conn);
        flagCmd.Parameters.AddWithValue("@Threshold", threshold);
        var flagged = await flagCmd.ExecuteNonQueryAsync();
        if (flagged > 0) _logger.LogWarning("Auto-flagged {count} partners for fraud", flagged);

        // 3. Auto-suspend score >= 100
        await using var suspendCmd = new SqlCommand(
            "UPDATE Partners SET ApplicationStatus = 'Suspended', IsActive = 0, UpdatedAt = GETUTCDATE() WHERE FraudScore >= 100 AND IsActive = 1", conn);
        var suspended = await suspendCmd.ExecuteNonQueryAsync();
        if (suspended > 0) _logger.LogWarning("Auto-suspended {count} partners for fraud score >= 100", suspended);
    }
}

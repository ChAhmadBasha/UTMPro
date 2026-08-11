using Microsoft.Data.SqlClient;
using UTMPro.Data;

namespace UTMPro.Web.BackgroundServices;

/// <summary>
/// Background service that checks for expired trial plans and downgrades 
/// workspaces to the plan's FallbackPlanId (typically Free plan).
/// Runs every hour.
/// </summary>
public class PlanExpiryService : BackgroundService
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<PlanExpiryService> _logger;

    public PlanExpiryService(IDbConnectionFactory db, ILogger<PlanExpiryService> logger)
    {
        _db = db; _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Initial delay to let app start
        await Task.Delay(TimeSpan.FromSeconds(30), ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await DowngradeExpiredPlansAsync();
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Plan expiry check error");
            }

            await Task.Delay(TimeSpan.FromHours(1), ct);
        }
    }

    private async Task DowngradeExpiredPlansAsync()
    {
        // Find workspaces whose PlanEndDate has passed and whose current plan has a FallbackPlanId
        const string sql = @"
            SELECT w.Id AS WorkspaceId, w.PlanId, w.Name AS WorkspaceName, 
                   p.Name AS PlanName, p.FallbackPlanId, fp.Name AS FallbackPlanName
            FROM Workspaces w
            INNER JOIN Plans p ON w.PlanId = p.Id
            INNER JOIN Plans fp ON p.FallbackPlanId = fp.Id
            WHERE w.PlanEndDate IS NOT NULL 
              AND w.PlanEndDate <= GETUTCDATE()
              AND w.DeletedAt IS NULL
              AND w.IsActive = 1
              AND p.FallbackPlanId IS NOT NULL
              AND w.PlanId != p.FallbackPlanId";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        
        var workspacesToDowngrade = new List<(long wsId, int fallbackPlanId, string wsName, string oldPlan, string newPlan)>();
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                workspacesToDowngrade.Add((
                    reader.GetInt64(reader.GetOrdinal("WorkspaceId")),
                    reader.GetInt32(reader.GetOrdinal("FallbackPlanId")),
                    reader.GetString(reader.GetOrdinal("WorkspaceName")),
                    reader.GetString(reader.GetOrdinal("PlanName")),
                    reader.GetString(reader.GetOrdinal("FallbackPlanName"))
                ));
            }
        }

        foreach (var (wsId, fallbackPlanId, wsName, oldPlan, newPlan) in workspacesToDowngrade)
        {
            try
            {
                // Downgrade the workspace
                const string updateSql = @"
                    UPDATE Workspaces SET PlanId = @PlanId, PlanEndDate = NULL, UpdatedAt = GETUTCDATE() 
                    WHERE Id = @Id";
                await using var updateCmd = new SqlCommand(updateSql, conn);
                updateCmd.Parameters.AddWithValue("@Id", wsId);
                updateCmd.Parameters.AddWithValue("@PlanId", fallbackPlanId);
                await updateCmd.ExecuteNonQueryAsync();

                // Record billing history (AssignedBy = workspace owner via subquery)
                const string historySql = @"
                    INSERT INTO WorkspaceBillingHistory (WorkspaceId, PlanId, Action, AssignedBy, Notes, StartDate, CreatedAt)
                    VALUES (@WorkspaceId, @PlanId, 'TrialExpired', 
                        (SELECT OwnerId FROM Workspaces WHERE Id = @WorkspaceId), 
                        @Notes, GETUTCDATE(), GETUTCDATE())";
                await using var historyCmd = new SqlCommand(historySql, conn);
                historyCmd.Parameters.AddWithValue("@WorkspaceId", wsId);
                historyCmd.Parameters.AddWithValue("@PlanId", fallbackPlanId);
                historyCmd.Parameters.AddWithValue("@Notes", $"Trial expired. Downgraded from {oldPlan} to {newPlan}.");
                await historyCmd.ExecuteNonQueryAsync();

                _logger.LogInformation("Workspace '{Name}' (Id={Id}) downgraded from {Old} to {New} (trial expired)",
                    wsName, wsId, oldPlan, newPlan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to downgrade workspace {Id}", wsId);
            }
        }

        if (workspacesToDowngrade.Count > 0)
            _logger.LogInformation("Plan expiry check: downgraded {Count} workspace(s)", workspacesToDowngrade.Count);
    }
}

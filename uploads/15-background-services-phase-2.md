# PART 15: BACKGROUND SERVICES (PHASE 2)

```csharp
// ============================================================
// File: UTMPro.Web/BackgroundServices/
//       PartnerPayoutScheduler.cs
// ============================================================
public class PartnerPayoutScheduler : BackgroundService
{
    // Runs daily at 00:00 UTC
    // Checks for partners eligible for payout
    // Creates payout records for manual review
    // OR auto-processes via Stripe Connect

    protected override async Task ExecuteAsync(
        CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = new DateTime(
                now.Year, now.Month, now.Day, 
                0, 0, 0, DateTimeKind.Utc)
                .AddDays(1);
            
            var delay = nextRun - now;
            await Task.Delay(delay, ct);

            await ProcessScheduledPayoutsAsync();
        }
    }

    private async Task ProcessScheduledPayoutsAsync()
    {
        // 1. Find all programs with PayoutFrequency='Monthly'
        // 2. Find partners with PendingBalance >= PayoutThreshold
        // 3. Create PartnerPayout records (Status=Pending)
        // 4. If PayoutMethod=Stripe and StripeConnectEnabled:
        //    → Process via Stripe Transfers API
        // 5. Send notification to workspace owner
        // 6. Send payout confirmation to partner
    }
}

// ============================================================
// File: UTMPro.Web/BackgroundServices/
//       MonthlyUsageResetService.cs
// ============================================================
public class MonthlyUsageResetService : BackgroundService
{
    // Runs daily, checks workspaces whose UsageResetDate <= NOW
    // Resets LinksUsedThisMonth and EventsUsedThisMonth to 0
    // Sets new UsageResetDate = DATEADD(MONTH, 1, GETUTCDATE())

    protected override async Task ExecuteAsync(
        CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(
                TimeSpan.FromHours(1), ct);

            await ResetExpiredUsageAsync();
        }
    }

    private async Task ResetExpiredUsageAsync()
    {
        const string sql = """
            UPDATE Workspaces
            SET LinksUsedThisMonth  = 0,
                EventsUsedThisMonth = 0,
                UsageResetDate = DATEADD(MONTH, 1, GETUTCDATE()),
                UpdatedAt = GETUTCDATE()
            WHERE UsageResetDate <= GETUTCDATE()
              AND IsActive = 1
              AND DeletedAt IS NULL
            """;
        // Execute SQL
    }
}

// ============================================================
// File: UTMPro.Web/BackgroundServices/
//       WebhookRetryProcessor.cs
// ============================================================
public class WebhookRetryProcessor : BackgroundService
{
    // Runs every 5 minutes
    // Finds failed webhook deliveries scheduled for retry
    // Re-delivers them

    protected override async Task ExecuteAsync(
        CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(
                TimeSpan.FromMinutes(5), ct);

            await _webhookService.RetryFailedAsync();
        }
    }
}

// ============================================================
// File: UTMPro.Web/BackgroundServices/
//       FraudDetectionService.cs
// ============================================================
public class FraudDetectionService : BackgroundService
{
    // Runs every 15 minutes
    // Checks for fraud patterns:
    //   1. Partners with > X same-IP clicks in 24h
    //   2. Partners with self-referral sales
    //   3. Chargeback patterns
    //   4. VPN/proxy usage patterns
    // Updates FraudScore on Partners
    // Auto-flags if FraudScore >= threshold

    protected override async Task ExecuteAsync(
        CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(
                TimeSpan.FromMinutes(15), ct);

            await RunFraudChecksAsync();
        }
    }

    private async Task RunFraudChecksAsync()
    {
        // Check SelfReferral
        const string selfReferralSql = """
            SELECT p.Id, p.ProgramId, p.Email, COUNT(*) AS Count
            FROM Partners p
            INNER JOIN PartnerSales ps ON ps.PartnerId = p.Id
            WHERE ps.CustomerEmail = p.Email
              AND ps.Status = 'Pending'
              AND ps.CreatedAt >= DATEADD(DAY, -1, GETUTCDATE())
            GROUP BY p.Id, p.ProgramId, p.Email
            HAVING COUNT(*) > 0
            """;

        // For each self-referral: create fraud event, flag partner

        // Check duplicate IP
        const string dupIpSql = """
            SELECT p.Id, p.ProgramId, ce.IPAddress, 
                   COUNT(*) AS ClickCount
            FROM Partners p
            INNER JOIN ClickEvents ce ON ce.PartnerId = p.Id
            WHERE ce.ClickedAt >= DATEADD(HOUR, -24, GETUTCDATE())
            GROUP BY p.Id, p.ProgramId, ce.IPAddress
            HAVING COUNT(*) > 10
            """;
        // For each: create fraud event if not already logged
    }
}
```

---

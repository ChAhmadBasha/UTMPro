using System.Data;
using Microsoft.Data.SqlClient;
using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public class BillingRepository : IBillingRepository
{
    private readonly IDbConnectionFactory _db;
    public BillingRepository(IDbConnectionFactory db) => _db = db;

    public async Task<StripeCustomerModel?> GetStripeCustomerAsync(long workspaceId)
    {
        const string sql = "SELECT * FROM StripeCustomers WHERE WorkspaceId = @WsId";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WsId", workspaceId);
        await using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new StripeCustomerModel { Id = r.GetInt64(0), WorkspaceId = r.GetInt64(1), StripeCustomerId = r.GetString(2) };
    }

    public async Task UpsertStripeCustomerAsync(long workspaceId, string stripeCustomerId)
    {
        const string sql = @"IF EXISTS (SELECT 1 FROM StripeCustomers WHERE WorkspaceId=@WsId)
            UPDATE StripeCustomers SET StripeCustomerId=@Cid,UpdatedAt=GETUTCDATE() WHERE WorkspaceId=@WsId
            ELSE INSERT INTO StripeCustomers (WorkspaceId,StripeCustomerId,CreatedAt,UpdatedAt) VALUES (@WsId,@Cid,GETUTCDATE(),GETUTCDATE())";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WsId", workspaceId);
        cmd.Parameters.AddWithValue("@Cid", stripeCustomerId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<long> GetWorkspaceIdByStripeCustomerIdAsync(string stripeCustomerId)
    {
        const string sql = "SELECT WorkspaceId FROM StripeCustomers WHERE StripeCustomerId = @Cid";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Cid", stripeCustomerId);
        var result = await cmd.ExecuteScalarAsync();
        return result != null ? (long)result : 0;
    }

    public async Task UpsertSubscriptionAsync(StripeSubscriptionModel sub)
    {
        const string sql = @"IF EXISTS (SELECT 1 FROM StripeSubscriptions WHERE StripeSubscriptionId=@Sid)
            UPDATE StripeSubscriptions SET Status=@St,StripePriceId=@Pr,PlanId=@Pid,CurrentPeriodStart=@CPS,CurrentPeriodEnd=@CPE,CancelAtPeriodEnd=@CAP,TrialStart=@TS,TrialEnd=@TE,UpdatedAt=GETUTCDATE() WHERE StripeSubscriptionId=@Sid
            ELSE INSERT INTO StripeSubscriptions (WorkspaceId,StripeSubscriptionId,StripeCustomerId,StripePriceId,PlanId,Status,CurrentPeriodStart,CurrentPeriodEnd,TrialStart,TrialEnd,CreatedAt,UpdatedAt) VALUES (@WsId,@Sid,@Cid,@Pr,@Pid,@St,@CPS,@CPE,@TS,@TE,GETUTCDATE(),GETUTCDATE())";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WsId", sub.WorkspaceId);
        cmd.Parameters.AddWithValue("@Sid", sub.StripeSubscriptionId);
        cmd.Parameters.AddWithValue("@Cid", sub.StripeCustomerId);
        cmd.Parameters.AddWithValue("@Pr", sub.StripePriceId);
        cmd.Parameters.AddWithValue("@Pid", sub.PlanId);
        cmd.Parameters.AddWithValue("@St", sub.Status);
        cmd.Parameters.AddWithValue("@CPS", sub.CurrentPeriodStart);
        cmd.Parameters.AddWithValue("@CPE", sub.CurrentPeriodEnd);
        cmd.Parameters.AddWithValue("@CAP", sub.CancelAtPeriodEnd);
        cmd.Parameters.AddWithValue("@TS", (object?)sub.TrialStart ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TE", (object?)sub.TrialEnd ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<StripeSubscriptionModel?> GetSubscriptionByStripeIdAsync(string stripeSubId)
    {
        const string sql = "SELECT ss.*, p.Name AS PlanName, p.Price AS PlanPrice FROM StripeSubscriptions ss INNER JOIN Plans p ON ss.PlanId = p.Id WHERE ss.StripeSubscriptionId = @Sid";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Sid", stripeSubId);
        await using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new StripeSubscriptionModel { Id = r.GetInt64(r.GetOrdinal("Id")), WorkspaceId = r.GetInt64(r.GetOrdinal("WorkspaceId")), StripeSubscriptionId = r.GetString(r.GetOrdinal("StripeSubscriptionId")), PlanId = r.GetInt32(r.GetOrdinal("PlanId")), Status = r.GetString(r.GetOrdinal("Status")), CurrentPeriodStart = r.GetDateTime(r.GetOrdinal("CurrentPeriodStart")), CurrentPeriodEnd = r.GetDateTime(r.GetOrdinal("CurrentPeriodEnd")), PlanName = r.GetString(r.GetOrdinal("PlanName")) };
    }

    public async Task<StripeSubscriptionModel?> GetActiveSubscriptionAsync(long workspaceId)
    {
        const string sql = "SELECT ss.*, p.Name AS PlanName, p.Price AS PlanPrice FROM StripeSubscriptions ss INNER JOIN Plans p ON ss.PlanId = p.Id WHERE ss.WorkspaceId = @WsId AND ss.Status IN ('active','trialing','past_due') ORDER BY ss.CreatedAt DESC";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WsId", workspaceId);
        await using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new StripeSubscriptionModel { Id = r.GetInt64(r.GetOrdinal("Id")), WorkspaceId = r.GetInt64(r.GetOrdinal("WorkspaceId")), StripeSubscriptionId = r.GetString(r.GetOrdinal("StripeSubscriptionId")), PlanId = r.GetInt32(r.GetOrdinal("PlanId")), Status = r.GetString(r.GetOrdinal("Status")), CurrentPeriodStart = r.GetDateTime(r.GetOrdinal("CurrentPeriodStart")), CurrentPeriodEnd = r.GetDateTime(r.GetOrdinal("CurrentPeriodEnd")), CancelAtPeriodEnd = r.GetBoolean(r.GetOrdinal("CancelAtPeriodEnd")), PlanName = r.GetString(r.GetOrdinal("PlanName")), PlanPrice = r.GetDecimal(r.GetOrdinal("PlanPrice")) };
    }

    public async Task UpdateSubscriptionAsync(string stripeSubId, string status, DateTime periodStart, DateTime periodEnd, bool cancelAtPeriodEnd, int? newPlanId)
    {
        var sql = "UPDATE StripeSubscriptions SET Status=@St,CurrentPeriodStart=@CPS,CurrentPeriodEnd=@CPE,CancelAtPeriodEnd=@CAP" + (newPlanId.HasValue ? ",PlanId=@Pid" : "") + ",UpdatedAt=GETUTCDATE() WHERE StripeSubscriptionId=@Sid";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Sid", stripeSubId);
        cmd.Parameters.AddWithValue("@St", status);
        cmd.Parameters.AddWithValue("@CPS", periodStart);
        cmd.Parameters.AddWithValue("@CPE", periodEnd);
        cmd.Parameters.AddWithValue("@CAP", cancelAtPeriodEnd);
        if (newPlanId.HasValue) cmd.Parameters.AddWithValue("@Pid", newPlanId.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpsertInvoiceAsync(StripeInvoiceModel inv)
    {
        const string sql = @"IF EXISTS (SELECT 1 FROM StripeInvoices WHERE StripeInvoiceId=@Iid)
            UPDATE StripeInvoices SET Amount=@A,AmountPaid=@AP,Status=@St,PdfUrl=@Pdf,PaidAt=@PA WHERE StripeInvoiceId=@Iid
            ELSE INSERT INTO StripeInvoices (WorkspaceId,StripeInvoiceId,StripeCustomerId,Amount,AmountPaid,Currency,Status,PeriodStart,PeriodEnd,PdfUrl,InvoiceNumber,PaidAt,DueDate,CreatedAt) VALUES (@WsId,@Iid,@Cid,@A,@AP,@Cur,@St,@PS,@PE,@Pdf,@IN,@PA,@DD,GETUTCDATE())";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WsId", inv.WorkspaceId);
        cmd.Parameters.AddWithValue("@Iid", inv.StripeInvoiceId);
        cmd.Parameters.AddWithValue("@Cid", inv.StripeCustomerId);
        cmd.Parameters.AddWithValue("@A", inv.Amount);
        cmd.Parameters.AddWithValue("@AP", inv.AmountPaid);
        cmd.Parameters.AddWithValue("@Cur", inv.Currency);
        cmd.Parameters.AddWithValue("@St", inv.Status);
        cmd.Parameters.AddWithValue("@PS", (object?)inv.PeriodStart ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PE", (object?)inv.PeriodEnd ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Pdf", (object?)inv.PdfUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IN", (object?)inv.InvoiceNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PA", (object?)inv.PaidAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@DD", (object?)inv.DueDate ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<StripeInvoiceModel>> GetInvoicesAsync(long workspaceId, int page, int pageSize)
    {
        const string sql = "SELECT * FROM StripeInvoices WHERE WorkspaceId = @WsId ORDER BY CreatedAt DESC OFFSET @Off ROWS FETCH NEXT @PS ROWS ONLY";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WsId", workspaceId);
        cmd.Parameters.AddWithValue("@Off", (page - 1) * pageSize);
        cmd.Parameters.AddWithValue("@PS", pageSize);
        var list = new List<StripeInvoiceModel>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(new StripeInvoiceModel { Id = r.GetInt64(r.GetOrdinal("Id")), StripeInvoiceId = r.GetString(r.GetOrdinal("StripeInvoiceId")), Amount = r.GetDecimal(r.GetOrdinal("Amount")), AmountPaid = r.GetDecimal(r.GetOrdinal("AmountPaid")), Currency = r.GetString(r.GetOrdinal("Currency")), Status = r.GetString(r.GetOrdinal("Status")), PdfUrl = r.IsDBNull(r.GetOrdinal("PdfUrl")) ? null : r.GetString(r.GetOrdinal("PdfUrl")), InvoiceNumber = r.IsDBNull(r.GetOrdinal("InvoiceNumber")) ? null : r.GetString(r.GetOrdinal("InvoiceNumber")), PaidAt = r.IsDBNull(r.GetOrdinal("PaidAt")) ? null : r.GetDateTime(r.GetOrdinal("PaidAt")), CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")) });
        return list;
    }

    public async Task<string?> GetStripePriceIdAsync(int planId, string billingCycle)
    {
        const string sql = "SELECT StripePriceId FROM StripePrices WHERE PlanId = @Pid AND BillingCycle = @BC AND IsActive = 1";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Pid", planId);
        cmd.Parameters.AddWithValue("@BC", billingCycle);
        return await cmd.ExecuteScalarAsync() as string;
    }

    public async Task<int?> GetPlanByStripePriceIdAsync(string stripePriceId)
    {
        const string sql = "SELECT PlanId FROM StripePrices WHERE StripePriceId = @Spid";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Spid", stripePriceId);
        var result = await cmd.ExecuteScalarAsync();
        return result != null ? (int)result : null;
    }

    public async Task<bool> WebhookEventExistsAsync(string stripeEventId)
    {
        const string sql = "SELECT COUNT(*) FROM StripeWebhookEvents WHERE StripeEventId = @Eid";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Eid", stripeEventId);
        return (int)(await cmd.ExecuteScalarAsync())! > 0;
    }

    public async Task SaveWebhookEventAsync(string stripeEventId, string eventType)
    {
        const string sql = "INSERT INTO StripeWebhookEvents (StripeEventId,EventType,CreatedAt) VALUES (@Eid,@ET,GETUTCDATE())";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Eid", stripeEventId);
        cmd.Parameters.AddWithValue("@ET", eventType);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task MarkWebhookProcessedAsync(string stripeEventId)
    {
        const string sql = "UPDATE StripeWebhookEvents SET Processed=1,ProcessedAt=GETUTCDATE() WHERE StripeEventId=@Eid";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Eid", stripeEventId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SaveWebhookErrorAsync(string stripeEventId, string error)
    {
        const string sql = "UPDATE StripeWebhookEvents SET Error=@Err WHERE StripeEventId=@Eid";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Eid", stripeEventId);
        cmd.Parameters.AddWithValue("@Err", error);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<BillingSummary> GetBillingSummaryAsync(long workspaceId)
    {
        var summary = new BillingSummary();
        summary.Subscription = await GetActiveSubscriptionAsync(workspaceId);
        summary.Invoices = await GetInvoicesAsync(workspaceId, 1, 12);
        var customer = await GetStripeCustomerAsync(workspaceId);
        summary.StripeCustomerId = customer?.StripeCustomerId;
        summary.DefaultPaymentMethod = customer?.DefaultPaymentMethod;
        return summary;
    }
}

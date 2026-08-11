using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public interface IBillingRepository
{
    // Stripe Customers
    Task<StripeCustomerModel?> GetStripeCustomerAsync(long workspaceId);
    Task UpsertStripeCustomerAsync(long workspaceId, string stripeCustomerId);
    Task<long> GetWorkspaceIdByStripeCustomerIdAsync(string stripeCustomerId);
    // Subscriptions
    Task UpsertSubscriptionAsync(StripeSubscriptionModel sub);
    Task<StripeSubscriptionModel?> GetSubscriptionByStripeIdAsync(string stripeSubId);
    Task<StripeSubscriptionModel?> GetActiveSubscriptionAsync(long workspaceId);
    Task UpdateSubscriptionAsync(string stripeSubId, string status, DateTime periodStart, DateTime periodEnd, bool cancelAtPeriodEnd, int? newPlanId);
    // Invoices
    Task UpsertInvoiceAsync(StripeInvoiceModel invoice);
    Task<List<StripeInvoiceModel>> GetInvoicesAsync(long workspaceId, int page, int pageSize);
    // Prices
    Task<string?> GetStripePriceIdAsync(int planId, string billingCycle);
    Task<int?> GetPlanByStripePriceIdAsync(string stripePriceId);
    // Webhook events
    Task<bool> WebhookEventExistsAsync(string stripeEventId);
    Task SaveWebhookEventAsync(string stripeEventId, string eventType);
    Task MarkWebhookProcessedAsync(string stripeEventId);
    Task SaveWebhookErrorAsync(string stripeEventId, string error);
    // Billing Summary
    Task<BillingSummary> GetBillingSummaryAsync(long workspaceId);
}

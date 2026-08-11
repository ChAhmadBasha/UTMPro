namespace UTMPro.Data.Models;

public class StripeCustomerModel
{
    public long Id { get; set; }
    public long WorkspaceId { get; set; }
    public string StripeCustomerId { get; set; } = string.Empty;
    public string? DefaultPaymentMethod { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class StripeSubscriptionModel
{
    public long Id { get; set; }
    public long WorkspaceId { get; set; }
    public string StripeSubscriptionId { get; set; } = string.Empty;
    public string StripeCustomerId { get; set; } = string.Empty;
    public string StripePriceId { get; set; } = string.Empty;
    public int PlanId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = "Monthly";
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public DateTime? CanceledAt { get; set; }
    public DateTime? TrialStart { get; set; }
    public DateTime? TrialEnd { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? PlanName { get; set; }
    public decimal PlanPrice { get; set; }
}

public class StripeInvoiceModel
{
    public long Id { get; set; }
    public long WorkspaceId { get; set; }
    public string StripeInvoiceId { get; set; } = string.Empty;
    public string StripeCustomerId { get; set; } = string.Empty;
    public long? SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountPaid { get; set; }
    public string Currency { get; set; } = "usd";
    public string Status { get; set; } = string.Empty;
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public string? PdfUrl { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BillingSummary
{
    public StripeSubscriptionModel? Subscription { get; set; }
    public List<StripeInvoiceModel> Invoices { get; set; } = new();
    public string? StripeCustomerId { get; set; }
    public string? DefaultPaymentMethod { get; set; }
    public Plan CurrentPlan { get; set; } = new();
    public int LinksUsedThisMonth { get; set; }
    public int EventsUsedThisMonth { get; set; }
    public DateTime UsageResetDate { get; set; }
    public bool IsTrialing => Subscription?.Status == "trialing";
    public bool IsCanceled => Subscription?.CancelAtPeriodEnd == true;
    public bool IsActive => Subscription?.Status is "active" or "trialing";
}

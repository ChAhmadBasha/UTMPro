# PART 4: PHASE 2 C# MODELS

```csharp
// ============================================================
// File: UTMPro.Data/Models/Phase2/PartnerProgram.cs
// ============================================================
namespace UTMPro.Data.Models;

public class PartnerProgram
{
    public long Id { get; set; }
    public long WorkspaceId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string BrandColor { get; set; } = "#000000";
    public string? Description { get; set; }
    public string CommissionType { get; set; } = "Percentage";
    // Values: 'Percentage' | 'FlatRate'
    public decimal CommissionValue { get; set; } = 20;
    public string CommissionDuration { get; set; } = "Lifetime";
    // Values: 'OneTime' | 'Recurring' | 'Lifetime'
    public int? CommissionDurationMonths { get; set; }
    public decimal PayoutThreshold { get; set; } = 50;
    public string PayoutFrequency { get; set; } = "Monthly";
    public string PayoutMethod { get; set; } = "Stripe";
    public int CookieDays { get; set; } = 90;
    public bool RequireApplication { get; set; }
    public bool AutoApprove { get; set; } = true;
    public string? ApplicationQuestions { get; set; }
    public string? TermsUrl { get; set; }
    public string? TermsText { get; set; }
    public bool IsPublic { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int TotalPartners { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalPayouts { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // Navigation
    public int ActivePartnerCount { get; set; }
    public int PendingApplications { get; set; }
    public string? WorkspaceName { get; set; }
}

// ============================================================
// File: UTMPro.Data/Models/Phase2/Partner.cs
// ============================================================
public class Partner
{
    public long Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public long ProgramId { get; set; }
    public long WorkspaceId { get; set; }
    public long? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Country { get; set; }
    public string? CountryCode { get; set; }
    public string ReferralCode { get; set; } = string.Empty;
    public string ReferralUrl { get; set; } = string.Empty;
    public string ApplicationStatus { get; set; } = "Approved";
    // Values: 'Pending'|'Approved'|'Rejected'|'Suspended'
    public string? ApplicationData { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public long? ApprovedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? PayoutMethod { get; set; }
    public string? StripeAccountId { get; set; }
    public string? PayPalEmail { get; set; }
    public long TotalClicks { get; set; }
    public int TotalLeads { get; set; }
    public int TotalSales { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal PendingBalance { get; set; }
    public int FraudScore { get; set; }
    public bool IsFlagged { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // Navigation
    public string? ProgramName { get; set; }
    public string? WorkspaceName { get; set; }
}

// ============================================================
// File: UTMPro.Data/Models/Phase2/PartnerSale.cs
// ============================================================
public class PartnerSale
{
    public long Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public long PartnerId { get; set; }
    public long ProgramId { get; set; }
    public long WorkspaceId { get; set; }
    public string? CustomerEmail { get; set; }
    public long? CustomerId { get; set; }
    public decimal SaleAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string CommissionType { get; set; } = string.Empty;
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public string Status { get; set; } = "Pending";
    // Values: 'Pending'|'Approved'|'Paid'|'Reversed'|'Fraud'
    public string? ReferralCode { get; set; }
    public long? ClickId { get; set; }
    public string? StripeChargeId { get; set; }
    public string? StripePayoutId { get; set; }
    public string? ExternalOrderId { get; set; }
    public DateTime SaleDate { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ReversedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    // Navigation
    public string? PartnerName { get; set; }
    public string? PartnerEmail { get; set; }
}

// ============================================================
// File: UTMPro.Data/Models/Phase2/PartnerPayout.cs
// ============================================================
public class PartnerPayout
{
    public long Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public long PartnerId { get; set; }
    public long ProgramId { get; set; }
    public long WorkspaceId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PayoutMethod { get; set; } = string.Empty;
    public string? StripeTransferId { get; set; }
    public string? StripePayoutStatus { get; set; }
    public string Status { get; set; } = "Pending";
    // Values: 'Pending'|'Processing'|'Paid'|'Failed'|'Cancelled'
    public string? FailureReason { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public string? SaleIds { get; set; }
    public string? Notes { get; set; }
    public long? ProcessedBy { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // Navigation
    public string? PartnerName { get; set; }
    public string? PartnerEmail { get; set; }
    public string? ProcessedByName { get; set; }
}

// ============================================================
// File: UTMPro.Data/Models/Phase2/StripeModels.cs
// ============================================================
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
    // Navigation
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

// ============================================================
// File: UTMPro.Data/Models/Phase2/BillingSummary.cs
// ============================================================
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
    public bool IsTrialing => 
        Subscription?.Status == "trialing";
    public bool IsCanceled => 
        Subscription?.CancelAtPeriodEnd == true;
    public bool IsActive => 
        Subscription?.Status is "active" or "trialing";
}
```

---

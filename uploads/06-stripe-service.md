# PART 6: STRIPE SERVICE

```csharp
// ============================================================
// File: UTMPro.Web/Services/Phase2/StripeService.cs
// ============================================================
using Stripe;

namespace UTMPro.Web.Services;

public interface IStripeService
{
    Task<string> CreateCustomerAsync(
        long workspaceId, string email, string name);
    Task<string> CreateCheckoutSessionAsync(
        long workspaceId, int planId, 
        string billingCycle, string successUrl, 
        string cancelUrl);
    Task<string> CreateBillingPortalSessionAsync(
        long workspaceId, string returnUrl);
    Task HandleWebhookAsync(string payload, string signature);
    Task<bool> CancelSubscriptionAsync(long workspaceId);
    Task<bool> ResumeSubscriptionAsync(long workspaceId);
    Task<BillingSummary> GetBillingSummaryAsync(
        long workspaceId);
}

public class StripeService : IStripeService
{
    private readonly IDbConnectionFactory _db;
    private readonly IWorkspaceRepository _wsRepo;
    private readonly IBillingRepository _billingRepo;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;
    private readonly ILogger<StripeService> _logger;

    public StripeService(
        IDbConnectionFactory db,
        IWorkspaceRepository wsRepo,
        IBillingRepository billingRepo,
        IEmailService emailService,
        IConfiguration config,
        ILogger<StripeService> logger)
    {
        _db = db;
        _wsRepo = wsRepo;
        _billingRepo = billingRepo;
        _emailService = emailService;
        _config = config;
        _logger = logger;

        StripeConfiguration.ApiKey = config[
            "Stripe:SecretKey"];
    }

    public async Task<string> CreateCustomerAsync(
        long workspaceId, string email, string name)
    {
        var service = new CustomerService();
        var customer = await service.CreateAsync(
            new CustomerCreateOptions
            {
                Email = email,
                Name = name,
                Metadata = new Dictionary<string, string>
                {
                    ["workspace_id"] = workspaceId.ToString()
                }
            });

        await _billingRepo.UpsertStripeCustomerAsync(
            workspaceId, customer.Id);

        return customer.Id;
    }

    public async Task<string> CreateCheckoutSessionAsync(
        long workspaceId, int planId,
        string billingCycle, string successUrl,
        string cancelUrl)
    {
        // Get Stripe price ID
        var priceId = await _billingRepo
            .GetStripePriceIdAsync(planId, billingCycle);
        if (priceId == null)
            throw new Exception(
                $"No Stripe price for plan {planId} " +
                $"{billingCycle}");

        // Get or create Stripe customer
        var customer = await _billingRepo
            .GetStripeCustomerAsync(workspaceId);
        string? customerId = customer?.StripeCustomerId;

        if (customerId == null)
        {
            var ws = await _wsRepo.GetByIdAsync(workspaceId);
            customerId = await CreateCustomerAsync(
                workspaceId, ws!.BillingEmail ?? "",
                ws.Name);
        }

        var trialDays = int.Parse(
            _config["Stripe:TrialDays"] ?? "0");

        var options = new SessionCreateOptions
        {
            Customer = customerId,
            Mode = "subscription",
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Price = priceId,
                    Quantity = 1
                }
            },
            SuccessUrl = successUrl + 
                "?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = cancelUrl,
            SubscriptionData = trialDays > 0
                ? new SessionSubscriptionDataOptions
                  {
                      TrialPeriodDays = trialDays
                  }
                : null,
            Metadata = new Dictionary<string, string>
            {
                ["workspace_id"] = workspaceId.ToString(),
                ["plan_id"] = planId.ToString()
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);
        return session.Url;
    }

    public async Task<string> CreateBillingPortalSessionAsync(
        long workspaceId, string returnUrl)
    {
        var customer = await _billingRepo
            .GetStripeCustomerAsync(workspaceId);
        if (customer == null)
            throw new Exception("No Stripe customer found");

        var service = new BillingPortalSessionService();
        var session = await service.CreateAsync(
            new BillingPortalSessionCreateOptions
            {
                Customer = customer.StripeCustomerId,
                ReturnUrl = returnUrl
            });

        return session.Url;
    }

    public async Task HandleWebhookAsync(
        string payload, string signature)
    {
        var webhookSecret = _config["Stripe:WebhookSecret"]!;
        Event stripeEvent;

        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                payload, signature, webhookSecret);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Stripe webhook signature invalid: {msg}", 
                ex.Message);
            throw;
        }

        // Idempotency check
        var alreadyProcessed = await _billingRepo
            .WebhookEventExistsAsync(stripeEvent.Id);
        if (alreadyProcessed) return;

        await _billingRepo.SaveWebhookEventAsync(
            stripeEvent.Id, stripeEvent.Type);

        try
        {
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutCompletedAsync(
                        stripeEvent);
                    break;

                case "customer.subscription.updated":
                    await HandleSubscriptionUpdatedAsync(
                        stripeEvent);
                    break;

                case "customer.subscription.deleted":
                    await HandleSubscriptionCanceledAsync(
                        stripeEvent);
                    break;

                case "invoice.payment_succeeded":
                    await HandleInvoicePaidAsync(stripeEvent);
                    break;

                case "invoice.payment_failed":
                    await HandleInvoiceFailedAsync(stripeEvent);
                    break;

                case "customer.subscription.trial_will_end":
                    await HandleTrialEndingAsync(stripeEvent);
                    break;

                default:
                    _logger.LogDebug(
                        "Unhandled Stripe event: {type}", 
                        stripeEvent.Type);
                    break;
            }

            await _billingRepo.MarkWebhookProcessedAsync(
                stripeEvent.Id);
        }
        catch (Exception ex)
        {
            await _billingRepo.SaveWebhookErrorAsync(
                stripeEvent.Id, ex.Message);
            throw;
        }
    }

    private async Task HandleCheckoutCompletedAsync(
        Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session == null) return;

        var workspaceId = long.Parse(
            session.Metadata["workspace_id"]);
        var planId = int.Parse(
            session.Metadata["plan_id"]);

        // Get subscription from Stripe
        var subService = new SubscriptionService();
        var sub = await subService.GetAsync(session.SubscriptionId);

        // Save subscription
        await _billingRepo.UpsertSubscriptionAsync(
            new StripeSubscriptionModel
            {
                WorkspaceId = workspaceId,
                StripeSubscriptionId = sub.Id,
                StripeCustomerId = sub.CustomerId,
                StripePriceId = sub.Items.Data[0].Price.Id,
                PlanId = planId,
                Status = sub.Status,
                CurrentPeriodStart = sub.CurrentPeriodStart,
                CurrentPeriodEnd = sub.CurrentPeriodEnd,
                TrialStart = sub.TrialStart,
                TrialEnd = sub.TrialEnd
            });

        // Update workspace plan
        await _wsRepo.UpdatePlanAsync(
            workspaceId, planId,
            sub.CurrentPeriodStart,
            sub.CurrentPeriodEnd);

        _logger.LogInformation(
            "Workspace {id} upgraded to plan {planId}",
            workspaceId, planId);
    }

    private async Task HandleSubscriptionUpdatedAsync(
        Event stripeEvent)
    {
        var sub = stripeEvent.Data.Object as Subscription;
        if (sub == null) return;

        var existing = await _billingRepo
            .GetSubscriptionByStripeIdAsync(sub.Id);
        if (existing == null) return;

        // Update plan if price changed
        var newPlanId = await _billingRepo
            .GetPlanByStripePriceIdAsync(
                sub.Items.Data[0].Price.Id);

        await _billingRepo.UpdateSubscriptionAsync(
            sub.Id, sub.Status,
            sub.CurrentPeriodStart, sub.CurrentPeriodEnd,
            sub.CancelAtPeriodEnd, newPlanId);

        if (newPlanId.HasValue && 
            newPlanId != existing.PlanId)
        {
            await _wsRepo.UpdatePlanAsync(
                existing.WorkspaceId, newPlanId.Value,
                sub.CurrentPeriodStart, sub.CurrentPeriodEnd);
        }
    }

    private async Task HandleSubscriptionCanceledAsync(
        Event stripeEvent)
    {
        var sub = stripeEvent.Data.Object as Subscription;
        if (sub == null) return;

        var existing = await _billingRepo
            .GetSubscriptionByStripeIdAsync(sub.Id);
        if (existing == null) return;

        await _billingRepo.UpdateSubscriptionAsync(
            sub.Id, "canceled",
            sub.CurrentPeriodStart, sub.CurrentPeriodEnd,
            false, null);

        // Downgrade to free plan
        await _wsRepo.UpdatePlanAsync(
            existing.WorkspaceId, 1,
            DateTime.UtcNow, null);

        // Send cancellation email
        var ws = await _wsRepo.GetByIdAsync(
            existing.WorkspaceId);
        if (ws != null)
            await _emailService.SendSubscriptionCanceledAsync(
                ws.BillingEmail ?? "", ws.Name);
    }

    private async Task HandleInvoicePaidAsync(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null) return;

        await _billingRepo.UpsertInvoiceAsync(
            new StripeInvoiceModel
            {
                WorkspaceId = await GetWorkspaceByCustomerIdAsync(
                    invoice.CustomerId),
                StripeInvoiceId = invoice.Id,
                StripeCustomerId = invoice.CustomerId,
                Amount = invoice.AmountDue / 100m,
                AmountPaid = invoice.AmountPaid / 100m,
                Currency = invoice.Currency,
                Status = invoice.Status,
                PeriodStart = invoice.PeriodStart,
                PeriodEnd = invoice.PeriodEnd,
                PdfUrl = invoice.InvoicePdf,
                InvoiceNumber = invoice.Number,
                PaidAt = invoice.StatusTransitions.PaidAt,
                DueDate = invoice.DueDate
            });
    }

    private async Task HandleInvoiceFailedAsync(
        Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null) return;

        var workspaceId = await GetWorkspaceByCustomerIdAsync(
            invoice.CustomerId);
        var ws = await _wsRepo.GetByIdAsync(workspaceId);

        if (ws != null)
            await _emailService.SendPaymentFailedAsync(
                ws.BillingEmail ?? "", ws.Name,
                invoice.AmountDue / 100m,
                invoice.Currency.ToUpper());
    }

    private async Task HandleTrialEndingAsync(Event stripeEvent)
    {
        var sub = stripeEvent.Data.Object as Subscription;
        if (sub == null) return;

        var existing = await _billingRepo
            .GetSubscriptionByStripeIdAsync(sub.Id);
        if (existing == null) return;

        var ws = await _wsRepo.GetByIdAsync(existing.WorkspaceId);
        if (ws != null)
            await _emailService.SendTrialEndingAsync(
                ws.BillingEmail ?? "", ws.Name,
                sub.TrialEnd ?? DateTime.UtcNow.AddDays(3));
    }

    private async Task<long> GetWorkspaceByCustomerIdAsync(
        string stripeCustomerId)
    {
        return await _billingRepo
            .GetWorkspaceIdByStripeCustomerIdAsync(
                stripeCustomerId);
    }
}
```

---

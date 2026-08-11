using Stripe;
using Stripe.Checkout;
using Stripe.BillingPortal;
using UTMPro.Data;
using UTMPro.Data.Models;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Services;

public interface IStripeService
{
    Task<string> CreateCustomerAsync(long workspaceId, string email, string name);
    Task<string> CreateCheckoutSessionAsync(long workspaceId, int planId, string billingCycle, string successUrl, string cancelUrl);
    Task<string> CreateBillingPortalSessionAsync(long workspaceId, string returnUrl);
    Task HandleWebhookAsync(string payload, string signature);
    Task<BillingSummary> GetBillingSummaryAsync(long workspaceId);
}

public class StripeService : IStripeService
{
    private readonly IDbConnectionFactory _db;
    private readonly IWorkspaceRepository _wsRepo;
    private readonly IBillingRepository _billingRepo;
    private readonly IPlanRepository _planRepo;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;
    private readonly ILogger<StripeService> _logger;

    public StripeService(UTMPro.Data.IDbConnectionFactory db, IWorkspaceRepository wsRepo,
        IBillingRepository billingRepo, IPlanRepository planRepo, IEmailService emailService,
        IConfiguration config, ILogger<StripeService> logger)
    {
        _db = db; _wsRepo = wsRepo; _billingRepo = billingRepo; _planRepo = planRepo;
        _emailService = emailService; _config = config; _logger = logger;
        StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
    }

    public async Task<string> CreateCustomerAsync(long workspaceId, string email, string name)
    {
        var service = new CustomerService();
        var customer = await service.CreateAsync(new CustomerCreateOptions
        {
            Email = email, Name = name,
            Metadata = new Dictionary<string, string> { ["workspace_id"] = workspaceId.ToString() }
        });
        await _billingRepo.UpsertStripeCustomerAsync(workspaceId, customer.Id);
        return customer.Id;
    }

    public async Task<string> CreateCheckoutSessionAsync(long workspaceId, int planId, string billingCycle, string successUrl, string cancelUrl)
    {
        var priceId = await _billingRepo.GetStripePriceIdAsync(planId, billingCycle);
        if (priceId == null) throw new Exception($"No Stripe price for plan {planId} {billingCycle}");

        var customer = await _billingRepo.GetStripeCustomerAsync(workspaceId);
        string? customerId = customer?.StripeCustomerId;

        if (customerId == null)
        {
            var ws = await _wsRepo.GetByIdAsync(workspaceId);
            customerId = await CreateCustomerAsync(workspaceId, ws!.OwnerEmail ?? "", ws.Name);
        }

        var trialDays = int.Parse(_config["Stripe:TrialDays"] ?? "0");

        var options = new Stripe.Checkout.SessionCreateOptions
        {
            Customer = customerId,
            Mode = "subscription",
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions> { new() { Price = priceId, Quantity = 1 } },
            SuccessUrl = successUrl + "?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                ["workspace_id"] = workspaceId.ToString(),
                ["plan_id"] = planId.ToString()
            }
        };

        if (trialDays > 0)
            options.SubscriptionData = new SessionSubscriptionDataOptions { TrialPeriodDays = trialDays };

        var service = new Stripe.Checkout.SessionService();
        var session = await service.CreateAsync(options);
        return session.Url!;
    }

    public async Task<string> CreateBillingPortalSessionAsync(long workspaceId, string returnUrl)
    {
        var customer = await _billingRepo.GetStripeCustomerAsync(workspaceId);
        if (customer == null) throw new Exception("No Stripe customer found");

        var service = new Stripe.BillingPortal.SessionService();
        var session = await service.CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = customer.StripeCustomerId,
            ReturnUrl = returnUrl
        });
        return session.Url;
    }

    public async Task HandleWebhookAsync(string payload, string signature)
    {
        var webhookSecret = _config["Stripe:WebhookSecret"]!;
        Event stripeEvent;
        try { stripeEvent = EventUtility.ConstructEvent(payload, signature, webhookSecret); }
        catch (Exception ex) { _logger.LogWarning("Stripe webhook signature invalid: {msg}", ex.Message); throw; }

        if (await _billingRepo.WebhookEventExistsAsync(stripeEvent.Id)) return;
        await _billingRepo.SaveWebhookEventAsync(stripeEvent.Id, stripeEvent.Type);

        try
        {
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutCompletedAsync(stripeEvent); break;
                case "customer.subscription.updated":
                    await HandleSubscriptionUpdatedAsync(stripeEvent); break;
                case "customer.subscription.deleted":
                    await HandleSubscriptionCanceledAsync(stripeEvent); break;
                case "invoice.payment_succeeded":
                    await HandleInvoicePaidAsync(stripeEvent); break;
                case "invoice.payment_failed":
                    await HandleInvoiceFailedAsync(stripeEvent); break;
                default:
                    _logger.LogDebug("Unhandled Stripe event: {type}", stripeEvent.Type); break;
            }
            await _billingRepo.MarkWebhookProcessedAsync(stripeEvent.Id);
        }
        catch (Exception ex)
        {
            await _billingRepo.SaveWebhookErrorAsync(stripeEvent.Id, ex.Message);
            throw;
        }
    }

    private async Task HandleCheckoutCompletedAsync(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
        if (session == null) return;
        var workspaceId = long.Parse(session.Metadata["workspace_id"]);
        var planId = int.Parse(session.Metadata["plan_id"]);

        var subService = new SubscriptionService();
        var sub = await subService.GetAsync(session.SubscriptionId);

        await _billingRepo.UpsertSubscriptionAsync(new StripeSubscriptionModel
        {
            WorkspaceId = workspaceId, StripeSubscriptionId = sub.Id, StripeCustomerId = sub.CustomerId,
            StripePriceId = sub.Items.Data[0].Price.Id, PlanId = planId, Status = sub.Status,
            CurrentPeriodStart = sub.CurrentPeriodStart, CurrentPeriodEnd = sub.CurrentPeriodEnd,
            TrialStart = sub.TrialStart, TrialEnd = sub.TrialEnd
        });

        await _wsRepo.AssignPlanAsync(workspaceId, planId, sub.CurrentPeriodStart, sub.CurrentPeriodEnd, "Stripe checkout", 0);
        _logger.LogInformation("Workspace {id} upgraded to plan {planId} via Stripe", workspaceId, planId);
    }

    private async Task HandleSubscriptionUpdatedAsync(Event stripeEvent)
    {
        var sub = stripeEvent.Data.Object as Subscription;
        if (sub == null) return;
        var existing = await _billingRepo.GetSubscriptionByStripeIdAsync(sub.Id);
        if (existing == null) return;

        var newPlanId = await _billingRepo.GetPlanByStripePriceIdAsync(sub.Items.Data[0].Price.Id);
        await _billingRepo.UpdateSubscriptionAsync(sub.Id, sub.Status, sub.CurrentPeriodStart, sub.CurrentPeriodEnd, sub.CancelAtPeriodEnd, newPlanId);

        if (newPlanId.HasValue && newPlanId != existing.PlanId)
            await _wsRepo.AssignPlanAsync(existing.WorkspaceId, newPlanId.Value, sub.CurrentPeriodStart, sub.CurrentPeriodEnd, "Stripe update", 0);
    }

    private async Task HandleSubscriptionCanceledAsync(Event stripeEvent)
    {
        var sub = stripeEvent.Data.Object as Subscription;
        if (sub == null) return;
        var existing = await _billingRepo.GetSubscriptionByStripeIdAsync(sub.Id);
        if (existing == null) return;

        await _billingRepo.UpdateSubscriptionAsync(sub.Id, "canceled", sub.CurrentPeriodStart, sub.CurrentPeriodEnd, false, null);
        await _wsRepo.AssignPlanAsync(existing.WorkspaceId, 1, DateTime.UtcNow, null, "Subscription canceled", 0);
        _logger.LogInformation("Workspace {id} downgraded to Free (subscription canceled)", existing.WorkspaceId);
    }

    private async Task HandleInvoicePaidAsync(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null) return;
        var wsId = await _billingRepo.GetWorkspaceIdByStripeCustomerIdAsync(invoice.CustomerId);
        if (wsId == 0) return;

        await _billingRepo.UpsertInvoiceAsync(new StripeInvoiceModel
        {
            WorkspaceId = wsId, StripeInvoiceId = invoice.Id, StripeCustomerId = invoice.CustomerId,
            Amount = invoice.AmountDue / 100m, AmountPaid = invoice.AmountPaid / 100m,
            Currency = invoice.Currency ?? "usd", Status = invoice.Status ?? "paid",
            PeriodStart = invoice.PeriodStart, PeriodEnd = invoice.PeriodEnd,
            PdfUrl = invoice.InvoicePdf, InvoiceNumber = invoice.Number,
            PaidAt = invoice.StatusTransitions?.PaidAt, DueDate = invoice.DueDate
        });
    }

    private async Task HandleInvoiceFailedAsync(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null) return;
        _logger.LogWarning("Payment failed for invoice {id}", invoice.Id);
    }

    public async Task<BillingSummary> GetBillingSummaryAsync(long workspaceId)
    {
        var summary = await _billingRepo.GetBillingSummaryAsync(workspaceId);
        var plan = await _planRepo.GetByIdAsync((await _wsRepo.GetByIdAsync(workspaceId))?.PlanId ?? 1);
        summary.CurrentPlan = plan ?? new UTMPro.Data.Models.Plan { Name = "Free" };
        var ws = await _wsRepo.GetByIdAsync(workspaceId);
        if (ws != null)
        {
            summary.LinksUsedThisMonth = ws.LinksUsedThisMonth;
            summary.EventsUsedThisMonth = ws.EventsUsedThisMonth;
            summary.UsageResetDate = ws.UsageResetDate;
        }
        return summary;
    }
}

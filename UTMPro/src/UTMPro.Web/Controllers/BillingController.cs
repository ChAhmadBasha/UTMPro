using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Repositories;
using UTMPro.Web.Services;

namespace UTMPro.Web.Controllers;

[Route("{workspaceSlug}/settings/billing")]
public class BillingController : BaseWorkspaceController
{
    private readonly IWorkspaceRepository _wsRepo;
    private readonly IStripeService _stripeService;
    private readonly IPlanRepository _planRepo;
    private readonly IBillingRepository _billingRepo;

    public BillingController(IWorkspaceRepository wsRepo, IStripeService stripeService,
        IPlanRepository planRepo, IBillingRepository billingRepo)
    {
        _wsRepo = wsRepo; _stripeService = stripeService; _planRepo = planRepo; _billingRepo = billingRepo;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string workspaceSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();

        var plan = await _planRepo.GetByIdAsync(CurrentWorkspace!.PlanId);
        var plans = await _planRepo.GetAllActiveAsync();
        var subscription = await _billingRepo.GetActiveSubscriptionAsync(CurrentWorkspace.Id);
        var invoices = await _billingRepo.GetInvoicesAsync(CurrentWorkspace.Id, 1, 12);
        var customer = await _billingRepo.GetStripeCustomerAsync(CurrentWorkspace.Id);

        ViewBag.CurrentPlan = plan;
        ViewBag.AllPlans = plans;
        ViewBag.Subscription = subscription;
        ViewBag.Invoices = invoices;
        ViewBag.HasStripeCustomer = customer != null;
        ViewBag.Workspace = CurrentWorkspace;

        return View("~/Views/Settings/BillingPage.cshtml");
    }

    [HttpGet("upgrade")]
    public async Task<IActionResult> Upgrade(string workspaceSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        var plans = await _planRepo.GetAllActiveAsync();
        var currentPlan = await _planRepo.GetByIdAsync(CurrentWorkspace!.PlanId);
        ViewBag.CurrentPlan = currentPlan;
        return View("~/Views/Settings/Upgrade.cshtml", plans);
    }

    [HttpPost("checkout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(string workspaceSlug, int planId, string billingCycle = "Monthly")
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!IsOwner()) return Forbidden();

        try
        {
            var successUrl = $"{Request.Scheme}://{Request.Host}/{workspaceSlug}/settings/billing/success";
            var cancelUrl = $"{Request.Scheme}://{Request.Host}/{workspaceSlug}/settings/billing";
            var url = await _stripeService.CreateCheckoutSessionAsync(CurrentWorkspace!.Id, planId, billingCycle, successUrl, cancelUrl);
            return Redirect(url);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Payment setup failed: {ex.Message}";
            return Redirect($"/{workspaceSlug}/settings/billing");
        }
    }

    [HttpGet("portal")]
    public async Task<IActionResult> Portal(string workspaceSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!IsOwner()) return Forbidden();

        try
        {
            var returnUrl = $"{Request.Scheme}://{Request.Host}/{workspaceSlug}/settings/billing";
            var url = await _stripeService.CreateBillingPortalSessionAsync(CurrentWorkspace!.Id, returnUrl);
            return Redirect(url);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not open billing portal: {ex.Message}";
            return Redirect($"/{workspaceSlug}/settings/billing");
        }
    }

    [HttpGet("success")]
    public async Task<IActionResult> Success(string workspaceSlug, string? session_id)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        TempData["Success"] = "🎉 Your plan has been upgraded! Changes will take effect shortly.";
        return Redirect($"/{workspaceSlug}/settings/billing");
    }
}

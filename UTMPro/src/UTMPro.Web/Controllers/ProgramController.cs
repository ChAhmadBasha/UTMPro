using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Models;
using UTMPro.Data.Repositories;
using UTMPro.Web.Services;

namespace UTMPro.Web.Controllers;

[Route("{workspaceSlug}/program")]
public class ProgramController : BaseWorkspaceController
{
    private readonly IPartnerRepository _partnerRepo;
    private readonly IWorkspaceRepository _wsRepo;
    private readonly IPartnerService _partnerService;

    public ProgramController(IPartnerRepository partnerRepo, IWorkspaceRepository wsRepo, IPartnerService partnerService)
    {
        _partnerRepo = partnerRepo; _wsRepo = wsRepo; _partnerService = partnerService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string workspaceSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        var program = await _partnerRepo.GetProgramByWorkspaceAsync(CurrentWorkspace!.Id);
        if (program == null) return Redirect($"/{workspaceSlug}/program/setup");
        return View("~/Views/Program/Index.cshtml", program);
    }

    [HttpGet("setup")]
    public async Task<IActionResult> Setup(string workspaceSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanAdmin()) return Forbidden();
        return View("~/Views/Program/Setup.cshtml");
    }

    [HttpPost("setup")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProgram(string workspaceSlug, CreateProgramRequest request)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanAdmin()) return Forbidden();

        var result = await _partnerService.CreateProgramAsync(CurrentWorkspace!.Id, request);
        if (!result.Success) { TempData["Error"] = result.Error; return View("~/Views/Program/Setup.cshtml"); }

        TempData["Success"] = "Partner program created!";
        return Redirect($"/{workspaceSlug}/program");
    }

    [HttpGet("partners")]
    public async Task<IActionResult> Partners(string workspaceSlug, string? status, int page = 1)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        var program = await _partnerRepo.GetProgramByWorkspaceAsync(CurrentWorkspace!.Id);
        if (program == null) return Redirect($"/{workspaceSlug}/program/setup");

        var partners = await _partnerRepo.GetPartnersByProgramAsync(program.Id, status, page, 25);
        var total = await _partnerRepo.GetPartnerCountByProgramAsync(program.Id, status);
        ViewBag.Program = program; ViewBag.Status = status; ViewBag.TotalCount = total; ViewBag.CurrentPage = page;
        return View("~/Views/Program/Partners.cshtml", partners);
    }

    [HttpGet("partners/{id}")]
    public async Task<IActionResult> PartnerDetail(string workspaceSlug, long id)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        var partner = await _partnerRepo.GetPartnerByIdAsync(id);
        if (partner == null || partner.WorkspaceId != CurrentWorkspace!.Id) return NotFound();
        var sales = await _partnerRepo.GetSalesByPartnerAsync(id, 1, 10);
        var payouts = await _partnerRepo.GetPayoutsByPartnerAsync(id, 1, 10);
        ViewBag.Sales = sales; ViewBag.Payouts = payouts;
        return View("~/Views/Program/PartnerDetail.cshtml", partner);
    }

    [HttpPost("partners/{id}/approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(string workspaceSlug, long id)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanAdmin()) return Forbidden();
        await _partnerService.ApprovePartnerAsync(id, CurrentUserId);
        TempData["Success"] = "Partner approved";
        return Redirect($"/{workspaceSlug}/program/partners/{id}");
    }

    [HttpPost("partners/{id}/reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(string workspaceSlug, long id, string reason)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanAdmin()) return Forbidden();
        await _partnerService.RejectPartnerAsync(id, reason, CurrentUserId);
        TempData["Success"] = "Partner rejected";
        return Redirect($"/{workspaceSlug}/program/partners");
    }

    [HttpPost("partners/{id}/suspend")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspend(string workspaceSlug, long id)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanAdmin()) return Forbidden();
        var partner = await _partnerRepo.GetPartnerByIdAsync(id);
        if (partner != null) { partner.ApplicationStatus = "Suspended"; partner.IsActive = false; await _partnerRepo.UpdatePartnerAsync(partner); }
        TempData["Success"] = "Partner suspended";
        return Redirect($"/{workspaceSlug}/program/partners");
    }

    [HttpGet("sales")]
    public async Task<IActionResult> Sales(string workspaceSlug, string? status, int page = 1)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        var program = await _partnerRepo.GetProgramByWorkspaceAsync(CurrentWorkspace!.Id);
        if (program == null) return NotFound();
        var sales = await _partnerRepo.GetSalesByProgramAsync(program.Id, status, page, 25);
        ViewBag.Program = program; ViewBag.Status = status;
        return View("~/Views/Program/Sales.cshtml", sales);
    }

    [HttpPost("sales/{id}/approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveSale(string workspaceSlug, long id)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanAdmin()) return Forbidden();
        await _partnerRepo.UpdateSaleStatusAsync(id, "Approved");
        TempData["Success"] = "Sale approved";
        return Redirect($"/{workspaceSlug}/program/sales");
    }

    [HttpGet("payouts")]
    public async Task<IActionResult> Payouts(string workspaceSlug, int page = 1)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        var program = await _partnerRepo.GetProgramByWorkspaceAsync(CurrentWorkspace!.Id);
        if (program == null) return NotFound();
        var payouts = await _partnerRepo.GetPayoutsByProgramAsync(program.Id, page, 25);
        ViewBag.Program = program;
        return View("~/Views/Program/Payouts.cshtml", payouts);
    }

    [HttpPost("payouts/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePayout(string workspaceSlug, long partnerId, decimal amount)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanAdmin()) return Forbidden();
        var program = await _partnerRepo.GetProgramByWorkspaceAsync(CurrentWorkspace!.Id);
        if (program == null) return NotFound();
        var result = await _partnerService.CreatePayoutAsync(partnerId, program.Id, CurrentWorkspace!.Id, amount, "Stripe", CurrentUserId);
        if (!result.Success) TempData["Error"] = result.Error;
        else TempData["Success"] = $"Payout of ${amount} created";
        return Redirect($"/{workspaceSlug}/program/payouts");
    }

    [HttpGet("bounties")]
    public async Task<IActionResult> Bounties(string workspaceSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        var program = await _partnerRepo.GetProgramByWorkspaceAsync(CurrentWorkspace!.Id);
        if (program == null) return NotFound();
        var bounties = await _partnerRepo.GetBountiesByProgramAsync(program.Id);
        ViewBag.Program = program;
        return View("~/Views/Program/Bounties.cshtml", bounties);
    }

    [HttpPost("bounties")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBounty(string workspaceSlug, string title, decimal amount, string bountyType)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanAdmin()) return Forbidden();
        var program = await _partnerRepo.GetProgramByWorkspaceAsync(CurrentWorkspace!.Id);
        if (program == null) return NotFound();
        await _partnerRepo.CreateBountyAsync(new PartnerBounty { ProgramId = program.Id, Title = title, BountyAmount = amount, BountyType = bountyType });
        TempData["Success"] = "Bounty created";
        return Redirect($"/{workspaceSlug}/program/bounties");
    }

    [HttpGet("messages")]
    public async Task<IActionResult> Messages(string workspaceSlug, int page = 1)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        var program = await _partnerRepo.GetProgramByWorkspaceAsync(CurrentWorkspace!.Id);
        if (program == null) return NotFound();
        var messages = await _partnerRepo.GetMessagesByProgramAsync(program.Id, page, 25);
        ViewBag.Program = program;
        return View("~/Views/Program/Messages.cshtml", messages);
    }

    [HttpPost("messages")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(string workspaceSlug, string subject, string body, long? partnerId)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanAdmin()) return Forbidden();
        var program = await _partnerRepo.GetProgramByWorkspaceAsync(CurrentWorkspace!.Id);
        if (program == null) return NotFound();
        await _partnerRepo.CreateMessageAsync(new PartnerMessage { ProgramId = program.Id, PartnerId = partnerId, SenderId = CurrentUserId, Subject = subject, Body = body });
        TempData["Success"] = "Message sent";
        return Redirect($"/{workspaceSlug}/program/messages");
    }

    [HttpGet("fraud")]
    public async Task<IActionResult> Fraud(string workspaceSlug, int page = 1)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        var program = await _partnerRepo.GetProgramByWorkspaceAsync(CurrentWorkspace!.Id);
        if (program == null) return NotFound();
        var events = await _partnerRepo.GetFraudEventsByProgramAsync(program.Id, page, 25);
        ViewBag.Program = program;
        return View("~/Views/Program/Fraud.cshtml", events);
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> Analytics(string workspaceSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        var program = await _partnerRepo.GetProgramByWorkspaceAsync(CurrentWorkspace!.Id);
        if (program == null) return NotFound();
        ViewBag.Program = program;
        return View("~/Views/Program/Analytics.cshtml", program);
    }
}

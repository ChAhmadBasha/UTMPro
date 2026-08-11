using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Models;
using UTMPro.Data.Repositories;
using UTMPro.Web.Models.Requests;
using UTMPro.Web.Services;

namespace UTMPro.Web.Controllers.Api;

[Route("api/v1")]
[ApiController]
public class PublicApiController : ControllerBase
{
    private readonly ILinkRepository _linkRepo;
    private readonly IDomainRepository _domainRepo;
    private readonly ITagRepository _tagRepo;
    private readonly IAnalyticsRepository _analyticsRepo;
    private readonly IWorkspaceRepository _wsRepo;
    private readonly IPlanRepository _planRepo;
    private readonly ILinkService _linkService;
    private readonly IPartnerService _partnerService;
    private readonly IPartnerRepository _partnerRepo;

    public PublicApiController(ILinkRepository linkRepo, IDomainRepository domainRepo, ITagRepository tagRepo,
        IAnalyticsRepository analyticsRepo, IWorkspaceRepository wsRepo, IPlanRepository planRepo,
        ILinkService linkService, IPartnerService partnerService, IPartnerRepository partnerRepo)
    {
        _linkRepo = linkRepo; _domainRepo = domainRepo; _tagRepo = tagRepo;
        _analyticsRepo = analyticsRepo; _wsRepo = wsRepo; _planRepo = planRepo;
        _linkService = linkService; _partnerService = partnerService; _partnerRepo = partnerRepo;
    }

    // ── Links ────────────────────────────────────────────
    [HttpGet("links")]
    public async Task<IActionResult> GetLinks([FromQuery] long workspaceId, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? search = null)
    {
        var (links, total) = await _linkRepo.GetListAsync(workspaceId, search, null, null, null, false, page, pageSize, "CreatedAt", "DESC");
        return Ok(new { data = links.Select(l => new { l.Id, l.ExternalId, l.Slug, l.Domain, l.PrimaryUrl, l.TotalClicks, l.CreatedAt }), pagination = new { page, pageSize, total } });
    }

    [HttpPost("links")]
    public async Task<IActionResult> CreateLink([FromBody] CreateLinkRequest request, [FromQuery] long workspaceId, [FromQuery] long userId = 0)
    {
        var ws = await _wsRepo.GetByIdAsync(workspaceId);
        if (ws == null) return NotFound(new { error = "Workspace not found" });
        var result = await _linkService.CreateAsync(ws, userId, request);
        if (!result.Success) return BadRequest(new { error = result.Error });
        return Created($"/api/v1/links/{result.Link!.ExternalId}", new { success = true, link = new { result.Link.Id, result.Link.ExternalId, result.Link.Slug, result.Link.Domain, result.Link.PrimaryUrl } });
    }

    [HttpGet("links/{id}")]
    public async Task<IActionResult> GetLink(long id, [FromQuery] long workspaceId)
    {
        var link = await _linkRepo.GetByIdAsync(id, workspaceId);
        if (link == null) return NotFound(new { error = "Link not found" });
        return Ok(new { link.Id, link.ExternalId, link.Slug, link.Domain, link.PrimaryUrl, link.TotalClicks, link.CreatedAt, link.UTMSource, link.UTMMedium, link.UTMCampaign, link.Comments, link.RedirectMode });
    }

    [HttpPut("links/{id}")]
    public async Task<IActionResult> UpdateLink(long id, [FromBody] UpdateLinkRequest request, [FromQuery] long workspaceId)
    {
        var link = await _linkRepo.GetByIdAsync(id, workspaceId);
        if (link == null) return NotFound(new { error = "Link not found" });
        var result = await _linkService.UpdateAsync(link, request);
        if (!result.Success) return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpDelete("links/{id}")]
    public async Task<IActionResult> DeleteLink(long id, [FromQuery] long workspaceId)
    {
        var link = await _linkRepo.GetByIdAsync(id, workspaceId);
        if (link == null) return NotFound(new { error = "Link not found" });
        await _linkRepo.DeleteAsync(id);
        return Ok(new { success = true });
    }

    [HttpGet("links/{id}/analytics")]
    public async Task<IActionResult> GetLinkAnalytics(long id, [FromQuery] long workspaceId, [FromQuery] string interval = "7d")
    {
        var end = DateTime.UtcNow;
        var start = interval switch { "1h" => end.AddHours(-1), "24h" => end.AddDays(-1), "7d" => end.AddDays(-7), "30d" => end.AddDays(-30), "90d" => end.AddDays(-90), _ => end.AddDays(-7) };
        var data = await _analyticsRepo.GetSummaryAsync(workspaceId, start, end, id);
        return Ok(data);
    }

    // ── Domains ──────────────────────────────────────────
    [HttpGet("domains")]
    public async Task<IActionResult> GetDomains([FromQuery] long workspaceId)
    {
        var domains = await _domainRepo.GetByWorkspaceIdAsync(workspaceId);
        return Ok(new { data = domains.Select(d => new { d.Id, d.DomainName, d.IsSystemDomain, d.IsVerified, d.ClickCount }) });
    }

    [HttpPost("domains")]
    public async Task<IActionResult> AddDomain([FromBody] AddDomainApiRequest request, [FromQuery] long workspaceId)
    {
        var id = await _domainRepo.CreateAsync(new Domain { WorkspaceId = workspaceId, DomainName = request.Domain.ToLower().Trim(), IsSystemDomain = false, IsVerified = false });
        return Created($"/api/v1/domains", new { success = true, id });
    }

    // ── Tags ─────────────────────────────────────────────
    [HttpGet("tags")]
    public async Task<IActionResult> GetTags([FromQuery] long workspaceId)
    {
        var tags = await _tagRepo.GetByWorkspaceIdAsync(workspaceId);
        return Ok(new { data = tags.Select(t => new { t.Id, t.Name, t.Color, t.LinkCount }) });
    }

    [HttpPost("tags")]
    public async Task<IActionResult> CreateTag([FromBody] CreateTagApiRequest request, [FromQuery] long workspaceId)
    {
        var id = await _tagRepo.CreateAsync(new Tag { WorkspaceId = workspaceId, Name = request.Name, Color = request.Color ?? "#22c55e" });
        return Created($"/api/v1/tags", new { success = true, id });
    }

    // ── Analytics ────────────────────────────────────────
    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics([FromQuery] long workspaceId, [FromQuery] string interval = "24h")
    {
        var end = DateTime.UtcNow;
        var start = interval switch { "1h" => end.AddHours(-1), "24h" => end.AddDays(-1), "7d" => end.AddDays(-7), "30d" => end.AddDays(-30), _ => end.AddDays(-1) };
        var data = await _analyticsRepo.GetSummaryAsync(workspaceId, start, end);
        return Ok(data);
    }

    // ── Events ───────────────────────────────────────────
    [HttpPost("events/lead")]
    public async Task<IActionResult> TrackLead([FromBody] TrackLeadApiRequest request, [FromQuery] long workspaceId)
    {
        // Would create a lead event - simplified
        return Ok(new { success = true, message = "Lead tracked" });
    }

    [HttpPost("events/sale")]
    public async Task<IActionResult> TrackSale([FromBody] TrackSaleApiRequest request, [FromQuery] long workspaceId)
    {
        // If referral code provided, attribute to partner
        if (!string.IsNullOrEmpty(request.ReferralCode))
        {
            var result = await _partnerService.RecordSaleAsync(new RecordSaleRequest
            {
                ReferralCode = request.ReferralCode, SaleAmount = request.Amount,
                Currency = request.Currency ?? "USD", CustomerEmail = request.CustomerEmail,
                ExternalOrderId = request.ExternalOrderId, StripeChargeId = request.StripeChargeId
            });
            if (!result.Success) return BadRequest(new { error = result.Error });
            return Ok(new { success = true, partnerSale = new { result.Data!.Id, result.Data.CommissionAmount } });
        }
        return Ok(new { success = true, message = "Sale tracked" });
    }

    // ── Workspace ────────────────────────────────────────
    [HttpGet("workspace")]
    public async Task<IActionResult> GetWorkspace([FromQuery] long workspaceId)
    {
        var ws = await _wsRepo.GetByIdAsync(workspaceId);
        if (ws == null) return NotFound(new { error = "Workspace not found" });
        return Ok(new { ws.Id, ws.ExternalId, ws.Name, ws.Slug, ws.PlanName, ws.LinksUsedThisMonth, ws.EventsUsedThisMonth, ws.MemberCount, ws.LinkCount });
    }

    [HttpGet("qr/{linkId}")]
    public async Task<IActionResult> GetQrData(long linkId, [FromQuery] long workspaceId)
    {
        var link = await _linkRepo.GetByIdAsync(linkId, workspaceId);
        if (link == null) return NotFound(new { error = "Link not found" });
        return Ok(new { url = $"https://{link.Domain}/{link.Slug}", linkId = link.Id, slug = link.Slug, domain = link.Domain });
    }

    // ── Partner API ──────────────────────────────────────
    [HttpPost("partner/sales")]
    public async Task<IActionResult> RecordPartnerSale([FromBody] RecordSaleRequest request)
    {
        var result = await _partnerService.RecordSaleAsync(request);
        if (!result.Success) return BadRequest(new { error = result.Error });
        return Ok(new { success = true, sale = new { result.Data!.Id, result.Data.ExternalId, result.Data.CommissionAmount, result.Data.Status } });
    }

    [HttpGet("partner/program")]
    public async Task<IActionResult> GetPartnerProgram([FromQuery] long workspaceId)
    {
        var program = await _partnerRepo.GetProgramByWorkspaceAsync(workspaceId);
        if (program == null) return NotFound(new { error = "No partner program" });
        return Ok(new { program.Id, program.ProgramName, program.Slug, program.CommissionType, program.CommissionValue, program.CommissionDuration, program.CookieDays, program.TotalPartners });
    }
}

// API Request DTOs
public class AddDomainApiRequest { public string Domain { get; set; } = string.Empty; }
public class CreateTagApiRequest { public string Name { get; set; } = string.Empty; public string? Color { get; set; } }
public class TrackLeadApiRequest { public string? LinkId { get; set; } public string? CustomerEmail { get; set; } public string? EventName { get; set; } }
public class TrackSaleApiRequest { public string? LinkId { get; set; } public decimal Amount { get; set; } public string? Currency { get; set; } public string? CustomerEmail { get; set; } public string? ExternalOrderId { get; set; } public string? StripeChargeId { get; set; } public string? ReferralCode { get; set; } }

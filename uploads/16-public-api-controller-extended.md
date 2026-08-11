# PART 16: PUBLIC API CONTROLLER (Extended)

```csharp
// ============================================================
// File: UTMPro.Web/Controllers/APIv1Controller.cs
// ============================================================
[Route("api/v1")]
[ApiController]
public class APIv1Controller : ControllerBase
{
    // API Key authentication middleware handles auth
    // Must have valid API key in Authorization: Bearer header

    // Middleware pipeline for /api/v1/*:
    // 1. Extract Bearer token
    // 2. Hash token (SHA256)
    // 3. Look up APIKeys table
    // 4. Get WorkspaceId and Scopes
    // 5. Set HttpContext.Items["WorkspaceId"]
    // 6. Set HttpContext.Items["Scopes"]
    // 7. Log to APILogs table (async)

    [HttpGet("links")]
    public async Task<IActionResult> GetLinks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] string? domain = null,
        [FromQuery] string? tag = null)
    {
        var workspaceId = GetWorkspaceId();
        RequireScope("links:read");

        var links = await _linkRepo.GetListAsync(
            workspaceId, search, null, null, null,
            false, page, pageSize, "CreatedAt", "DESC");

        return Ok(new
        {
            data = links.Select(l => MapLinkToApiResponse(l)),
            pagination = new
            {
                page, pageSize,
                total = links.FirstOrDefault()?.TotalCount ?? 0
            }
        });
    }

    [HttpPost("links")]
    public async Task<IActionResult> CreateLink(
        [FromBody] CreateLinkRequest request)
    {
        var workspaceId = GetWorkspaceId();
        RequireScope("links:write");

        var workspace = await _wsRepo.GetByIdAsync(workspaceId);
        var result = await _linkService.CreateAsync(
            workspace!, GetApiUserId(), request);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return CreatedAtAction(
            nameof(GetLink), 
            new { id = result.Link!.ExternalId },
            MapLinkToApiResponse(result.Link));
    }

    [HttpPost("events/lead")]
    public async Task<IActionResult> TrackLead(
        [FromBody] TrackLeadRequest request)
    {
        var workspaceId = GetWorkspaceId();
        RequireScope("events:write");

        // Find link by ID or external ID
        var link = await _linkRepo.GetByExternalIdAsync(
            request.LinkId, workspaceId);
        if (link == null)
            return NotFound(new { error = "Link not found" });

        // Find or create customer
        long? customerId = null;
        if (!string.IsNullOrEmpty(request.CustomerEmail))
        {
            customerId = await _customerRepo
                .FindOrCreateAsync(
                    workspaceId,
                    request.CustomerEmail,
                    request.CustomerName,
                    request.ExternalId);
        }

        await _eventRepo.CreateLeadEventAsync(new LeadEvent
        {
            LinkId = link.Id,
            WorkspaceId = workspaceId,
            CustomerId = customerId,
            EventName = request.EventName ?? "Lead",
            ExternalId = request.ExternalId
        });

        // Broadcast to SignalR
        await _realTimeService.BroadcastLeadAsync(
            workspaceId, new LeadEventDto(
                link.Id, request.CustomerEmail,
                request.EventName ?? "Lead",
                DateTime.UtcNow));

        return Ok(new { success = true });
    }

    [HttpPost("events/sale")]
    public async Task<IActionResult> TrackSale(
        [FromBody] TrackSaleRequest request)
    {
        var workspaceId = GetWorkspaceId();
        RequireScope("events:write");

        var link = await _linkRepo.GetByExternalIdAsync(
            request.LinkId, workspaceId);
        if (link == null)
            return NotFound(new { error = "Link not found" });

        // Create sale event
        await _eventRepo.CreateSaleEventAsync(new SaleEvent
        {
            LinkId = link.Id,
            WorkspaceId = workspaceId,
            Amount = request.Amount,
            Currency = request.Currency ?? "USD",
            EventName = request.EventName ?? "Sale",
            ExternalId = request.ExternalOrderId
        });

        // Try partner attribution
        if (!string.IsNullOrEmpty(request.ReferralCode))
        {
            await _partnerService.RecordSaleAsync(
                new RecordSaleRequest
                {
                    ReferralCode = request.ReferralCode,
                    SaleAmount = request.Amount,
                    Currency = request.Currency ?? "USD",
                    CustomerEmail = request.CustomerEmail,
                    ExternalOrderId = request.ExternalOrderId,
                    StripeChargeId = request.StripeChargeId
                });
        }

        return Ok(new { success = true });
    }

    private long GetWorkspaceId() =>
        (long)HttpContext.Items["WorkspaceId"]!;

    private void RequireScope(string scope)
    {
        var scopes = HttpContext.Items["Scopes"]?.ToString()
            ?? "";
        if (!scopes.Contains(scope) && !scopes.Contains("write"))
            throw new UnauthorizedAccessException(
                $"Missing scope: {scope}");
    }
}
```

---

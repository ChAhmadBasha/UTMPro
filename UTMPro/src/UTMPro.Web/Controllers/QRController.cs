using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Controllers;

[Route("{workspaceSlug}/qr")]
public class QRController : BaseWorkspaceController
{
    private readonly ILinkRepository _linkRepo;
    private readonly IWorkspaceRepository _wsRepo;

    public QRController(ILinkRepository linkRepo, IWorkspaceRepository wsRepo)
    {
        _linkRepo = linkRepo; _wsRepo = wsRepo;
    }

    // AJAX: Get QR data for rendering client-side with customization
    [HttpGet("{linkId}/data")]
    public async Task<IActionResult> GetQRData(string workspaceSlug, long linkId,
        string? fgColor = "#000000", string? bgColor = "#ffffff", int size = 256)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        var link = await _linkRepo.GetByIdAsync(linkId, CurrentWorkspace!.Id);
        if (link == null) return NotFound();

        return Ok(new
        {
            url = $"https://{link.Domain}/{link.Slug}",
            slug = link.Slug,
            domain = link.Domain,
            fgColor = fgColor ?? "#000000",
            bgColor = bgColor ?? "#ffffff",
            size,
            linkId = link.Id
        });
    }
}

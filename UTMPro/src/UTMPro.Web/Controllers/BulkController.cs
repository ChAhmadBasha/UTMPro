using System.Text;
using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Helpers;
using UTMPro.Data.Models;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Controllers;

[Route("{workspaceSlug}/links/bulk")]
public class BulkController : BaseWorkspaceController
{
    private readonly ILinkRepository _linkRepo;
    private readonly IWorkspaceRepository _wsRepo;
    private readonly IDomainRepository _domainRepo;

    public BulkController(ILinkRepository linkRepo, IWorkspaceRepository wsRepo, IDomainRepository domainRepo)
    {
        _linkRepo = linkRepo; _wsRepo = wsRepo; _domainRepo = domainRepo;
    }

    [HttpGet("import")]
    public async Task<IActionResult> Import(string workspaceSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanEdit()) return Forbidden();
        var domains = await _domainRepo.GetByWorkspaceIdAsync(CurrentWorkspace!.Id);
        ViewBag.Domains = domains;
        return View("~/Views/Links/BulkImport.cshtml");
    }

    [HttpPost("import")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessImport(string workspaceSlug, IFormFile csvFile, long domainId)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanEdit()) return Forbidden();
        if (csvFile == null || csvFile.Length == 0) { TempData["Error"] = "Please upload a CSV file"; return Redirect($"/{workspaceSlug}/links/bulk/import"); }

        var domain = await _domainRepo.GetByIdAsync(domainId);
        if (domain == null) { TempData["Error"] = "Invalid domain"; return Redirect($"/{workspaceSlug}/links/bulk/import"); }

        int success = 0, errors = 0;
        var errorLines = new List<string>();

        using var reader = new StreamReader(csvFile.OpenReadStream());
        var header = await reader.ReadLineAsync(); // Skip header
        int lineNum = 1;

        while (!reader.EndOfStream)
        {
            lineNum++;
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var parts = line.Split(',');
                var destUrl = parts[0].Trim().Trim('"');
                var customSlug = parts.Length > 1 ? parts[1].Trim().Trim('"') : null;
                var utmSource = parts.Length > 2 ? parts[2].Trim().Trim('"') : null;
                var utmMedium = parts.Length > 3 ? parts[3].Trim().Trim('"') : null;
                var utmCampaign = parts.Length > 4 ? parts[4].Trim().Trim('"') : null;

                if (!Uri.TryCreate(destUrl, UriKind.Absolute, out _)) { errorLines.Add($"Line {lineNum}: Invalid URL"); errors++; continue; }

                string slug;
                if (!string.IsNullOrEmpty(customSlug))
                {
                    if (await _linkRepo.SlugExistsAsync(domain.Id, customSlug)) { errorLines.Add($"Line {lineNum}: Slug '{customSlug}' taken"); errors++; continue; }
                    slug = customSlug;
                }
                else
                {
                    slug = IdGenerator.NewSlug(7);
                    int tries = 0;
                    while (await _linkRepo.SlugExistsAsync(domain.Id, slug) && tries < 5) { slug = IdGenerator.NewSlug(7); tries++; }
                }

                var link = new Link
                {
                    ExternalId = IdGenerator.NewExternalId("lnk_"), WorkspaceId = CurrentWorkspace!.Id,
                    DomainId = domain.Id, Slug = slug, CreatedBy = CurrentUserId,
                    UTMSource = utmSource, UTMMedium = utmMedium, UTMCampaign = utmCampaign,
                    RedirectMode = "Single"
                };
                var linkId = await _linkRepo.CreateAsync(link);
                await _linkRepo.AddDestinationAsync(new LinkDestination { LinkId = linkId, Url = destUrl, Weight = 100, IsActive = true });
                success++;
            }
            catch (Exception ex) { errorLines.Add($"Line {lineNum}: {ex.Message}"); errors++; }
        }

        TempData["Success"] = $"Import complete: {success} links created, {errors} errors";
        if (errorLines.Count > 0) TempData["Error"] = string.Join("\n", errorLines.Take(10));
        return Redirect($"/{workspaceSlug}/links");
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(string workspaceSlug)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();

        var (links, _) = await _linkRepo.GetListAsync(CurrentWorkspace!.Id, null, null, null, null, false, 1, 10000, "CreatedAt", "DESC");

        var sb = new StringBuilder();
        sb.AppendLine("ShortURL,DestinationURL,Slug,Domain,TotalClicks,UTMSource,UTMMedium,UTMCampaign,CreatedAt,Tags");
        foreach (var l in links)
        {
            sb.AppendLine($"\"https://{l.Domain}/{l.Slug}\",\"{l.PrimaryUrl}\",\"{l.Slug}\",\"{l.Domain}\",{l.TotalClicks},\"{l.UTMSource}\",\"{l.UTMMedium}\",\"{l.UTMCampaign}\",\"{l.CreatedAt:yyyy-MM-dd}\",\"{string.Join(";", l.TagNames)}\"");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"links-export-{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}

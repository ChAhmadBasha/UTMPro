using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Repositories;
using UTMPro.Web.Models.Requests;
using UTMPro.Web.Models.ViewModels;
using UTMPro.Web.Services;

namespace UTMPro.Web.Controllers;

[Route("{workspaceSlug}/links")]
public class LinksController : BaseWorkspaceController
{
    private readonly ILinkRepository _linkRepo;
    private readonly IWorkspaceRepository _wsRepo;
    private readonly IDomainRepository _domainRepo;
    private readonly ITagRepository _tagRepo;
    private readonly IFolderRepository _folderRepo;
    private readonly IPlanRepository _planRepo;
    private readonly ILinkService _linkService;
    private readonly IUrlMetadataService _metaService;

    public LinksController(ILinkRepository linkRepo, IWorkspaceRepository wsRepo,
        IDomainRepository domainRepo, ITagRepository tagRepo, IFolderRepository folderRepo,
        IPlanRepository planRepo, ILinkService linkService, IUrlMetadataService metaService)
    {
        _linkRepo = linkRepo;
        _wsRepo = wsRepo;
        _domainRepo = domainRepo;
        _tagRepo = tagRepo;
        _folderRepo = folderRepo;
        _planRepo = planRepo;
        _linkService = linkService;
        _metaService = metaService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string workspaceSlug, string? search, long? domainId,
        long? folderId, long? tagId, bool archived = false, int page = 1, int pageSize = 25,
        string sortBy = "CreatedAt", string sortDir = "DESC")
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();

        var (links, totalCount) = await _linkRepo.GetListAsync(
            CurrentWorkspace!.Id, search, domainId, folderId, tagId, archived, page, pageSize, sortBy, sortDir);

        var domains = await _domainRepo.GetByWorkspaceIdAsync(CurrentWorkspace.Id);
        var folders = await _folderRepo.GetByWorkspaceIdAsync(CurrentWorkspace.Id);
        var tags = await _tagRepo.GetByWorkspaceIdAsync(CurrentWorkspace.Id);
        var plan = await _planRepo.GetByIdAsync(CurrentWorkspace.PlanId);

        var vm = new LinksViewModel
        {
            Links = links,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize,
            Search = search,
            DomainId = domainId,
            FolderId = folderId,
            TagId = tagId,
            ShowArchived = archived,
            Domains = domains,
            Folders = folders,
            Tags = tags,
            Plan = plan!,
            LinksUsedThisMonth = CurrentWorkspace.LinksUsedThisMonth
        };

        return View("~/Views/Links/Index.cshtml", vm);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string workspaceSlug, [FromBody] CreateLinkRequest request)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanEdit()) return Forbidden();

        var result = await _linkService.CreateAsync(CurrentWorkspace!, CurrentUserId, request);
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            success = true,
            link = result.Link,
            shortUrl = $"https://{result.Link!.Domain}/{result.Link.Slug}"
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Detail(string workspaceSlug, long id)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        var link = await _linkRepo.GetByIdAsync(id, CurrentWorkspace!.Id);
        if (link == null) return NotFound();
        ViewBag.Folders = await _folderRepo.GetByWorkspaceIdAsync(CurrentWorkspace.Id);
        return View("~/Views/Links/Detail.cshtml", link);
    }

    [HttpPut("{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string workspaceSlug, long id, [FromBody] UpdateLinkRequest request)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanEdit()) return Forbidden();

        var link = await _linkRepo.GetByIdAsync(id, CurrentWorkspace!.Id);
        if (link == null) return NotFound();

        var result = await _linkService.UpdateAsync(link, request);
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        // Invalidate redirect engine cache for this link
        try
        {
            var httpFactory = HttpContext.RequestServices.GetService<IHttpClientFactory>();
            if (httpFactory != null)
            {
                var redirectUrl = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["App:RedirectEngineUrl"] ?? "https://go.utmpro.link";
                var client = httpFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(3);
                await client.PostAsync($"{redirectUrl}/cache/invalidate?domain={link.Domain}&slug={link.Slug}", null);
            }
        }
        catch { /* Cache invalidation failure is non-critical */ }

        return Ok(new { success = true });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string workspaceSlug, long id)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanAdmin()) return Forbidden();

        var link = await _linkRepo.GetByIdAsync(id, CurrentWorkspace!.Id);
        if (link == null) return NotFound();

        await _linkRepo.DeleteAsync(id);
        return Ok(new { success = true });
    }

    [HttpPost("{id}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(string workspaceSlug, long id)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (!CanEdit()) return Forbidden();

        var link = await _linkRepo.GetByIdAsync(id, CurrentWorkspace!.Id);
        if (link == null) return NotFound();

        link.IsActive = !link.IsActive;
        await _linkRepo.UpdateAsync(link);

        TempData["Success"] = link.IsActive ? "Link enabled" : "Link disabled";
        return Redirect($"/{workspaceSlug}/links/{id}");
    }

    // AJAX: Fetch URL metadata (title, description, image) from destination URL
    [HttpPost("api/fetch-metadata")]
    public async Task<IActionResult> FetchMetadata(string workspaceSlug, [FromBody] FetchMetadataRequest req)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        if (string.IsNullOrWhiteSpace(req?.Url)) return BadRequest(new { error = "URL is required" });

        var meta = await _metaService.FetchAsync(req.Url);
        return Ok(new { title = meta.Title, description = meta.Description, image = meta.Image, favicon = meta.Favicon, siteName = meta.SiteName });
    }

    [HttpPost("api/check-slug")]
    public async Task<IActionResult> CheckSlug(string workspaceSlug, [FromBody] CheckSlugRequest req)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();
        var exists = await _linkRepo.SlugExistsAsync(req.DomainId, req.Slug);
        return Ok(new { available = !exists });
    }
}

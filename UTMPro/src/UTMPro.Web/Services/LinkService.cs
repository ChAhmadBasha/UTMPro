using UTMPro.Data.Helpers;
using UTMPro.Data.Models;
using UTMPro.Data.Repositories;
using UTMPro.Web.Models.Requests;

namespace UTMPro.Web.Services;

public interface ILinkService
{
    Task<LinkResult> CreateAsync(Workspace workspace, long userId, CreateLinkRequest request);
    Task<LinkResult> UpdateAsync(Link link, UpdateLinkRequest request);
}

public class LinkResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public Link? Link { get; set; }
}

public class LinkService : ILinkService
{
    private readonly ILinkRepository _linkRepo;
    private readonly IDomainRepository _domainRepo;
    private readonly IPlanRepository _planRepo;
    private readonly IWorkspaceRepository _wsRepo;

    public LinkService(ILinkRepository linkRepo, IDomainRepository domainRepo,
        IPlanRepository planRepo, IWorkspaceRepository wsRepo)
    {
        _linkRepo = linkRepo;
        _domainRepo = domainRepo;
        _planRepo = planRepo;
        _wsRepo = wsRepo;
    }

    public async Task<LinkResult> CreateAsync(Workspace workspace, long userId, CreateLinkRequest request)
    {
        var plan = await _planRepo.GetByIdAsync(workspace.PlanId);
        if (plan == null)
            return new LinkResult { Error = "Plan not found" };

        // Check link limit
        if (workspace.LinksUsedThisMonth >= plan.MaxLinksPerMonth)
            return new LinkResult { Error = $"Monthly link limit reached ({plan.MaxLinksPerMonth})" };

        // Validate domain
        var domain = await _domainRepo.GetByIdAsync(request.DomainId);
        if (domain == null)
            return new LinkResult { Error = "Domain not found" };

        // Generate or validate slug
        string slug;
        if (!string.IsNullOrEmpty(request.CustomSlug))
        {
            slug = request.CustomSlug.Trim();
            if (await _linkRepo.SlugExistsAsync(domain.Id, slug))
                return new LinkResult { Error = "Slug already taken" };
        }
        else
        {
            slug = await GenerateUniqueSlugAsync(domain.Id);
        }

        // Password hashing
        string? passwordHash = null;
        bool hasPassword = false;
        if (!string.IsNullOrEmpty(request.Password))
        {
            if (!plan.HasPasswordProtection)
                return new LinkResult { Error = "Password protection requires Pro plan or higher" };
            passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12);
            hasPassword = true;
        }

        // Determine redirect mode
        string redirectMode = "Single";
        if (request.Destinations.Count > 1)
        {
            if (request.EnableABTest && plan.HasABTesting)
                redirectMode = "ABTest";
            else if (plan.HasWeightedURLs)
                redirectMode = "Weighted";
        }

        var link = new Link
        {
            ExternalId = IdGenerator.NewExternalId("lnk_"),
            WorkspaceId = workspace.Id,
            DomainId = domain.Id,
            Slug = slug,
            FolderId = request.FolderId,
            CreatedBy = userId,
            UTMSource = request.UTMSource,
            UTMMedium = request.UTMMedium,
            UTMCampaign = request.UTMCampaign,
            UTMTerm = request.UTMTerm,
            UTMContent = request.UTMContent,
            UTMReferral = request.UTMReferral,
            Comments = request.Comments,
            ExternalRefId = request.ExternalRefId,
            TenantId = request.TenantId,
            HasPassword = hasPassword,
            PasswordHash = passwordHash,
            ExpiresAt = request.ExpiresAt,
            ExpirationUrl = request.ExpirationUrl,
            IsCloaked = request.IsCloaked,
            IsIndexed = request.IsIndexed,
            AdminTrafficPercent = request.AdminTrafficPercent,
            AdminTrafficEnabled = request.AdminTrafficEnabled,
            RedirectMode = redirectMode,
            CustomTitle = request.CustomTitle,
            CustomDescription = request.CustomDescription,
            CustomImageUrl = request.CustomImageUrl,
            ABTestEnabled = request.EnableABTest,
            ABTestEndsAt = request.ABTestEndsAt,
            Domain = domain.DomainName
        };

        var linkId = await _linkRepo.CreateAsync(link);
        link.Id = linkId;

        // Add destinations
        if (request.Destinations.Count > 0)
        {
            foreach (var dest in request.Destinations)
            {
                await _linkRepo.AddDestinationAsync(new LinkDestination
                {
                    LinkId = linkId,
                    Url = dest.Url,
                    Weight = dest.Weight,
                    IsAdminUrl = dest.IsAdminUrl,
                    IsActive = true,
                    Label = dest.Label,
                    SortOrder = dest.SortOrder
                });
            }
        }
        else
        {
            // Single URL destination
            await _linkRepo.AddDestinationAsync(new LinkDestination
            {
                LinkId = linkId,
                Url = request.PrimaryUrl,
                Weight = 100,
                IsAdminUrl = false,
                IsActive = true,
                SortOrder = 0
            });
        }

        // Set tags
        if (request.TagIds.Count > 0)
            await _linkRepo.SetTagsAsync(linkId, request.TagIds);

        // Set targeting rules
        if (request.TargetingRules.Count > 0)
        {
            var rules = request.TargetingRules.Select(r => new LinkTargetingRule
            {
                LinkId = linkId,
                RuleType = r.RuleType,
                RuleValue = r.RuleValue,
                RedirectUrl = r.RedirectUrl,
                SortOrder = r.SortOrder
            }).ToList();
            await _linkRepo.SetTargetingRulesAsync(linkId, rules);
        }

        // Increment usage
        await _wsRepo.IncrementLinksUsedAsync(workspace.Id);

        link.PrimaryUrl = request.PrimaryUrl;
        return new LinkResult { Success = true, Link = link };
    }

    public async Task<LinkResult> UpdateAsync(Link link, UpdateLinkRequest request)
    {
        if (request.Archive)
        {
            await _linkRepo.ArchiveAsync(link.Id);
            return new LinkResult { Success = true, Link = link };
        }
        if (request.Unarchive)
        {
            await _linkRepo.UnarchiveAsync(link.Id);
            return new LinkResult { Success = true, Link = link };
        }

        link.FolderId = request.FolderId;
        link.UTMSource = request.UTMSource;
        link.UTMMedium = request.UTMMedium;
        link.UTMCampaign = request.UTMCampaign;
        link.UTMTerm = request.UTMTerm;
        link.UTMContent = request.UTMContent;
        link.UTMReferral = request.UTMReferral;
        link.Comments = request.Comments;
        link.ExpiresAt = request.ExpiresAt;
        link.ExpirationUrl = request.ExpirationUrl;
        link.IsCloaked = request.IsCloaked;
        link.IsIndexed = request.IsIndexed;
        link.CustomTitle = request.CustomTitle;
        link.CustomDescription = request.CustomDescription;
        link.CustomImageUrl = request.CustomImageUrl;

        if (!string.IsNullOrEmpty(request.Password))
        {
            link.HasPassword = true;
            link.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12);
        }

        await _linkRepo.UpdateAsync(link);

        // Update destinations
        if (request.Destinations.Count > 0)
        {
            var dests = request.Destinations.Select(d => new LinkDestination
            {
                Url = d.Url, Weight = d.Weight, IsAdminUrl = d.IsAdminUrl,
                IsActive = true, Label = d.Label, SortOrder = d.SortOrder
            }).ToList();
            await _linkRepo.UpdateDestinationsAsync(link.Id, dests);
        }

        if (request.TagIds.Count > 0)
            await _linkRepo.SetTagsAsync(link.Id, request.TagIds);

        if (request.TargetingRules.Count > 0)
        {
            var rules = request.TargetingRules.Select(r => new LinkTargetingRule
            {
                LinkId = link.Id, RuleType = r.RuleType,
                RuleValue = r.RuleValue, RedirectUrl = r.RedirectUrl, SortOrder = r.SortOrder
            }).ToList();
            await _linkRepo.SetTargetingRulesAsync(link.Id, rules);
        }

        return new LinkResult { Success = true, Link = link };
    }

    private async Task<string> GenerateUniqueSlugAsync(long domainId, int length = 7)
    {
        for (int i = 0; i < 5; i++)
        {
            var slug = IdGenerator.NewSlug(length);
            if (!await _linkRepo.SlugExistsAsync(domainId, slug))
                return slug;
        }
        return IdGenerator.NewSlug(length + 2);
    }
}

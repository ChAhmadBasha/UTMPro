using UTMPro.Data.Models;

namespace UTMPro.Web.Models.ViewModels;

public class LinksViewModel
{
    public List<Link> Links { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public string? Search { get; set; }
    public long? DomainId { get; set; }
    public long? FolderId { get; set; }
    public long? TagId { get; set; }
    public bool ShowArchived { get; set; }
    public List<Domain> Domains { get; set; } = new();
    public List<Folder> Folders { get; set; } = new();
    public List<Tag> Tags { get; set; } = new();
    public Plan Plan { get; set; } = new();
    public int LinksUsedThisMonth { get; set; }
}

public class DashboardViewModel
{
    public long TotalUsers { get; set; }
    public long NewUsersToday { get; set; }
    public long TotalWorkspaces { get; set; }
    public long TotalLinks { get; set; }
    public long ClicksToday { get; set; }
    public long ClicksLastHour { get; set; }
    public long VerifiedDomains { get; set; }
}

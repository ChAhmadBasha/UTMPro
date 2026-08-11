namespace UTMPro.Web.Models.ViewModels;

public class AdminDashboardViewModel
{
    // ── Summary Counters ─────────────────────────────────
    public long TotalUsers { get; set; }
    public long NewUsersToday { get; set; }
    public long NewUsersWeek { get; set; }
    public long TotalWorkspaces { get; set; }
    public long ActiveWorkspaces { get; set; }
    public long TotalLinks { get; set; }
    public long LinksCreatedToday { get; set; }
    public long ClicksToday { get; set; }
    public long ClicksLastHour { get; set; }
    public long ClicksWeek { get; set; }
    public long ClicksMonth { get; set; }
    public long ClicksAllTime { get; set; }
    public long VerifiedDomains { get; set; }
    public long SystemDomains { get; set; }

    // ── Chart Data ───────────────────────────────────────
    public List<DayCount> ClicksByDay { get; set; } = new();
    public List<DayCount> SignupsByDay { get; set; } = new();
    public List<HourCount> ClicksByHour { get; set; } = new();

    // ── Breakdowns ───────────────────────────────────────
    public List<NameCount> TopCountries { get; set; } = new();
    public List<TopLinkItem> TopLinks { get; set; } = new();
    public List<TopWorkspaceItem> TopWorkspaces { get; set; } = new();
    public List<NameCount> DeviceBreakdown { get; set; } = new();
    public List<NameCount> BrowserBreakdown { get; set; } = new();
    public List<NameCount> OSBreakdown { get; set; } = new();
    public List<NameCount> TopReferrers { get; set; } = new();
    public List<NameCount> PlanDistribution { get; set; } = new();
}

public class DayCount
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

public class HourCount
{
    public int Hour { get; set; }
    public int Count { get; set; }
}

public class NameCount
{
    public string Name { get; set; } = "";
    public string? Code { get; set; }
    public long Count { get; set; }
}

public class TopLinkItem
{
    public long LinkId { get; set; }
    public string Domain { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? PrimaryUrl { get; set; }
    public string WorkspaceName { get; set; } = "";
    public string WorkspaceSlug { get; set; } = "";
    public long Clicks30d { get; set; }
    public long AllTimeClicks { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TopWorkspaceItem
{
    public long WorkspaceId { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string PlanName { get; set; } = "";
    public int MemberCount { get; set; }
    public int LinkCount { get; set; }
    public long Clicks30d { get; set; }
    public int LinksUsed { get; set; }
    public int EventsUsed { get; set; }
    public DateTime CreatedAt { get; set; }
}

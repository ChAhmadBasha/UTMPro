namespace UTMPro.Web.Models.ViewModels;

public class AdminAnalyticsDetailViewModel
{
    public int Days { get; set; } = 30;
    public long TotalClicks { get; set; }
    public long UniqueVisitors { get; set; }
    public List<DayCount> ClicksByDay { get; set; } = new();
    public List<NameCount> Countries { get; set; } = new();
    public List<TopLinkItem> TopLinks { get; set; } = new();
    public List<TopWorkspaceItem> TopWorkspaces { get; set; } = new();
    public List<NameCount> Devices { get; set; } = new();
    public List<NameCount> Browsers { get; set; } = new();
    public List<NameCount> OperatingSystems { get; set; } = new();
    public List<NameCount> Referrers { get; set; } = new();
}

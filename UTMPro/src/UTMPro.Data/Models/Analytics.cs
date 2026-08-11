namespace UTMPro.Data.Models;

public class AnalyticsSummary
{
    public long TotalClicks { get; set; }
    public long UniqueClicks { get; set; }
    public long UserClicks { get; set; }
    public long AdminClicks { get; set; }
    public int TotalLeads { get; set; }
    public decimal TotalSales { get; set; }
    public List<TimeSeriesPoint> TimeSeries { get; set; } = new();
    public List<CountryStats> Countries { get; set; } = new();
    public List<DeviceStats> Devices { get; set; } = new();
    public List<BrowserStats> Browsers { get; set; } = new();
    public List<OSStats> OSStats { get; set; } = new();
    public List<ReferrerStats> Referrers { get; set; } = new();
    public List<LinkStats> TopLinks { get; set; } = new();
}

public record TimeSeriesPoint(DateTime Date, long Clicks);
public record CountryStats(string Country, string CountryCode, long Clicks);
public record DeviceStats(string Device, long Clicks, decimal Percentage);
public record BrowserStats(string Browser, long Clicks);
public record OSStats(string OS, long Clicks);
public record ReferrerStats(string Referrer, long Clicks);
public record LinkStats(
    long Id, string Slug, string Domain, 
    long TotalClicks, long PeriodClicks);

namespace UTMPro.Data.Models;

public class ClickEvent
{
    public long Id { get; set; }
    public long LinkId { get; set; }
    public long WorkspaceId { get; set; }
    public string? DestinationUrl { get; set; }
    public bool IsAdminRedirect { get; set; }
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Referer { get; set; }
    public string? Country { get; set; }
    public string? CountryCode { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? Continent { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Device { get; set; }
    public string? Browser { get; set; }
    public string? BrowserVersion { get; set; }
    public string? OS { get; set; }
    public string? OSVersion { get; set; }
    public string? UTMSource { get; set; }
    public string? UTMMedium { get; set; }
    public string? UTMCampaign { get; set; }
    public string? UTMTerm { get; set; }
    public string? UTMContent { get; set; }
    public string? Trigger { get; set; }
    public DateTime ClickedAt { get; set; }
}

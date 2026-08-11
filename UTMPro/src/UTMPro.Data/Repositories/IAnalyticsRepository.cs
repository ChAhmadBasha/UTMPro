using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public interface IAnalyticsRepository
{
    /// <summary>
    /// Returns analytics for the workspace. Admin-traffic redirects (clicks sent to
    /// an admin link via AdminTrafficRules) are excluded unless <paramref name="includeAdmin"/>
    /// is true, so ordinary link owners never see injected admin traffic in their stats.
    /// </summary>
    Task<AnalyticsSummary> GetSummaryAsync(long workspaceId, DateTime startDate, DateTime endDate, long? linkId = null, bool includeAdmin = false);
    Task<List<ClickEvent>> GetEventsAsync(long workspaceId, int page, int pageSize, long? linkId = null, bool includeAdmin = false);
    Task<int> GetEventsCountAsync(long workspaceId, long? linkId = null, bool includeAdmin = false);
}

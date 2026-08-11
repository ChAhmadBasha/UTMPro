using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public interface IAnalyticsRepository
{
    Task<AnalyticsSummary> GetSummaryAsync(long workspaceId, DateTime startDate, DateTime endDate, long? linkId = null);
    Task<List<ClickEvent>> GetEventsAsync(long workspaceId, int page, int pageSize, long? linkId = null);
    Task<int> GetEventsCountAsync(long workspaceId, long? linkId = null);
}

using System.Data;
using Microsoft.Data.SqlClient;
using UTMPro.Data;
using UTMPro.RedirectEngine.Models;

namespace UTMPro.RedirectEngine.Services;

public class AdminTrafficService
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<AdminTrafficService> _logger;

    public AdminTrafficService(IDbConnectionFactory db, ILogger<AdminTrafficService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<DestinationModel>> GetAdminUrlsForWorkspaceAsync(long workspaceId)
    {
        const string sql = @"
            ;WITH SelectedRule AS (
                SELECT TOP (1) atr.Id
                FROM AdminTrafficRules atr
                WHERE atr.IsActive = 1
                  AND ((atr.IsGlobal = 0 AND atr.WorkspaceId = @WorkspaceId)
                       OR atr.IsGlobal = 1)
                ORDER BY CASE WHEN atr.IsGlobal = 0 THEN 0 ELSE 1 END,
                         atr.UpdatedAt DESC,
                         atr.Id DESC
            )
            SELECT atu.Id, atu.Url, atu.Weight
            FROM AdminTrafficUrls atu
            INNER JOIN SelectedRule selected ON atu.RuleId = selected.Id
            WHERE atu.IsActive = 1
            ORDER BY atu.Weight DESC, atu.Id ASC";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WorkspaceId", workspaceId);

        var urls = new List<DestinationModel>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var urlId = reader.GetInt64(0);
            urls.Add(new DestinationModel
            {
                Id = urlId,
                AdminTrafficUrlId = urlId,
                Url = reader.GetString(1),
                Weight = reader.GetInt32(2),
                IsAdminUrl = true
            });
        }
        return urls;
    }
}

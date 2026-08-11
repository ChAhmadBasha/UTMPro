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
            SELECT atu.Url, atu.Weight
            FROM AdminTrafficUrls atu
            INNER JOIN AdminTrafficRules atr ON atu.RuleId = atr.Id
            WHERE atr.IsActive = 1 AND atu.IsActive = 1
              AND (atr.IsGlobal = 1 OR atr.WorkspaceId = @WorkspaceId)
            ORDER BY atr.IsGlobal ASC, atu.Weight DESC";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WorkspaceId", workspaceId);

        var urls = new List<DestinationModel>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            urls.Add(new DestinationModel
            {
                Url = reader.GetString(0),
                Weight = reader.GetInt32(1),
                IsAdminUrl = true
            });
        }
        return urls;
    }
}

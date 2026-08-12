using Microsoft.Data.SqlClient;
using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public class SystemSettingsRepository : ISystemSettingsRepository
{
    private readonly IDbConnectionFactory _db;
    public SystemSettingsRepository(IDbConnectionFactory db) => _db = db;

    public async Task<string?> GetValueAsync(string key)
    {
        const string sql = "SELECT SettingValue FROM SystemSettings WHERE SettingKey = @Key";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Key", key);
        var result = await cmd.ExecuteScalarAsync();
        return result as string;
    }

    public async Task SetValueAsync(string key, string value, long? updatedBy = null, string? description = null)
    {
        const string sql = @"
            MERGE SystemSettings AS target
            USING (SELECT @Key AS SettingKey) AS source
            ON target.SettingKey = source.SettingKey
            WHEN MATCHED THEN
                UPDATE SET
                    SettingValue = @Value,
                    UpdatedAt = GETUTCDATE(),
                    UpdatedBy = @UpdatedBy,
                    Description = COALESCE(@Description, target.Description)
            WHEN NOT MATCHED THEN
                INSERT (SettingKey, SettingValue, Description, UpdatedAt, UpdatedBy)
                VALUES (@Key, @Value, @Description, GETUTCDATE(), @UpdatedBy);";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Key", key);
        cmd.Parameters.AddWithValue("@Value", value);
        cmd.Parameters.AddWithValue("@UpdatedBy", (object?)updatedBy ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Description", (object?)description ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<SystemSetting>> GetAllAsync()
    {
        const string sql = "SELECT * FROM SystemSettings ORDER BY SettingKey";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        var list = new List<SystemSetting>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new SystemSetting
            {
                Id = r.GetInt32(r.GetOrdinal("Id")),
                SettingKey = r.GetString(r.GetOrdinal("SettingKey")),
                SettingValue = r.GetString(r.GetOrdinal("SettingValue")),
                Description = r.IsDBNull(r.GetOrdinal("Description")) ? null : r.GetString(r.GetOrdinal("Description")),
                UpdatedAt = r.GetDateTime(r.GetOrdinal("UpdatedAt")),
            });
        }
        return list;
    }
}

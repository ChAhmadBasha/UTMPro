using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public interface ISystemSettingsRepository
{
    Task<string?> GetValueAsync(string key);
    Task SetValueAsync(string key, string value, long? updatedBy = null);
    Task<List<SystemSetting>> GetAllAsync();
}

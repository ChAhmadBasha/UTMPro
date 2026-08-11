using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public interface IPlanRepository
{
    Task<Plan?> GetByIdAsync(int id);
    Task<Plan?> GetDefaultPlanAsync();
    Task<List<Plan>> GetAllActiveAsync();
    Task<List<Plan>> GetAllAsync();
    Task<int> CreateAsync(Plan plan);
    Task UpdateAsync(Plan plan);
    Task ToggleActiveAsync(int id);
    Task DeleteAsync(int id);
}

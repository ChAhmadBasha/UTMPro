using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public interface IBlogRepository
{
    Task<BlogPost?> GetByIdAsync(long id);
    Task<BlogPost?> GetBySlugAsync(string slug);
    Task<List<BlogPost>> GetPublishedAsync(int page, int pageSize, int? categoryId = null);
    Task<List<BlogPost>> GetLatestAsync(int count);
    Task<List<BlogPost>> GetAllAsync(int page, int pageSize);
    Task<int> GetCountAsync(string? status = null);
    Task<long> CreateAsync(BlogPost post);
    Task UpdateAsync(BlogPost post);
    Task DeleteAsync(long id);
    Task IncrementViewCountAsync(long id);
    // Categories
    Task<List<BlogCategory>> GetCategoriesAsync();
    Task SetPostCategoriesAsync(long postId, List<int> categoryIds);
}

using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(long id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByExternalIdAsync(string externalId);
    Task<User?> GetByGoogleIdAsync(string googleId);
    Task<long> CreateAsync(User user);
    Task UpdateAsync(User user);
    Task UpdateLastLoginAsync(long userId);
    Task<List<User>> GetAllAsync(string? search, int page, int pageSize);
    Task<int> GetTotalCountAsync(string? search);
    Task SetSuperAdminAsync(long userId, bool isSuperAdmin);
    Task<bool> HasAnySuperAdminAsync();
    Task SoftDeleteAsync(long userId);
    // Tokens
    Task CreateTokenAsync(UserToken token);
    Task<UserToken?> GetTokenAsync(string token, string tokenType);
    Task<UserToken?> GetTokenByCodeAsync(long userId, string code, string tokenType);
    Task MarkTokenUsedAsync(long tokenId);
    Task SetEmailVerifiedAsync(long userId);
}

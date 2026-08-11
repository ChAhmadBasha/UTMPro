using System.Data;
using Microsoft.Data.SqlClient;
using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _db;

    public UserRepository(IDbConnectionFactory db) => _db = db;

    public async Task<User?> GetByIdAsync(long id)
    {
        const string sql = "SELECT * FROM Users WHERE Id = @Id AND DeletedAt IS NULL";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        return await ReadUserAsync(cmd);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        const string sql = "SELECT * FROM Users WHERE Email = @Email AND DeletedAt IS NULL";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Email", email);
        return await ReadUserAsync(cmd);
    }

    public async Task<User?> GetByExternalIdAsync(string externalId)
    {
        const string sql = "SELECT * FROM Users WHERE ExternalId = @ExternalId AND DeletedAt IS NULL";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ExternalId", externalId);
        return await ReadUserAsync(cmd);
    }

    public async Task<User?> GetByGoogleIdAsync(string googleId)
    {
        const string sql = "SELECT * FROM Users WHERE GoogleId = @GoogleId AND DeletedAt IS NULL";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@GoogleId", googleId);
        return await ReadUserAsync(cmd);
    }

    public async Task<long> CreateAsync(User user)
    {
        const string sql = @"
            INSERT INTO Users (ExternalId, Name, Email, EmailVerified, PasswordHash, AvatarUrl, GoogleId, IsActive, IsSuperAdmin, CreatedAt, UpdatedAt)
            VALUES (@ExternalId, @Name, @Email, @EmailVerified, @PasswordHash, @AvatarUrl, @GoogleId, @IsActive, @IsSuperAdmin, @CreatedAt, @UpdatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ExternalId", user.ExternalId);
        cmd.Parameters.AddWithValue("@Name", user.Name);
        cmd.Parameters.AddWithValue("@Email", user.Email);
        cmd.Parameters.AddWithValue("@EmailVerified", user.EmailVerified);
        cmd.Parameters.AddWithValue("@PasswordHash", (object?)user.PasswordHash ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@AvatarUrl", (object?)user.AvatarUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@GoogleId", (object?)user.GoogleId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsActive", user.IsActive);
        cmd.Parameters.AddWithValue("@IsSuperAdmin", user.IsSuperAdmin);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

        var result = await cmd.ExecuteScalarAsync();
        return (long)result!;
    }

    public async Task UpdateAsync(User user)
    {
        const string sql = @"
            UPDATE Users SET 
                Name = @Name, Email = @Email, EmailVerified = @EmailVerified,
                PasswordHash = @PasswordHash, AvatarUrl = @AvatarUrl,
                GoogleId = @GoogleId, IsActive = @IsActive,
                IsSuperAdmin = @IsSuperAdmin, UpdatedAt = @UpdatedAt
            WHERE Id = @Id";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", user.Id);
        cmd.Parameters.AddWithValue("@Name", user.Name);
        cmd.Parameters.AddWithValue("@Email", user.Email);
        cmd.Parameters.AddWithValue("@EmailVerified", user.EmailVerified);
        cmd.Parameters.AddWithValue("@PasswordHash", (object?)user.PasswordHash ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@AvatarUrl", (object?)user.AvatarUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@GoogleId", (object?)user.GoogleId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsActive", user.IsActive);
        cmd.Parameters.AddWithValue("@IsSuperAdmin", user.IsSuperAdmin);
        cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateLastLoginAsync(long userId)
    {
        const string sql = "UPDATE Users SET LastLoginAt = @Now, UpdatedAt = @Now WHERE Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", userId);
        cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<User>> GetAllAsync(string? search, int page, int pageSize)
    {
        var sql = @"
            SELECT *, COUNT(*) OVER() AS TotalCount FROM Users 
            WHERE DeletedAt IS NULL
            AND (@Search IS NULL OR Name LIKE '%' + @Search + '%' OR Email LIKE '%' + @Search + '%')
            ORDER BY CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
        cmd.Parameters.AddWithValue("@PageSize", pageSize);

        var users = new List<User>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            users.Add(MapUser(reader));
        return users;
    }

    public async Task<int> GetTotalCountAsync(string? search)
    {
        const string sql = @"
            SELECT COUNT(*) FROM Users 
            WHERE DeletedAt IS NULL
            AND (@Search IS NULL OR Name LIKE '%' + @Search + '%' OR Email LIKE '%' + @Search + '%')";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task SetSuperAdminAsync(long userId, bool isSuperAdmin)
    {
        const string sql = "UPDATE Users SET IsSuperAdmin = @IsSuperAdmin, UpdatedAt = GETUTCDATE() WHERE Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", userId);
        cmd.Parameters.AddWithValue("@IsSuperAdmin", isSuperAdmin);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> HasAnySuperAdminAsync()
    {
        const string sql = "SELECT COUNT(*) FROM Users WHERE IsSuperAdmin = 1 AND DeletedAt IS NULL";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        return (int)(await cmd.ExecuteScalarAsync())! > 0;
    }

    public async Task SoftDeleteAsync(long userId)
    {
        const string sql = "UPDATE Users SET DeletedAt = GETUTCDATE(), IsActive = 0, UpdatedAt = GETUTCDATE() WHERE Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", userId);
        await cmd.ExecuteNonQueryAsync();
    }

    // Tokens
    public async Task CreateTokenAsync(UserToken token)
    {
        const string sql = @"
            INSERT INTO UserTokens (UserId, Token, TokenType, VerificationCode, ExpiresAt, CreatedAt)
            VALUES (@UserId, @Token, @TokenType, @Code, @ExpiresAt, @CreatedAt)";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", token.UserId);
        cmd.Parameters.AddWithValue("@Token", token.Token);
        cmd.Parameters.AddWithValue("@TokenType", token.TokenType);
        cmd.Parameters.AddWithValue("@Code", (object?)token.VerificationCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ExpiresAt", token.ExpiresAt);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<UserToken?> GetTokenAsync(string token, string tokenType)
    {
        const string sql = @"
            SELECT * FROM UserTokens 
            WHERE Token = @Token AND TokenType = @TokenType 
            AND UsedAt IS NULL AND ExpiresAt > GETUTCDATE()";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Token", token);
        cmd.Parameters.AddWithValue("@TokenType", tokenType);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return new UserToken
        {
            Id = reader.GetInt64(reader.GetOrdinal("Id")),
            UserId = reader.GetInt64(reader.GetOrdinal("UserId")),
            Token = reader.GetString(reader.GetOrdinal("Token")),
            TokenType = reader.GetString(reader.GetOrdinal("TokenType")),
            VerificationCode = reader.IsDBNull(reader.GetOrdinal("VerificationCode")) ? null : reader.GetString(reader.GetOrdinal("VerificationCode")),
            ExpiresAt = reader.GetDateTime(reader.GetOrdinal("ExpiresAt")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    public async Task<UserToken?> GetTokenByCodeAsync(long userId, string code, string tokenType)
    {
        const string sql = @"
            SELECT * FROM UserTokens 
            WHERE UserId = @UserId AND VerificationCode = @Code AND TokenType = @TokenType 
            AND UsedAt IS NULL AND ExpiresAt > GETUTCDATE()
            ORDER BY CreatedAt DESC";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@Code", code);
        cmd.Parameters.AddWithValue("@TokenType", tokenType);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return new UserToken
        {
            Id = reader.GetInt64(reader.GetOrdinal("Id")),
            UserId = reader.GetInt64(reader.GetOrdinal("UserId")),
            Token = reader.GetString(reader.GetOrdinal("Token")),
            TokenType = reader.GetString(reader.GetOrdinal("TokenType")),
            VerificationCode = reader.IsDBNull(reader.GetOrdinal("VerificationCode")) ? null : reader.GetString(reader.GetOrdinal("VerificationCode")),
            ExpiresAt = reader.GetDateTime(reader.GetOrdinal("ExpiresAt")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    public async Task SetEmailVerifiedAsync(long userId)
    {
        const string sql = "UPDATE Users SET EmailVerified = 1, UpdatedAt = GETUTCDATE() WHERE Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", userId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task MarkTokenUsedAsync(long tokenId)
    {
        const string sql = "UPDATE UserTokens SET UsedAt = GETUTCDATE() WHERE Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", tokenId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<User?> ReadUserAsync(SqlCommand cmd)
    {
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return MapUser(reader);
    }

    private static User MapUser(SqlDataReader reader) => new()
    {
        Id = reader.GetInt64(reader.GetOrdinal("Id")),
        ExternalId = reader.GetString(reader.GetOrdinal("ExternalId")),
        Name = reader.GetString(reader.GetOrdinal("Name")),
        Email = reader.GetString(reader.GetOrdinal("Email")),
        EmailVerified = reader.GetBoolean(reader.GetOrdinal("EmailVerified")),
        PasswordHash = reader.IsDBNull(reader.GetOrdinal("PasswordHash")) ? null : reader.GetString(reader.GetOrdinal("PasswordHash")),
        AvatarUrl = reader.IsDBNull(reader.GetOrdinal("AvatarUrl")) ? null : reader.GetString(reader.GetOrdinal("AvatarUrl")),
        GoogleId = reader.IsDBNull(reader.GetOrdinal("GoogleId")) ? null : reader.GetString(reader.GetOrdinal("GoogleId")),
        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
        IsSuperAdmin = reader.GetBoolean(reader.GetOrdinal("IsSuperAdmin")),
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
        LastLoginAt = reader.IsDBNull(reader.GetOrdinal("LastLoginAt")) ? null : reader.GetDateTime(reader.GetOrdinal("LastLoginAt")),
        DeletedAt = reader.IsDBNull(reader.GetOrdinal("DeletedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("DeletedAt"))
    };
}

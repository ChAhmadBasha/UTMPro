using Microsoft.Data.SqlClient;
using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly IDbConnectionFactory _db;
    public CustomerRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Customer?> GetByIdAsync(long id, long workspaceId)
    {
        const string sql = "SELECT * FROM Customers WHERE Id = @Id AND WorkspaceId = @WsId";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@WsId", workspaceId);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapCustomer(r) : null;
    }

    public async Task<List<Customer>> GetByWorkspaceIdAsync(long workspaceId, string? search, int page, int pageSize)
    {
        const string sql = @"SELECT * FROM Customers WHERE WorkspaceId = @WsId
            AND (@Search IS NULL OR Name LIKE '%'+@Search+'%' OR Email LIKE '%'+@Search+'%')
            ORDER BY CreatedAt DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WsId", workspaceId);
        cmd.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
        cmd.Parameters.AddWithValue("@PageSize", pageSize);
        var list = new List<Customer>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapCustomer(r));
        return list;
    }

    public async Task<int> GetTotalCountAsync(long workspaceId, string? search)
    {
        const string sql = @"SELECT COUNT(*) FROM Customers WHERE WorkspaceId = @WsId
            AND (@Search IS NULL OR Name LIKE '%'+@Search+'%' OR Email LIKE '%'+@Search+'%')";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WsId", workspaceId);
        cmd.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task<long> CreateAsync(Customer customer)
    {
        const string sql = @"INSERT INTO Customers (WorkspaceId, ExternalId, Name, Email, AvatarUrl, Country, CountryCode, LTV, FirstSeenAt, CreatedAt, UpdatedAt)
            VALUES (@WsId, @ExtId, @Name, @Email, @Avatar, @Country, @CC, @LTV, GETUTCDATE(), GETUTCDATE(), GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WsId", customer.WorkspaceId);
        cmd.Parameters.AddWithValue("@ExtId", (object?)customer.ExternalId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Name", (object?)customer.Name ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Email", (object?)customer.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Avatar", (object?)customer.AvatarUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Country", (object?)customer.Country ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CC", (object?)customer.CountryCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@LTV", customer.LTV);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task UpdateAsync(Customer customer)
    {
        const string sql = "UPDATE Customers SET Name=@Name, Email=@Email, LTV=@LTV, UpdatedAt=GETUTCDATE() WHERE Id=@Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", customer.Id);
        cmd.Parameters.AddWithValue("@Name", (object?)customer.Name ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Email", (object?)customer.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@LTV", customer.LTV);
        await cmd.ExecuteNonQueryAsync();
    }

    private static Customer MapCustomer(SqlDataReader r) => new()
    {
        Id = r.GetInt64(r.GetOrdinal("Id")),
        WorkspaceId = r.GetInt64(r.GetOrdinal("WorkspaceId")),
        ExternalId = r.IsDBNull(r.GetOrdinal("ExternalId")) ? null : r.GetString(r.GetOrdinal("ExternalId")),
        Name = r.IsDBNull(r.GetOrdinal("Name")) ? null : r.GetString(r.GetOrdinal("Name")),
        Email = r.IsDBNull(r.GetOrdinal("Email")) ? null : r.GetString(r.GetOrdinal("Email")),
        Country = r.IsDBNull(r.GetOrdinal("Country")) ? null : r.GetString(r.GetOrdinal("Country")),
        CountryCode = r.IsDBNull(r.GetOrdinal("CountryCode")) ? null : r.GetString(r.GetOrdinal("CountryCode")),
        LTV = r.GetDecimal(r.GetOrdinal("LTV")),
        CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
    };
}

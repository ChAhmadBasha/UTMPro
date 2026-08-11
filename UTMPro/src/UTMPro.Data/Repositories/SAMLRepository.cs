using Microsoft.Data.SqlClient;
using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public class SAMLRepository : ISAMLRepository
{
    private readonly IDbConnectionFactory _db;
    public SAMLRepository(IDbConnectionFactory db) => _db = db;

    public async Task<SAMLConfiguration?> GetByWorkspaceIdAsync(long workspaceId)
    {
        const string sql = "SELECT * FROM SAMLConfigurations WHERE WorkspaceId = @WsId";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WsId", workspaceId);
        await using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new SAMLConfiguration
        {
            Id = r.GetInt64(r.GetOrdinal("Id")), WorkspaceId = r.GetInt64(r.GetOrdinal("WorkspaceId")),
            IdpEntityId = r.IsDBNull(r.GetOrdinal("IdpEntityId")) ? null : r.GetString(r.GetOrdinal("IdpEntityId")),
            IdpSSOUrl = r.IsDBNull(r.GetOrdinal("IdpSSOUrl")) ? null : r.GetString(r.GetOrdinal("IdpSSOUrl")),
            SpEntityId = r.IsDBNull(r.GetOrdinal("SpEntityId")) ? null : r.GetString(r.GetOrdinal("SpEntityId")),
            SpAcsUrl = r.IsDBNull(r.GetOrdinal("SpAcsUrl")) ? null : r.GetString(r.GetOrdinal("SpAcsUrl")),
            EmailAttribute = r.GetString(r.GetOrdinal("EmailAttribute")),
            NameAttribute = r.GetString(r.GetOrdinal("NameAttribute")),
            RequireSAML = r.GetBoolean(r.GetOrdinal("RequireSAML")),
            AutoProvision = r.GetBoolean(r.GetOrdinal("AutoProvision")),
            DefaultRole = r.GetString(r.GetOrdinal("DefaultRole")),
            IsActive = r.GetBoolean(r.GetOrdinal("IsActive")),
        };
    }

    public async Task UpsertAsync(SAMLConfiguration c)
    {
        const string sql = @"IF EXISTS (SELECT 1 FROM SAMLConfigurations WHERE WorkspaceId=@WsId)
            UPDATE SAMLConfigurations SET IdpEntityId=@IE,IdpSSOUrl=@IS,IdpSLOUrl=@IL,IdpCertificate=@IC,SpEntityId=@SE,SpAcsUrl=@SA,EmailAttribute=@EA,NameAttribute=@NA,RoleAttribute=@RA,RequireSAML=@RS,AutoProvision=@AP,DefaultRole=@DR,IsActive=@Act,UpdatedAt=GETUTCDATE() WHERE WorkspaceId=@WsId
            ELSE INSERT INTO SAMLConfigurations (WorkspaceId,IdpEntityId,IdpSSOUrl,IdpSLOUrl,IdpCertificate,SpEntityId,SpAcsUrl,EmailAttribute,NameAttribute,RoleAttribute,RequireSAML,AutoProvision,DefaultRole,IsActive,CreatedAt,UpdatedAt)
            VALUES (@WsId,@IE,@IS,@IL,@IC,@SE,@SA,@EA,@NA,@RA,@RS,@AP,@DR,@Act,GETUTCDATE(),GETUTCDATE())";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WsId", c.WorkspaceId);
        cmd.Parameters.AddWithValue("@IE", (object?)c.IdpEntityId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IS", (object?)c.IdpSSOUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IL", (object?)c.IdpSLOUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IC", (object?)c.IdpCertificate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SE", (object?)c.SpEntityId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SA", (object?)c.SpAcsUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@EA", c.EmailAttribute);
        cmd.Parameters.AddWithValue("@NA", c.NameAttribute);
        cmd.Parameters.AddWithValue("@RA", (object?)c.RoleAttribute ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RS", c.RequireSAML);
        cmd.Parameters.AddWithValue("@AP", c.AutoProvision);
        cmd.Parameters.AddWithValue("@DR", c.DefaultRole);
        cmd.Parameters.AddWithValue("@Act", c.IsActive);
        await cmd.ExecuteNonQueryAsync();
    }
}

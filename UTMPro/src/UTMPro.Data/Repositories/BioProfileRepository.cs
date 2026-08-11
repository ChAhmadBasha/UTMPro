using Microsoft.Data.SqlClient;
using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public class BioProfileRepository : IBioProfileRepository
{
    private readonly IDbConnectionFactory _db;
    public BioProfileRepository(IDbConnectionFactory db) => _db = db;

    public async Task<BioProfile?> GetByUsernameAsync(string username)
    {
        const string sql = "SELECT * FROM BioProfiles WHERE Username = @U AND IsActive = 1";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@U", username);
        await using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        var p = MapProfile(r);
        await r.CloseAsync();
        p.Links = await GetLinksAsync(conn, p.Id);
        return p;
    }

    public async Task<BioProfile?> GetByUserIdAsync(long userId)
    {
        const string sql = "SELECT * FROM BioProfiles WHERE UserId = @Uid";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Uid", userId);
        await using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        var p = MapProfile(r);
        await r.CloseAsync();
        p.Links = await GetLinksAsync(conn, p.Id);
        return p;
    }

    public async Task<long> CreateAsync(BioProfile p)
    {
        const string sql = @"INSERT INTO BioProfiles (UserId,Username,DisplayName,Bio,AvatarUrl,Theme,BgColor,TextColor,ButtonStyle,SocialTwitter,SocialInstagram,SocialLinkedIn,SocialGithub,SocialYoutube,SocialTiktok,IsActive,CreatedAt,UpdatedAt)
            VALUES (@Uid,@Un,@Dn,@Bio,@Av,@Th,@Bg,@Tc,@Bs,@Tw,@Ig,@Li,@Gh,@Yt,@Tk,1,GETUTCDATE(),GETUTCDATE()); SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Uid", p.UserId); cmd.Parameters.AddWithValue("@Un", p.Username);
        cmd.Parameters.AddWithValue("@Dn", (object?)p.DisplayName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Bio", (object?)p.Bio ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Av", (object?)p.AvatarUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Th", p.Theme); cmd.Parameters.AddWithValue("@Bg", p.BgColor);
        cmd.Parameters.AddWithValue("@Tc", p.TextColor); cmd.Parameters.AddWithValue("@Bs", p.ButtonStyle);
        cmd.Parameters.AddWithValue("@Tw", (object?)p.SocialTwitter ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Ig", (object?)p.SocialInstagram ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Li", (object?)p.SocialLinkedIn ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Gh", (object?)p.SocialGithub ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Yt", (object?)p.SocialYoutube ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Tk", (object?)p.SocialTiktok ?? DBNull.Value);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task UpdateAsync(BioProfile p)
    {
        const string sql = @"UPDATE BioProfiles SET DisplayName=@Dn,Bio=@Bio,AvatarUrl=@Av,Theme=@Th,BgColor=@Bg,TextColor=@Tc,ButtonStyle=@Bs,SocialTwitter=@Tw,SocialInstagram=@Ig,SocialLinkedIn=@Li,SocialGithub=@Gh,SocialYoutube=@Yt,SocialTiktok=@Tk,UpdatedAt=GETUTCDATE() WHERE Id=@Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", p.Id);
        cmd.Parameters.AddWithValue("@Dn", (object?)p.DisplayName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Bio", (object?)p.Bio ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Av", (object?)p.AvatarUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Th", p.Theme); cmd.Parameters.AddWithValue("@Bg", p.BgColor);
        cmd.Parameters.AddWithValue("@Tc", p.TextColor); cmd.Parameters.AddWithValue("@Bs", p.ButtonStyle);
        cmd.Parameters.AddWithValue("@Tw", (object?)p.SocialTwitter ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Ig", (object?)p.SocialInstagram ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Li", (object?)p.SocialLinkedIn ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Gh", (object?)p.SocialGithub ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Yt", (object?)p.SocialYoutube ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Tk", (object?)p.SocialTiktok ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand("SELECT COUNT(*) FROM BioProfiles WHERE Username=@U", conn);
        cmd.Parameters.AddWithValue("@U", username);
        return (int)(await cmd.ExecuteScalarAsync())! > 0;
    }

    public async Task<long> AddLinkAsync(BioLink link)
    {
        const string sql = "INSERT INTO BioLinks (ProfileId,Title,Url,IconEmoji,ThumbnailUrl,IsActive,SortOrder,CreatedAt) VALUES (@Pid,@T,@U,@I,@Th,1,@S,GETUTCDATE()); SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Pid", link.ProfileId); cmd.Parameters.AddWithValue("@T", link.Title);
        cmd.Parameters.AddWithValue("@U", link.Url); cmd.Parameters.AddWithValue("@I", (object?)link.IconEmoji ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Th", (object?)link.ThumbnailUrl ?? DBNull.Value); cmd.Parameters.AddWithValue("@S", link.SortOrder);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task UpdateLinkAsync(BioLink link)
    {
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand("UPDATE BioLinks SET Title=@T,Url=@U,IconEmoji=@I,IsActive=@A,SortOrder=@S WHERE Id=@Id", conn);
        cmd.Parameters.AddWithValue("@Id", link.Id); cmd.Parameters.AddWithValue("@T", link.Title);
        cmd.Parameters.AddWithValue("@U", link.Url); cmd.Parameters.AddWithValue("@I", (object?)link.IconEmoji ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@A", link.IsActive); cmd.Parameters.AddWithValue("@S", link.SortOrder);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteLinkAsync(long id)
    {
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand("DELETE FROM BioLinks WHERE Id=@Id", conn);
        cmd.Parameters.AddWithValue("@Id", id); await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<BioLink>> GetLinksAsync(long profileId)
    {
        await using var conn = await _db.CreateOpenConnectionAsync();
        return await GetLinksAsync(conn, profileId);
    }

    private static async Task<List<BioLink>> GetLinksAsync(SqlConnection conn, long profileId)
    {
        await using var cmd = new SqlCommand("SELECT * FROM BioLinks WHERE ProfileId=@Pid AND IsActive=1 ORDER BY SortOrder", conn);
        cmd.Parameters.AddWithValue("@Pid", profileId);
        var list = new List<BioLink>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(new BioLink {
            Id = r.GetInt64(0), ProfileId = profileId, Title = r.GetString(r.GetOrdinal("Title")),
            Url = r.GetString(r.GetOrdinal("Url")), IconEmoji = r.IsDBNull(r.GetOrdinal("IconEmoji")) ? null : r.GetString(r.GetOrdinal("IconEmoji")),
            ClickCount = r.GetInt64(r.GetOrdinal("ClickCount")), SortOrder = r.GetInt32(r.GetOrdinal("SortOrder")) });
        return list;
    }

    public async Task IncrementClickAsync(long linkId)
    {
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand("UPDATE BioLinks SET ClickCount=ClickCount+1 WHERE Id=@Id", conn);
        cmd.Parameters.AddWithValue("@Id", linkId); await cmd.ExecuteNonQueryAsync();
    }

    public async Task IncrementViewAsync(long profileId)
    {
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand("UPDATE BioProfiles SET ViewCount=ViewCount+1 WHERE Id=@Id", conn);
        cmd.Parameters.AddWithValue("@Id", profileId); await cmd.ExecuteNonQueryAsync();
    }

    private static BioProfile MapProfile(SqlDataReader r) => new()
    {
        Id = r.GetInt64(r.GetOrdinal("Id")), UserId = r.GetInt64(r.GetOrdinal("UserId")),
        Username = r.GetString(r.GetOrdinal("Username")),
        DisplayName = r.IsDBNull(r.GetOrdinal("DisplayName")) ? null : r.GetString(r.GetOrdinal("DisplayName")),
        Bio = r.IsDBNull(r.GetOrdinal("Bio")) ? null : r.GetString(r.GetOrdinal("Bio")),
        AvatarUrl = r.IsDBNull(r.GetOrdinal("AvatarUrl")) ? null : r.GetString(r.GetOrdinal("AvatarUrl")),
        Theme = r.GetString(r.GetOrdinal("Theme")), BgColor = r.GetString(r.GetOrdinal("BgColor")),
        TextColor = r.GetString(r.GetOrdinal("TextColor")), ButtonStyle = r.GetString(r.GetOrdinal("ButtonStyle")),
        ViewCount = r.GetInt64(r.GetOrdinal("ViewCount")), IsActive = r.GetBoolean(r.GetOrdinal("IsActive")),
        CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
    };
}

using Microsoft.Data.SqlClient;
using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public class BlogRepository : IBlogRepository
{
    private readonly IDbConnectionFactory _db;
    public BlogRepository(IDbConnectionFactory db) => _db = db;

    public async Task<BlogPost?> GetByIdAsync(long id)
    {
        const string sql = @"SELECT bp.*, u.Name AS AuthorName, u.AvatarUrl AS AuthorAvatarUrl,
            (SELECT STRING_AGG(bc.Name, ',') FROM BlogPostCategories bpc INNER JOIN BlogCategories bc ON bpc.CategoryId = bc.Id WHERE bpc.PostId = bp.Id) AS CategoryNames
            FROM BlogPosts bp INNER JOIN Users u ON bp.AuthorId = u.Id WHERE bp.Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapPost(r) : null;
    }

    public async Task<BlogPost?> GetBySlugAsync(string slug)
    {
        const string sql = @"SELECT bp.*, u.Name AS AuthorName, u.AvatarUrl AS AuthorAvatarUrl,
            (SELECT STRING_AGG(bc.Name, ',') FROM BlogPostCategories bpc INNER JOIN BlogCategories bc ON bpc.CategoryId = bc.Id WHERE bpc.PostId = bp.Id) AS CategoryNames
            FROM BlogPosts bp INNER JOIN Users u ON bp.AuthorId = u.Id WHERE bp.Slug = @Slug AND bp.IsActive = 1";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Slug", slug);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapPost(r) : null;
    }

    public async Task<List<BlogPost>> GetPublishedAsync(int page, int pageSize, int? categoryId = null)
    {
        var sql = @"SELECT bp.*, u.Name AS AuthorName, u.AvatarUrl AS AuthorAvatarUrl,
            (SELECT STRING_AGG(bc.Name, ',') FROM BlogPostCategories bpc INNER JOIN BlogCategories bc ON bpc.CategoryId = bc.Id WHERE bpc.PostId = bp.Id) AS CategoryNames
            FROM BlogPosts bp INNER JOIN Users u ON bp.AuthorId = u.Id
            WHERE bp.Status = 'Published' AND bp.IsActive = 1" +
            (categoryId.HasValue ? " AND EXISTS(SELECT 1 FROM BlogPostCategories WHERE PostId=bp.Id AND CategoryId=@CatId)" : "") +
            " ORDER BY bp.PublishedAt DESC OFFSET @Off ROWS FETCH NEXT @PS ROWS ONLY";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        if (categoryId.HasValue) cmd.Parameters.AddWithValue("@CatId", categoryId.Value);
        cmd.Parameters.AddWithValue("@Off", (page - 1) * pageSize);
        cmd.Parameters.AddWithValue("@PS", pageSize);
        return await ReadListAsync(cmd);
    }

    public async Task<List<BlogPost>> GetLatestAsync(int count)
    {
        const string sql = @"SELECT TOP (@Count) bp.*, u.Name AS AuthorName, u.AvatarUrl AS AuthorAvatarUrl,
            (SELECT STRING_AGG(bc.Name, ',') FROM BlogPostCategories bpc INNER JOIN BlogCategories bc ON bpc.CategoryId = bc.Id WHERE bpc.PostId = bp.Id) AS CategoryNames
            FROM BlogPosts bp INNER JOIN Users u ON bp.AuthorId = u.Id
            WHERE bp.Status = 'Published' AND bp.IsActive = 1 ORDER BY bp.PublishedAt DESC";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Count", count);
        return await ReadListAsync(cmd);
    }

    public async Task<List<BlogPost>> GetAllAsync(int page, int pageSize)
    {
        const string sql = @"SELECT bp.*, u.Name AS AuthorName, u.AvatarUrl AS AuthorAvatarUrl,
            (SELECT STRING_AGG(bc.Name, ',') FROM BlogPostCategories bpc INNER JOIN BlogCategories bc ON bpc.CategoryId = bc.Id WHERE bpc.PostId = bp.Id) AS CategoryNames
            FROM BlogPosts bp INNER JOIN Users u ON bp.AuthorId = u.Id ORDER BY bp.CreatedAt DESC OFFSET @Off ROWS FETCH NEXT @PS ROWS ONLY";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Off", (page - 1) * pageSize);
        cmd.Parameters.AddWithValue("@PS", pageSize);
        return await ReadListAsync(cmd);
    }

    public async Task<int> GetCountAsync(string? status = null)
    {
        var sql = "SELECT COUNT(*) FROM BlogPosts WHERE IsActive = 1" + (status != null ? " AND Status = @St" : "");
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        if (status != null) cmd.Parameters.AddWithValue("@St", status);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task<long> CreateAsync(BlogPost p)
    {
        const string sql = @"INSERT INTO BlogPosts (Slug,Title,Excerpt,Content,FeaturedImage,AuthorId,MetaTitle,MetaDescription,MetaKeywords,CanonicalUrl,OgImage,Status,PublishedAt,IsActive,CreatedAt,UpdatedAt)
            VALUES (@Slug,@Title,@Exc,@Content,@Img,@Author,@MT,@MD,@MK,@CU,@OG,@Status,@PubAt,1,GETUTCDATE(),GETUTCDATE()); SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Slug", p.Slug);
        cmd.Parameters.AddWithValue("@Title", p.Title);
        cmd.Parameters.AddWithValue("@Exc", (object?)p.Excerpt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Content", p.Content);
        cmd.Parameters.AddWithValue("@Img", (object?)p.FeaturedImage ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Author", p.AuthorId);
        cmd.Parameters.AddWithValue("@MT", (object?)p.MetaTitle ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@MD", (object?)p.MetaDescription ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@MK", (object?)p.MetaKeywords ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CU", (object?)p.CanonicalUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@OG", (object?)p.OgImage ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", p.Status);
        cmd.Parameters.AddWithValue("@PubAt", (object?)p.PublishedAt ?? DBNull.Value);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task UpdateAsync(BlogPost p)
    {
        const string sql = @"UPDATE BlogPosts SET Title=@Title,Slug=@Slug,Excerpt=@Exc,Content=@Content,FeaturedImage=@Img,MetaTitle=@MT,MetaDescription=@MD,MetaKeywords=@MK,CanonicalUrl=@CU,OgImage=@OG,Status=@Status,PublishedAt=@PubAt,UpdatedAt=GETUTCDATE() WHERE Id=@Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", p.Id);
        cmd.Parameters.AddWithValue("@Title", p.Title);
        cmd.Parameters.AddWithValue("@Slug", p.Slug);
        cmd.Parameters.AddWithValue("@Exc", (object?)p.Excerpt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Content", p.Content);
        cmd.Parameters.AddWithValue("@Img", (object?)p.FeaturedImage ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@MT", (object?)p.MetaTitle ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@MD", (object?)p.MetaDescription ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@MK", (object?)p.MetaKeywords ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CU", (object?)p.CanonicalUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@OG", (object?)p.OgImage ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", p.Status);
        cmd.Parameters.AddWithValue("@PubAt", (object?)p.PublishedAt ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(long id)
    {
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand("UPDATE BlogPosts SET IsActive=0, UpdatedAt=GETUTCDATE() WHERE Id=@Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task IncrementViewCountAsync(long id)
    {
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand("UPDATE BlogPosts SET ViewCount=ViewCount+1 WHERE Id=@Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<BlogCategory>> GetCategoriesAsync()
    {
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand("SELECT * FROM BlogCategories ORDER BY Name", conn);
        var list = new List<BlogCategory>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(new BlogCategory { Id = r.GetInt32(0), Name = r.GetString(1), Slug = r.GetString(2) });
        return list;
    }

    public async Task SetPostCategoriesAsync(long postId, List<int> categoryIds)
    {
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var del = new SqlCommand("DELETE FROM BlogPostCategories WHERE PostId=@Id", conn);
        del.Parameters.AddWithValue("@Id", postId);
        await del.ExecuteNonQueryAsync();
        foreach (var cid in categoryIds)
        {
            await using var ins = new SqlCommand("INSERT INTO BlogPostCategories (PostId,CategoryId) VALUES (@Pid,@Cid)", conn);
            ins.Parameters.AddWithValue("@Pid", postId); ins.Parameters.AddWithValue("@Cid", cid);
            await ins.ExecuteNonQueryAsync();
        }
    }

    private static async Task<List<BlogPost>> ReadListAsync(SqlCommand cmd)
    {
        var list = new List<BlogPost>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapPost(r));
        return list;
    }

    private static BlogPost MapPost(SqlDataReader r)
    {
        var post = new BlogPost
        {
            Id = r.GetInt64(r.GetOrdinal("Id")), Slug = r.GetString(r.GetOrdinal("Slug")),
            Title = r.GetString(r.GetOrdinal("Title")),
            Excerpt = r.IsDBNull(r.GetOrdinal("Excerpt")) ? null : r.GetString(r.GetOrdinal("Excerpt")),
            Content = r.GetString(r.GetOrdinal("Content")),
            FeaturedImage = r.IsDBNull(r.GetOrdinal("FeaturedImage")) ? null : r.GetString(r.GetOrdinal("FeaturedImage")),
            AuthorId = r.GetInt64(r.GetOrdinal("AuthorId")),
            MetaTitle = r.IsDBNull(r.GetOrdinal("MetaTitle")) ? null : r.GetString(r.GetOrdinal("MetaTitle")),
            MetaDescription = r.IsDBNull(r.GetOrdinal("MetaDescription")) ? null : r.GetString(r.GetOrdinal("MetaDescription")),
            Status = r.GetString(r.GetOrdinal("Status")),
            PublishedAt = r.IsDBNull(r.GetOrdinal("PublishedAt")) ? null : r.GetDateTime(r.GetOrdinal("PublishedAt")),
            ViewCount = r.GetInt64(r.GetOrdinal("ViewCount")),
            IsActive = r.GetBoolean(r.GetOrdinal("IsActive")),
            CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
            UpdatedAt = r.GetDateTime(r.GetOrdinal("UpdatedAt")),
            AuthorName = r.IsDBNull(r.GetOrdinal("AuthorName")) ? null : r.GetString(r.GetOrdinal("AuthorName")),
        };
        var catNames = r.IsDBNull(r.GetOrdinal("CategoryNames")) ? null : r.GetString(r.GetOrdinal("CategoryNames"));
        if (!string.IsNullOrEmpty(catNames)) post.Categories = catNames.Split(',').ToList();
        return post;
    }
}

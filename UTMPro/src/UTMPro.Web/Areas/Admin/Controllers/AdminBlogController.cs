using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Models;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin")]
[Route("admin/blog")]
public class AdminBlogController : Controller
{
    private readonly IBlogRepository _blogRepo;

    public AdminBlogController(IBlogRepository blogRepo) => _blogRepo = blogRepo;

    private long AdminId => long.Parse(User.FindFirst("UserId")!.Value);

    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1)
    {
        var posts = await _blogRepo.GetAllAsync(page, 25);
        var total = await _blogRepo.GetCountAsync();
        ViewBag.TotalCount = total; ViewBag.CurrentPage = page;
        return View("~/Areas/Admin/Views/Blog/Index.cshtml", posts);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        var categories = await _blogRepo.GetCategoriesAsync();
        ViewBag.Categories = categories;
        return View("~/Areas/Admin/Views/Blog/Create.cshtml");
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePost(string title, string slug, string content, string? excerpt,
        string? featuredImage, string? metaTitle, string? metaDescription, string? metaKeywords,
        string status = "Draft", int[]? categoryIds = null)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
        {
            TempData["Error"] = "Title and content are required";
            return Redirect("/admin/blog/create");
        }

        slug = slug?.Trim().ToLower().Replace(" ", "-") ?? title.ToLower().Replace(" ", "-");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");

        var post = new BlogPost
        {
            Title = title, Slug = slug, Content = content, Excerpt = excerpt,
            FeaturedImage = featuredImage, AuthorId = AdminId,
            MetaTitle = metaTitle ?? title, MetaDescription = metaDescription ?? excerpt,
            MetaKeywords = metaKeywords, Status = status,
            PublishedAt = status == "Published" ? DateTime.UtcNow : null
        };

        var id = await _blogRepo.CreateAsync(post);
        if (categoryIds?.Length > 0)
            await _blogRepo.SetPostCategoriesAsync(id, categoryIds.ToList());

        TempData["Success"] = status == "Published" ? "Blog post published!" : "Draft saved";
        return Redirect("/admin/blog");
    }

    [HttpGet("{id}/edit")]
    public async Task<IActionResult> Edit(long id)
    {
        var post = await _blogRepo.GetByIdAsync(id);
        if (post == null) return NotFound();
        var categories = await _blogRepo.GetCategoriesAsync();
        ViewBag.Categories = categories;
        return View("~/Areas/Admin/Views/Blog/Edit.cshtml", post);
    }

    [HttpPost("{id}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPost(long id, string title, string slug, string content, string? excerpt,
        string? featuredImage, string? metaTitle, string? metaDescription, string? metaKeywords,
        string status = "Draft", int[]? categoryIds = null)
    {
        var post = await _blogRepo.GetByIdAsync(id);
        if (post == null) return NotFound();

        post.Title = title; post.Slug = slug; post.Content = content; post.Excerpt = excerpt;
        post.FeaturedImage = featuredImage; post.MetaTitle = metaTitle; post.MetaDescription = metaDescription;
        post.MetaKeywords = metaKeywords; post.Status = status;
        if (status == "Published" && post.PublishedAt == null) post.PublishedAt = DateTime.UtcNow;

        await _blogRepo.UpdateAsync(post);
        if (categoryIds != null)
            await _blogRepo.SetPostCategoriesAsync(id, categoryIds.ToList());

        TempData["Success"] = "Blog post updated";
        return Redirect("/admin/blog");
    }

    [HttpPost("{id}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        await _blogRepo.DeleteAsync(id);
        TempData["Success"] = "Blog post deleted";
        return Redirect("/admin/blog");
    }
}

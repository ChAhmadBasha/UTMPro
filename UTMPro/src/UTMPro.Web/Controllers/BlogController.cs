using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Controllers;

[Route("blog")]
public class BlogController : Controller
{
    private readonly IBlogRepository _blogRepo;
    private readonly ISystemSettingsRepository _settingsRepo;

    public BlogController(IBlogRepository blogRepo, ISystemSettingsRepository settingsRepo)
    {
        _blogRepo = blogRepo; _settingsRepo = settingsRepo;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1, int? category = null)
    {
        var posts = await _blogRepo.GetPublishedAsync(page, 12, category);
        var total = await _blogRepo.GetCountAsync("Published");
        var categories = await _blogRepo.GetCategoriesAsync();
        ViewBag.Categories = categories;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / 12);
        ViewBag.CategoryId = category;
        ViewBag.SiteLogo = await _settingsRepo.GetValueAsync("SiteLogoUrl") ?? "/uploads/logos/logo.png";
        return View("~/Views/Blog/Index.cshtml", posts);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Post(string slug)
    {
        var post = await _blogRepo.GetBySlugAsync(slug);
        if (post == null) return NotFound();
        await _blogRepo.IncrementViewCountAsync(post.Id);
        var recent = await _blogRepo.GetLatestAsync(3);
        ViewBag.RecentPosts = recent.Where(p => p.Id != post.Id).Take(3).ToList();
        ViewBag.SiteLogo = await _settingsRepo.GetValueAsync("SiteLogoUrl") ?? "/uploads/logos/logo.png";
        return View("~/Views/Blog/Post.cshtml", post);
    }
}

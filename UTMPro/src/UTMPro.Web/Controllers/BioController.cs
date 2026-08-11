using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Models;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Controllers;

// Public bio page: /@username
[Route("@{username}")]
public class BioPublicController : Controller
{
    private readonly IBioProfileRepository _bioRepo;

    public BioPublicController(IBioProfileRepository bioRepo) => _bioRepo = bioRepo;

    [HttpGet("")]
    public async Task<IActionResult> Profile(string username)
    {
        var profile = await _bioRepo.GetByUsernameAsync(username);
        if (profile == null || !profile.IsActive) return NotFound();
        await _bioRepo.IncrementViewAsync(profile.Id);
        return View("~/Views/Bio/Profile.cshtml", profile);
    }

    [HttpGet("click/{linkId}")]
    public async Task<IActionResult> Click(string username, long linkId)
    {
        await _bioRepo.IncrementClickAsync(linkId);
        var links = await _bioRepo.GetLinksAsync(0); // Will redirect below
        return Redirect("/"); // Handled by JS on the page
    }
}

// Bio management (authenticated)
[Authorize]
[Route("account/bio")]
public class BioManageController : Controller
{
    private readonly IBioProfileRepository _bioRepo;

    public BioManageController(IBioProfileRepository bioRepo) => _bioRepo = bioRepo;

    private long UserId => long.Parse(User.FindFirst("UserId")!.Value);

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var profile = await _bioRepo.GetByUserIdAsync(UserId);
        if (profile == null) return View("~/Views/Bio/Setup.cshtml");
        return View("~/Views/Bio/Manage.cshtml", profile);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string username, string? displayName, string? bio)
    {
        username = username.ToLower().Trim().Replace(" ", "");
        if (await _bioRepo.UsernameExistsAsync(username))
        {
            TempData["Error"] = "Username already taken";
            return Redirect("/account/bio");
        }

        var profile = new BioProfile { UserId = UserId, Username = username, DisplayName = displayName, Bio = bio };
        await _bioRepo.CreateAsync(profile);
        TempData["Success"] = "Bio page created!";
        return Redirect("/account/bio");
    }

    [HttpPost("update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string? displayName, string? bio, string? avatarUrl,
        string theme, string bgColor, string textColor, string buttonStyle,
        string? twitter, string? instagram, string? linkedin, string? github)
    {
        var profile = await _bioRepo.GetByUserIdAsync(UserId);
        if (profile == null) return NotFound();
        profile.DisplayName = displayName; profile.Bio = bio; profile.AvatarUrl = avatarUrl;
        profile.Theme = theme; profile.BgColor = bgColor; profile.TextColor = textColor; profile.ButtonStyle = buttonStyle;
        profile.SocialTwitter = twitter; profile.SocialInstagram = instagram; profile.SocialLinkedIn = linkedin; profile.SocialGithub = github;
        await _bioRepo.UpdateAsync(profile);
        TempData["Success"] = "Bio updated";
        return Redirect("/account/bio");
    }

    [HttpPost("links/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddLink(string title, string url, string? emoji)
    {
        var profile = await _bioRepo.GetByUserIdAsync(UserId);
        if (profile == null) return NotFound();
        await _bioRepo.AddLinkAsync(new BioLink { ProfileId = profile.Id, Title = title, Url = url, IconEmoji = emoji });
        TempData["Success"] = "Link added";
        return Redirect("/account/bio");
    }

    [HttpPost("links/{id}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLink(long id)
    {
        await _bioRepo.DeleteLinkAsync(id);
        return Redirect("/account/bio");
    }
}

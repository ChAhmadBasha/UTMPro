using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Controllers;

[Authorize]
[Route("account")]
public class AccountController : Controller
{
    private readonly IUserRepository _userRepo;
    private readonly IWorkspaceRepository _wsRepo;

    public AccountController(IUserRepository userRepo, IWorkspaceRepository wsRepo)
    {
        _userRepo = userRepo;
        _wsRepo = wsRepo;
    }

    private long UserId => long.Parse(User.FindFirst("UserId")!.Value);

    [HttpGet("settings")]
    public async Task<IActionResult> Settings()
    {
        var user = await _userRepo.GetByIdAsync(UserId);
        if (user == null) return NotFound();
        var workspaces = await _wsRepo.GetByUserIdAsync(UserId);
        ViewBag.Workspaces = workspaces;
        return View("~/Views/Account/Settings.cshtml", user);
    }

    [HttpPost("settings")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSettings(string name, string email, string? avatarUrl)
    {
        var user = await _userRepo.GetByIdAsync(UserId);
        if (user == null) return NotFound();

        user.Name = name;
        user.Email = email.ToLower().Trim();
        user.AvatarUrl = avatarUrl;
        await _userRepo.UpdateAsync(user);

        TempData["Success"] = "Account updated";
        return Redirect("/account/settings");
    }

    [HttpGet("settings/security")]
    public async Task<IActionResult> Security()
    {
        var user = await _userRepo.GetByIdAsync(UserId);
        return View("~/Views/Account/Security.cshtml", user);
    }

    [HttpPost("settings/security")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePassword(string currentPassword, string newPassword)
    {
        var user = await _userRepo.GetByIdAsync(UserId);
        if (user == null) return NotFound();

        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            if (string.IsNullOrEmpty(currentPassword) || !BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                ViewBag.Error = "Current password is incorrect";
                return View("~/Views/Account/Security.cshtml", user);
            }
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, 12);
        await _userRepo.UpdateAsync(user);

        TempData["Success"] = "Password updated";
        return Redirect("/account/settings/security");
    }

    [HttpGet("settings/referrals")]
    public IActionResult Referrals()
    {
        return View("~/Views/Account/Referrals.cshtml");
    }
}

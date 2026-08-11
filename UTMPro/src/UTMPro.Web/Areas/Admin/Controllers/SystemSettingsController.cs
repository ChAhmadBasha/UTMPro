using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin")]
[Route("admin/settings")]
public class SystemSettingsController : Controller
{
    private readonly ISystemSettingsRepository _settingsRepo;

    public SystemSettingsController(ISystemSettingsRepository settingsRepo) => _settingsRepo = settingsRepo;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var settings = await _settingsRepo.GetAllAsync();
        return View("~/Areas/Admin/Views/Settings/Index.cshtml", settings);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(IFormCollection form)
    {
        var adminId = long.Parse(User.FindFirst("UserId")!.Value);

        foreach (var key in form.Keys)
        {
            // Form fields are named "setting_KEYNAME" to avoid binding issues
            if (key.StartsWith("setting_"))
            {
                var settingKey = key.Substring(8); // Remove "setting_" prefix
                var value = form[key].ToString();
                await _settingsRepo.SetValueAsync(settingKey, value, adminId);
            }
        }

        TempData["Success"] = "Settings updated successfully";
        return Redirect("/admin/settings");
    }
}

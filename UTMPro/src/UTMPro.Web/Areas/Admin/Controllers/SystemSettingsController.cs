using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Models;
using UTMPro.Data.Repositories;
using UTMPro.Web.Services;

namespace UTMPro.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin")]
[Route("admin/settings")]
public class SystemSettingsController : Controller
{
    private readonly ISystemSettingsRepository _settingsRepo;
    private readonly IRedirectCacheInvalidationService _cacheInvalidation;

    public SystemSettingsController(
        ISystemSettingsRepository settingsRepo,
        IRedirectCacheInvalidationService cacheInvalidation)
    {
        _settingsRepo = settingsRepo;
        _cacheInvalidation = cacheInvalidation;
    }

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
        var invalidateRedirectCache = false;

        foreach (var key in form.Keys)
        {
            // Form fields are named "setting_KEYNAME" to avoid binding issues
            if (!key.StartsWith("setting_"))
                continue;

            var settingKey = key.Substring(8); // Remove "setting_" prefix
            var value = form[key].ToString();

            if (settingKey == SystemSettingKeys.AdminTrafficMinClicks)
            {
                if (!int.TryParse(value, out var parsed)
                    || parsed < 0
                    || parsed > SystemSettingKeys.AdminTrafficMinClicksMax)
                {
                    TempData["Error"] =
                        $"AdminTrafficMinClicks must be an integer between 0 and {SystemSettingKeys.AdminTrafficMinClicksMax}.";
                    return Redirect("/admin/settings");
                }

                value = parsed.ToString();
                invalidateRedirectCache = true;
            }

            await _settingsRepo.SetValueAsync(settingKey, value, adminId);
        }

        if (invalidateRedirectCache)
            await _cacheInvalidation.InvalidateAllAsync();

        TempData["Success"] = "Settings updated successfully";
        return Redirect("/admin/settings");
    }
}

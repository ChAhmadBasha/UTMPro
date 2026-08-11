using Microsoft.AspNetCore.Mvc;

namespace UTMPro.Web.Controllers;

public class ErrorController : Controller
{
    [HttpGet("/error")]
    public IActionResult Error() => View("~/Views/Error/500.cshtml");

    [HttpGet("/access-denied")]
    public IActionResult AccessDenied() => View("~/Views/Error/403.cshtml");
}

using Microsoft.AspNetCore.Mvc;

namespace UTMPro.Web.Controllers;

public class ApiDocsController : Controller
{
    [HttpGet("/docs/api")]
    public IActionResult Index() => View("~/Views/ApiDocs/Index.cshtml");
}

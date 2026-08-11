using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Controllers;

[Route("{workspaceSlug}/customers")]
public class CustomersController : BaseWorkspaceController
{
    private readonly ICustomerRepository _customerRepo;
    private readonly IWorkspaceRepository _wsRepo;

    public CustomersController(ICustomerRepository customerRepo, IWorkspaceRepository wsRepo)
    {
        _customerRepo = customerRepo;
        _wsRepo = wsRepo;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string workspaceSlug, string? search, int page = 1)
    {
        if (!await LoadWorkspaceAsync(workspaceSlug, _wsRepo)) return NotFound();

        var customers = await _customerRepo.GetByWorkspaceIdAsync(CurrentWorkspace!.Id, search, page, 25);
        var total = await _customerRepo.GetTotalCountAsync(CurrentWorkspace.Id, search);

        ViewBag.TotalCount = total;
        ViewBag.CurrentPage = page;
        ViewBag.Search = search;
        return View("~/Views/Customers/Index.cshtml", customers);
    }
}

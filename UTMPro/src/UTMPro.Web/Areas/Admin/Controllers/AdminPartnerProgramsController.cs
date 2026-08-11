using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin")]
[Route("admin/partner-programs")]
public class AdminPartnerProgramsController : Controller
{
    private readonly IPartnerRepository _partnerRepo;
    public AdminPartnerProgramsController(IPartnerRepository partnerRepo) => _partnerRepo = partnerRepo;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var programs = await _partnerRepo.GetAllProgramsAsync();
        return View("~/Areas/Admin/Views/PartnerPrograms/Index.cshtml", programs);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Detail(long id)
    {
        var program = await _partnerRepo.GetProgramByIdAsync(id);
        if (program == null) return NotFound();
        var partners = await _partnerRepo.GetPartnersByProgramAsync(id, null, 1, 20);
        ViewBag.Partners = partners;
        return View("~/Areas/Admin/Views/PartnerPrograms/Detail.cshtml", program);
    }
}

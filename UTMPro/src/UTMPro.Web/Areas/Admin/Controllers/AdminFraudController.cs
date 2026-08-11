using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using UTMPro.Data;
using UTMPro.Data.Models;

namespace UTMPro.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin")]
[Route("admin/fraud")]
public class AdminFraudController : Controller
{
    private readonly IDbConnectionFactory _db;

    public AdminFraudController(IDbConnectionFactory db) => _db = db;

    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1)
    {
        const string sql = @"SELECT pfe.*, p.Name AS PartnerName, p.Email AS PartnerEmail, pp.ProgramName
            FROM PartnerFraudEvents pfe 
            INNER JOIN Partners p ON pfe.PartnerId = p.Id
            INNER JOIN PartnerPrograms pp ON pfe.ProgramId = pp.Id
            ORDER BY pfe.CreatedAt DESC OFFSET @Off ROWS FETCH NEXT 25 ROWS ONLY";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Off", (page - 1) * 25);
        var events = new List<PartnerFraudEvent>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            events.Add(new PartnerFraudEvent
            {
                Id = r.GetInt64(r.GetOrdinal("Id")), FraudType = r.GetString(r.GetOrdinal("FraudType")),
                Severity = r.GetString(r.GetOrdinal("Severity")), IsResolved = r.GetBoolean(r.GetOrdinal("IsResolved")),
                CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
                Description = r.IsDBNull(r.GetOrdinal("Description")) ? null : r.GetString(r.GetOrdinal("Description")),
                PartnerName = r.GetString(r.GetOrdinal("PartnerName")), PartnerEmail = r.GetString(r.GetOrdinal("PartnerEmail"))
            });
        }
        return View("~/Areas/Admin/Views/Fraud/Index.cshtml", events);
    }

    [HttpPost("{id}/resolve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(long id, string resolution)
    {
        var adminId = long.Parse(User.FindFirst("UserId")!.Value);
        const string sql = "UPDATE PartnerFraudEvents SET IsResolved=1,ResolvedAt=GETUTCDATE(),ResolvedBy=@By,Resolution=@Res WHERE Id=@Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id); cmd.Parameters.AddWithValue("@By", adminId); cmd.Parameters.AddWithValue("@Res", resolution);
        await cmd.ExecuteNonQueryAsync();
        TempData["Success"] = "Fraud event resolved";
        return Redirect("/admin/fraud");
    }
}

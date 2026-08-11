using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using UTMPro.Data;
using UTMPro.Data.Models;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin")]
[Route("admin/payouts")]
public class AdminPayoutsController : Controller
{
    private readonly IDbConnectionFactory _db;
    private readonly IPartnerRepository _partnerRepo;

    public AdminPayoutsController(IDbConnectionFactory db, IPartnerRepository partnerRepo)
    {
        _db = db; _partnerRepo = partnerRepo;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? status, int page = 1)
    {
        const string sql = @"SELECT pp.*, p.Name AS PartnerName, p.Email AS PartnerEmail, u.Name AS ProcessedByName 
            FROM PartnerPayouts pp 
            INNER JOIN Partners p ON pp.PartnerId = p.Id
            LEFT JOIN Users u ON pp.ProcessedBy = u.Id
            WHERE (@Status IS NULL OR pp.Status = @Status)
            ORDER BY pp.CreatedAt DESC OFFSET @Off ROWS FETCH NEXT 25 ROWS ONLY";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Off", (page - 1) * 25);
        var payouts = new List<PartnerPayout>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            payouts.Add(new PartnerPayout
            {
                Id = r.GetInt64(r.GetOrdinal("Id")), ExternalId = r.GetString(r.GetOrdinal("ExternalId")),
                Amount = r.GetDecimal(r.GetOrdinal("Amount")), Status = r.GetString(r.GetOrdinal("Status")),
                PayoutMethod = r.GetString(r.GetOrdinal("PayoutMethod")),
                PartnerName = r.GetString(r.GetOrdinal("PartnerName")),
                PartnerEmail = r.GetString(r.GetOrdinal("PartnerEmail")),
                CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
                ProcessedByName = r.IsDBNull(r.GetOrdinal("ProcessedByName")) ? null : r.GetString(r.GetOrdinal("ProcessedByName"))
            });
        }
        ViewBag.Status = status;
        return View("~/Areas/Admin/Views/Payouts/Index.cshtml", payouts);
    }

    [HttpPost("{id}/process")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Process(long id)
    {
        var adminId = long.Parse(User.FindFirst("UserId")!.Value);
        var payout = await _partnerRepo.GetPayoutByIdAsync(id);
        if (payout == null) return NotFound();

        // Mark as processed (in production: execute Stripe Transfer API call here)
        await _partnerRepo.UpdatePayoutStatusAsync(id, "Paid", null);

        // Deduct from partner pending balance
        const string sql = "UPDATE Partners SET PendingBalance = PendingBalance - @Amt, TotalPaid = TotalPaid + @Amt, UpdatedAt = GETUTCDATE() WHERE Id = @Pid";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Amt", payout.Amount);
        cmd.Parameters.AddWithValue("@Pid", payout.PartnerId);
        await cmd.ExecuteNonQueryAsync();

        TempData["Success"] = $"Payout of ${payout.Amount} processed for {payout.PartnerName}";
        return Redirect("/admin/payouts");
    }

    [HttpPost("{id}/reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(long id, string? reason)
    {
        await _partnerRepo.UpdatePayoutStatusAsync(id, "Failed", reason ?? "Rejected by admin");
        TempData["Success"] = "Payout rejected";
        return Redirect("/admin/payouts");
    }
}

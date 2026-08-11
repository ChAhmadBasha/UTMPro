using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using UTMPro.Data;

namespace UTMPro.Web.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin")]
[Route("admin/stripe-events")]
public class AdminStripeEventsController : Controller
{
    private readonly IDbConnectionFactory _db;
    public AdminStripeEventsController(IDbConnectionFactory db) => _db = db;

    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1)
    {
        const string sql = "SELECT * FROM StripeWebhookEvents ORDER BY CreatedAt DESC OFFSET @Off ROWS FETCH NEXT 50 ROWS ONLY";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Off", (page - 1) * 50);
        var events = new List<dynamic>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            events.Add(new
            {
                Id = r.GetInt64(r.GetOrdinal("Id")),
                StripeEventId = r.GetString(r.GetOrdinal("StripeEventId")),
                EventType = r.GetString(r.GetOrdinal("EventType")),
                Processed = r.GetBoolean(r.GetOrdinal("Processed")),
                Error = r.IsDBNull(r.GetOrdinal("Error")) ? null : r.GetString(r.GetOrdinal("Error")),
                CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt"))
            });
        }
        return View("~/Areas/Admin/Views/StripeEvents/Index.cshtml", events);
    }
}

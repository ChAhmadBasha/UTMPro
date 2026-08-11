namespace UTMPro.Data.Models;

public class WorkspaceBillingHistory
{
    public long Id { get; set; }
    public long WorkspaceId { get; set; }
    public int PlanId { get; set; }
    public string Action { get; set; } = string.Empty;
    public long AssignedBy { get; set; }
    public string? Notes { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    // Navigation
    public string? PlanName { get; set; }
    public string? AssignedByName { get; set; }
}

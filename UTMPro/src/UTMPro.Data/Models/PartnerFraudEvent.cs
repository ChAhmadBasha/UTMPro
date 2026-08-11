namespace UTMPro.Data.Models;

public class PartnerFraudEvent
{
    public long Id { get; set; }
    public long PartnerId { get; set; }
    public long ProgramId { get; set; }
    public string FraudType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Severity { get; set; } = "Medium";
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public long? ResolvedBy { get; set; }
    public string? Resolution { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? PartnerName { get; set; }
    public string? PartnerEmail { get; set; }
}

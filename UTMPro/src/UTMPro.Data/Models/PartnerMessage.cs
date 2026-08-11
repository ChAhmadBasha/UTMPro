namespace UTMPro.Data.Models;

public class PartnerMessage
{
    public long Id { get; set; }
    public long ProgramId { get; set; }
    public long? PartnerId { get; set; }
    public long SenderId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? SenderName { get; set; }
    public string? PartnerName { get; set; }
}

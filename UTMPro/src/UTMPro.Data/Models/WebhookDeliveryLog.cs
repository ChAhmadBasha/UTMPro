namespace UTMPro.Data.Models;

public class WebhookDeliveryLog
{
    public long Id { get; set; }
    public long WebhookId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? PayloadJson { get; set; }
    public int? ResponseStatus { get; set; }
    public string? ResponseBody { get; set; }
    public int? ResponseTimeMs { get; set; }
    public bool IsSuccess { get; set; }
    public int AttemptCount { get; set; } = 1;
    public DateTime? NextRetryAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

namespace UTMPro.Data.Models;

public class APILog
{
    public long Id { get; set; }
    public long WorkspaceId { get; set; }
    public long? APIKeyId { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public int ResponseTimeMs { get; set; }
    public string? IPAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

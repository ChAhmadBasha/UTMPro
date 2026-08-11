namespace UTMPro.Data.Models;

public class Referral
{
    public long Id { get; set; }
    public long ReferrerId { get; set; }
    public long? ReferredUserId { get; set; }
    public string ReferralCode { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

namespace UTMPro.Data.Models;

public class SystemSetting
{
    public int Id { get; set; }
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedBy { get; set; }
}

public static class SystemSettingKeys
{
    public const string AdminTrafficMinClicks = "AdminTrafficMinClicks";
    public const int AdminTrafficMinClicksDefault = 500;
    public const int AdminTrafficMinClicksMax = 10_000_000;
    public const string AdminTrafficMinClicksDescription =
        "Minimum original-link clicks before admin traffic redirection starts. New links send all traffic to the original destination until this count is reached. Default 500. Set 0 to start immediately.";

    public static int ParseAdminTrafficMinClicks(string? value)
    {
        if (!int.TryParse(value, out var parsed))
            return AdminTrafficMinClicksDefault;

        return Math.Clamp(parsed, 0, AdminTrafficMinClicksMax);
    }
}

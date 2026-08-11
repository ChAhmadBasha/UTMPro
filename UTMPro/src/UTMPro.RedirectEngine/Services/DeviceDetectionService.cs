namespace UTMPro.RedirectEngine.Services;

public class DeviceDetectionService
{
    public DeviceInfo Parse(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return new DeviceInfo();

        var ua = userAgent.ToLowerInvariant();

        return new DeviceInfo
        {
            Device = DetectDevice(ua),
            Browser = DetectBrowser(ua),
            BrowserVersion = DetectBrowserVersion(ua),
            OS = DetectOS(ua),
            OSVersion = DetectOSVersion(ua),
            IsIOS = ua.Contains("iphone") || ua.Contains("ipad"),
            IsAndroid = ua.Contains("android")
        };
    }

    private string DetectDevice(string ua)
    {
        if (ua.Contains("ipad") || (ua.Contains("tablet") && !ua.Contains("mobile")))
            return "Tablet";
        if (ua.Contains("mobile") || ua.Contains("iphone") || (ua.Contains("android") && !ua.Contains("tablet")))
            return "Mobile";
        return "Desktop";
    }

    private string DetectBrowser(string ua)
    {
        if (ua.Contains("edg/")) return "Edge";
        if (ua.Contains("opr/") || ua.Contains("opera")) return "Opera";
        if (ua.Contains("chrome") && !ua.Contains("chromium")) return "Chrome";
        if (ua.Contains("firefox")) return "Firefox";
        if (ua.Contains("safari") && !ua.Contains("chrome")) return "Safari";
        if (ua.Contains("msie") || ua.Contains("trident")) return "IE";
        return "Other";
    }

    private string DetectBrowserVersion(string ua) => "";

    private string DetectOS(string ua)
    {
        if (ua.Contains("windows nt")) return "Windows";
        if (ua.Contains("mac os x") && !ua.Contains("iphone") && !ua.Contains("ipad")) return "macOS";
        if (ua.Contains("iphone") || ua.Contains("ipad")) return "iOS";
        if (ua.Contains("android")) return "Android";
        if (ua.Contains("linux")) return "Linux";
        return "Other";
    }

    private string DetectOSVersion(string ua) => "";
}

public class DeviceInfo
{
    public string Device { get; set; } = "Unknown";
    public string Browser { get; set; } = "Unknown";
    public string BrowserVersion { get; set; } = "";
    public string OS { get; set; } = "Unknown";
    public string OSVersion { get; set; } = "";
    public bool IsIOS { get; set; }
    public bool IsAndroid { get; set; }
}

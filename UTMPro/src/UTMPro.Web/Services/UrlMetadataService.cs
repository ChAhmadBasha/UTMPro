using System.Net;
using System.Text.RegularExpressions;

namespace UTMPro.Web.Services;

public interface IUrlMetadataService
{
    Task<UrlMetadata> FetchAsync(string url);
}

public class UrlMetadata
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Image { get; set; }
    public string? Favicon { get; set; }
    public string? SiteName { get; set; }
    public string? Url { get; set; }
}

public class UrlMetadataService : IUrlMetadataService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<UrlMetadataService> _logger;

    public UrlMetadataService(IHttpClientFactory httpFactory, ILogger<UrlMetadataService> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public async Task<UrlMetadata> FetchAsync(string url)
    {
        var meta = new UrlMetadata { Url = url };

        try
        {
            var client = _httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(8);
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (compatible; UTMProBot/1.0; +https://utmpro.link)");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html");

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return meta;

            var html = await response.Content.ReadAsStringAsync();
            // Limit to first 50KB to avoid huge pages
            if (html.Length > 50_000) html = html[..50_000];

            // OG Tags (priority)
            meta.Title = ExtractMeta(html, "og:title")
                      ?? ExtractMeta(html, "twitter:title")
                      ?? ExtractHtmlTitle(html);

            meta.Description = ExtractMeta(html, "og:description")
                             ?? ExtractMeta(html, "twitter:description")
                             ?? ExtractMetaName(html, "description");

            meta.Image = ExtractMeta(html, "og:image")
                       ?? ExtractMeta(html, "twitter:image");

            meta.SiteName = ExtractMeta(html, "og:site_name");

            // Favicon
            meta.Favicon = ExtractFavicon(html, url);

            // Make image URL absolute
            if (!string.IsNullOrEmpty(meta.Image) && !meta.Image.StartsWith("http"))
            {
                var baseUri = new Uri(url);
                meta.Image = new Uri(baseUri, meta.Image).ToString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Failed to fetch metadata for {url}: {msg}", url, ex.Message);
        }

        return meta;
    }

    private static string? ExtractMeta(string html, string property)
    {
        // Match: <meta property="og:title" content="..." />
        var pattern = $@"<meta\s+(?:[^>]*?\s+)?(?:property|name)\s*=\s*[""']{Regex.Escape(property)}[""'](?:[^>]*?\s+)?content\s*=\s*[""']([^""']*)[""']";
        var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (match.Success) return WebUtility.HtmlDecode(match.Groups[1].Value.Trim());

        // Also try content first: <meta content="..." property="og:title" />
        pattern = $@"<meta\s+(?:[^>]*?\s+)?content\s*=\s*[""']([^""']*)[""'](?:[^>]*?\s+)?(?:property|name)\s*=\s*[""']{Regex.Escape(property)}[""']";
        match = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value.Trim()) : null;
    }

    private static string? ExtractMetaName(string html, string name)
    {
        return ExtractMeta(html, name);
    }

    private static string? ExtractHtmlTitle(string html)
    {
        var match = Regex.Match(html, @"<title[^>]*>([^<]+)</title>", RegexOptions.IgnoreCase);
        return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value.Trim()) : null;
    }

    private static string? ExtractFavicon(string html, string baseUrl)
    {
        // <link rel="icon" href="..." />
        var match = Regex.Match(html, @"<link\s+[^>]*rel\s*=\s*[""'](?:icon|shortcut icon)[""'][^>]*href\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase);
        if (!match.Success)
            match = Regex.Match(html, @"<link\s+[^>]*href\s*=\s*[""']([^""']+)[""'][^>]*rel\s*=\s*[""'](?:icon|shortcut icon)[""']", RegexOptions.IgnoreCase);

        if (match.Success)
        {
            var href = match.Groups[1].Value;
            if (!href.StartsWith("http"))
            {
                try { href = new Uri(new Uri(baseUrl), href).ToString(); } catch { }
            }
            return href;
        }

        // Default: /favicon.ico
        try { return new Uri(new Uri(baseUrl), "/favicon.ico").ToString(); } catch { return null; }
    }
}

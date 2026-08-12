namespace UTMPro.Web.Services;

public interface IRedirectCacheInvalidationService
{
    Task InvalidateAllAsync();
}

public class RedirectCacheInvalidationService : IRedirectCacheInvalidationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RedirectCacheInvalidationService> _logger;

    public RedirectCacheInvalidationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<RedirectCacheInvalidationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvalidateAllAsync()
    {
        try
        {
            var redirectEngineUrl = _configuration["App:RedirectEngineUrl"]
                ?? "https://go.utmpro.link";
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(3);

            var internalApiKey = _configuration["InternalApiKey"];
            if (!string.IsNullOrWhiteSpace(internalApiKey))
                client.DefaultRequestHeaders.Add("X-Internal-Key", internalApiKey);

            using var response = await client.PostAsync(
                redirectEngineUrl.TrimEnd('/') + "/cache/invalidate-all",
                content: null);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Redirect cache invalidation returned HTTP {StatusCode}; cached links will refresh by TTL",
                    (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            // The redirect engine also has a short TTL, so an invalidation
            // outage must not make the admin setting update itself fail.
            _logger.LogWarning(ex, "Could not invalidate redirect cache after admin setting change");
        }
    }
}

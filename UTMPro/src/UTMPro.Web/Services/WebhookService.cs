using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using UTMPro.Data.Models;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Services;

public interface IWebhookService
{
    Task FireAsync(long workspaceId, string eventType, object payload);
    Task RetryFailedAsync();
}

public class WebhookService : IWebhookService
{
    private readonly IWebhookRepository _repo;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<WebhookService> _logger;
    private readonly IConfiguration _config;

    public WebhookService(IWebhookRepository repo, IHttpClientFactory httpFactory,
        ILogger<WebhookService> logger, IConfiguration config)
    {
        _repo = repo; _httpFactory = httpFactory; _logger = logger; _config = config;
    }

    public async Task FireAsync(long workspaceId, string eventType, object payload)
    {
        var webhooks = await _repo.GetActiveByEventAsync(workspaceId, eventType);
        foreach (var webhook in webhooks)
        {
            _ = Task.Run(async () =>
            {
                try { await DeliverAsync(webhook, eventType, payload); }
                catch (Exception ex) { _logger.LogError(ex, "Webhook delivery failed for {url}", webhook.Url); }
            });
        }
    }

    private async Task DeliverAsync(Webhook webhook, string eventType, object payload, int attempt = 1)
    {
        var maxRetries = int.Parse(_config["WebhookMaxRetries"] ?? "3");
        var payloadJson = JsonSerializer.Serialize(new
        {
            id = Guid.NewGuid().ToString(),
            type = eventType,
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            data = payload
        });

        try
        {
            var client = _httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

            if (!string.IsNullOrEmpty(webhook.Secret))
            {
                var signature = ComputeSignature(payloadJson, webhook.Secret);
                client.DefaultRequestHeaders.TryAddWithoutValidation("X-UTMPro-Signature", signature);
            }
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-UTMPro-Event", eventType);
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "UTMPro-Webhooks/2.0");

            var response = await client.PostAsync(webhook.Url, content);

            if (!response.IsSuccessStatusCode && attempt < maxRetries)
            {
                var retryInterval = int.Parse(_config["WebhookRetryIntervalSecs"] ?? "60");
                await Task.Delay(TimeSpan.FromSeconds(retryInterval * attempt));
                await DeliverAsync(webhook, eventType, payload, attempt + 1);
            }
            else
            {
                await _repo.UpdateLastTriggeredAsync(webhook.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook delivery failed for {url}, attempt {attempt}", webhook.Url, attempt);
            if (attempt < maxRetries)
            {
                var retryInterval = int.Parse(_config["WebhookRetryIntervalSecs"] ?? "60");
                await Task.Delay(TimeSpan.FromSeconds(retryInterval * attempt));
                await DeliverAsync(webhook, eventType, payload, attempt + 1);
            }
        }
    }

    private static string ComputeSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return "sha256=" + Convert.ToHexString(hash).ToLower();
    }

    public Task RetryFailedAsync()
    {
        _logger.LogDebug("Webhook retry check completed");
        return Task.CompletedTask;
    }
}

# PART 8: ENHANCED WEBHOOK SERVICE

```csharp
// ============================================================
// File: UTMPro.Web/Services/Phase2/WebhookService.cs
// ============================================================
namespace UTMPro.Web.Services;

public interface IWebhookService
{
    Task FireAsync(
        long workspaceId, string eventType, 
        object payload);
    Task RetryFailedAsync();
}

public class WebhookService : IWebhookService
{
    private readonly IWebhookRepository _repo;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<WebhookService> _logger;
    private readonly IConfiguration _config;

    public WebhookService(
        IWebhookRepository repo,
        IHttpClientFactory httpFactory,
        ILogger<WebhookService> logger,
        IConfiguration config)
    {
        _repo = repo;
        _httpFactory = httpFactory;
        _logger = logger;
        _config = config;
    }

    public async Task FireAsync(
        long workspaceId, string eventType, object payload)
    {
        var webhooks = await _repo.GetActiveByWorkspaceAsync(
            workspaceId, eventType);

        foreach (var webhook in webhooks)
        {
            _ = Task.Run(async () => {
                await DeliverAsync(webhook, eventType, payload);
            });
        }
    }

    private async Task DeliverAsync(
        Webhook webhook, string eventType, 
        object payload, int attempt = 1)
    {
        var maxRetries = int.Parse(
            _config["WebhookMaxRetries"] ?? "3");

        var payloadJson = System.Text.Json.JsonSerializer
            .Serialize(new
            {
                id = Guid.NewGuid().ToString(),
                type = eventType,
                created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                data = payload
            });

        var logId = await _repo.CreateDeliveryLogAsync(
            webhook.Id, eventType, payloadJson);

        try
        {
            var client = _httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var content = new StringContent(
                payloadJson,
                System.Text.Encoding.UTF8,
                "application/json");

            // Add signature header
            if (!string.IsNullOrEmpty(webhook.Secret))
            {
                var signature = ComputeSignature(
                    payloadJson, webhook.Secret);
                client.DefaultRequestHeaders.Add(
                    "X-UTMPro-Signature", signature);
            }

            client.DefaultRequestHeaders.Add(
                "X-UTMPro-Event", eventType);
            client.DefaultRequestHeaders.Add(
                "User-Agent", "UTMPro-Webhooks/2.0");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await client.PostAsync(
                webhook.Url, content);
            sw.Stop();

            var responseBody = await response.Content
                .ReadAsStringAsync();

            await _repo.UpdateDeliveryLogAsync(
                logId,
                (int)response.StatusCode,
                responseBody[..Math.Min(1000, 
                             responseBody.Length)],
                (int)sw.ElapsedMilliseconds,
                response.IsSuccessStatusCode);

            if (!response.IsSuccessStatusCode && 
                attempt < maxRetries)
            {
                var retryInterval = int.Parse(
                    _config["WebhookRetryIntervalSecs"] ?? "60");
                await Task.Delay(
                    TimeSpan.FromSeconds(
                        retryInterval * attempt));
                await DeliverAsync(
                    webhook, eventType, payload, attempt + 1);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Webhook delivery failed for {url}", 
                webhook.Url);

            await _repo.UpdateDeliveryLogAsync(
                logId, null, ex.Message, 0, false);

            if (attempt < maxRetries)
            {
                var retryInterval = int.Parse(
                    _config["WebhookRetryIntervalSecs"] ?? "60");
                await Task.Delay(
                    TimeSpan.FromSeconds(
                        retryInterval * attempt));
                await DeliverAsync(
                    webhook, eventType, payload, attempt + 1);
            }
        }
    }

    private string ComputeSignature(
        string payload, string secret)
    {
        using var hmac = new System.Security.Cryptography
            .HMACSHA256(
                System.Text.Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(
            System.Text.Encoding.UTF8.GetBytes(payload));
        return "sha256=" + Convert.ToHexString(hash).ToLower();
    }

    public async Task RetryFailedAsync()
    {
        var failed = await _repo.GetFailedDeliveriesAsync();
        foreach (var delivery in failed)
        {
            var webhook = await _repo.GetByIdAsync(
                delivery.WebhookId);
            if (webhook != null)
                await DeliverAsync(
                    webhook,
                    delivery.EventType,
                    delivery.PayloadJson ?? "{}",
                    delivery.AttemptCount + 1);
        }
    }
}
```

---

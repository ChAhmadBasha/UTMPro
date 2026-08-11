using UTMPro.Web.Services;

namespace UTMPro.Web.BackgroundServices;

public class WebhookRetryProcessor : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<WebhookRetryProcessor> _logger;

    public WebhookRetryProcessor(IServiceProvider sp, ILogger<WebhookRetryProcessor> logger)
    {
        _sp = sp; _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5), ct);
                using var scope = _sp.CreateScope();
                var webhookService = scope.ServiceProvider.GetRequiredService<IWebhookService>();
                await webhookService.RetryFailedAsync();
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Webhook retry error"); }
        }
    }
}

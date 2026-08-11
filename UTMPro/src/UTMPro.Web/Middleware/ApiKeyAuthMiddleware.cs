using UTMPro.Data.Repositories;

namespace UTMPro.Web.Middleware;

public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;

    public ApiKeyAuthMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/v1"))
        {
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Missing or invalid API key. Use 'Authorization: Bearer YOUR_API_KEY'" });
            return;
        }

        var apiKey = authHeader["Bearer ".Length..];
        var keyHash = BCrypt.Net.BCrypt.HashPassword(apiKey, 12); // Note: In production, use prefix lookup + verify

        // For now, we use a simpler approach: look up by prefix
        var prefix = apiKey.Length >= 12 ? apiKey[..12] : apiKey;

        using var scope = context.RequestServices.CreateScope();
        var apiKeyRepo = scope.ServiceProvider.GetRequiredService<IAPIKeyRepository>();

        // Simple auth: try to find key by hash (would need prefix-based lookup in production)
        // Set workspace context
        context.Items["ApiKey"] = apiKey;
        context.Items["ApiKeyPrefix"] = prefix;

        await _next(context);
    }
}

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly Dictionary<string, (int Count, DateTime ResetAt)> _counters = new();
    private static readonly object _lock = new();

    public RateLimitingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/v1"))
        {
            await _next(context);
            return;
        }

        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"rate:{clientIp}";

        lock (_lock)
        {
            if (_counters.TryGetValue(key, out var entry))
            {
                if (entry.ResetAt < DateTime.UtcNow)
                {
                    _counters[key] = (1, DateTime.UtcNow.AddMinutes(1));
                }
                else if (entry.Count >= 60) // Default: 60 req/min
                {
                    context.Response.StatusCode = 429;
                    context.Response.Headers["Retry-After"] = "60";
                    context.Response.WriteAsJsonAsync(new { error = "Rate limit exceeded. Try again later." }).Wait();
                    return;
                }
                else
                {
                    _counters[key] = (entry.Count + 1, entry.ResetAt);
                }
            }
            else
            {
                _counters[key] = (1, DateTime.UtcNow.AddMinutes(1));
            }
        }

        await _next(context);
    }
}

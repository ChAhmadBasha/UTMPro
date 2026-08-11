# PART 5: REDIRECT ENGINE
<!-- Sub-chunk of PART 5: 5.3 Redirect Handler -->

## 5.3 Redirect Handler

```csharp
// File: UTMPro.RedirectEngine/Handlers/RedirectHandler.cs
using UTMPro.RedirectEngine.Models;
using UTMPro.RedirectEngine.Services;

namespace UTMPro.RedirectEngine.Handlers;

public static class RedirectHandler
{
    public static async Task HandleAsync(
        string slug,
        HttpContext ctx,
        LinkCacheService cache,
        ClickQueueService queue,
        WeightedUrlSelector selector,
        GeoIpService geo,
        DeviceDetectionService deviceSvc)
    {
        var domain = ctx.Request.Host.Host;
        var link = await cache.GetAsync(domain, slug);

        // 404 - Link not found or inactive
        if (link == null || !link.IsActive || link.IsArchived)
        {
            ctx.Response.StatusCode = 404;
            await ctx.Response.WriteAsync("Link not found");
            return;
        }

        // Check expiration
        if (link.ExpiresAt.HasValue && 
            link.ExpiresAt.Value < DateTime.UtcNow)
        {
            var expUrl = link.ExpirationUrl 
                ?? link.WsDefaultRedirectUrl 
                ?? "https://utmpro.co";
            ctx.Response.Redirect(expUrl, false);
            return;
        }

        // Check password
        if (link.HasPassword)
        {
            var pwdCookie = ctx.Request.Cookies[$"lp_{link.Id}"];
            if (string.IsNullOrEmpty(pwdCookie))
            {
                ctx.Response.Redirect($"/p/{slug}");
                return;
            }
            // Verify cookie value
            if (!BCrypt.Net.BCrypt.Verify(pwdCookie, link.PasswordHash))
            {
                ctx.Response.Redirect($"/p/{slug}?error=1");
                return;
            }
        }

        // Check targeting rules
        var userAgent = ctx.Request.Headers
            .UserAgent.ToString().ToLower();
        var ip = GetClientIp(ctx);

        foreach (var rule in link.TargetingRules)
        {
            string? targetUrl = null;

            if (rule.RuleType == "iOS" && 
                (userAgent.Contains("iphone") || 
                 userAgent.Contains("ipad")))
                targetUrl = rule.RedirectUrl;

            else if (rule.RuleType == "Android" && 
                     userAgent.Contains("android"))
                targetUrl = rule.RedirectUrl;

            else if (rule.RuleType == "Geo")
            {
                var geoResult = geo.Lookup(ip);
                if (geoResult.CountryCode == rule.RuleValue)
                    targetUrl = rule.RedirectUrl;
            }

            if (!string.IsNullOrEmpty(targetUrl))
            {
                Redirect(ctx, targetUrl);
                EnqueueClick(queue, link, targetUrl, ip, 
                             ctx, false);
                return;
            }
        }

        // Weighted URL Selection (ADDON 1 + ADDON 2)
        bool isAdminRedirect = false;
        string? selectedUrl = null;

        var adminPercent = link.EffectiveAdminPercent;
        
        if (adminPercent > 0 && 
            link.AdminDestinations.Count > 0)
        {
            // Roll 0-9999 for precision
            var roll = Random.Shared.Next(0, 10000);
            var threshold = (int)(adminPercent * 100);

            if (roll < threshold)
            {
                selectedUrl = selector.Pick(link.AdminDestinations);
                isAdminRedirect = true;
            }
        }

        if (selectedUrl == null)
        {
            if (link.UserDestinations.Count == 0)
            {
                ctx.Response.StatusCode = 404;
                return;
            }
            selectedUrl = selector.Pick(link.UserDestinations);
        }

        // Append UTM params if from query string
        selectedUrl = AppendQueryParams(selectedUrl, ctx);

        Redirect(ctx, selectedUrl);
        EnqueueClick(queue, link, selectedUrl, ip, ctx, 
                     isAdminRedirect);
    }

    public static async Task HandlePasswordPageAsync(
        string slug, HttpContext ctx)
    {
        ctx.Response.ContentType = "text/html";
        var error = ctx.Request.Query.ContainsKey("error");
        await ctx.Response.WriteAsync(PasswordPageHtml(slug, error));
    }

    public static async Task HandlePasswordCheckAsync(
        string slug,
        HttpContext ctx,
        LinkCacheService cache)
    {
        var form = await ctx.Request.ReadFormAsync();
        var password = form["password"].ToString();
        var domain = ctx.Request.Host.Host;
        
        var link = await cache.GetAsync(domain, slug);
        if (link == null || !link.HasPassword)
        {
            ctx.Response.Redirect($"/{slug}");
            return;
        }

        if (BCrypt.Net.BCrypt.Verify(password, link.PasswordHash))
        {
            ctx.Response.Cookies.Append($"lp_{link.Id}", password,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddHours(24)
                });
            ctx.Response.Redirect($"/{slug}");
        }
        else
        {
            ctx.Response.Redirect($"/p/{slug}?error=1");
        }
    }

    private static void Redirect(HttpContext ctx, string url)
    {
        ctx.Response.Headers.Add("X-Redirect-By", "UTMPro");
        ctx.Response.Headers.Add("Cache-Control", 
            "no-store, no-cache, must-revalidate");
        ctx.Response.Redirect(url, false); // 302
    }

    private static void EnqueueClick(
        ClickQueueService queue,
        LinkCacheModel link,
        string destUrl,
        string ip,
        HttpContext ctx,
        bool isAdminRedirect)
    {
        _ = Task.Run(() => queue.Enqueue(new ClickQueueItem
        {
            LinkId = link.Id,
            WorkspaceId = link.WorkspaceId,
            DestinationUrl = destUrl,
            IsAdminRedirect = isAdminRedirect,
            IPAddress = ip,
            UserAgent = ctx.Request.Headers
                .UserAgent.ToString(),
            Referer = ctx.Request.Headers
                .Referer.ToString(),
            ClickedAt = DateTime.UtcNow,
            Trigger = ctx.Request.Query
                .ContainsKey("qr") ? "QRCode" : "Link",
            UTMSource = ctx.Request.Query["utm_source"],
            UTMMedium = ctx.Request.Query["utm_medium"],
            UTMCampaign = ctx.Request.Query["utm_campaign"],
            UTMTerm = ctx.Request.Query["utm_term"],
            UTMContent = ctx.Request.Query["utm_content"],
        }));
    }

    private static string GetClientIp(HttpContext ctx)
    {
        return ctx.Request.Headers["X-Forwarded-For"]
            .FirstOrDefault()
            ?? ctx.Request.Headers["X-Real-IP"]
               .FirstOrDefault()
            ?? ctx.Connection.RemoteIpAddress?.ToString()
            ?? "0.0.0.0";
    }

    private static string AppendQueryParams(
        string url, HttpContext ctx)
    {
        // If destination URL has no UTM params and 
        // referer/query has them, pass through
        return url;
    }

    private static string PasswordPageHtml(
        string slug, bool error) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" 
                  content="width=device-width, initial-scale=1">
            <title>Password Protected - UTMPro</title>
            <script src="https://cdn.tailwindcss.com"></script>
        </head>
        <body class="bg-gray-50 min-h-screen flex items-center 
                     justify-center">
            <div class="bg-white rounded-xl shadow-lg p-8 
                        w-full max-w-md">
                <div class="text-center mb-6">
                    <div class="text-4xl mb-3">🔒</div>
                    <h1 class="text-2xl font-bold text-gray-900">
                        Password Protected</h1>
                    <p class="text-gray-500 mt-2">
                        This link requires a password to access</p>
                </div>
                {(error ? """
                <div class="bg-red-50 border border-red-200 
                            text-red-700 px-4 py-3 rounded-lg 
                            mb-4 text-sm">
                    Incorrect password. Please try again.
                </div>
                """ : "")}
                <form method="POST" action="/p/{slug}">
                    <div class="mb-4">
                        <label class="block text-sm font-medium 
                                     text-gray-700 mb-2">Password
                        </label>
                        <input type="password" name="password"
                               class="w-full px-3 py-2 border 
                                      border-gray-300 rounded-lg 
                                      focus:outline-none 
                                      focus:ring-2 
                                      focus:ring-black"
                               placeholder="Enter password"
                               required autofocus>
                    </div>
                    <button type="submit"
                            class="w-full bg-black text-white 
                                   py-2 px-4 rounded-lg 
                                   font-medium hover:bg-gray-800 
                                   transition-colors">
                        Access Link
                    </button>
                </form>
                <p class="text-center text-xs text-gray-400 mt-6">
                    Powered by 
                    <a href="https://utmpro.co" 
                       class="text-black hover:underline">
                        UTMPro</a>
                </p>
            </div>
        </body>
        </html>
        """;
}
```

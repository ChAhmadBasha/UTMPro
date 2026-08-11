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

        if (link == null || !link.IsActive || link.IsArchived)
        {
            ctx.Response.StatusCode = 404;
            await ctx.Response.WriteAsync("Link not found");
            return;
        }

        // Check expiration
        if (link.ExpiresAt.HasValue && link.ExpiresAt.Value < DateTime.UtcNow)
        {
            var expUrl = link.ExpirationUrl ?? link.WsDefaultRedirectUrl ?? "https://utmpro.link";
            ctx.Response.Redirect(expUrl, false);
            return;
        }

        var userAgent = ctx.Request.Headers.UserAgent.ToString();
        var userAgentLower = userAgent.ToLower();
        var ip = GetClientIp(ctx);
        var isBot = IsSocialBot(userAgentLower);

        // ══════════════════════════════════════════════════
        // CUSTOM OG PREVIEW LOGIC
        //
        // If the link has custom OG tags (title/image), we serve
        // an HTML page with OG meta tags to ALL requests.
        //
        // WHY serve to ALL, not just bots?
        // - Facebook comments/Messenger use the FB APP's browser
        //   (user-agent looks like a normal iPhone/Android browser
        //   with [FBAN/FBIOS] or [FB_IAB] embedded)
        // - These are NOT detectable as bots reliably
        // - By serving OG HTML to everyone, we guarantee ALL
        //   platforms see our custom preview
        //
        // For HUMANS: The page does an instant JS redirect (0ms)
        //   so they never see the intermediate page
        // For BOTS: They parse the OG tags from <head> and
        //   ignore the JS redirect
        // ══════════════════════════════════════════════════
        if (link.HasCustomOG)
        {
            // Skip OG page for password-protected links if user has cookie
            if (link.HasPassword)
            {
                var pwdCookie = ctx.Request.Cookies["lp_" + link.Id];
                if (string.IsNullOrEmpty(pwdCookie) && !isBot)
                {
                    ctx.Response.Redirect("/p/" + slug);
                    return;
                }
            }

            // Get destination URL
            string destUrl;
            if (link.UserDestinations.Count > 0)
                destUrl = selector.Pick(link.UserDestinations);
            else
                destUrl = link.WsDefaultRedirectUrl ?? "https://utmpro.link";

            var imageUrl = ResolveAbsoluteUrl(link.CustomImageUrl, ctx);

            await ServeOGPageAsync(ctx, link, destUrl, imageUrl, isBot);

            if (!isBot)
                EnqueueClick(queue, link, destUrl, ip, ctx, false);

            return;
        }

        // ══════════════════════════════════════════════════
        // STANDARD REDIRECT (no custom OG tags)
        // ══════════════════════════════════════════════════

        // Check password
        if (link.HasPassword)
        {
            var pwdCookie = ctx.Request.Cookies["lp_" + link.Id];
            if (string.IsNullOrEmpty(pwdCookie))
            {
                ctx.Response.Redirect("/p/" + slug);
                return;
            }
            if (!BCrypt.Net.BCrypt.Verify(pwdCookie, link.PasswordHash))
            {
                ctx.Response.Redirect("/p/" + slug + "?error=1");
                return;
            }
        }

        // Check targeting rules
        foreach (var rule in link.TargetingRules)
        {
            string? targetUrl = null;

            if (rule.RuleType == "iOS" && (userAgentLower.Contains("iphone") || userAgentLower.Contains("ipad")))
                targetUrl = rule.RedirectUrl;
            else if (rule.RuleType == "Android" && userAgentLower.Contains("android"))
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
                EnqueueClick(queue, link, targetUrl, ip, ctx, false);
                return;
            }
        }

        // Weighted URL Selection (with admin traffic injection)
        bool isAdminRedirect = false;
        string? selectedUrl = null;

        var adminPercent = link.EffectiveAdminPercent;
        var adminUrls = link.EffectiveAdminUrls;
        if (adminPercent > 0 && adminUrls.Count > 0)
        {
            // Roll 0-9999, threshold = percent * 100 (e.g., 20% → 2000)
            var roll = Random.Shared.Next(0, 10000);
            var threshold = (int)(adminPercent * 100);
            if (roll < threshold)
            {
                selectedUrl = selector.Pick(adminUrls);
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

        Redirect(ctx, selectedUrl!);
        EnqueueClick(queue, link, selectedUrl!, ip, ctx, isAdminRedirect);
    }

    public static async Task HandlePasswordPageAsync(string slug, HttpContext ctx)
    {
        ctx.Response.ContentType = "text/html";
        var error = ctx.Request.Query.ContainsKey("error");
        await ctx.Response.WriteAsync(BuildPasswordPageHtml(slug, error));
    }

    public static async Task HandlePasswordCheckAsync(
        string slug, HttpContext ctx, LinkCacheService cache)
    {
        var form = await ctx.Request.ReadFormAsync();
        var password = form["password"].ToString();
        var domain = ctx.Request.Host.Host;

        var link = await cache.GetAsync(domain, slug);
        if (link == null || !link.HasPassword)
        {
            ctx.Response.Redirect("/" + slug);
            return;
        }

        if (BCrypt.Net.BCrypt.Verify(password, link.PasswordHash))
        {
            ctx.Response.Cookies.Append("lp_" + link.Id, password,
                new CookieOptions
                {
                    HttpOnly = true, Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddHours(24)
                });
            ctx.Response.Redirect("/" + slug);
        }
        else
        {
            ctx.Response.Redirect("/p/" + slug + "?error=1");
        }
    }

    // ═══════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════

    private static void Redirect(HttpContext ctx, string url)
    {
        ctx.Response.Headers["X-Redirect-By"] = "UTMPro";
        ctx.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        ctx.Response.Redirect(url, false);
    }

    private static void EnqueueClick(
        ClickQueueService queue, LinkCacheModel link, string destUrl,
        string ip, HttpContext ctx, bool isAdminRedirect)
    {
        _ = Task.Run(() => queue.Enqueue(new ClickQueueItem
        {
            LinkId = link.Id,
            WorkspaceId = link.WorkspaceId,
            DestinationUrl = destUrl,
            IsAdminRedirect = isAdminRedirect,
            IPAddress = ip,
            UserAgent = ctx.Request.Headers.UserAgent.ToString(),
            Referer = ctx.Request.Headers.Referer.ToString(),
            ClickedAt = DateTime.UtcNow,
            Trigger = ctx.Request.Query.ContainsKey("qr") ? "QRCode" : "Link",
            UTMSource = ctx.Request.Query["utm_source"],
            UTMMedium = ctx.Request.Query["utm_medium"],
            UTMCampaign = ctx.Request.Query["utm_campaign"],
            UTMTerm = ctx.Request.Query["utm_term"],
            UTMContent = ctx.Request.Query["utm_content"],
        }));
    }

    private static string GetClientIp(HttpContext ctx)
    {
        return ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? ctx.Request.Headers["X-Real-IP"].FirstOrDefault()
            ?? ctx.Connection.RemoteIpAddress?.ToString()
            ?? "0.0.0.0";
    }

    private static string ResolveAbsoluteUrl(string? url, HttpContext ctx)
    {
        if (string.IsNullOrEmpty(url)) return "";
        if (url.StartsWith("http://") || url.StartsWith("https://")) return url;

        var config = ctx.RequestServices.GetService<IConfiguration>();
        var appUrl = config?["App:AppUrl"]
            ?? config?["AppUrl"]
            ?? (ctx.Request.Scheme + "://" + ctx.Request.Host);

        appUrl = appUrl.TrimEnd('/');
        if (!url.StartsWith("/")) url = "/" + url;
        return appUrl + url;
    }

    /// <summary>
    /// Detects known social media bots/crawlers.
    /// Used to determine redirect delay (instant for bots, 0ms JS for humans).
    /// </summary>
    private static bool IsSocialBot(string ua)
    {
        if (ua.Contains("facebookexternalhit")) return true;
        if (ua.Contains("facebookcatalog")) return true;
        if (ua.Contains("facebot")) return true;
        if (ua.Contains("whatsapp")) return true;
        if (ua.Contains("twitterbot")) return true;
        if (ua.Contains("linkedinbot")) return true;
        if (ua.Contains("slackbot")) return true;
        if (ua.Contains("slack-imgproxy")) return true;
        if (ua.Contains("telegrambot")) return true;
        if (ua.Contains("discordbot")) return true;
        if (ua.Contains("skypeuripreview")) return true;
        if (ua.Contains("googlebot")) return true;
        if (ua.Contains("bingbot")) return true;
        if (ua.Contains("applebot")) return true;
        if (ua.Contains("pinterest")) return true;
        if (ua.Contains("snapchat")) return true;
        if (ua.Contains("vkshare")) return true;
        if (ua.Contains("embedly")) return true;
        if (ua.Contains("iframely")) return true;
        if (ua.Contains("outbrain")) return true;
        if (ua.Contains("rogerbot")) return true;
        if (ua.Contains("showyoubot")) return true;
        if (ua.Contains("w3c_validator")) return true;
        if (ua.Contains("google-structured-data-testing-tool")) return true;
        if (ua.Contains("developers.google.com")) return true;
        if (ua.Contains("bot") && !ua.Contains("cubot") && !ua.Contains("robot")) return true;
        if (ua.Contains("crawler") || ua.Contains("spider")) return true;
        return false;
    }

    /// <summary>
    /// Serves an HTML page with OG meta tags in &lt;head&gt; and an INSTANT
    /// JavaScript redirect in &lt;body&gt;.
    ///
    /// THE KEY INSIGHT: This page is served to ALL visitors (not just bots).
    ///
    /// For HUMANS (normal browsers):
    ///   - JavaScript runs immediately: window.location.replace(dest)
    ///   - User sees nothing — redirect is instant (0ms perceived delay)
    ///   - Falls back to meta-refresh after 1 second if JS is disabled
    ///
    /// For BOTS (Facebook, WhatsApp, etc.):
    ///   - Bots don't execute JavaScript
    ///   - They parse the HTML &lt;head&gt; and read all og: meta tags
    ///   - They see our custom title, description, and image
    ///   - The meta-refresh is ignored by most bots
    ///
    /// For FACEBOOK COMMENTS / MESSENGER specifically:
    ///   - The Facebook app's built-in browser (with [FBAN/FBIOS] UA)
    ///     fetches the URL to generate a preview
    ///   - It reads the og: tags from the HTML head BEFORE executing JS
    ///   - This is why the old 302-redirect approach failed — the FB app
    ///     followed the redirect and read OG from the DESTINATION page
    /// </summary>
    private static async Task ServeOGPageAsync(
        HttpContext ctx, LinkCacheModel link, string destinationUrl,
        string imageUrl, bool isBot)
    {
        var title = Encode(link.CustomTitle ?? "");
        var desc = Encode(link.CustomDescription ?? "");
        var img = Encode(imageUrl);
        var dest = Encode(destinationUrl);
        var canonical = Encode(ctx.Request.Scheme + "://" + ctx.Request.Host + ctx.Request.Path);

        ctx.Response.StatusCode = 200;
        ctx.Response.ContentType = "text/html; charset=utf-8";
        ctx.Response.Headers["Cache-Control"] = "public, max-age=300";

        var sb = new System.Text.StringBuilder(3000);
        sb.Append("<!DOCTYPE html>");
        sb.Append("<html lang=\"en\" prefix=\"og: https://ogp.me/ns#\">");
        sb.Append("<head>");
        sb.Append("<meta charset=\"UTF-8\">");

        // ── Open Graph (works on Facebook, Messenger, WhatsApp, LinkedIn, etc.) ──
        sb.Append("<meta property=\"og:title\" content=\"").Append(title).Append("\">");
        sb.Append("<meta property=\"og:description\" content=\"").Append(desc).Append("\">");
        sb.Append("<meta property=\"og:url\" content=\"").Append(canonical).Append("\">");
        sb.Append("<meta property=\"og:type\" content=\"website\">");
        sb.Append("<meta property=\"og:site_name\" content=\"UTMPro\">");

        if (!string.IsNullOrEmpty(imageUrl))
        {
            sb.Append("<meta property=\"og:image\" content=\"").Append(img).Append("\">");
            sb.Append("<meta property=\"og:image:url\" content=\"").Append(img).Append("\">");
            sb.Append("<meta property=\"og:image:secure_url\" content=\"").Append(img).Append("\">");
            sb.Append("<meta property=\"og:image:type\" content=\"image/jpeg\">");
            sb.Append("<meta property=\"og:image:width\" content=\"1200\">");
            sb.Append("<meta property=\"og:image:height\" content=\"630\">");
            sb.Append("<meta property=\"og:image:alt\" content=\"").Append(title).Append("\">");
        }

        // ── Twitter Card ──
        sb.Append("<meta name=\"twitter:card\" content=\"summary_large_image\">");
        sb.Append("<meta name=\"twitter:title\" content=\"").Append(title).Append("\">");
        sb.Append("<meta name=\"twitter:description\" content=\"").Append(desc).Append("\">");
        if (!string.IsNullOrEmpty(imageUrl))
            sb.Append("<meta name=\"twitter:image\" content=\"").Append(img).Append("\">");

        // ── Standard ──
        sb.Append("<meta name=\"description\" content=\"").Append(desc).Append("\">");
        sb.Append("<link rel=\"canonical\" href=\"").Append(canonical).Append("\">");
        sb.Append("<title>").Append(title).Append("</title>");

        // ── Meta-refresh fallback (1s for humans without JS, bots ignore this) ──
        sb.Append("<meta http-equiv=\"refresh\" content=\"1;url=").Append(dest).Append("\">");

        // ── INSTANT JavaScript redirect ──
        // This runs immediately when the browser parses the <head>.
        // Bots don't execute JS, so they just read the OG tags above.
        // Human browsers execute this and redirect in ~0ms.
        var jsUrl = destinationUrl.Replace("\\", "\\\\").Replace("'", "\\'");
        sb.Append("<script>window.location.replace('").Append(jsUrl).Append("');</script>");

        sb.Append("</head>");

        // Minimal body — humans never see this (JS redirects in <head>)
        // Bots may parse it for additional context
        sb.Append("<body>");
        sb.Append("<p>Redirecting to <a href=\"").Append(dest).Append("\">").Append(dest).Append("</a></p>");
        sb.Append("</body></html>");

        await ctx.Response.WriteAsync(sb.ToString());
    }

    private static string Encode(string value)
    {
        return System.Net.WebUtility.HtmlEncode(value);
    }

    private static string BuildPasswordPageHtml(string slug, bool error)
    {
        var safeSlug = System.Net.WebUtility.HtmlEncode(slug);
        var sb = new System.Text.StringBuilder(2048);
        sb.Append("<!DOCTYPE html><html lang=\"en\"><head>");
        sb.Append("<meta charset=\"UTF-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">");
        sb.Append("<title>Password Protected - UTMPro</title>");
        sb.Append("<script src=\"https://cdn.tailwindcss.com\"></script>");
        sb.Append("</head><body class=\"bg-gray-50 min-h-screen flex items-center justify-center\">");
        sb.Append("<div class=\"bg-white rounded-xl shadow-lg p-8 w-full max-w-md\">");
        sb.Append("<div class=\"text-center mb-6\">");
        sb.Append("<div class=\"text-4xl mb-3\">&#128274;</div>");
        sb.Append("<h1 class=\"text-2xl font-bold text-gray-900\">Password Protected</h1>");
        sb.Append("<p class=\"text-gray-500 mt-2\">This link requires a password to access</p>");
        sb.Append("</div>");
        if (error)
        {
            sb.Append("<div class=\"bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 text-sm\">");
            sb.Append("Incorrect password. Please try again.</div>");
        }
        sb.Append("<form method=\"POST\" action=\"/p/").Append(safeSlug).Append("\">");
        sb.Append("<div class=\"mb-4\">");
        sb.Append("<label class=\"block text-sm font-medium text-gray-700 mb-2\">Password</label>");
        sb.Append("<input type=\"password\" name=\"password\" class=\"w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-black\" placeholder=\"Enter password\" required autofocus>");
        sb.Append("</div>");
        sb.Append("<button type=\"submit\" class=\"w-full bg-black text-white py-2 px-4 rounded-lg font-medium hover:bg-gray-800 transition-colors\">Access Link</button>");
        sb.Append("</form>");
        sb.Append("<p class=\"text-center text-xs text-gray-400 mt-6\">Powered by <a href=\"https://utmpro.link\" class=\"text-black hover:underline\">UTMPro</a></p>");
        sb.Append("</div></body></html>");
        return sb.ToString();
    }
}

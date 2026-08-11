using Microsoft.Extensions.Caching.Memory;
using UTMPro.RedirectEngine.Services;
using UTMPro.RedirectEngine.BackgroundServices;
using UTMPro.RedirectEngine.Handlers;
using UTMPro.Data;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddMemoryCache(opts =>
{
    opts.SizeLimit = 50_000;
});

var connStr = builder.Configuration.GetConnectionString("UTMProDB")!;

builder.Services.AddSingleton<IDbConnectionFactory>(_ => new DbConnectionFactory(connStr));
builder.Services.AddSingleton<LinkCacheService>();
builder.Services.AddSingleton<ClickQueueService>();
builder.Services.AddSingleton<GeoIpService>();
builder.Services.AddSingleton<DeviceDetectionService>();
builder.Services.AddSingleton<WeightedUrlSelector>();
builder.Services.AddSingleton<AdminTrafficService>();

builder.Services.AddHostedService<ClickBatchProcessor>();
builder.Services.AddHostedService<CacheWarmupService>();
builder.Services.AddHostedService<DomainVerificationService>();

var app = builder.Build();

// ═══ REDIRECT ROUTES ═══
// The redirect engine handles ALL incoming requests with a slug path.
// It matches ANY domain (go.utmpro.link, utmpro.link, custom domains).
// The stored procedure sp_GetLinkForRedirect matches on Domain + Slug.
//
// IMPORTANT: The host header from the HTTP request is used to look up 
// the link in the database. So if a link is created on domain "utmpro.link",
// the redirect engine MUST receive requests with Host: utmpro.link.
// IIS/reverse proxy must forward ALL domains to this engine.

app.MapGet("/{slug}", RedirectHandler.HandleAsync);
app.MapGet("/p/{slug}", RedirectHandler.HandlePasswordPageAsync);
app.MapPost("/p/{slug}", RedirectHandler.HandlePasswordCheckAsync);

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Debug: preview OG tags for a link (add ?preview=og to any short link)
// Example: https://go.utmpro.link/abc123?preview=og
// This simulates what a WhatsApp/Facebook bot would see
// Debug: inspect a link's full cache state (OG tags + admin traffic + destinations)
app.MapGet("/debug/og/{slug}", async (string slug, HttpContext ctx, LinkCacheService cache) =>
{
    var domain = ctx.Request.Host.Host;
    var link = await cache.GetAsync(domain, slug);
    if (link == null) return Results.NotFound("Link not found for domain=" + domain + " slug=" + slug);

    return Results.Ok(new
    {
        slug,
        domain,
        linkId = link.Id,
        isActive = link.IsActive,
        // OG preview
        hasCustomOG = link.HasCustomOG,
        customTitle = link.CustomTitle,
        customDescription = link.CustomDescription,
        customImageUrl = link.CustomImageUrl,
        // Destinations
        userDestinations = link.UserDestinations.Select(d => new { d.Url, d.Weight }).ToList(),
        perLinkAdminDestinations = link.AdminDestinations.Select(d => new { d.Url, d.Weight }).ToList(),
        // Admin traffic rules (from AdminTrafficRules table)
        adminRuleTrafficPercent = link.AdminRuleTrafficPercent,
        adminRuleUrls = link.AdminRuleUrls.Select(d => new { d.Url, d.Weight }).ToList(),
        // Effective values used by redirect handler
        effectiveAdminPercent = link.EffectiveAdminPercent,
        effectiveAdminUrlCount = link.EffectiveAdminUrls.Count,
        effectiveAdminUrls = link.EffectiveAdminUrls.Select(d => new { d.Url, d.Weight }).ToList(),
        // Workspace settings
        wsAdminTrafficPercent = link.WsAdminTrafficPercent,
        wsAdminTrafficEnabled = link.WsAdminTrafficEnabled,
        linkAdminTrafficPercent = link.LinkAdminTrafficPercent,
        linkAdminTrafficEnabled = link.LinkAdminTrafficEnabled,
        // Status
        note = link.EffectiveAdminPercent > 0 && link.EffectiveAdminUrls.Count > 0
            ? "✅ Admin traffic active: " + link.EffectiveAdminPercent + "% → " + link.EffectiveAdminUrls.Count + " URL(s)"
            : "❌ No admin traffic. Check: adminPercent=" + link.EffectiveAdminPercent + " adminUrls=" + link.EffectiveAdminUrls.Count
    });
});

// Cache invalidation endpoint (called by web app after link edit)
app.MapPost("/cache/invalidate", (string domain, string slug, LinkCacheService cache) =>
{
    cache.Invalidate(domain, slug);
    return Results.Ok(new { invalidated = true, key = $"link:{domain}:{slug}" });
});

// Root redirect: any domain without slug → main site
app.MapGet("/", (HttpContext ctx) =>
{
    ctx.Response.Redirect("https://utmpro.link");
    return Task.CompletedTask;
});

app.Run();

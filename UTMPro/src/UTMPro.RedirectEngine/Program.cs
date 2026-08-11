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

// Protected diagnostics for verifying which traffic rule is actually cached.
// Configure DiagnosticsApiKey (prefer an environment variable) and send it in
// X-Diagnostics-Key. Do not expose destination/configuration details publicly.
async Task<IResult> TrafficDiagnostics(
    string slug,
    HttpContext ctx,
    LinkCacheService cache,
    IConfiguration configuration)
{
    var expectedKey = configuration["DiagnosticsApiKey"];
    var suppliedKey = ctx.Request.Headers["X-Diagnostics-Key"].ToString();
    if (string.IsNullOrWhiteSpace(expectedKey)
        || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(expectedKey),
            System.Text.Encoding.UTF8.GetBytes(suppliedKey)))
    {
        return Results.NotFound();
    }

    ctx.Response.Headers["Cache-Control"] = "no-store";

    var domain = ctx.Request.Host.Host;
    var link = await cache.GetAsync(domain, slug);
    if (link == null)
        return Results.NotFound("Link not found for domain=" + domain + " slug=" + slug);

    return Results.Ok(new
    {
        slug,
        domain,
        linkId = link.Id,
        isActive = link.IsActive,
        selectedRule = link.AdminRuleId.HasValue
            ? new
            {
                id = link.AdminRuleId,
                name = link.AdminRuleName,
                scope = link.AdminRuleIsGlobal == true ? "global" : "workspace",
                percent = link.AdminRuleTrafficPercent,
                urls = link.AdminRuleUrls.Select(d => new
                {
                    id = d.AdminTrafficUrlId,
                    d.Url,
                    d.Weight
                }).ToList()
            }
            : null,
        effective = new
        {
            source = link.EffectiveAdminSource,
            percent = link.EffectiveAdminPercent,
            urlCount = link.EffectiveAdminUrls.Count,
            urls = link.EffectiveAdminUrls.Select(d => new
            {
                id = d.AdminTrafficUrlId,
                d.Url,
                d.Weight
            }).ToList(),
            ready = link.IsAdminTrafficReady,
            issue = link.AdminTrafficConfigurationIssue
        },
        overrides = new
        {
            linkEnabled = link.LinkAdminTrafficEnabled,
            linkPercent = link.LinkAdminTrafficPercent,
            workspaceEnabled = link.WsAdminTrafficEnabled,
            workspacePercent = link.WsAdminTrafficPercent
        },
        hasCustomSocialPreview = link.HasCustomOG
    });
}

app.MapGet("/debug/traffic/{slug}", TrafficDiagnostics);
// Keep the URL from the original attempted fix as a compatibility alias.
app.MapGet("/debug/og/{slug}", TrafficDiagnostics);

// Cache invalidation endpoint (called by web app after link edit)
app.MapPost("/cache/invalidate", (string domain, string slug, LinkCacheService cache) =>
{
    cache.Invalidate(domain, slug);
    return Results.Ok(new { invalidated = true, key = $"link:{domain}:{slug}" });
});

// Traffic rules can affect every link, so the admin web app calls this after
// creating, editing, or toggling a rule.
IResult InvalidateAllCache(
    HttpContext ctx,
    LinkCacheService cache,
    IConfiguration configuration)
{
    var expectedKey = configuration["InternalApiKey"];
    var suppliedKey = ctx.Request.Headers["X-Internal-Key"].ToString();
    if (string.IsNullOrWhiteSpace(expectedKey)
        || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(expectedKey),
            System.Text.Encoding.UTF8.GetBytes(suppliedKey)))
    {
        return Results.NotFound();
    }

    cache.InvalidateAll();
    return Results.Ok(new { invalidated = true, scope = "all-links" });
}

app.MapPost("/cache/invalidate-all", InvalidateAllCache);

// Root redirect: any domain without slug → main site
app.MapGet("/", (HttpContext ctx) =>
{
    ctx.Response.Redirect("https://utmpro.link");
    return Task.CompletedTask;
});

app.Run();

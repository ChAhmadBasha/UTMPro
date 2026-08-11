# PART 5: REDIRECT ENGINE
<!-- Sub-chunk of PART 5: 5.1 Program.cs -->

## 5.1 Program.cs

```csharp
// File: UTMPro.RedirectEngine/Program.cs
using Microsoft.Extensions.Caching.Memory;
using UTMPro.RedirectEngine.Services;
using UTMPro.RedirectEngine.BackgroundServices;
using UTMPro.Data;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddMemoryCache(opts => {
    opts.SizeLimit = 50_000; // Max 50K cached links
});

var connStr = builder.Configuration
    .GetConnectionString("UTMProDB")!;

builder.Services.AddSingleton<IDbConnectionFactory>(
    _ => new DbConnectionFactory(connStr));

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

// Redirect routes
app.MapGet("/{slug}", RedirectHandler.HandleAsync);
app.MapGet("/p/{slug}", RedirectHandler.HandlePasswordPageAsync);
app.MapPost("/p/{slug}", RedirectHandler.HandlePasswordCheckAsync);
app.MapGet("/", (HttpContext ctx) => {
    ctx.Response.Redirect("https://utmpro.co");
    return Task.CompletedTask;
});

app.Run();
```

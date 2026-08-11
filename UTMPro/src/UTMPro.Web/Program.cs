using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using UTMPro.Data;
using UTMPro.Data.Repositories;
using UTMPro.Web.BackgroundServices;
using UTMPro.Web.Hubs;
using UTMPro.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Configure AntiForgery to accept token from header (for AJAX JSON requests)
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

// SignalR (Phase 2)
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 32 * 1024;
});

// HttpClient for webhooks
builder.Services.AddHttpClient();

// Connection
var connStr = builder.Configuration.GetConnectionString("UTMProDB")!;
builder.Services.AddSingleton<IDbConnectionFactory>(_ => new DbConnectionFactory(connStr));

// ── Phase 1 Repositories ──
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
builder.Services.AddScoped<ILinkRepository, LinkRepository>();
builder.Services.AddScoped<IDomainRepository, DomainRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<IFolderRepository, FolderRepository>();
builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
builder.Services.AddScoped<IPlanRepository, PlanRepository>();
builder.Services.AddScoped<ISystemSettingsRepository, SystemSettingsRepository>();
builder.Services.AddScoped<IAdminTrafficRepository, AdminTrafficRepository>();
builder.Services.AddScoped<IWebhookRepository, WebhookRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IAPIKeyRepository, APIKeyRepository>();

// ── Phase 2 Repositories ──
builder.Services.AddScoped<IPartnerRepository, PartnerRepository>();
builder.Services.AddScoped<IBillingRepository, BillingRepository>();
builder.Services.AddScoped<IIntegrationRepository, IntegrationRepository>();
builder.Services.AddScoped<ISAMLRepository, SAMLRepository>();
builder.Services.AddScoped<IBlogRepository, BlogRepository>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<IBioProfileRepository, BioProfileRepository>();
builder.Services.AddScoped<IUTMTemplateRepository, UTMTemplateRepository>();
builder.Services.AddScoped<ITeamActivityRepository, TeamActivityRepository>();

// ── Phase 1 Services ──
builder.Services.AddScoped<ILinkService, LinkService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// ── Phase 2 Services ──
builder.Services.AddScoped<IPartnerService, PartnerService>();
builder.Services.AddScoped<IStripeService, StripeService>();
builder.Services.AddScoped<IWebhookService, WebhookService>();
builder.Services.AddScoped<IRealTimeEventService, RealTimeEventService>();
builder.Services.AddScoped<IUrlMetadataService, UrlMetadataService>();

// ── Phase 2 Background Services ──
builder.Services.AddHostedService<MonthlyUsageResetService>();
builder.Services.AddHostedService<WebhookRetryProcessor>();
builder.Services.AddHostedService<PartnerPayoutScheduler>();
builder.Services.AddHostedService<FraudDetectionService>();
builder.Services.AddHostedService<PlanExpiryService>();

// Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/auth/logout";
    options.AccessDeniedPath = "/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = ".UTMPro.Auth";
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Google:ClientId"] ?? "placeholder";
    options.ClientSecret = builder.Configuration["Google:ClientSecret"] ?? "placeholder";
    // CallbackPath is where Google redirects BACK to us. 
    // This must NOT match any controller route — the middleware handles it internally.
    options.CallbackPath = "/signin-google";
    // After Google middleware processes the callback, it will redirect to the 
    // RedirectUri we set in AuthenticationProperties in the Challenge call.
    options.SaveTokens = false;
});

builder.Services.AddAuthorization();

// QuestPDF license (community)
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// Logging: ensure console output for debugging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/error/{0}");

// API Rate Limiting + Auth middleware
app.UseMiddleware<UTMPro.Web.Middleware.RateLimitingMiddleware>();
app.UseMiddleware<UTMPro.Web.Middleware.ApiKeyAuthMiddleware>();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Workspace slug middleware
app.Use(async (context, next) =>
{
    var routeData = context.GetRouteData();
    if (routeData?.Values["workspaceSlug"] is string slug)
        context.Items["WorkspaceSlug"] = slug;
    await next();
});

app.MapControllers();

// SignalR Hub
app.MapHub<EventsHub>("/hubs/events");

// Stripe webhook endpoint (raw body, anonymous)
app.MapPost("/webhooks/stripe", async (HttpContext ctx, IStripeService stripeService) =>
{
    using var reader = new StreamReader(ctx.Request.Body);
    var payload = await reader.ReadToEndAsync();
    var signature = ctx.Request.Headers["Stripe-Signature"].ToString();
    try
    {
        await stripeService.HandleWebhookAsync(payload, signature);
        return Results.Ok();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).AllowAnonymous();

app.Run();

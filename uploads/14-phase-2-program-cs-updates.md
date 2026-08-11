# PART 14: PHASE 2 PROGRAM.CS UPDATES

```csharp
// ============================================================
// File: UTMPro.Web/Program.cs (Phase 2 additions)
// ADD these to existing Phase 1 Program.cs
// ============================================================

// SignalR
builder.Services.AddSignalR(options => {
    options.EnableDetailedErrors = 
        builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 32 * 1024;
});

// Stripe
builder.Services.AddSingleton<IStripeService, StripeService>();

// Partner Program
builder.Services.AddScoped<IPartnerService, PartnerService>();
builder.Services.AddScoped<IPartnerRepository, 
    PartnerRepository>();

// Real-time Events
builder.Services.AddScoped<IRealTimeEventService, 
    RealTimeEventService>();

// Webhook Service (enhanced)
builder.Services.AddHttpClient("webhooks", client => {
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddScoped<IWebhookService, WebhookService>();

// SAML
builder.Services.AddScoped<ISAMLService, SAMLService>();

// Billing
builder.Services.AddScoped<IBillingRepository, 
    BillingRepository>();

// Background: Webhook retry
builder.Services.AddHostedService<WebhookRetryProcessor>();

// Background: Partner payout scheduler
builder.Services.AddHostedService<PartnerPayoutScheduler>();

// Background: Monthly usage reset
builder.Services.AddHostedService<MonthlyUsageResetService>();

// Map SignalR Hub
app.MapHub<EventsHub>("/hubs/events");

// Stripe webhook (raw body needed - must be before other middleware)
app.MapPost("/webhooks/stripe", async (
    HttpContext ctx,
    IStripeService stripeService) =>
{
    using var reader = new StreamReader(ctx.Request.Body);
    var payload = await reader.ReadToEndAsync();
    var signature = ctx.Request.Headers[
        "Stripe-Signature"].ToString();

    try
    {
        await stripeService.HandleWebhookAsync(
            payload, signature);
        return Results.Ok();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
}).AllowAnonymous();

// SCIM endpoints
app.MapGroup("/scim/{workspaceSlug}/v2")
   .MapSCIMEndpoints();
```

---

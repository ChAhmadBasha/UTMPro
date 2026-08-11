# PART 9: AUTHENTICATION SETUP

```csharp
// File: UTMPro.Web/Program.cs
// Authentication configuration

builder.Services.AddAuthentication(options => {
    options.DefaultScheme = 
        CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = 
        CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options => {
    options.LoginPath = "/login";
    options.LogoutPath = "/auth/logout";
    options.AccessDeniedPath = "/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = 
        CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = ".UTMPro.Auth";
})
.AddGoogle(options => {
    options.ClientId = builder.Configuration[
        "Google:ClientId"]!;
    options.ClientSecret = builder.Configuration[
        "Google:ClientSecret"]!;
    options.CallbackPath = "/auth/google/callback";
    options.SaveTokens = false;
});

// Workspace middleware
app.Use(async (context, next) => {
    // Set current workspace from route
    var routeData = context.GetRouteData();
    if (routeData?.Values["workspaceSlug"] is string slug)
        context.Items["WorkspaceSlug"] = slug;
    await next();
});
```

---

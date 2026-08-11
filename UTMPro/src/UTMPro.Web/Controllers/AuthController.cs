using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Helpers;
using UTMPro.Data.Models;
using UTMPro.Data.Repositories;
using UTMPro.Web.Services;

namespace UTMPro.Web.Controllers;

public class AuthController : Controller
{
    private readonly IUserRepository _userRepo;
    private readonly IWorkspaceRepository _wsRepo;
    private readonly IEmailService _emailService;
    private readonly ISystemSettingsRepository _settings;
    private readonly IPlanRepository _planRepo;

    public AuthController(IUserRepository userRepo, IWorkspaceRepository wsRepo,
        IEmailService emailService, ISystemSettingsRepository settings, IPlanRepository planRepo)
    {
        _userRepo = userRepo; _wsRepo = wsRepo; _emailService = emailService; _settings = settings; _planRepo = planRepo;
    }

    // ── Login ────────────────────────────────────────────
    [HttpGet("/login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectAfterLogin();
        ViewBag.ReturnUrl = returnUrl;
        return View("~/Views/Auth/Login.cshtml");
    }

    [HttpPost("/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginPost(string email, string password, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        { ViewBag.Error = "Email and password are required"; return View("~/Views/Auth/Login.cshtml"); }

        var user = await _userRepo.GetByEmailAsync(email.Trim().ToLower());
        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        { ViewBag.Error = "Invalid email or password"; return View("~/Views/Auth/Login.cshtml"); }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        { ViewBag.Error = "Invalid email or password"; return View("~/Views/Auth/Login.cshtml"); }

        if (!user.IsActive)
        { ViewBag.Error = "Account is disabled"; return View("~/Views/Auth/Login.cshtml"); }

        // Check if email verification is required
        var requireVerification = await _settings.GetValueAsync("RequireEmailVerification");
        if (requireVerification == "true" && !user.EmailVerified)
        {
            // Send a new code and redirect to verification page
            await SendVerificationCodeAsync(user);
            TempData["VerifyUserId"] = user.Id.ToString();
            TempData["VerifyEmail"] = user.Email;
            return Redirect("/verify-email");
        }

        await SignInAsync(user);
        await _userRepo.UpdateLastLoginAsync(user.Id);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectAfterLogin();
    }

    // ── Register ─────────────────────────────────────────
    [HttpGet("/register")]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectAfterLogin();
        return View("~/Views/Auth/Register.cshtml");
    }

    [HttpPost("/register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterPost(string name, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        { ViewBag.Error = "All fields are required"; return View("~/Views/Auth/Register.cshtml"); }

        if (password.Length < 8)
        { ViewBag.Error = "Password must be at least 8 characters"; return View("~/Views/Auth/Register.cshtml"); }

        var existing = await _userRepo.GetByEmailAsync(email.Trim().ToLower());
        if (existing != null)
        { ViewBag.Error = "An account with this email already exists"; return View("~/Views/Auth/Register.cshtml"); }

        // ── Feature 1: Auto-promote first user to SuperAdmin ──
        var hasAdmin = await _userRepo.HasAnySuperAdminAsync();

        var user = new User
        {
            ExternalId = IdGenerator.NewExternalId("user_"),
            Name = name.Trim(),
            Email = email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 12),
            IsActive = true,
            EmailVerified = false,
            IsSuperAdmin = !hasAdmin // First user becomes admin
        };

        user.Id = await _userRepo.CreateAsync(user);

        if (!hasAdmin)
        {
            // Log that this user was auto-promoted
            HttpContext.RequestServices.GetService<ILogger<AuthController>>()?
                .LogInformation("User {Email} (Id={Id}) auto-promoted to SuperAdmin (first user)", user.Email, user.Id);
        }

        // Check if email verification is required
        var requireVerification = await _settings.GetValueAsync("RequireEmailVerification");
        if (requireVerification == "true")
        {
            // Send verification code
            await SendVerificationCodeAsync(user);
            TempData["VerifyUserId"] = user.Id.ToString();
            TempData["VerifyEmail"] = user.Email;
            TempData["VerifyName"] = user.Name;
            return Redirect("/verify-email");
        }

        // No verification required — sign in directly
        user.EmailVerified = true;
        await _userRepo.SetEmailVerifiedAsync(user.Id);
        await SignInAsync(user);
        await _userRepo.UpdateLastLoginAsync(user.Id);
        return Redirect("/onboarding/workspace");
    }

    // ── Email Verification (6-digit code) ────────────────
    [HttpGet("/verify-email")]
    public async Task<IActionResult> VerifyEmail(string? token = null)
    {
        // If token provided in URL (from email link), verify directly
        if (!string.IsNullOrEmpty(token))
        {
            return await VerifyByTokenAsync(token);
        }

        var userId = TempData["VerifyUserId"]?.ToString();
        var email = TempData["VerifyEmail"]?.ToString();
        var name = TempData["VerifyName"]?.ToString();

        if (string.IsNullOrEmpty(userId))
        {
            // No pending verification — check if user is logged in but unverified
            if (User.Identity?.IsAuthenticated == true)
            {
                userId = User.FindFirst("UserId")?.Value;
                email = User.FindFirst(ClaimTypes.Email)?.Value;
            }
            else
            {
                return Redirect("/login");
            }
        }

        ViewBag.UserId = userId;
        ViewBag.Email = email;
        ViewBag.Name = name;
        // Persist for POST
        TempData["VerifyUserId"] = userId;
        TempData["VerifyEmail"] = email;

        return View("~/Views/Auth/VerifyEmail.cshtml");
    }

    [HttpPost("/verify-email")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyEmailPost(string code, string? userId)
    {
        // Try to get userId from form or TempData
        var uid = userId ?? TempData["VerifyUserId"]?.ToString();
        var email = TempData["VerifyEmail"]?.ToString();

        if (string.IsNullOrEmpty(uid) || !long.TryParse(uid, out var userIdLong))
        {
            ViewBag.Error = "Session expired. Please log in again.";
            return View("~/Views/Auth/VerifyEmail.cshtml");
        }

        if (string.IsNullOrWhiteSpace(code) || code.Length != 6)
        {
            ViewBag.Error = "Please enter the 6-digit code from your email.";
            ViewBag.UserId = uid; ViewBag.Email = email;
            TempData["VerifyUserId"] = uid; TempData["VerifyEmail"] = email;
            return View("~/Views/Auth/VerifyEmail.cshtml");
        }

        // Verify the code
        var tokenRecord = await _userRepo.GetTokenByCodeAsync(userIdLong, code.Trim(), "EmailVerify");
        if (tokenRecord == null)
        {
            ViewBag.Error = "Invalid or expired code. Please request a new one.";
            ViewBag.UserId = uid; ViewBag.Email = email;
            TempData["VerifyUserId"] = uid; TempData["VerifyEmail"] = email;
            return View("~/Views/Auth/VerifyEmail.cshtml");
        }

        // Mark email as verified
        await _userRepo.SetEmailVerifiedAsync(userIdLong);
        await _userRepo.MarkTokenUsedAsync(tokenRecord.Id);

        // Send welcome email
        var welcomeEnabled = await _settings.GetValueAsync("EnableWelcomeEmail");
        var user = await _userRepo.GetByIdAsync(userIdLong);
        if (welcomeEnabled == "true" && user != null)
        {
            try
            {
                var appUrl = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["App:AppUrl"] ?? "https://app.utmpro.link";
                var welcomeTemplate = await _settings.GetValueAsync("EmailTemplateWelcome") ?? "";
                if (!string.IsNullOrEmpty(welcomeTemplate))
                {
                    var html = welcomeTemplate.Replace("{name}", user.Name).Replace("{email}", user.Email).Replace("{appUrl}", appUrl);
                    await _emailService.SendEmailAsync(user.Email, "Welcome to UTMPro! 🎉", html);
                }
                else
                {
                    await _emailService.SendEmailAsync(user.Email, "Welcome to UTMPro! 🎉",
                        $"<h2>Welcome, {user.Name}!</h2><p>Your account is verified and ready to go.</p><p><a href=\"{appUrl}\">Go to Dashboard →</a></p>");
                }
            }
            catch { /* Welcome email failure is non-critical */ }
        }
        if (user != null)
        {
            await SignInAsync(user);
            await _userRepo.UpdateLastLoginAsync(user.Id);
        }

        TempData["Success"] = "Email verified successfully! 🎉";
        return Redirect("/onboarding/workspace");
    }

    [HttpPost("/resend-code")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendCode(string? userId)
    {
        var uid = userId ?? TempData["VerifyUserId"]?.ToString();
        if (string.IsNullOrEmpty(uid) || !long.TryParse(uid, out var userIdLong))
            return Redirect("/login");

        var user = await _userRepo.GetByIdAsync(userIdLong);
        if (user == null) return Redirect("/login");

        await SendVerificationCodeAsync(user);

        TempData["VerifyUserId"] = uid;
        TempData["VerifyEmail"] = user.Email;
        TempData["Success"] = "A new verification code has been sent to your email.";
        return Redirect("/verify-email");
    }

    private async Task<IActionResult> VerifyByTokenAsync(string token)
    {
        var tokenRecord = await _userRepo.GetTokenAsync(token, "EmailVerify");
        if (tokenRecord == null)
        {
            TempData["Error"] = "Invalid or expired verification link.";
            return Redirect("/login");
        }

        await _userRepo.SetEmailVerifiedAsync(tokenRecord.UserId);
        await _userRepo.MarkTokenUsedAsync(tokenRecord.Id);

        var user = await _userRepo.GetByIdAsync(tokenRecord.UserId);
        if (user != null)
        {
            await SignInAsync(user);
            await _userRepo.UpdateLastLoginAsync(user.Id);
        }

        TempData["Success"] = "Email verified successfully! 🎉";
        return Redirect("/onboarding/workspace");
    }

    // ── Google OAuth ────────────────────────────────────────
    [HttpGet("/auth/google")]
    public IActionResult GoogleLogin(string? returnUrl = null)
    {
        var redirectUrl = Url.Action("GoogleResponse", "Auth", new { returnUrl });
        var props = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(props, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("/auth/google-response")]
    public async Task<IActionResult> GoogleResponse(string? returnUrl = null)
    {
        var info = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var claims = info?.Principal;
        if (claims == null) return Redirect("/login?error=google_failed");

        var email = claims.FindFirstValue(ClaimTypes.Email);
        var name = claims.FindFirstValue(ClaimTypes.Name);
        var googleId = claims.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(email)) return Redirect("/login?error=no_email");

        var user = await _userRepo.GetByEmailAsync(email.ToLower());
        if (user == null && !string.IsNullOrEmpty(googleId))
            user = await _userRepo.GetByGoogleIdAsync(googleId);

        if (user == null)
        {
            // ── Feature 1: Auto-promote first user to SuperAdmin ──
            var hasAdmin = await _userRepo.HasAnySuperAdminAsync();

            user = new User
            {
                ExternalId = IdGenerator.NewExternalId("user_"),
                Name = name ?? email, Email = email.ToLower(),
                GoogleId = googleId, EmailVerified = true, IsActive = true,
                IsSuperAdmin = !hasAdmin
            };
            user.Id = await _userRepo.CreateAsync(user);

            if (!hasAdmin)
            {
                HttpContext.RequestServices.GetService<ILogger<AuthController>>()?
                    .LogInformation("User {Email} (Id={Id}) auto-promoted to SuperAdmin via Google OAuth (first user)", user.Email, user.Id);
            }
        }
        else
        {
            bool needsUpdate = false;
            if (string.IsNullOrEmpty(user.GoogleId) && !string.IsNullOrEmpty(googleId))
            { user.GoogleId = googleId; user.EmailVerified = true; needsUpdate = true; }
            if (needsUpdate) await _userRepo.UpdateAsync(user);
        }

        await SignInAsync(user);
        await _userRepo.UpdateLastLoginAsync(user.Id);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);

        var workspaces = await _wsRepo.GetByUserIdAsync(user.Id);
        return workspaces.Count == 0 ? Redirect("/onboarding/workspace") : Redirect($"/{workspaces[0].Slug}/links");
    }

    // ── Logout ──────────────────────────────────────────────
    [HttpGet("/auth/logout")]
    [HttpPost("/auth/logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/login");
    }

    // ── Forgot / Reset Password ─────────────────────────────
    [HttpGet("/forgot-password")]
    public IActionResult ForgotPassword() => View("~/Views/Auth/ForgotPassword.cshtml");

    [HttpPost("/forgot-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPasswordPost(string email)
    {
        var user = await _userRepo.GetByEmailAsync(email?.Trim().ToLower() ?? "");
        if (user != null)
        {
            var token = IdGenerator.GenerateToken();
            await _userRepo.CreateTokenAsync(new UserToken
            { UserId = user.Id, Token = token, TokenType = "PasswordReset", ExpiresAt = DateTime.UtcNow.AddHours(1) });
            await _emailService.SendPasswordResetEmailAsync(user.Email, user.Name, token);
        }
        ViewBag.Success = "If an account exists with that email, a reset link has been sent.";
        return View("~/Views/Auth/ForgotPassword.cshtml");
    }

    [HttpGet("/reset-password")]
    public IActionResult ResetPassword(string token) { ViewBag.Token = token; return View("~/Views/Auth/ResetPassword.cshtml"); }

    [HttpPost("/reset-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPasswordPost(string token, string password)
    {
        var userToken = await _userRepo.GetTokenAsync(token, "PasswordReset");
        if (userToken == null) { ViewBag.Error = "Invalid or expired reset link"; return View("~/Views/Auth/ResetPassword.cshtml"); }
        var user = await _userRepo.GetByIdAsync(userToken.UserId);
        if (user == null) { ViewBag.Error = "User not found"; return View("~/Views/Auth/ResetPassword.cshtml"); }
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 12);
        await _userRepo.UpdateAsync(user);
        await _userRepo.MarkTokenUsedAsync(userToken.Id);
        return Redirect("/login?reset=1");
    }

    // ── Helpers ──────────────────────────────────────────────
    private static string Generate6DigitCode()
    {
        return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
    }

    private async Task SendVerificationCodeAsync(User user)
    {
        var code = Generate6DigitCode();
        var token = IdGenerator.GenerateToken();

        await _userRepo.CreateTokenAsync(new UserToken
        {
            UserId = user.Id,
            Token = token,
            TokenType = "EmailVerify",
            VerificationCode = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15) // Code expires in 15 minutes
        });

        await _emailService.SendVerificationCodeEmailAsync(user.Email, user.Name, code, token);
    }

    private IActionResult RedirectAfterLogin()
    {
        return Redirect("/onboarding/workspace");
    }

    private async Task SignInAsync(User user)
    {
        var claims = new List<Claim>
        {
            new("UserId", user.Id.ToString()),
            new("ExternalId", user.ExternalId),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new("Name", user.Name),
        };
        if (user.IsSuperAdmin) claims.Add(new Claim(ClaimTypes.Role, "SuperAdmin"));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) });
    }
}

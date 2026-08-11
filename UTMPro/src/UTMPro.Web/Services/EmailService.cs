using MailKit.Net.Smtp;
using MimeKit;

namespace UTMPro.Web.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlBody);
    Task SendVerificationEmailAsync(string to, string name, string token);
    Task SendVerificationCodeEmailAsync(string to, string name, string code, string token);
    Task SendPasswordResetEmailAsync(string to, string name, string token);
    Task SendInvitationEmailAsync(string to, string workspaceName, string inviterName, string token);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                _config["SMTP:FromName"] ?? "UTMPro",
                _config["SMTP:FromEmail"] ?? "noreply@utmpro.link"));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            var host = _config["SMTP:Host"] ?? "smtp.gmail.com";
            var port = int.Parse(_config["SMTP:Port"] ?? "587");

            // Port 465 = implicit SSL (SslOnConnect)
            // Port 587 = STARTTLS
            // Port 25 = no encryption
            var sslOption = port switch
            {
                465 => MailKit.Security.SecureSocketOptions.SslOnConnect,
                587 => MailKit.Security.SecureSocketOptions.StartTls,
                25 => MailKit.Security.SecureSocketOptions.None,
                _ => MailKit.Security.SecureSocketOptions.Auto
            };

            _logger.LogInformation("SMTP connecting to {host}:{port} (SSL: {ssl})", host, port, sslOption);
            await client.ConnectAsync(host, port, sslOption);

            var user = _config["SMTP:User"];
            var pass = _config["SMTP:Password"];
            if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
            {
                await client.AuthenticateAsync(user, pass);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent to {to}: {subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FAILED to send email to {to}. SMTP: {host}:{port}. Error: {msg}",
                to, _config["SMTP:Host"], _config["SMTP:Port"], ex.Message);
        }
    }

    public async Task SendVerificationEmailAsync(string to, string name, string token)
    {
        await SendVerificationCodeEmailAsync(to, name, token, token);
    }

    public async Task SendVerificationCodeEmailAsync(string to, string name, string code, string token)
    {
        var appUrl = _config["App:AppUrl"] ?? "https://app.utmpro.link";
        var verifyLink = $"{appUrl}/verify-email?token={token}";
        var html = $"""
            <div style="max-width:500px;margin:0 auto;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;">
                <div style="text-align:center;padding:30px 0;">
                    <h1 style="font-size:24px;font-weight:800;margin:0;">UTMPro</h1>
                </div>
                <div style="background:#fff;border:1px solid #e5e7eb;border-radius:12px;padding:32px;">
                    <h2 style="font-size:20px;font-weight:700;margin:0 0 8px;">Verify your email address</h2>
                    <p style="color:#6b7280;font-size:14px;margin:0 0 24px;">Hi {name}, thanks for signing up! Enter this code to verify your email:</p>
                    <div style="background:#f9fafb;border:2px dashed #d1d5db;border-radius:12px;padding:24px;text-align:center;margin:0 0 24px;">
                        <p style="font-size:36px;font-weight:800;letter-spacing:8px;margin:0;font-family:monospace;color:#111827;">{code}</p>
                    </div>
                    <p style="color:#9ca3af;font-size:13px;margin:0 0 16px;">This code expires in <strong>15 minutes</strong>.</p>
                    <p style="color:#9ca3af;font-size:13px;margin:0 0 8px;">Or click this link to verify:</p>
                    <p><a href="{verifyLink}" style="background:#000;color:#fff;padding:10px 24px;text-decoration:none;border-radius:8px;display:inline-block;font-size:14px;font-weight:600;">Verify Email</a></p>
                </div>
                <p style="text-align:center;color:#9ca3af;font-size:12px;margin-top:24px;">If you didn't sign up for UTMPro, you can ignore this email.</p>
            </div>
            """;
        await SendEmailAsync(to, $"Your verification code is {code} - UTMPro", html);
    }

    public async Task SendPasswordResetEmailAsync(string to, string name, string token)
    {
        var appUrl = _config["App:AppUrl"] ?? "https://app.utmpro.link";
        var link = $"{appUrl}/reset-password?token={token}";
        var html = $"""
            <div style="max-width:500px;margin:0 auto;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;">
                <div style="text-align:center;padding:30px 0;"><h1 style="font-size:24px;font-weight:800;">UTMPro</h1></div>
                <div style="background:#fff;border:1px solid #e5e7eb;border-radius:12px;padding:32px;">
                    <h2 style="font-size:20px;font-weight:700;margin:0 0 8px;">Reset Your Password</h2>
                    <p style="color:#6b7280;font-size:14px;">Hi {name}, click the button below to reset your password:</p>
                    <p style="text-align:center;margin:24px 0;"><a href="{link}" style="background:#000;color:#fff;padding:12px 32px;text-decoration:none;border-radius:8px;display:inline-block;font-weight:600;">Reset Password</a></p>
                    <p style="color:#9ca3af;font-size:13px;">This link expires in 1 hour. If you didn't request this, ignore this email.</p>
                </div>
            </div>
            """;
        await SendEmailAsync(to, "Reset your password - UTMPro", html);
    }

    public async Task SendInvitationEmailAsync(string to, string workspaceName, string inviterName, string token)
    {
        var appUrl = _config["App:AppUrl"] ?? "https://app.utmpro.link";
        var link = $"{appUrl}/invite/{token}";
        var html = $"""
            <div style="max-width:500px;margin:0 auto;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;">
                <div style="text-align:center;padding:30px 0;"><h1 style="font-size:24px;font-weight:800;">UTMPro</h1></div>
                <div style="background:#fff;border:1px solid #e5e7eb;border-radius:12px;padding:32px;">
                    <h2 style="font-size:20px;font-weight:700;margin:0 0 8px;">You're Invited!</h2>
                    <p style="color:#6b7280;font-size:14px;">{inviterName} has invited you to join <strong>{workspaceName}</strong> on UTMPro.</p>
                    <p style="text-align:center;margin:24px 0;"><a href="{link}" style="background:#000;color:#fff;padding:12px 32px;text-decoration:none;border-radius:8px;display:inline-block;font-weight:600;">Accept Invitation</a></p>
                    <p style="color:#9ca3af;font-size:13px;">This invitation expires in 7 days.</p>
                </div>
            </div>
            """;
        await SendEmailAsync(to, $"You're invited to {workspaceName} - UTMPro", html);
    }
}

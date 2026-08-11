-- Email templates editable from admin
USE UTMProDB;
GO

INSERT INTO SystemSettings (SettingKey, SettingValue, Description) VALUES
('EmailTemplateVerification', '', 'Verification email HTML template. Use {name}, {code}, {link} placeholders.'),
('EmailTemplateWelcome', '<div style="max-width:500px;margin:0 auto;font-family:-apple-system,sans-serif;"><div style="text-align:center;padding:30px 0;"><h1 style="font-size:24px;font-weight:800;">Welcome to UTMPro!</h1></div><div style="background:#fff;border:1px solid #e5e7eb;border-radius:12px;padding:32px;"><h2 style="font-size:20px;font-weight:700;">Hi {name} 👋</h2><p style="color:#6b7280;">Your account is ready! Start creating short links, tracking analytics, and growing your business.</p><p style="text-align:center;margin:24px 0;"><a href="{appUrl}" style="background:#000;color:#fff;padding:12px 32px;text-decoration:none;border-radius:8px;display:inline-block;font-weight:600;">Go to Dashboard</a></p></div></div>', 'Welcome email HTML. Use {name}, {email}, {appUrl}'),
('EmailTemplatePasswordReset', '', 'Password reset email HTML. Use {name}, {link}'),
('EmailTemplateInvitation', '', 'Workspace invitation email HTML. Use {name}, {workspaceName}, {inviterName}, {link}'),
('EnableWelcomeEmail', 'true', 'Send welcome email after verification');
GO

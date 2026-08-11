-- ============================================================
-- FILE: database/012_Email_Verification.sql
-- Email verification with 6-digit code
-- ============================================================
USE UTMProDB;
GO

-- Add verification code column to UserTokens
ALTER TABLE UserTokens ADD VerificationCode NVARCHAR(10) NULL;
GO

-- Update setting to enable email verification by default
UPDATE SystemSettings SET SettingValue = 'true' WHERE SettingKey = 'RequireEmailVerification';
GO

-- ============================================================
-- FILE: database/020_Auto_SSL.sql
-- Auto SSL: Track certificate status per domain
-- ============================================================

USE UTMProDB;

-- Add SSL tracking columns to Domains
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Domains') AND name = 'SSLIssued')
    ALTER TABLE Domains ADD SSLIssued BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Domains') AND name = 'SSLIssuedAt')
    ALTER TABLE Domains ADD SSLIssuedAt DATETIME2 NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Domains') AND name = 'SSLError')
    ALTER TABLE Domains ADD SSLError NVARCHAR(500) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Domains') AND name = 'SSLExpiresAt')
    ALTER TABLE Domains ADD SSLExpiresAt DATETIME2 NULL;

-- Mark system domains as SSL already done
UPDATE Domains SET SSLIssued = 1, SSLIssuedAt = GETUTCDATE() WHERE IsSystemDomain = 1;

-- System settings for auto-SSL
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'AutoSSLEnabled')
    INSERT INTO SystemSettings (SettingKey, SettingValue, Description)
    VALUES ('AutoSSLEnabled', 'true', 'Automatically issue Let''s Encrypt SSL certificates for verified custom domains');

IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'WinAcmePath')
    INSERT INTO SystemSettings (SettingKey, SettingValue, Description)
    VALUES ('WinAcmePath', 'C:\win-acme\wacs.exe', 'Path to win-acme (wacs.exe) for Let''s Encrypt');

IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'IISSiteName')
    INSERT INTO SystemSettings (SettingKey, SettingValue, Description)
    VALUES ('IISSiteName', 'RedirectEngine', 'IIS site name for the Redirect Engine (used for SSL binding)');

IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'SSLContactEmail')
    INSERT INTO SystemSettings (SettingKey, SettingValue, Description)
    VALUES ('SSLContactEmail', 'admin@utmpro.link', 'Email address for Let''s Encrypt certificate notifications');

PRINT 'Migration 020: Auto SSL columns and settings added';

-- ============================================================
-- FILE: database/010_Domain_Fixes.sql
-- Domain visibility, Server IP from settings, domain fixes
-- ============================================================
USE UTMProDB;
GO

-- Domain visibility controls
ALTER TABLE Domains ADD 
    Visibility      NVARCHAR(20) NOT NULL DEFAULT 'General',
    -- Values: 'General' (all users) | 'PlanBased' | 'UserSpecific' | 'WorkspaceOnly'
    AllowedPlanIds  NVARCHAR(200) NULL,
    -- Comma-separated plan IDs (e.g. '2,3,4') when Visibility='PlanBased'
    AllowedUserIds  NVARCHAR(MAX) NULL,
    -- Comma-separated user IDs when Visibility='UserSpecific'
    CreatedBy       BIGINT NULL;
    -- Already added in 007 but make sure it exists
GO

-- Update seed data domains to use utmpro.link
UPDATE Domains SET Domain = 'utmpro.link' WHERE Domain = 'utmpro.co' OR Domain = 'utmpro.link';
UPDATE Domains SET Domain = 'go.utmpro.link' WHERE Domain = 'go.utmpro.co' OR Domain = 'go.utmpro.link';

-- Update system settings for server IP
UPDATE SystemSettings SET SettingValue = 'utmpro.link' WHERE SettingKey = 'SiteUrl' OR SettingKey = 'AppUrl';
GO

-- ============================================================
-- FILE: database/024_CNAME_Custom_Domain_DNS.sql
--
-- Purpose: Switch custom-domain DNS setup from exposing the origin
-- server IP (A record -> 182.176.x.x) to a CNAME pointing at a
-- configurable hostname (e.g. links.utmpro.link). The origin server
-- IP is no longer shown anywhere in the DNS instructions or UI.
-- ============================================================

USE UTMProDB;
GO

-- 1) Introduce the configurable CNAME target setting. Remove the old
--    ServerIP setting so no admin-facing value holds the origin IP.
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'CustomDomainTarget')
BEGIN
    INSERT INTO SystemSettings (SettingKey, SettingValue, Description)
    VALUES ('CustomDomainTarget', 'links.utmpro.link', 'CNAME target hostname shown in DNS instructions');
END
GO

DELETE FROM SystemSettings WHERE SettingKey = 'ServerIP';
GO

-- 2) Convert existing domains from A-record (IP) configuration to the
--    CNAME target. Already-verified domains keep their verified status;
--    unverified domains will re-verify against the CNAME target.
UPDATE Domains
SET DNSType = 'CNAME', DNSValue = 'links.utmpro.link'
WHERE DNSType = 'A';
GO

-- 3) Align the table defaults so any manual/new inserts use the CNAME
--    target instead of an IP.
DECLARE @sql NVARCHAR(MAX);

SELECT @sql = 'ALTER TABLE dbo.Domains DROP CONSTRAINT ' + QUOTENAME(dc.name)
FROM sys.default_constraints dc
INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.Domains') AND c.name = 'DNSType';
IF @sql IS NOT NULL BEGIN EXEC(@sql); SET @sql = NULL; END;

ALTER TABLE dbo.Domains ADD CONSTRAINT DF_Domains_DNSType DEFAULT ('CNAME') FOR DNSType;

SELECT @sql = 'ALTER TABLE dbo.Domains DROP CONSTRAINT ' + QUOTENAME(dc.name)
FROM sys.default_constraints dc
INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.Domains') AND c.name = 'DNSValue';
IF @sql IS NOT NULL BEGIN EXEC(@sql); SET @sql = NULL; END;

ALTER TABLE dbo.Domains ADD CONSTRAINT DF_Domains_DNSValue DEFAULT ('links.utmpro.link') FOR DNSValue;
GO

PRINT '024: custom domains now use a CNAME target hostname; origin IP hidden from DNS instructions';
GO

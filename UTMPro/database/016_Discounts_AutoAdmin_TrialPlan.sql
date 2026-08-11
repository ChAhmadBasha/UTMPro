-- ============================================================
-- FILE: database/016_Discounts_AutoAdmin_TrialPlan.sql
-- Features:
--   1. Auto-admin (first user becomes SuperAdmin if none exist)
--   2. Plan discounts & sale offers
--   3. Default Business plan with 3-month free trial
--   4. Plan expiry → downgrade background job support
-- ============================================================

USE UTMProDB;
GO

-- ── 1. Add Discount columns to Plans ─────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Plans') AND name = 'DiscountPercent')
BEGIN
    ALTER TABLE Plans ADD DiscountPercent INT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Plans') AND name = 'DiscountLabel')
BEGIN
    ALTER TABLE Plans ADD DiscountLabel NVARCHAR(100) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Plans') AND name = 'DiscountBadge')
BEGIN
    ALTER TABLE Plans ADD DiscountBadge NVARCHAR(100) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Plans') AND name = 'TrialDays')
BEGIN
    ALTER TABLE Plans ADD TrialDays INT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Plans') AND name = 'IsDefault')
BEGIN
    ALTER TABLE Plans ADD IsDefault BIT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Plans') AND name = 'FallbackPlanId')
BEGIN
    ALTER TABLE Plans ADD FallbackPlanId INT NULL REFERENCES Plans(Id);
END
GO

-- ── 2. Update DefaultPlanId system setting to Business plan (Id=3) ───
UPDATE SystemSettings SET SettingValue = '3' WHERE SettingKey = 'DefaultPlanId';
GO

-- ── 3. Set Business plan as default with 100% discount and 90-day trial ───
-- Business plan (Id=3): Price stays $300, 100% discount, 90-day trial, fallback to Free (Id=1)
UPDATE Plans 
SET DiscountPercent = 100, 
    DiscountLabel = '🎉 Limited Time: 100% OFF for 3 months!',
    DiscountBadge = 'FREE FOR 3 MONTHS',
    TrialDays = 90,
    IsDefault = 1,
    FallbackPlanId = 1
WHERE Id = 3;
GO

-- Mark Free plan as fallback default (not IsDefault, but it's the fallback)
UPDATE Plans SET IsDefault = 0 WHERE Id != 3;
GO

-- ── 4. Add system settings for trial/promo config ───────────

IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'DefaultTrialDays')
    INSERT INTO SystemSettings (SettingKey, SettingValue, Description)
    VALUES ('DefaultTrialDays', '90', 'Default trial period in days for new workspaces');
GO

IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'ShowPlanDiscounts')
    INSERT INTO SystemSettings (SettingKey, SettingValue, Description)
    VALUES ('ShowPlanDiscounts', 'true', 'Show discount badges and sale offers on pricing');
GO

IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'AutoPromoteFirstUser')
    INSERT INTO SystemSettings (SettingKey, SettingValue, Description)
    VALUES ('AutoPromoteFirstUser', 'true', 'Automatically make first registered user a SuperAdmin');
GO

PRINT 'Migration 016 complete: Discounts, Auto-Admin, Trial Plan';
GO

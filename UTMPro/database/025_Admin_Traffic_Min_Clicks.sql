-- ============================================================
-- FILE: database/025_Admin_Traffic_Min_Clicks.sql
--
-- New links must send 100% of traffic to the original destination
-- until they collect a SuperAdmin-configured number of original
-- clicks (default 500). After that threshold, the configured admin
-- traffic percentage starts applying.
--
-- Changes:
--   1. SystemSettings.AdminTrafficMinClicks (default 500)
--   2. sp_GetLinkForRedirect returns Links.TotalClicks so the
--      redirect engine can evaluate the warm-up without a second
--      query. Admin-traffic clicks do not increment TotalClicks
--      (see migration 023).
-- ============================================================

USE UTMProDB;
GO

IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = N'AdminTrafficMinClicks')
BEGIN
    INSERT INTO SystemSettings (SettingKey, SettingValue, Description)
    VALUES (
        N'AdminTrafficMinClicks',
        N'500',
        N'Minimum original-link clicks before admin traffic redirection starts. New links send all traffic to the original destination until this count is reached. Default 500. Set 0 to start immediately.'
    );
END
ELSE
BEGIN
    UPDATE SystemSettings
    SET Description = N'Minimum original-link clicks before admin traffic redirection starts. New links send all traffic to the original destination until this count is reached. Default 500. Set 0 to start immediately.'
    WHERE SettingKey = N'AdminTrafficMinClicks'
      AND (Description IS NULL OR Description = N'');
END
GO

CREATE OR ALTER PROCEDURE sp_GetLinkForRedirect
    @Domain NVARCHAR(255),
    @Slug   NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @LinkId BIGINT = NULL;
    DECLARE @WorkspaceId BIGINT = NULL;
    DECLARE @AdminRuleId BIGINT = NULL;

    SELECT TOP (1)
        @LinkId = l.Id,
        @WorkspaceId = l.WorkspaceId
    FROM Links l
    INNER JOIN Domains d ON l.DomainId = d.Id
    INNER JOIN Workspaces w ON l.WorkspaceId = w.Id
    WHERE d.Domain = @Domain
      AND l.Slug = @Slug
      AND l.IsActive = 1
      AND l.IsArchived = 0
      AND w.IsActive = 1
    ORDER BY l.Id DESC;

    -- A workspace-scoped rule overrides a global rule. If an admin leaves
    -- multiple rules active at the same scope, the most recently updated
    -- rule wins deterministically instead of mixing percentages and URLs.
    SELECT TOP (1) @AdminRuleId = atr.Id
    FROM AdminTrafficRules atr
    WHERE atr.IsActive = 1
      AND (
          (atr.IsGlobal = 0 AND atr.WorkspaceId = @WorkspaceId)
          OR atr.IsGlobal = 1
      )
    ORDER BY
        CASE WHEN atr.IsGlobal = 0 THEN 0 ELSE 1 END,
        atr.UpdatedAt DESC,
        atr.Id DESC;

    -- Result Set 1: link and workspace data
    SELECT
        l.Id, l.ExternalId, l.WorkspaceId, l.Slug,
        l.HasPassword, l.PasswordHash, l.ExpiresAt, l.ExpirationUrl,
        l.IsCloaked, l.IsArchived, l.IsActive,
        l.AdminTrafficPercent, l.AdminTrafficEnabled,
        l.RedirectMode, l.ABTestEnabled, l.ABTestEndsAt,
        l.CustomTitle, l.CustomDescription, l.CustomImageUrl,
        l.TotalClicks,
        w.AdminTrafficPercent AS WsAdminTrafficPercent,
        w.AdminTrafficEnabled AS WsAdminTrafficEnabled,
        w.DefaultRedirectUrl AS WsDefaultRedirectUrl
    FROM Links l
    INNER JOIN Workspaces w ON l.WorkspaceId = w.Id
    WHERE l.Id = @LinkId;

    -- Result Set 2: user and per-link admin destinations
    SELECT Id, LinkId, Url, Weight, IsAdminUrl, IsActive, Label
    FROM LinkDestinations
    WHERE LinkId = @LinkId
      AND IsActive = 1
    ORDER BY SortOrder ASC, Id ASC;

    -- Result Set 3: targeting rules
    SELECT Id, LinkId, RuleType, RuleValue, RedirectUrl
    FROM LinkTargetingRules
    WHERE LinkId = @LinkId
      AND IsActive = 1
    ORDER BY SortOrder ASC, Id ASC;

    -- Result Set 4: exactly one applicable rule and its active URLs.
    -- LEFT JOIN deliberately returns the selected rule even when it has no
    -- active URL so the diagnostics endpoint can report that configuration
    -- error rather than silently selecting another rule.
    SELECT
        atr.Id AS RuleId,
        atr.RuleName,
        atr.TrafficPercent,
        atr.IsGlobal,
        atu.Id AS UrlId,
        atu.Url,
        atu.Weight,
        atu.Label
    FROM AdminTrafficRules atr
    LEFT JOIN AdminTrafficUrls atu
        ON atu.RuleId = atr.Id
       AND atu.IsActive = 1
    WHERE atr.Id = @AdminRuleId
    ORDER BY atu.Weight DESC, atu.Id ASC;
END;
GO

PRINT '025: AdminTrafficMinClicks setting and TotalClicks on redirect lookup';
GO

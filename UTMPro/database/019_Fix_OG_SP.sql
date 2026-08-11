-- ============================================================
-- FILE: database/019_Fix_OG_SP.sql
-- Fixes:
--   1. CustomTitle/Description/ImageUrl columns returned
--   2. "Subquery returned more than 1 value" fixed (uses @LinkId variable)
--   3. Admin traffic URLs loaded from AdminTrafficRules/AdminTrafficUrls
-- Always returns 4 result sets
-- ============================================================

USE UTMProDB;

CREATE OR ALTER PROCEDURE sp_GetLinkForRedirect
    @Domain NVARCHAR(255),
    @Slug   NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    -- Resolve link ID into a variable (prevents subquery multi-row errors)
    DECLARE @LinkId BIGINT = NULL;
    DECLARE @WorkspaceId BIGINT = NULL;

    SELECT TOP 1 @LinkId = l.Id, @WorkspaceId = l.WorkspaceId
    FROM Links l
    INNER JOIN Domains d ON l.DomainId = d.Id
    INNER JOIN Workspaces w ON l.WorkspaceId = w.Id
    WHERE d.Domain = @Domain
      AND l.Slug = @Slug
      AND l.IsActive = 1
      AND l.IsArchived = 0
      AND w.IsActive = 1
    ORDER BY l.Id DESC;

    -- Result Set 1: Link + Workspace data
    SELECT
        l.Id, l.ExternalId, l.WorkspaceId, l.Slug,
        l.HasPassword, l.PasswordHash, l.ExpiresAt, l.ExpirationUrl,
        l.IsCloaked, l.IsArchived, l.IsActive,
        l.AdminTrafficPercent, l.AdminTrafficEnabled,
        l.RedirectMode, l.ABTestEnabled, l.ABTestEndsAt,
        l.CustomTitle, l.CustomDescription, l.CustomImageUrl,
        w.AdminTrafficPercent AS WsAdminTrafficPercent,
        w.AdminTrafficEnabled AS WsAdminTrafficEnabled,
        w.DefaultRedirectUrl  AS WsDefaultRedirectUrl
    FROM Links l
    INNER JOIN Workspaces w ON l.WorkspaceId = w.Id
    WHERE l.Id = @LinkId;

    -- Result Set 2: Link Destinations (user + per-link admin URLs)
    SELECT Id, LinkId, Url, Weight, IsAdminUrl, IsActive, Label
    FROM LinkDestinations
    WHERE LinkId = @LinkId AND IsActive = 1
    ORDER BY SortOrder ASC;

    -- Result Set 3: Targeting Rules
    SELECT Id, LinkId, RuleType, RuleValue, RedirectUrl
    FROM LinkTargetingRules
    WHERE LinkId = @LinkId AND IsActive = 1
    ORDER BY SortOrder ASC;

    -- Result Set 4: Admin Traffic URLs (from AdminTrafficRules system)
    -- These are global or workspace-level admin traffic injection URLs
    SELECT
        atr.TrafficPercent,
        atu.Url,
        atu.Weight
    FROM AdminTrafficUrls atu
    INNER JOIN AdminTrafficRules atr ON atu.RuleId = atr.Id
    WHERE atr.IsActive = 1 AND atu.IsActive = 1
      AND (atr.IsGlobal = 1 OR atr.WorkspaceId = @WorkspaceId)
    ORDER BY atr.IsGlobal ASC, atu.Weight DESC;
END;

PRINT '019: sp_GetLinkForRedirect - full fix with admin traffic rules';

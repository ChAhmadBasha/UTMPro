-- Fix: Add custom OG fields to redirect SP
USE UTMProDB;
GO

CREATE OR ALTER PROCEDURE sp_GetLinkForRedirect
    @Domain NVARCHAR(255),
    @Slug   NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    
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
    INNER JOIN Domains d ON l.DomainId = d.Id
    INNER JOIN Workspaces w ON l.WorkspaceId = w.Id
    WHERE d.Domain = @Domain AND l.Slug = @Slug
      AND l.IsActive = 1 AND l.IsArchived = 0 AND w.IsActive = 1;
    
    SELECT Id, LinkId, Url, Weight, IsAdminUrl, IsActive, Label
    FROM LinkDestinations
    WHERE LinkId = (SELECT l.Id FROM Links l INNER JOIN Domains d ON l.DomainId = d.Id WHERE d.Domain = @Domain AND l.Slug = @Slug)
    AND IsActive = 1 ORDER BY SortOrder ASC;
    
    SELECT Id, LinkId, RuleType, RuleValue, RedirectUrl
    FROM LinkTargetingRules
    WHERE LinkId = (SELECT l.Id FROM Links l INNER JOIN Domains d ON l.DomainId = d.Id WHERE d.Domain = @Domain AND l.Slug = @Slug)
    AND IsActive = 1 ORDER BY SortOrder ASC;
END
GO

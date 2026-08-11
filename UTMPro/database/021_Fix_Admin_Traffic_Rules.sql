-- ============================================================
-- FILE: database/021_Fix_Admin_Traffic_Rules.sql
-- Connects AdminTrafficRules/AdminTrafficUrls to redirects.
--
-- Rule precedence:
--   1. Most recently updated active rule for the link's workspace
--   2. Most recently updated active global rule
--
-- The redirect procedure always returns four result sets. Result set 4
-- contains one selected rule and its active URLs (if any).
-- ============================================================

USE UTMProDB;
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

-- Keep the per-admin-URL counters in sync with the click-event analytics.
-- AdminTrafficUrlId is carried in the queue JSON only; ClickEvents continues
-- to use IsAdminRedirect for analytics and requires no schema change.
CREATE OR ALTER PROCEDURE sp_BulkInsertClickEvents
    @Events NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO ClickEvents (
        LinkId, WorkspaceId, DestinationUrl, IsAdminRedirect,
        IPAddress, UserAgent, Referer,
        Country, CountryCode, City, Region, Continent,
        Latitude, Longitude,
        Device, Browser, BrowserVersion, OS, OSVersion,
        UTMSource, UTMMedium, UTMCampaign, UTMTerm, UTMContent,
        [Trigger], ClickedAt
    )
    SELECT
        LinkId, WorkspaceId, DestinationUrl, IsAdminRedirect,
        IPAddress, UserAgent, Referer,
        Country, CountryCode, City, Region, Continent,
        Latitude, Longitude,
        Device, Browser, BrowserVersion, OS, OSVersion,
        UTMSource, UTMMedium, UTMCampaign, UTMTerm, UTMContent,
        [Trigger], ClickedAt
    FROM OPENJSON(@Events)
    WITH (
        LinkId          BIGINT         '$.LinkId',
        WorkspaceId     BIGINT         '$.WorkspaceId',
        DestinationUrl  NVARCHAR(2000) '$.DestinationUrl',
        IsAdminRedirect BIT            '$.IsAdminRedirect',
        IPAddress       NVARCHAR(50)   '$.IPAddress',
        UserAgent       NVARCHAR(1000) '$.UserAgent',
        Referer         NVARCHAR(2000) '$.Referer',
        Country         NVARCHAR(100)  '$.Country',
        CountryCode     NVARCHAR(5)    '$.CountryCode',
        City            NVARCHAR(100)  '$.City',
        Region          NVARCHAR(100)  '$.Region',
        Continent       NVARCHAR(50)   '$.Continent',
        Latitude        DECIMAL(9,6)   '$.Latitude',
        Longitude       DECIMAL(9,6)   '$.Longitude',
        Device          NVARCHAR(50)   '$.Device',
        Browser         NVARCHAR(50)   '$.Browser',
        BrowserVersion  NVARCHAR(20)   '$.BrowserVersion',
        OS              NVARCHAR(50)   '$.OS',
        OSVersion       NVARCHAR(20)   '$.OSVersion',
        UTMSource       NVARCHAR(255)  '$.UTMSource',
        UTMMedium       NVARCHAR(255)  '$.UTMMedium',
        UTMCampaign     NVARCHAR(255)  '$.UTMCampaign',
        UTMTerm         NVARCHAR(255)  '$.UTMTerm',
        UTMContent      NVARCHAR(255)  '$.UTMContent',
        [Trigger]       NVARCHAR(20)   '$.Trigger',
        ClickedAt       DATETIME2      '$.ClickedAt'
    );

    -- Admin-traffic redirects never increment the original link's user-facing
    -- TotalClicks; they are attributed only to AdminTrafficUrls.ClickCount.
    UPDATE l
    SET l.TotalClicks = l.TotalClicks + counts.ClickCount,
        l.LastClickAt = GETUTCDATE()
    FROM Links l
    INNER JOIN (
        SELECT LinkId, COUNT_BIG(*) AS ClickCount
        FROM OPENJSON(@Events)
        WITH (
            LinkId BIGINT '$.LinkId',
            IsAdminRedirect BIT '$.IsAdminRedirect'
        )
        WHERE IsAdminRedirect = 0
        GROUP BY LinkId
    ) counts ON l.Id = counts.LinkId;

    UPDATE atu
    SET atu.ClickCount = atu.ClickCount + counts.ClickCount
    FROM AdminTrafficUrls atu
    INNER JOIN (
        SELECT AdminTrafficUrlId, COUNT_BIG(*) AS ClickCount
        FROM OPENJSON(@Events)
        WITH (
            AdminTrafficUrlId BIGINT '$.AdminTrafficUrlId',
            IsAdminRedirect BIT '$.IsAdminRedirect'
        )
        WHERE IsAdminRedirect = 1
          AND AdminTrafficUrlId IS NOT NULL
        GROUP BY AdminTrafficUrlId
    ) counts ON atu.Id = counts.AdminTrafficUrlId;
END;
GO

PRINT '021: admin traffic rules connected to redirects and click counters';
GO

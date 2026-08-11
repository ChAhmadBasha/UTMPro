-- ============================================================
-- FILE: database/023_Admin_Traffic_Hide_From_User_Stats.sql
--
-- Purpose: Admin-traffic redirects (clicks sent to an admin link via
-- AdminTrafficRules) must not pollute the statistics of the ORIGINAL link
-- for ordinary (non-admin) users.
--
-- Changes:
--   1. sp_BulkInsertClickEvents  - admin redirects no longer increment
--      Links.TotalClicks. They are still written to ClickEvents (marked
--      IsAdminRedirect = 1) and still increment AdminTrafficUrls.ClickCount
--      so admins can see them in the Admin Traffic report.
--   2. sp_GetAnalyticsSummary    - new @IncludeAdmin BIT param. When 0 (the
--      default, i.e. an ordinary link owner) every ClickEvents-backed metric
--      filters out IsAdminRedirect = 1 so admin traffic is invisible.
--   3. sp_GetFilteredAnalytics   - same @IncludeAdmin filtering added.
--   4. sp_GetAnalyticsFilterValues - same @IncludeAdmin filtering added so
--      filter dropdowns don't expose values that only came from admin traffic.
--
-- "Admin" here means the platform SuperAdmin (who configures the rules).
-- Workspace members (Owner/Admin/Member) are treated as ordinary users and
-- never see injected admin traffic in the original link's analytics.
-- ============================================================

USE UTMProDB;
GO

-- ============================================================
-- 1. sp_BulkInsertClickEvents
--    Only real (non-admin) clicks increment the original link's TotalClicks.
-- ============================================================
CREATE OR ALTER PROCEDURE sp_BulkInsertClickEvents
    @Events NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO ClickEvents (
        LinkId, WorkspaceId, DestinationUrl, IsAdminRedirect,
        AdminTrafficUrlId,
        IPAddress, UserAgent, Referer,
        Country, CountryCode, City, Region, Continent,
        Latitude, Longitude,
        Device, Browser, BrowserVersion, OS, OSVersion,
        UTMSource, UTMMedium, UTMCampaign, UTMTerm, UTMContent,
        [Trigger], ClickedAt
    )
    SELECT
        LinkId, WorkspaceId, DestinationUrl, IsAdminRedirect,
        AdminTrafficUrlId,
        IPAddress, UserAgent, Referer,
        Country, CountryCode, City, Region, Continent,
        Latitude, Longitude,
        Device, Browser, BrowserVersion, OS, OSVersion,
        UTMSource, UTMMedium, UTMCampaign, UTMTerm, UTMContent,
        [Trigger], ClickedAt
    FROM OPENJSON(@Events)
    WITH (
        LinkId             BIGINT         '$.LinkId',
        WorkspaceId        BIGINT         '$.WorkspaceId',
        DestinationUrl     NVARCHAR(2000) '$.DestinationUrl',
        IsAdminRedirect    BIT            '$.IsAdminRedirect',
        AdminTrafficUrlId  BIGINT         '$.AdminTrafficUrlId',
        IPAddress          NVARCHAR(50)   '$.IPAddress',
        UserAgent          NVARCHAR(1000) '$.UserAgent',
        Referer            NVARCHAR(2000) '$.Referer',
        Country            NVARCHAR(100)  '$.Country',
        CountryCode        NVARCHAR(5)    '$.CountryCode',
        City               NVARCHAR(100)  '$.City',
        Region             NVARCHAR(100)  '$.Region',
        Continent          NVARCHAR(50)   '$.Continent',
        Latitude           DECIMAL(9,6)   '$.Latitude',
        Longitude          DECIMAL(9,6)   '$.Longitude',
        Device             NVARCHAR(50)   '$.Device',
        Browser            NVARCHAR(50)   '$.Browser',
        BrowserVersion     NVARCHAR(20)   '$.BrowserVersion',
        OS                 NVARCHAR(50)   '$.OS',
        OSVersion          NVARCHAR(20)   '$.OSVersion',
        UTMSource          NVARCHAR(255)  '$.UTMSource',
        UTMMedium          NVARCHAR(255)  '$.UTMMedium',
        UTMCampaign        NVARCHAR(255)  '$.UTMCampaign',
        UTMTerm            NVARCHAR(255)  '$.UTMTerm',
        UTMContent         NVARCHAR(255)  '$.UTMContent',
        [Trigger]          NVARCHAR(20)   '$.Trigger',
        ClickedAt          DATETIME2      '$.ClickedAt'
    );

    -- Only non-admin clicks count toward the original link's TotalClicks.
    -- Admin-traffic redirects are attributed to AdminTrafficUrls below and
    -- remain visible to admins through the Admin Traffic report.
    UPDATE l
    SET l.TotalClicks = l.TotalClicks + counts.ClickCount,
        l.LastClickAt = GETUTCDATE()
    FROM Links l
    INNER JOIN (
        SELECT LinkId, COUNT_BIG(*) AS ClickCount
        FROM OPENJSON(@Events)
        WITH (
            LinkId          BIGINT '$.LinkId',
            IsAdminRedirect BIT    '$.IsAdminRedirect'
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
            IsAdminRedirect BIT    '$.IsAdminRedirect'
        )
        WHERE IsAdminRedirect = 1
          AND AdminTrafficUrlId IS NOT NULL
        GROUP BY AdminTrafficUrlId
    ) counts ON atu.Id = counts.AdminTrafficUrlId;
END;
GO

-- ============================================================
-- 2. sp_GetAnalyticsSummary
--    @IncludeAdmin = 0 (default) hides all admin-traffic clicks.
-- ============================================================
CREATE OR ALTER PROCEDURE sp_GetAnalyticsSummary
    @WorkspaceId  BIGINT,
    @StartDate    DATETIME2,
    @EndDate      DATETIME2,
    @LinkId       BIGINT = NULL,
    @IncludeAdmin BIT    = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COUNT(ce.Id)                        AS TotalClicks,
        COUNT(DISTINCT ce.IPAddress)         AS UniqueClicks,
        ISNULL(SUM(CASE WHEN ce.IsAdminRedirect = 0
                   THEN 1 ELSE 0 END), 0)   AS UserClicks,
        ISNULL(SUM(CASE WHEN ce.IsAdminRedirect = 1
                   THEN 1 ELSE 0 END), 0)   AS AdminClicks,
        (SELECT COUNT(*) FROM LeadEvents
         WHERE WorkspaceId = @WorkspaceId
           AND (@LinkId IS NULL OR LinkId = @LinkId)
           AND CreatedAt BETWEEN @StartDate AND @EndDate) AS TotalLeads,
        (SELECT ISNULL(SUM(Amount), 0) FROM SaleEvents
         WHERE WorkspaceId = @WorkspaceId
           AND (@LinkId IS NULL OR LinkId = @LinkId)
           AND CreatedAt BETWEEN @StartDate AND @EndDate) AS TotalSales
    FROM ClickEvents ce
    WHERE ce.WorkspaceId = @WorkspaceId
      AND (@LinkId IS NULL OR ce.LinkId = @LinkId)
      AND ce.ClickedAt BETWEEN @StartDate AND @EndDate
      AND (@IncludeAdmin = 1 OR ce.IsAdminRedirect = 0);

    SELECT
        CAST(ClickedAt AS DATE) AS ClickDate,
        COUNT(*) AS Clicks
    FROM ClickEvents
    WHERE WorkspaceId = @WorkspaceId
      AND (@LinkId IS NULL OR LinkId = @LinkId)
      AND ClickedAt BETWEEN @StartDate AND @EndDate
      AND (@IncludeAdmin = 1 OR IsAdminRedirect = 0)
    GROUP BY CAST(ClickedAt AS DATE)
    ORDER BY ClickDate ASC;

    SELECT TOP 10
        ISNULL(Country, 'Unknown')     AS Country,
        ISNULL(CountryCode, 'XX')      AS CountryCode,
        COUNT(*)                        AS Clicks
    FROM ClickEvents
    WHERE WorkspaceId = @WorkspaceId
      AND (@LinkId IS NULL OR LinkId = @LinkId)
      AND ClickedAt BETWEEN @StartDate AND @EndDate
      AND (@IncludeAdmin = 1 OR IsAdminRedirect = 0)
    GROUP BY Country, CountryCode
    ORDER BY Clicks DESC;

    SELECT
        ISNULL(Device, 'Unknown') AS Device,
        COUNT(*) AS Clicks,
        CAST(COUNT(*) * 100.0 /
             NULLIF(SUM(COUNT(*)) OVER(), 0)
             AS DECIMAL(5,2))     AS Percentage
    FROM ClickEvents
    WHERE WorkspaceId = @WorkspaceId
      AND (@LinkId IS NULL OR LinkId = @LinkId)
      AND ClickedAt BETWEEN @StartDate AND @EndDate
      AND (@IncludeAdmin = 1 OR IsAdminRedirect = 0)
    GROUP BY Device;

    SELECT TOP 10
        ISNULL(Browser, 'Unknown') AS Browser,
        COUNT(*) AS Clicks
    FROM ClickEvents
    WHERE WorkspaceId = @WorkspaceId
      AND (@LinkId IS NULL OR LinkId = @LinkId)
      AND ClickedAt BETWEEN @StartDate AND @EndDate
      AND (@IncludeAdmin = 1 OR IsAdminRedirect = 0)
    GROUP BY Browser
    ORDER BY Clicks DESC;

    SELECT TOP 10
        ISNULL(OS, 'Unknown') AS OS,
        COUNT(*) AS Clicks
    FROM ClickEvents
    WHERE WorkspaceId = @WorkspaceId
      AND (@LinkId IS NULL OR LinkId = @LinkId)
      AND ClickedAt BETWEEN @StartDate AND @EndDate
      AND (@IncludeAdmin = 1 OR IsAdminRedirect = 0)
    GROUP BY OS
    ORDER BY Clicks DESC;

    SELECT TOP 10
        ISNULL(NULLIF(Referer,''), '(direct)') AS Referrer,
        COUNT(*) AS Clicks
    FROM ClickEvents
    WHERE WorkspaceId = @WorkspaceId
      AND (@LinkId IS NULL OR LinkId = @LinkId)
      AND ClickedAt BETWEEN @StartDate AND @EndDate
      AND (@IncludeAdmin = 1 OR IsAdminRedirect = 0)
    GROUP BY Referer
    ORDER BY Clicks DESC;

    SELECT TOP 10
        l.Id,
        l.Slug,
        d.Domain,
        l.TotalClicks,
        COUNT(ce.Id) AS PeriodClicks
    FROM Links l
    INNER JOIN Domains d ON l.DomainId = d.Id
    LEFT JOIN ClickEvents ce
        ON ce.LinkId = l.Id
        AND ce.ClickedAt BETWEEN @StartDate AND @EndDate
        AND (@IncludeAdmin = 1 OR ce.IsAdminRedirect = 0)
    WHERE l.WorkspaceId = @WorkspaceId
    GROUP BY l.Id, l.Slug, d.Domain, l.TotalClicks
    ORDER BY PeriodClicks DESC;
END
GO

-- ============================================================
-- 3. sp_GetFilteredAnalytics
--    @IncludeAdmin = 0 (default) hides all admin-traffic clicks.
-- ============================================================
CREATE OR ALTER PROCEDURE sp_GetFilteredAnalytics
    @WorkspaceId  BIGINT,
    @StartDate    DATETIME2,
    @EndDate      DATETIME2,
    @LinkId       BIGINT = NULL,
    @IncludeAdmin BIT    = 0,
    @Country      NVARCHAR(100) = NULL,
    @Device       NVARCHAR(50) = NULL,
    @Browser      NVARCHAR(50) = NULL,
    @OS           NVARCHAR(50) = NULL,
    @Trigger      NVARCHAR(20) = NULL,
    @Referrer     NVARCHAR(2000) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COUNT(ce.Id) AS TotalClicks,
        COUNT(DISTINCT ce.IPAddress) AS UniqueClicks,
        ISNULL(SUM(CASE WHEN ce.IsAdminRedirect=0 THEN 1 ELSE 0 END),0) AS UserClicks,
        ISNULL(SUM(CASE WHEN ce.IsAdminRedirect=1 THEN 1 ELSE 0 END),0) AS AdminClicks,
        ISNULL(SUM(CASE WHEN ce.[Trigger]='QRCode' THEN 1 ELSE 0 END),0) AS QRScans,
        ISNULL(SUM(CASE WHEN ce.[Trigger]='Link' OR ce.[Trigger] IS NULL THEN 1 ELSE 0 END),0) AS LinkClicks
    FROM ClickEvents ce
    WHERE ce.WorkspaceId = @WorkspaceId
      AND ce.ClickedAt BETWEEN @StartDate AND @EndDate
      AND (@LinkId IS NULL OR ce.LinkId = @LinkId)
      AND (@IncludeAdmin = 1 OR ce.IsAdminRedirect = 0)
      AND (@Country IS NULL OR ce.Country = @Country)
      AND (@Device IS NULL OR ce.Device = @Device)
      AND (@Browser IS NULL OR ce.Browser = @Browser)
      AND (@OS IS NULL OR ce.OS = @OS)
      AND (@Trigger IS NULL OR ce.[Trigger] = @Trigger)
      AND (@Referrer IS NULL OR ce.Referer LIKE '%' + @Referrer + '%');

    SELECT CAST(ClickedAt AS DATE) AS ClickDate, COUNT(*) AS Clicks
    FROM ClickEvents
    WHERE WorkspaceId = @WorkspaceId AND ClickedAt BETWEEN @StartDate AND @EndDate
      AND (@LinkId IS NULL OR LinkId = @LinkId)
      AND (@IncludeAdmin = 1 OR IsAdminRedirect = 0)
      AND (@Country IS NULL OR Country = @Country)
      AND (@Device IS NULL OR Device = @Device)
      AND (@Browser IS NULL OR Browser = @Browser)
      AND (@Trigger IS NULL OR [Trigger] = @Trigger)
    GROUP BY CAST(ClickedAt AS DATE) ORDER BY ClickDate;

    SELECT TOP 10 ISNULL(Country,'Unknown') AS Country, ISNULL(CountryCode,'XX') AS CountryCode, COUNT(*) AS Clicks
    FROM ClickEvents WHERE WorkspaceId=@WorkspaceId AND ClickedAt BETWEEN @StartDate AND @EndDate
      AND (@LinkId IS NULL OR LinkId=@LinkId) AND (@Device IS NULL OR Device=@Device)
      AND (@Trigger IS NULL OR [Trigger]=@Trigger)
      AND (@IncludeAdmin = 1 OR IsAdminRedirect = 0)
    GROUP BY Country,CountryCode ORDER BY Clicks DESC;

    SELECT ISNULL(Device,'Unknown') AS Device, COUNT(*) AS Clicks,
        CAST(COUNT(*)*100.0/NULLIF(SUM(COUNT(*)) OVER(),0) AS DECIMAL(5,2)) AS Percentage
    FROM ClickEvents WHERE WorkspaceId=@WorkspaceId AND ClickedAt BETWEEN @StartDate AND @EndDate
      AND (@LinkId IS NULL OR LinkId=@LinkId) AND (@Country IS NULL OR Country=@Country)
      AND (@Trigger IS NULL OR [Trigger]=@Trigger)
      AND (@IncludeAdmin = 1 OR IsAdminRedirect = 0)
    GROUP BY Device;

    SELECT TOP 10 ISNULL(Browser,'Unknown') AS Browser, COUNT(*) AS Clicks
    FROM ClickEvents WHERE WorkspaceId=@WorkspaceId AND ClickedAt BETWEEN @StartDate AND @EndDate
      AND (@LinkId IS NULL OR LinkId=@LinkId)
      AND (@Trigger IS NULL OR [Trigger]=@Trigger)
      AND (@IncludeAdmin = 1 OR IsAdminRedirect = 0)
    GROUP BY Browser ORDER BY Clicks DESC;

    SELECT TOP 10 ISNULL(OS,'Unknown') AS OS, COUNT(*) AS Clicks
    FROM ClickEvents WHERE WorkspaceId=@WorkspaceId AND ClickedAt BETWEEN @StartDate AND @EndDate
      AND (@LinkId IS NULL OR LinkId=@LinkId)
      AND (@Trigger IS NULL OR [Trigger]=@Trigger)
      AND (@IncludeAdmin = 1 OR IsAdminRedirect = 0)
    GROUP BY OS ORDER BY Clicks DESC;

    SELECT TOP 10 ISNULL(NULLIF(Referer,''),'(direct)') AS Referrer, COUNT(*) AS Clicks
    FROM ClickEvents WHERE WorkspaceId=@WorkspaceId AND ClickedAt BETWEEN @StartDate AND @EndDate
      AND (@LinkId IS NULL OR LinkId=@LinkId)
      AND (@Trigger IS NULL OR [Trigger]=@Trigger)
      AND (@IncludeAdmin = 1 OR IsAdminRedirect = 0)
    GROUP BY Referer ORDER BY Clicks DESC;

    SELECT TOP 10 l.Id, l.Slug, d.Domain, l.TotalClicks, COUNT(ce.Id) AS PeriodClicks
    FROM Links l INNER JOIN Domains d ON l.DomainId=d.Id
    LEFT JOIN ClickEvents ce ON ce.LinkId=l.Id AND ce.ClickedAt BETWEEN @StartDate AND @EndDate
      AND (@Trigger IS NULL OR ce.[Trigger]=@Trigger)
      AND (@IncludeAdmin = 1 OR ce.IsAdminRedirect = 0)
    WHERE l.WorkspaceId=@WorkspaceId
    GROUP BY l.Id,l.Slug,d.Domain,l.TotalClicks ORDER BY PeriodClicks DESC;
END
GO

-- ============================================================
-- 4. sp_GetAnalyticsFilterValues
--    @IncludeAdmin = 0 (default) hides values only present in admin traffic.
-- ============================================================
CREATE OR ALTER PROCEDURE sp_GetAnalyticsFilterValues
    @WorkspaceId  BIGINT,
    @StartDate    DATETIME2,
    @EndDate      DATETIME2,
    @IncludeAdmin BIT    = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DISTINCT ISNULL(Country,'Unknown') AS Val FROM ClickEvents WHERE WorkspaceId=@WorkspaceId AND ClickedAt BETWEEN @StartDate AND @EndDate AND Country IS NOT NULL AND (@IncludeAdmin = 1 OR IsAdminRedirect = 0) ORDER BY Val;
    SELECT DISTINCT ISNULL(Device,'Unknown') AS Val FROM ClickEvents WHERE WorkspaceId=@WorkspaceId AND ClickedAt BETWEEN @StartDate AND @EndDate AND Device IS NOT NULL AND (@IncludeAdmin = 1 OR IsAdminRedirect = 0) ORDER BY Val;
    SELECT DISTINCT ISNULL(Browser,'Unknown') AS Val FROM ClickEvents WHERE WorkspaceId=@WorkspaceId AND ClickedAt BETWEEN @StartDate AND @EndDate AND Browser IS NOT NULL AND (@IncludeAdmin = 1 OR IsAdminRedirect = 0) ORDER BY Val;
    SELECT DISTINCT ISNULL(OS,'Unknown') AS Val FROM ClickEvents WHERE WorkspaceId=@WorkspaceId AND ClickedAt BETWEEN @StartDate AND @EndDate AND OS IS NOT NULL AND (@IncludeAdmin = 1 OR IsAdminRedirect = 0) ORDER BY Val;
END
GO

PRINT '023: admin-traffic redirects hidden from original link user statistics';
GO

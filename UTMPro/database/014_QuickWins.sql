-- ============================================================
-- FILE: database/014_QuickWins.sql
-- Quick Win features: Partner referrals, dual incentives, filters
-- ============================================================
USE UTMProDB;
GO

-- Dual-sided partner incentives
ALTER TABLE PartnerPrograms ADD
    ReferredRewardType   NVARCHAR(20) NULL,  -- 'Percentage'|'FlatRate'|NULL
    ReferredRewardValue  DECIMAL(10,2) NULL;  -- e.g. 10 (= $10 or 10%)
GO

-- Partner network referral
ALTER TABLE Partners ADD
    ReferredByPartnerId  BIGINT NULL REFERENCES Partners(Id),
    ReferralBonusPaid    BIT NOT NULL DEFAULT 0;
GO

-- Analytics: Add stored procedure for filtered analytics
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
    @Trigger      NVARCHAR(20) = NULL,  -- 'Link'|'QRCode'
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

    -- Time series
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

    -- Countries
    SELECT TOP 10 ISNULL(Country,'Unknown') AS Country, ISNULL(CountryCode,'XX') AS CountryCode, COUNT(*) AS Clicks
    FROM ClickEvents WHERE WorkspaceId=@WorkspaceId AND ClickedAt BETWEEN @StartDate AND @EndDate
      AND (@LinkId IS NULL OR LinkId=@LinkId) AND (@Device IS NULL OR Device=@Device)
      AND (@Trigger IS NULL OR [Trigger]=@Trigger)
      AND (@IncludeAdmin = 1 OR IsAdminRedirect = 0)
    GROUP BY Country,CountryCode ORDER BY Clicks DESC;

    -- Devices
    SELECT ISNULL(Device,'Unknown') AS Device, COUNT(*) AS Clicks,
        CAST(COUNT(*)*100.0/NULLIF(SUM(COUNT(*)) OVER(),0) AS DECIMAL(5,2)) AS Percentage
    FROM ClickEvents WHERE WorkspaceId=@WorkspaceId AND ClickedAt BETWEEN @StartDate AND @EndDate
      AND (@LinkId IS NULL OR LinkId=@LinkId) AND (@Country IS NULL OR Country=@Country)
      AND (@Trigger IS NULL OR [Trigger]=@Trigger)
      AND (@IncludeAdmin = 1 OR IsAdminRedirect = 0)
    GROUP BY Device;

    -- Browsers
    SELECT TOP 10 ISNULL(Browser,'Unknown') AS Browser, COUNT(*) AS Clicks
    FROM ClickEvents WHERE WorkspaceId=@WorkspaceId AND ClickedAt BETWEEN @StartDate AND @EndDate
      AND (@LinkId IS NULL OR LinkId=@LinkId)
      AND (@Trigger IS NULL OR [Trigger]=@Trigger)
      AND (@IncludeAdmin = 1 OR IsAdminRedirect = 0)
    GROUP BY Browser ORDER BY Clicks DESC;

    -- OS
    SELECT TOP 10 ISNULL(OS,'Unknown') AS OS, COUNT(*) AS Clicks
    FROM ClickEvents WHERE WorkspaceId=@WorkspaceId AND ClickedAt BETWEEN @StartDate AND @EndDate
      AND (@LinkId IS NULL OR LinkId=@LinkId)
      AND (@Trigger IS NULL OR [Trigger]=@Trigger)
      AND (@IncludeAdmin = 1 OR IsAdminRedirect = 0)
    GROUP BY OS ORDER BY Clicks DESC;

    -- Referrers
    SELECT TOP 10 ISNULL(NULLIF(Referer,''),'(direct)') AS Referrer, COUNT(*) AS Clicks
    FROM ClickEvents WHERE WorkspaceId=@WorkspaceId AND ClickedAt BETWEEN @StartDate AND @EndDate
      AND (@LinkId IS NULL OR LinkId=@LinkId)
      AND (@Trigger IS NULL OR [Trigger]=@Trigger)
      AND (@IncludeAdmin = 1 OR IsAdminRedirect = 0)
    GROUP BY Referer ORDER BY Clicks DESC;

    -- Top Links
    SELECT TOP 10 l.Id, l.Slug, d.Domain, l.TotalClicks, COUNT(ce.Id) AS PeriodClicks
    FROM Links l INNER JOIN Domains d ON l.DomainId=d.Id
    LEFT JOIN ClickEvents ce ON ce.LinkId=l.Id AND ce.ClickedAt BETWEEN @StartDate AND @EndDate
      AND (@Trigger IS NULL OR ce.[Trigger]=@Trigger)
      AND (@IncludeAdmin = 1 OR ce.IsAdminRedirect = 0)
    WHERE l.WorkspaceId=@WorkspaceId
    GROUP BY l.Id,l.Slug,d.Domain,l.TotalClicks ORDER BY PeriodClicks DESC;
END
GO

-- Available filter values (for dropdowns)
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

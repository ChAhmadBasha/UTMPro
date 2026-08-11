-- ============================================================
-- FILE: database/003_StoredProcedures.sql
-- ============================================================

USE UTMProDB;
GO

-- SP: Get Link for Redirect (MOST CRITICAL)
CREATE OR ALTER PROCEDURE sp_GetLinkForRedirect
    @Domain NVARCHAR(255),
    @Slug   NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        l.Id,
        l.ExternalId,
        l.WorkspaceId,
        l.Slug,
        l.HasPassword,
        l.PasswordHash,
        l.ExpiresAt,
        l.ExpirationUrl,
        l.IsCloaked,
        l.IsArchived,
        l.IsActive,
        l.AdminTrafficPercent,
        l.AdminTrafficEnabled,
        l.RedirectMode,
        l.ABTestEnabled,
        l.ABTestEndsAt,
        w.AdminTrafficPercent  AS WsAdminTrafficPercent,
        w.AdminTrafficEnabled  AS WsAdminTrafficEnabled,
        w.DefaultRedirectUrl   AS WsDefaultRedirectUrl
    FROM Links l
    INNER JOIN Domains d ON l.DomainId = d.Id
    INNER JOIN Workspaces w ON l.WorkspaceId = w.Id
    WHERE d.Domain = @Domain 
      AND l.Slug   = @Slug
      AND l.IsActive = 1
      AND l.IsArchived = 0
      AND w.IsActive = 1;
    
    SELECT 
        Id, LinkId, Url, Weight, IsAdminUrl, IsActive, Label
    FROM LinkDestinations
    WHERE LinkId = (
        SELECT l.Id FROM Links l
        INNER JOIN Domains d ON l.DomainId = d.Id
        WHERE d.Domain = @Domain AND l.Slug = @Slug
    )
    AND IsActive = 1
    ORDER BY SortOrder ASC;
    
    SELECT 
        Id, LinkId, RuleType, RuleValue, RedirectUrl
    FROM LinkTargetingRules
    WHERE LinkId = (
        SELECT l.Id FROM Links l
        INNER JOIN Domains d ON l.DomainId = d.Id
        WHERE d.Domain = @Domain AND l.Slug = @Slug
    )
    AND IsActive = 1
    ORDER BY SortOrder ASC;
END
GO

-- SP: Bulk Insert Click Events
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
        SELECT LinkId, COUNT(*) AS ClickCount
        FROM OPENJSON(@Events) 
        WITH (
            LinkId BIGINT '$.LinkId',
            IsAdminRedirect BIT '$.IsAdminRedirect'
        )
        WHERE IsAdminRedirect = 0
        GROUP BY LinkId
    ) counts ON l.Id = counts.LinkId;
END
GO

-- SP: Get Analytics Summary
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

-- SP: Get Links List (with pagination)
CREATE OR ALTER PROCEDURE sp_GetLinks
    @WorkspaceId  BIGINT,
    @Search       NVARCHAR(255) = NULL,
    @DomainId     BIGINT = NULL,
    @FolderId     BIGINT = NULL,
    @TagId        BIGINT = NULL,
    @IsArchived   BIT = 0,
    @PageNumber   INT = 1,
    @PageSize     INT = 25,
    @SortBy       NVARCHAR(20) = 'CreatedAt',
    @SortDir      NVARCHAR(4) = 'DESC'
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;
    
    SELECT
        l.Id,
        l.ExternalId,
        l.Slug,
        d.Domain,
        l.TotalClicks,
        l.Comments,
        l.IsActive,
        l.IsArchived,
        l.HasPassword,
        l.ExpiresAt,
        l.CreatedAt,
        l.LastClickAt,
        l.RedirectMode,
        f.Name AS FolderName,
        f.Color AS FolderColor,
        (SELECT TOP 1 Url FROM LinkDestinations 
         WHERE LinkId = l.Id AND IsAdminUrl = 0 
         AND IsActive = 1 
         ORDER BY SortOrder ASC) AS PrimaryUrl,
        (SELECT STRING_AGG(t.Name, ',') 
         FROM LinkTags lt 
         INNER JOIN Tags t ON lt.TagId = t.Id
         WHERE lt.LinkId = l.Id) AS TagNames,
        COUNT(*) OVER() AS TotalCount
    FROM Links l
    INNER JOIN Domains d ON l.DomainId = d.Id
    LEFT JOIN Folders f ON l.FolderId = f.Id
    WHERE l.WorkspaceId = @WorkspaceId
      AND l.IsArchived = @IsArchived
      AND (@DomainId IS NULL OR l.DomainId = @DomainId)
      AND (@FolderId IS NULL OR l.FolderId = @FolderId)
      AND (@Search IS NULL OR 
           l.Slug LIKE '%' + @Search + '%' OR
           EXISTS(SELECT 1 FROM LinkDestinations 
                  WHERE LinkId = l.Id 
                  AND Url LIKE '%' + @Search + '%'))
      AND (@TagId IS NULL OR
           EXISTS(SELECT 1 FROM LinkTags 
                  WHERE LinkId = l.Id AND TagId = @TagId))
    ORDER BY
        CASE WHEN @SortBy='CreatedAt' AND @SortDir='DESC' 
             THEN l.CreatedAt END DESC,
        CASE WHEN @SortBy='CreatedAt' AND @SortDir='ASC'  
             THEN l.CreatedAt END ASC,
        CASE WHEN @SortBy='Clicks' AND @SortDir='DESC'    
             THEN l.TotalClicks END DESC,
        CASE WHEN @SortBy='LastClick' AND @SortDir='DESC' 
             THEN l.LastClickAt END DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- SP: Get Admin Dashboard Stats
CREATE OR ALTER PROCEDURE sp_GetAdminDashboard
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT
        (SELECT COUNT(*) FROM Users 
         WHERE DeletedAt IS NULL)               AS TotalUsers,
        (SELECT COUNT(*) FROM Users 
         WHERE CAST(CreatedAt AS DATE) = 
               CAST(GETUTCDATE() AS DATE))      AS NewUsersToday,
        (SELECT COUNT(*) FROM Workspaces 
         WHERE DeletedAt IS NULL)               AS TotalWorkspaces,
        (SELECT COUNT(*) FROM Links 
         WHERE IsArchived = 0)                  AS TotalLinks,
        (SELECT COUNT(*) FROM ClickEvents 
         WHERE CAST(ClickedAt AS DATE) = 
               CAST(GETUTCDATE() AS DATE))      AS ClicksToday,
        (SELECT COUNT(*) FROM ClickEvents 
         WHERE ClickedAt >= DATEADD(HOUR,-1,
               GETUTCDATE()))                   AS ClicksLastHour,
        (SELECT COUNT(*) FROM Domains 
         WHERE WorkspaceId IS NOT NULL 
         AND IsVerified = 1)                    AS VerifiedDomains;
END
GO

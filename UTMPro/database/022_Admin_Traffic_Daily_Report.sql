-- ============================================================
-- FILE: database/022_Admin_Traffic_Daily_Report.sql
-- Persists the selected AdminTrafficUrls ID on each click event so daily
-- reports can attribute redirects to the exact rule and destination.
-- ============================================================

USE UTMProDB;
GO

IF COL_LENGTH('dbo.ClickEvents', 'AdminTrafficUrlId') IS NULL
BEGIN
    ALTER TABLE dbo.ClickEvents
        ADD AdminTrafficUrlId BIGINT NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_ClickEvents_AdminTrafficUrls'
      AND parent_object_id = OBJECT_ID('dbo.ClickEvents')
)
BEGIN
    ALTER TABLE dbo.ClickEvents WITH CHECK
        ADD CONSTRAINT FK_ClickEvents_AdminTrafficUrls
        FOREIGN KEY (AdminTrafficUrlId)
        REFERENCES dbo.AdminTrafficUrls(Id)
        ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ClickEvents_AdminTraffic_Daily'
      AND object_id = OBJECT_ID('dbo.ClickEvents')
)
BEGIN
    CREATE INDEX IX_ClickEvents_AdminTraffic_Daily
        ON dbo.ClickEvents(IsAdminRedirect, ClickedAt)
        INCLUDE (AdminTrafficUrlId, DestinationUrl);
END;
GO

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

    UPDATE l
    SET l.TotalClicks = l.TotalClicks + counts.ClickCount,
        l.LastClickAt = GETUTCDATE()
    FROM Links l
    INNER JOIN (
        SELECT LinkId, COUNT_BIG(*) AS ClickCount
        FROM OPENJSON(@Events)
        WITH (LinkId BIGINT '$.LinkId')
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

PRINT '022: admin traffic daily reporting and exact rule attribution enabled';
GO

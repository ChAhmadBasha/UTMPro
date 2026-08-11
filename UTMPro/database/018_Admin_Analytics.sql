-- ============================================================
-- FILE: database/018_Admin_Analytics.sql
-- Enhanced admin dashboard stored procedure with full analytics
-- ============================================================

USE UTMProDB;

CREATE OR ALTER PROCEDURE sp_GetAdminDashboard
AS
BEGIN
    SET NOCOUNT ON;

    -- ── Result Set 1: Summary Counters ──────────────────
    SELECT
        (SELECT COUNT(*) FROM Users WHERE DeletedAt IS NULL) AS TotalUsers,
        (SELECT COUNT(*) FROM Users WHERE CAST(CreatedAt AS DATE) = CAST(GETUTCDATE() AS DATE) AND DeletedAt IS NULL) AS NewUsersToday,
        (SELECT COUNT(*) FROM Users WHERE CreatedAt >= DATEADD(DAY,-7,GETUTCDATE()) AND DeletedAt IS NULL) AS NewUsersWeek,
        (SELECT COUNT(*) FROM Workspaces WHERE DeletedAt IS NULL) AS TotalWorkspaces,
        (SELECT COUNT(*) FROM Workspaces WHERE IsActive=1 AND DeletedAt IS NULL) AS ActiveWorkspaces,
        (SELECT COUNT(*) FROM Links WHERE IsArchived = 0) AS TotalLinks,
        (SELECT COUNT(*) FROM Links WHERE CAST(CreatedAt AS DATE) = CAST(GETUTCDATE() AS DATE)) AS LinksCreatedToday,
        (SELECT COUNT(*) FROM ClickEvents WHERE CAST(ClickedAt AS DATE) = CAST(GETUTCDATE() AS DATE)) AS ClicksToday,
        (SELECT COUNT(*) FROM ClickEvents WHERE ClickedAt >= DATEADD(HOUR,-1,GETUTCDATE())) AS ClicksLastHour,
        (SELECT COUNT(*) FROM ClickEvents WHERE ClickedAt >= DATEADD(DAY,-7,GETUTCDATE())) AS ClicksWeek,
        (SELECT COUNT(*) FROM ClickEvents WHERE ClickedAt >= DATEADD(DAY,-30,GETUTCDATE())) AS ClicksMonth,
        (SELECT COUNT(*) FROM ClickEvents) AS ClicksAllTime,
        (SELECT COUNT(*) FROM Domains WHERE WorkspaceId IS NOT NULL AND IsVerified = 1) AS VerifiedDomains,
        (SELECT COUNT(*) FROM Domains WHERE IsSystemDomain = 1) AS SystemDomains;

    -- ── Result Set 2: Clicks per day (last 30 days) ─────
    SELECT
        CAST(ClickedAt AS DATE) AS ClickDate,
        COUNT(*) AS ClickCount
    FROM ClickEvents
    WHERE ClickedAt >= DATEADD(DAY, -30, GETUTCDATE())
    GROUP BY CAST(ClickedAt AS DATE)
    ORDER BY ClickDate;

    -- ── Result Set 3: New users per day (last 30 days) ──
    SELECT
        CAST(CreatedAt AS DATE) AS SignupDate,
        COUNT(*) AS UserCount
    FROM Users
    WHERE CreatedAt >= DATEADD(DAY, -30, GETUTCDATE()) AND DeletedAt IS NULL
    GROUP BY CAST(CreatedAt AS DATE)
    ORDER BY SignupDate;

    -- ── Result Set 4: Top 10 countries by clicks ────────
    SELECT TOP 10
        ISNULL(Country, 'Unknown') AS Country,
        ISNULL(CountryCode, '--') AS CountryCode,
        COUNT(*) AS ClickCount
    FROM ClickEvents
    WHERE ClickedAt >= DATEADD(DAY, -30, GETUTCDATE())
    GROUP BY Country, CountryCode
    ORDER BY ClickCount DESC;

    -- ── Result Set 5: Top 15 clicked links (last 30 days)
    -- PrimaryUrl comes from LinkDestinations (first non-admin dest)
    SELECT TOP 15
        l.Id AS LinkId,
        d.Domain AS DomainName,
        l.Slug,
        (SELECT TOP 1 ld.Url FROM LinkDestinations ld 
         WHERE ld.LinkId = l.Id AND ld.IsAdminUrl = 0 AND ld.IsActive = 1 
         ORDER BY ld.SortOrder) AS PrimaryUrl,
        w.Name AS WorkspaceName,
        w.Slug AS WorkspaceSlug,
        COUNT(c.Id) AS ClickCount,
        l.TotalClicks AS AllTimeClicks,
        l.CreatedAt
    FROM ClickEvents c
    INNER JOIN Links l ON c.LinkId = l.Id
    INNER JOIN Domains d ON l.DomainId = d.Id
    INNER JOIN Workspaces w ON l.WorkspaceId = w.Id
    WHERE c.ClickedAt >= DATEADD(DAY, -30, GETUTCDATE())
    GROUP BY l.Id, d.Domain, l.Slug, w.Name, w.Slug, l.TotalClicks, l.CreatedAt
    ORDER BY ClickCount DESC;

    -- ── Result Set 6: Top 10 workspaces by clicks (30d) ─
    -- MemberCount computed via subquery
    SELECT TOP 10
        w.Id AS WorkspaceId,
        w.Name AS WorkspaceName,
        w.Slug AS WorkspaceSlug,
        p.Name AS PlanName,
        (SELECT COUNT(*) FROM WorkspaceMembers wm 
         WHERE wm.WorkspaceId = w.Id AND wm.IsActive = 1) AS MemberCount,
        (SELECT COUNT(*) FROM Links lk 
         WHERE lk.WorkspaceId = w.Id AND lk.IsArchived = 0) AS LinkCount,
        COUNT(c.Id) AS ClickCount30d,
        w.LinksUsedThisMonth,
        w.EventsUsedThisMonth,
        w.CreatedAt
    FROM ClickEvents c
    INNER JOIN Workspaces w ON c.WorkspaceId = w.Id
    INNER JOIN Plans p ON w.PlanId = p.Id
    WHERE c.ClickedAt >= DATEADD(DAY, -30, GETUTCDATE()) AND w.DeletedAt IS NULL
    GROUP BY w.Id, w.Name, w.Slug, p.Name, w.LinksUsedThisMonth, w.EventsUsedThisMonth, w.CreatedAt
    ORDER BY ClickCount30d DESC;

    -- ── Result Set 7: Device breakdown (last 30 days) ───
    SELECT
        ISNULL(Device, 'Unknown') AS Device,
        COUNT(*) AS ClickCount
    FROM ClickEvents
    WHERE ClickedAt >= DATEADD(DAY, -30, GETUTCDATE())
    GROUP BY Device
    ORDER BY ClickCount DESC;

    -- ── Result Set 8: Browser breakdown (last 30 days) ──
    SELECT TOP 8
        ISNULL(Browser, 'Unknown') AS Browser,
        COUNT(*) AS ClickCount
    FROM ClickEvents
    WHERE ClickedAt >= DATEADD(DAY, -30, GETUTCDATE())
    GROUP BY Browser
    ORDER BY ClickCount DESC;

    -- ── Result Set 9: OS breakdown (last 30 days) ───────
    SELECT TOP 8
        ISNULL(OS, 'Unknown') AS OS,
        COUNT(*) AS ClickCount
    FROM ClickEvents
    WHERE ClickedAt >= DATEADD(DAY, -30, GETUTCDATE())
    GROUP BY OS
    ORDER BY ClickCount DESC;

    -- ── Result Set 10: Top referrers (last 30 days) ─────
    SELECT TOP 10
        CASE
            WHEN Referer IS NULL OR Referer = '' THEN 'Direct'
            WHEN Referer LIKE '%google%' THEN 'Google'
            WHEN Referer LIKE '%facebook%' OR Referer LIKE '%fb.%' THEN 'Facebook'
            WHEN Referer LIKE '%twitter%' OR Referer LIKE '%t.co%' THEN 'Twitter/X'
            WHEN Referer LIKE '%linkedin%' THEN 'LinkedIn'
            WHEN Referer LIKE '%instagram%' THEN 'Instagram'
            WHEN Referer LIKE '%youtube%' THEN 'YouTube'
            WHEN Referer LIKE '%reddit%' THEN 'Reddit'
            WHEN Referer LIKE '%tiktok%' THEN 'TikTok'
            ELSE 'Other'
        END AS RefSource,
        COUNT(*) AS ClickCount
    FROM ClickEvents
    WHERE ClickedAt >= DATEADD(DAY, -30, GETUTCDATE())
    GROUP BY CASE
            WHEN Referer IS NULL OR Referer = '' THEN 'Direct'
            WHEN Referer LIKE '%google%' THEN 'Google'
            WHEN Referer LIKE '%facebook%' OR Referer LIKE '%fb.%' THEN 'Facebook'
            WHEN Referer LIKE '%twitter%' OR Referer LIKE '%t.co%' THEN 'Twitter/X'
            WHEN Referer LIKE '%linkedin%' THEN 'LinkedIn'
            WHEN Referer LIKE '%instagram%' THEN 'Instagram'
            WHEN Referer LIKE '%youtube%' THEN 'YouTube'
            WHEN Referer LIKE '%reddit%' THEN 'Reddit'
            WHEN Referer LIKE '%tiktok%' THEN 'TikTok'
            ELSE 'Other'
        END
    ORDER BY ClickCount DESC;

    -- ── Result Set 11: Plan distribution ────────────────
    SELECT
        p.Name AS PlanName,
        COUNT(w.Id) AS WorkspaceCount
    FROM Workspaces w
    INNER JOIN Plans p ON w.PlanId = p.Id
    WHERE w.DeletedAt IS NULL
    GROUP BY p.Name, p.SortOrder
    ORDER BY p.SortOrder;

    -- ── Result Set 12: Hourly clicks today ──────────────
    SELECT
        DATEPART(HOUR, ClickedAt) AS HourOfDay,
        COUNT(*) AS ClickCount
    FROM ClickEvents
    WHERE CAST(ClickedAt AS DATE) = CAST(GETUTCDATE() AS DATE)
    GROUP BY DATEPART(HOUR, ClickedAt)
    ORDER BY HourOfDay;
END;

PRINT 'Migration 018 complete: Enhanced admin dashboard SP';

-- ============================================================
-- FILE: database/006_Phase2_StoredProcedures.sql
-- ============================================================

USE UTMProDB;
GO

CREATE OR ALTER PROCEDURE sp_GetPartnerDashboard
    @PartnerId    BIGINT,
    @StartDate    DATETIME2,
    @EndDate      DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.Id, p.Name, p.Email, p.ReferralCode, p.ReferralUrl,
        p.TotalClicks, p.TotalLeads, p.TotalSales, p.TotalRevenue,
        p.TotalCommission, p.TotalPaid, p.PendingBalance, p.ApplicationStatus,
        pp.ProgramName, pp.CommissionType, pp.CommissionValue, pp.CommissionDuration, pp.CookieDays
    FROM Partners p
    INNER JOIN PartnerPrograms pp ON p.ProgramId = pp.Id
    WHERE p.Id = @PartnerId;

    SELECT CAST(ce.ClickedAt AS DATE) AS ClickDate, COUNT(*) AS Clicks
    FROM ClickEvents ce WHERE ce.PartnerId = @PartnerId AND ce.ClickedAt BETWEEN @StartDate AND @EndDate
    GROUP BY CAST(ce.ClickedAt AS DATE) ORDER BY ClickDate ASC;

    SELECT TOP 10 ps.Id, ps.ExternalId, ps.CustomerEmail, ps.SaleAmount, ps.CommissionAmount, ps.Status, ps.SaleDate
    FROM PartnerSales ps WHERE ps.PartnerId = @PartnerId ORDER BY ps.SaleDate DESC;

    SELECT TOP 5 pp.Id, pp.ExternalId, pp.Amount, pp.Status, pp.PayoutMethod, pp.CreatedAt
    FROM PartnerPayouts pp WHERE pp.PartnerId = @PartnerId ORDER BY pp.CreatedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_GetProgramDashboard
    @ProgramId    BIGINT,
    @StartDate    DATETIME2,
    @EndDate      DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT pp.*, COUNT(DISTINCT p.Id) AS ActivePartnerCount,
        COUNT(DISTINCT CASE WHEN p.ApplicationStatus = 'Pending' THEN p.Id END) AS PendingApplications
    FROM PartnerPrograms pp
    LEFT JOIN Partners p ON p.ProgramId = pp.Id AND p.IsActive = 1
    WHERE pp.Id = @ProgramId
    GROUP BY pp.Id, pp.WorkspaceId, pp.ProgramName, pp.Slug, pp.LogoUrl, pp.BrandColor, pp.Description,
        pp.CommissionType, pp.CommissionValue, pp.CommissionDuration, pp.CommissionDurationMonths,
        pp.PayoutThreshold, pp.PayoutFrequency, pp.PayoutMethod, pp.CookieDays,
        pp.RequireApplication, pp.AutoApprove, pp.ApplicationQuestions, pp.TermsUrl, pp.TermsText,
        pp.IsPublic, pp.IsActive, pp.TotalPartners, pp.TotalRevenue, pp.TotalPayouts, pp.CreatedAt, pp.UpdatedAt;

    SELECT TOP 10 p.Id, p.Name, p.Email, p.AvatarUrl, p.Country, p.CountryCode,
        p.TotalRevenue, p.TotalCommission, p.TotalPaid, p.PendingBalance, p.TotalClicks, p.TotalSales
    FROM Partners p WHERE p.ProgramId = @ProgramId AND p.IsActive = 1 AND p.ApplicationStatus = 'Approved'
    ORDER BY p.TotalRevenue DESC;

    SELECT CAST(ps.SaleDate AS DATE) AS SaleDate, SUM(ps.SaleAmount) AS Revenue,
        SUM(ps.CommissionAmount) AS Commission, COUNT(*) AS SaleCount
    FROM PartnerSales ps WHERE ps.ProgramId = @ProgramId AND ps.SaleDate BETWEEN @StartDate AND @EndDate
        AND ps.Status IN ('Approved', 'Paid')
    GROUP BY CAST(ps.SaleDate AS DATE) ORDER BY SaleDate ASC;

    SELECT SUM(CASE WHEN pout.Status = 'Pending' THEN pout.Amount ELSE 0 END) AS PendingPayouts,
        COUNT(CASE WHEN pout.Status = 'Pending' THEN 1 END) AS PendingCount
    FROM PartnerPayouts pout WHERE pout.ProgramId = @ProgramId;

    SELECT COUNT(*) AS UnresolvedFraudCount FROM PartnerFraudEvents WHERE ProgramId = @ProgramId AND IsResolved = 0;
END
GO

CREATE OR ALTER PROCEDURE sp_CalculateCommission
    @PartnerId   BIGINT,
    @SaleAmount  DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CASE pp.CommissionType
            WHEN 'Percentage' THEN ROUND(@SaleAmount * pp.CommissionValue / 100, 2)
            WHEN 'FlatRate' THEN pp.CommissionValue
            ELSE 0 END AS CommissionAmount,
        pp.CommissionType, pp.CommissionValue, pp.CommissionDuration
    FROM Partners p INNER JOIN PartnerPrograms pp ON p.ProgramId = pp.Id
    WHERE p.Id = @PartnerId;
END
GO

CREATE OR ALTER PROCEDURE sp_AttributePartnerClick
    @ReferralCode   NVARCHAR(50),
    @ClickEventId   BIGINT,
    @IPAddress      NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @PartnerId BIGINT, @ProgramId BIGINT, @CookieDays INT;
    SELECT @PartnerId = p.Id, @ProgramId = p.ProgramId, @CookieDays = pp.CookieDays
    FROM Partners p INNER JOIN PartnerPrograms pp ON p.ProgramId = pp.Id
    WHERE p.ReferralCode = @ReferralCode AND p.IsActive = 1 AND p.ApplicationStatus = 'Approved' AND pp.IsActive = 1;

    IF @PartnerId IS NULL BEGIN SELECT 0 AS Success, 'Partner not found' AS Message; RETURN; END

    DECLARE @DupIPWindow INT;
    SELECT @DupIPWindow = CAST(SettingValue AS INT) FROM SystemSettings WHERE SettingKey = 'DuplicateIPWindow';
    DECLARE @DuplicateIPCount INT;
    SELECT @DuplicateIPCount = COUNT(*) FROM ClickEvents
    WHERE PartnerId = @PartnerId AND IPAddress = @IPAddress AND ClickedAt >= DATEADD(HOUR, -@DupIPWindow, GETUTCDATE());

    IF @DuplicateIPCount > 5
    BEGIN
        INSERT INTO PartnerFraudEvents (PartnerId, ProgramId, FraudType, Description, Severity)
        VALUES (@PartnerId, @ProgramId, 'DuplicateIP', 'Multiple clicks from same IP: ' + @IPAddress, 'Medium');
    END

    UPDATE ClickEvents SET PartnerId = @PartnerId, ReferralCode = @ReferralCode WHERE Id = @ClickEventId;
    UPDATE Partners SET TotalClicks = TotalClicks + 1, UpdatedAt = GETUTCDATE() WHERE Id = @PartnerId;
    SELECT 1 AS Success, @CookieDays AS CookieDays, @PartnerId AS PartnerId;
END
GO

CREATE OR ALTER PROCEDURE sp_GetBillingSummary
    @WorkspaceId  BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ss.Id, ss.StripeSubscriptionId, ss.Status, ss.BillingCycle, ss.CurrentPeriodStart,
        ss.CurrentPeriodEnd, ss.CancelAtPeriodEnd, ss.TrialEnd, p.Id AS PlanId, p.Name AS PlanName,
        p.Price, p.MaxLinksPerMonth, p.MaxEventsPerMonth, p.AnalyticsRetentionDays
    FROM StripeSubscriptions ss INNER JOIN Plans p ON ss.PlanId = p.Id
    WHERE ss.WorkspaceId = @WorkspaceId AND ss.Status IN ('active','trialing','past_due')
    ORDER BY ss.CreatedAt DESC;

    SELECT TOP 12 si.Id, si.StripeInvoiceId, si.Amount, si.AmountPaid, si.Currency, si.Status,
        si.PeriodStart, si.PeriodEnd, si.PdfUrl, si.InvoiceNumber, si.PaidAt, si.CreatedAt
    FROM StripeInvoices si WHERE si.WorkspaceId = @WorkspaceId ORDER BY si.CreatedAt DESC;

    SELECT StripeCustomerId, DefaultPaymentMethod FROM StripeCustomers WHERE WorkspaceId = @WorkspaceId;
END
GO

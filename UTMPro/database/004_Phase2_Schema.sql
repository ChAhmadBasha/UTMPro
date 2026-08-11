-- ============================================================
-- FILE: database/004_Phase2_Schema.sql
-- Run AFTER 001, 002, 003 from Phase 1
-- ============================================================

USE UTMProDB;
GO

-- ============================================================
-- PARTNER PROGRAM TABLES
-- ============================================================

CREATE TABLE PartnerPrograms (
    Id                      BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId             BIGINT         NOT NULL REFERENCES Workspaces(Id) UNIQUE,
    ProgramName             NVARCHAR(100)  NOT NULL,
    Slug                    NVARCHAR(100)  NOT NULL UNIQUE,
    LogoUrl                 NVARCHAR(500)  NULL,
    BrandColor              NVARCHAR(20)   NOT NULL DEFAULT '#000000',
    Description             NVARCHAR(2000) NULL,
    CommissionType          NVARCHAR(20)   NOT NULL DEFAULT 'Percentage',
    CommissionValue         DECIMAL(10,2)  NOT NULL DEFAULT 20,
    CommissionDuration      NVARCHAR(20)   NOT NULL DEFAULT 'Lifetime',
    CommissionDurationMonths INT           NULL,
    PayoutThreshold         DECIMAL(10,2)  NOT NULL DEFAULT 50,
    PayoutFrequency         NVARCHAR(20)   NOT NULL DEFAULT 'Monthly',
    PayoutMethod            NVARCHAR(20)   NOT NULL DEFAULT 'Stripe',
    CookieDays              INT            NOT NULL DEFAULT 90,
    RequireApplication      BIT            NOT NULL DEFAULT 0,
    AutoApprove             BIT            NOT NULL DEFAULT 1,
    ApplicationQuestions    NVARCHAR(MAX)  NULL,
    TermsUrl                NVARCHAR(2000) NULL,
    TermsText               NVARCHAR(MAX)  NULL,
    IsPublic                BIT            NOT NULL DEFAULT 1,
    IsActive                BIT            NOT NULL DEFAULT 1,
    TotalPartners           INT            NOT NULL DEFAULT 0,
    TotalRevenue            DECIMAL(12,2)  NOT NULL DEFAULT 0,
    TotalPayouts            DECIMAL(12,2)  NOT NULL DEFAULT 0,
    CreatedAt               DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt               DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_PartnerPrograms_WorkspaceId ON PartnerPrograms(WorkspaceId);
CREATE INDEX IX_PartnerPrograms_Slug ON PartnerPrograms(Slug);

CREATE TABLE Partners (
    Id                  BIGINT IDENTITY(1,1) PRIMARY KEY,
    ExternalId          NVARCHAR(50)   NOT NULL UNIQUE,
    ProgramId           BIGINT         NOT NULL REFERENCES PartnerPrograms(Id),
    WorkspaceId         BIGINT         NOT NULL REFERENCES Workspaces(Id),
    UserId              BIGINT         NULL REFERENCES Users(Id),
    Name                NVARCHAR(100)  NOT NULL,
    Email               NVARCHAR(255)  NOT NULL,
    AvatarUrl           NVARCHAR(500)  NULL,
    Country             NVARCHAR(100)  NULL,
    CountryCode         NVARCHAR(5)    NULL,
    ReferralCode        NVARCHAR(50)   NOT NULL UNIQUE,
    ReferralUrl         NVARCHAR(2000) NOT NULL,
    ApplicationStatus   NVARCHAR(20)   NOT NULL DEFAULT 'Approved',
    ApplicationData     NVARCHAR(MAX)  NULL,
    ApprovedAt          DATETIME2      NULL,
    ApprovedBy          BIGINT         NULL REFERENCES Users(Id),
    RejectedAt          DATETIME2      NULL,
    RejectionReason     NVARCHAR(500)  NULL,
    PayoutMethod        NVARCHAR(20)   NULL,
    StripeAccountId     NVARCHAR(100)  NULL,
    PayPalEmail         NVARCHAR(255)  NULL,
    TotalClicks         BIGINT         NOT NULL DEFAULT 0,
    TotalLeads          INT            NOT NULL DEFAULT 0,
    TotalSales          INT            NOT NULL DEFAULT 0,
    TotalRevenue        DECIMAL(12,2)  NOT NULL DEFAULT 0,
    TotalCommission     DECIMAL(12,2)  NOT NULL DEFAULT 0,
    TotalPaid           DECIMAL(12,2)  NOT NULL DEFAULT 0,
    PendingBalance      DECIMAL(12,2)  NOT NULL DEFAULT 0,
    FraudScore          INT            NOT NULL DEFAULT 0,
    IsFlagged           BIT            NOT NULL DEFAULT 0,
    IsActive            BIT            NOT NULL DEFAULT 1,
    CreatedAt           DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_Partners_ProgramId    ON Partners(ProgramId);
CREATE INDEX IX_Partners_WorkspaceId  ON Partners(WorkspaceId);
CREATE INDEX IX_Partners_Email        ON Partners(Email);
CREATE INDEX IX_Partners_ReferralCode ON Partners(ReferralCode);
CREATE INDEX IX_Partners_UserId       ON Partners(UserId);

CREATE TABLE PartnerLinks (
    Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    PartnerId       BIGINT         NOT NULL REFERENCES Partners(Id),
    ProgramId       BIGINT         NOT NULL REFERENCES PartnerPrograms(Id),
    LinkId          BIGINT         NULL REFERENCES Links(Id),
    DestinationUrl  NVARCHAR(2000) NOT NULL,
    IsDefault       BIT            NOT NULL DEFAULT 0,
    TotalClicks     BIGINT         NOT NULL DEFAULT 0,
    TotalLeads      INT            NOT NULL DEFAULT 0,
    TotalSales      INT            NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_PartnerLinks_PartnerId ON PartnerLinks(PartnerId);
CREATE INDEX IX_PartnerLinks_LinkId    ON PartnerLinks(LinkId);

CREATE TABLE PartnerSales (
    Id                  BIGINT IDENTITY(1,1) PRIMARY KEY,
    ExternalId          NVARCHAR(50)   NOT NULL UNIQUE,
    PartnerId           BIGINT         NOT NULL REFERENCES Partners(Id),
    ProgramId           BIGINT         NOT NULL REFERENCES PartnerPrograms(Id),
    WorkspaceId         BIGINT         NOT NULL REFERENCES Workspaces(Id),
    CustomerEmail       NVARCHAR(255)  NULL,
    CustomerId          BIGINT         NULL REFERENCES Customers(Id),
    SaleAmount          DECIMAL(10,2)  NOT NULL DEFAULT 0,
    Currency            NVARCHAR(10)   NOT NULL DEFAULT 'USD',
    CommissionType      NVARCHAR(20)   NOT NULL,
    CommissionRate      DECIMAL(10,4)  NOT NULL,
    CommissionAmount    DECIMAL(10,2)  NOT NULL DEFAULT 0,
    Status              NVARCHAR(20)   NOT NULL DEFAULT 'Pending',
    ReferralCode        NVARCHAR(50)   NULL,
    ClickId             BIGINT         NULL REFERENCES ClickEvents(Id),
    StripeChargeId      NVARCHAR(100)  NULL,
    StripePayoutId      NVARCHAR(100)  NULL,
    ExternalOrderId     NVARCHAR(255)  NULL,
    SaleDate            DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    ApprovedAt          DATETIME2      NULL,
    PaidAt              DATETIME2      NULL,
    ReversedAt          DATETIME2      NULL,
    CreatedAt           DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_PartnerSales_PartnerId   ON PartnerSales(PartnerId);
CREATE INDEX IX_PartnerSales_ProgramId   ON PartnerSales(ProgramId);
CREATE INDEX IX_PartnerSales_WorkspaceId ON PartnerSales(WorkspaceId);
CREATE INDEX IX_PartnerSales_Status      ON PartnerSales(Status);
CREATE INDEX IX_PartnerSales_SaleDate    ON PartnerSales(SaleDate DESC);

CREATE TABLE PartnerPayouts (
    Id                  BIGINT IDENTITY(1,1) PRIMARY KEY,
    ExternalId          NVARCHAR(50)   NOT NULL UNIQUE,
    PartnerId           BIGINT         NOT NULL REFERENCES Partners(Id),
    ProgramId           BIGINT         NOT NULL REFERENCES PartnerPrograms(Id),
    WorkspaceId         BIGINT         NOT NULL REFERENCES Workspaces(Id),
    Amount              DECIMAL(10,2)  NOT NULL,
    Currency            NVARCHAR(10)   NOT NULL DEFAULT 'USD',
    PayoutMethod        NVARCHAR(20)   NOT NULL,
    StripeTransferId    NVARCHAR(100)  NULL,
    StripePayoutStatus  NVARCHAR(50)   NULL,
    Status              NVARCHAR(20)   NOT NULL DEFAULT 'Pending',
    FailureReason       NVARCHAR(500)  NULL,
    PeriodStart         DATETIME2      NULL,
    PeriodEnd           DATETIME2      NULL,
    SaleIds             NVARCHAR(MAX)  NULL,
    Notes               NVARCHAR(500)  NULL,
    ProcessedBy         BIGINT         NULL REFERENCES Users(Id),
    ProcessedAt         DATETIME2      NULL,
    CreatedAt           DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_PartnerPayouts_PartnerId   ON PartnerPayouts(PartnerId);
CREATE INDEX IX_PartnerPayouts_WorkspaceId ON PartnerPayouts(WorkspaceId);
CREATE INDEX IX_PartnerPayouts_Status      ON PartnerPayouts(Status);

CREATE TABLE PartnerMessages (
    Id            BIGINT IDENTITY(1,1) PRIMARY KEY,
    ProgramId     BIGINT         NOT NULL REFERENCES PartnerPrograms(Id),
    PartnerId     BIGINT         NULL REFERENCES Partners(Id),
    SenderId      BIGINT         NOT NULL REFERENCES Users(Id),
    Subject       NVARCHAR(255)  NOT NULL,
    Body          NVARCHAR(MAX)  NOT NULL,
    IsRead        BIT            NOT NULL DEFAULT 0,
    ReadAt        DATETIME2      NULL,
    CreatedAt     DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_PartnerMessages_ProgramId  ON PartnerMessages(ProgramId);
CREATE INDEX IX_PartnerMessages_PartnerId  ON PartnerMessages(PartnerId);

CREATE TABLE PartnerBounties (
    Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    ProgramId       BIGINT         NOT NULL REFERENCES PartnerPrograms(Id),
    Title           NVARCHAR(255)  NOT NULL,
    Description     NVARCHAR(2000) NULL,
    BountyAmount    DECIMAL(10,2)  NOT NULL,
    Currency        NVARCHAR(10)   NOT NULL DEFAULT 'USD',
    BountyType      NVARCHAR(20)   NOT NULL DEFAULT 'Signup',
    MaxClaims       INT            NULL,
    TotalClaims     INT            NOT NULL DEFAULT 0,
    IsActive        BIT            NOT NULL DEFAULT 1,
    ExpiresAt       DATETIME2      NULL,
    CreatedAt       DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE PartnerBountyClaims (
    Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    BountyId        BIGINT         NOT NULL REFERENCES PartnerBounties(Id),
    PartnerId       BIGINT         NOT NULL REFERENCES Partners(Id),
    Status          NVARCHAR(20)   NOT NULL DEFAULT 'Pending',
    ClaimedAt       DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    ApprovedAt      DATETIME2      NULL,
    PaidAt          DATETIME2      NULL,
    UNIQUE(BountyId, PartnerId)
);

CREATE TABLE PartnerFraudEvents (
    Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    PartnerId       BIGINT         NOT NULL REFERENCES Partners(Id),
    ProgramId       BIGINT         NOT NULL REFERENCES PartnerPrograms(Id),
    FraudType       NVARCHAR(50)   NOT NULL,
    Description     NVARCHAR(500)  NULL,
    Severity        NVARCHAR(20)   NOT NULL DEFAULT 'Medium',
    IsResolved      BIT            NOT NULL DEFAULT 0,
    ResolvedAt      DATETIME2      NULL,
    ResolvedBy      BIGINT         NULL REFERENCES Users(Id),
    Resolution      NVARCHAR(500)  NULL,
    CreatedAt       DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_PartnerFraudEvents_PartnerId ON PartnerFraudEvents(PartnerId);

-- ============================================================
-- STRIPE BILLING TABLES
-- ============================================================

CREATE TABLE StripeCustomers (
    Id                  BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId         BIGINT         NOT NULL REFERENCES Workspaces(Id) UNIQUE,
    StripeCustomerId    NVARCHAR(100)  NOT NULL UNIQUE,
    DefaultPaymentMethod NVARCHAR(100) NULL,
    CreatedAt           DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_StripeCustomers_WorkspaceId ON StripeCustomers(WorkspaceId);
CREATE INDEX IX_StripeCustomers_StripeCustomerId ON StripeCustomers(StripeCustomerId);

CREATE TABLE StripeSubscriptions (
    Id                      BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId             BIGINT         NOT NULL REFERENCES Workspaces(Id),
    StripeSubscriptionId    NVARCHAR(100)  NOT NULL UNIQUE,
    StripeCustomerId        NVARCHAR(100)  NOT NULL,
    StripePriceId           NVARCHAR(100)  NOT NULL,
    PlanId                  INT            NOT NULL REFERENCES Plans(Id),
    Status                  NVARCHAR(50)   NOT NULL,
    BillingCycle            NVARCHAR(20)   NOT NULL DEFAULT 'Monthly',
    CurrentPeriodStart      DATETIME2      NOT NULL,
    CurrentPeriodEnd        DATETIME2      NOT NULL,
    CancelAtPeriodEnd       BIT            NOT NULL DEFAULT 0,
    CanceledAt              DATETIME2      NULL,
    TrialStart              DATETIME2      NULL,
    TrialEnd                DATETIME2      NULL,
    CreatedAt               DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt               DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_StripeSubscriptions_WorkspaceId ON StripeSubscriptions(WorkspaceId);
CREATE INDEX IX_StripeSubscriptions_StripeSubscriptionId ON StripeSubscriptions(StripeSubscriptionId);

CREATE TABLE StripeInvoices (
    Id                  BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId         BIGINT         NOT NULL REFERENCES Workspaces(Id),
    StripeInvoiceId     NVARCHAR(100)  NOT NULL UNIQUE,
    StripeCustomerId    NVARCHAR(100)  NOT NULL,
    SubscriptionId      BIGINT         NULL REFERENCES StripeSubscriptions(Id),
    Amount              DECIMAL(10,2)  NOT NULL,
    AmountPaid          DECIMAL(10,2)  NOT NULL DEFAULT 0,
    Currency            NVARCHAR(10)   NOT NULL DEFAULT 'usd',
    Status              NVARCHAR(50)   NOT NULL,
    PeriodStart         DATETIME2      NULL,
    PeriodEnd           DATETIME2      NULL,
    PdfUrl              NVARCHAR(2000) NULL,
    InvoiceNumber       NVARCHAR(50)   NULL,
    PaidAt              DATETIME2      NULL,
    DueDate             DATETIME2      NULL,
    CreatedAt           DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_StripeInvoices_WorkspaceId ON StripeInvoices(WorkspaceId);

CREATE TABLE StripePrices (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    PlanId          INT           NOT NULL REFERENCES Plans(Id),
    StripePriceId   NVARCHAR(100) NOT NULL UNIQUE,
    BillingCycle    NVARCHAR(20)  NOT NULL,
    Amount          DECIMAL(10,2) NOT NULL,
    Currency        NVARCHAR(10)  NOT NULL DEFAULT 'usd',
    IsActive        BIT           NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE StripeWebhookEvents (
    Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    StripeEventId   NVARCHAR(100) NOT NULL UNIQUE,
    EventType       NVARCHAR(100) NOT NULL,
    Processed       BIT           NOT NULL DEFAULT 0,
    ProcessedAt     DATETIME2     NULL,
    Error           NVARCHAR(MAX) NULL,
    CreatedAt       DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_StripeWebhookEvents_StripeEventId ON StripeWebhookEvents(StripeEventId);

-- ============================================================
-- SAML SSO TABLES
-- ============================================================

CREATE TABLE SAMLConfigurations (
    Id                      BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId             BIGINT         NOT NULL REFERENCES Workspaces(Id) UNIQUE,
    IdpEntityId             NVARCHAR(500)  NULL,
    IdpSSOUrl               NVARCHAR(2000) NULL,
    IdpSLOUrl               NVARCHAR(2000) NULL,
    IdpCertificate          NVARCHAR(MAX)  NULL,
    SpEntityId              NVARCHAR(500)  NULL,
    SpAcsUrl                NVARCHAR(500)  NULL,
    EmailAttribute          NVARCHAR(100)  NOT NULL DEFAULT 'email',
    NameAttribute           NVARCHAR(100)  NOT NULL DEFAULT 'name',
    RoleAttribute           NVARCHAR(100)  NULL,
    RequireSAML             BIT            NOT NULL DEFAULT 0,
    AutoProvision           BIT            NOT NULL DEFAULT 1,
    DefaultRole             NVARCHAR(20)   NOT NULL DEFAULT 'Member',
    IsActive                BIT            NOT NULL DEFAULT 0,
    TestedAt                DATETIME2      NULL,
    CreatedAt               DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt               DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);

-- ============================================================
-- SCIM TABLES
-- ============================================================

CREATE TABLE SCIMConfigurations (
    Id                  BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId         BIGINT        NOT NULL REFERENCES Workspaces(Id) UNIQUE,
    SCIMToken           NVARCHAR(500) NOT NULL UNIQUE,
    SCIMTokenHash       NVARCHAR(500) NOT NULL,
    ProvisionUsers      BIT           NOT NULL DEFAULT 1,
    DeprovisionUsers    BIT           NOT NULL DEFAULT 1,
    DefaultRole         NVARCHAR(20)  NOT NULL DEFAULT 'Member',
    IsActive            BIT           NOT NULL DEFAULT 1,
    LastSyncAt          DATETIME2     NULL,
    CreatedAt           DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);

-- ============================================================
-- REAL-TIME EVENTS
-- ============================================================

CREATE TABLE RealTimeSubscriptions (
    Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId     BIGINT        NOT NULL REFERENCES Workspaces(Id),
    UserId          BIGINT        NOT NULL REFERENCES Users(Id),
    ConnectionId    NVARCHAR(100) NOT NULL,
    ConnectedAt     DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    DisconnectedAt  DATETIME2     NULL
);
CREATE INDEX IX_RealTimeSubscriptions_WorkspaceId ON RealTimeSubscriptions(WorkspaceId);
CREATE INDEX IX_RealTimeSubscriptions_ConnectionId ON RealTimeSubscriptions(ConnectionId);

-- ============================================================
-- INTEGRATIONS TABLES
-- ============================================================

CREATE TABLE Integrations (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Name            NVARCHAR(100)  NOT NULL,
    Slug            NVARCHAR(50)   NOT NULL UNIQUE,
    Description     NVARCHAR(500)  NULL,
    LogoUrl         NVARCHAR(500)  NULL,
    Category        NVARCHAR(50)   NOT NULL,
    DocsUrl         NVARCHAR(2000) NULL,
    IsActive        BIT            NOT NULL DEFAULT 1,
    SortOrder       INT            NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE WorkspaceIntegrations (
    Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId     BIGINT         NOT NULL REFERENCES Workspaces(Id),
    IntegrationId   INT            NOT NULL REFERENCES Integrations(Id),
    Config          NVARCHAR(MAX)  NULL,
    IsActive        BIT            NOT NULL DEFAULT 1,
    ConnectedBy     BIGINT         NOT NULL REFERENCES Users(Id),
    ConnectedAt     DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    LastSyncAt      DATETIME2      NULL,
    UNIQUE(WorkspaceId, IntegrationId)
);
CREATE INDEX IX_WorkspaceIntegrations_WorkspaceId ON WorkspaceIntegrations(WorkspaceId);

-- ============================================================
-- ENHANCED WEBHOOK TABLES
-- ============================================================

CREATE TABLE WebhookDeliveryLogs (
    Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    WebhookId       BIGINT         NOT NULL REFERENCES Webhooks(Id),
    EventType       NVARCHAR(100)  NOT NULL,
    PayloadJson     NVARCHAR(MAX)  NULL,
    ResponseStatus  INT            NULL,
    ResponseBody    NVARCHAR(MAX)  NULL,
    ResponseTimeMs  INT            NULL,
    IsSuccess       BIT            NOT NULL DEFAULT 0,
    AttemptCount    INT            NOT NULL DEFAULT 1,
    NextRetryAt     DATETIME2      NULL,
    CreatedAt       DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_WebhookDeliveryLogs_WebhookId ON WebhookDeliveryLogs(WebhookId);
CREATE INDEX IX_WebhookDeliveryLogs_CreatedAt ON WebhookDeliveryLogs(CreatedAt DESC);

-- ============================================================
-- PUBLIC API TABLES
-- ============================================================

CREATE TABLE APIRateLimits (
    Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId     BIGINT        NOT NULL REFERENCES Workspaces(Id) UNIQUE,
    RequestsPerMin  INT           NOT NULL DEFAULT 60,
    RequestsPerHour INT           NOT NULL DEFAULT 1000,
    RequestsPerDay  INT           NOT NULL DEFAULT 10000,
    CustomLimits    BIT           NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);

-- ============================================================
-- PARTNER PORTAL SESSIONS
-- ============================================================

CREATE TABLE PartnerPortalSessions (
    Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    PartnerId       BIGINT        NOT NULL REFERENCES Partners(Id),
    SessionToken    NVARCHAR(500) NOT NULL UNIQUE,
    TokenHash       NVARCHAR(500) NOT NULL,
    IPAddress       NVARCHAR(50)  NULL,
    UserAgent       NVARCHAR(500) NULL,
    ExpiresAt       DATETIME2     NOT NULL,
    CreatedAt       DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    LastActiveAt    DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_PartnerPortalSessions_TokenHash ON PartnerPortalSessions(TokenHash);
CREATE INDEX IX_PartnerPortalSessions_PartnerId ON PartnerPortalSessions(PartnerId);

-- ============================================================
-- ALTER EXISTING TABLES FOR PHASE 2
-- ============================================================

ALTER TABLE Workspaces
ADD StripeCustomerId    NVARCHAR(100) NULL,
    StripeSubscriptionId NVARCHAR(100) NULL,
    BillingEmail        NVARCHAR(255) NULL,
    BillingName         NVARCHAR(255) NULL,
    BillingAddress      NVARCHAR(MAX) NULL;

ALTER TABLE ClickEvents
ADD PartnerId       BIGINT NULL,
    ReferralCode    NVARCHAR(50) NULL;

ALTER TABLE LeadEvents
ADD PartnerId       BIGINT NULL,
    ReferralCode    NVARCHAR(50) NULL;

ALTER TABLE SaleEvents
ADD PartnerId       BIGINT NULL,
    ReferralCode    NVARCHAR(50) NULL,
    PartnerSaleId   BIGINT NULL;

ALTER TABLE Plans
ADD StripePriceIdMonthly NVARCHAR(100) NULL,
    StripePriceIdYearly  NVARCHAR(100) NULL;

ALTER TABLE Webhooks
ADD MaxRetries      INT NOT NULL DEFAULT 3,
    RetryInterval   INT NOT NULL DEFAULT 60,
    TotalDeliveries BIGINT NOT NULL DEFAULT 0,
    FailedDeliveries BIGINT NOT NULL DEFAULT 0;
GO

# PART 1: ADDITIONAL DATABASE SCHEMA (Phase 2)

```sql
-- ============================================================
-- FILE: database/004_Phase2_Schema.sql
-- Run AFTER 001, 002, 003 from Phase 1
-- ============================================================

USE UTMProDB;
GO

-- ============================================================
-- PARTNER PROGRAM TABLES
-- ============================================================

-- TABLE P1: PartnerPrograms
-- Each workspace can have ONE partner program
-- ============================================================
CREATE TABLE PartnerPrograms (
    Id                      BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId             BIGINT         NOT NULL 
                            REFERENCES Workspaces(Id) UNIQUE,
    ProgramName             NVARCHAR(100)  NOT NULL,
    Slug                    NVARCHAR(100)  NOT NULL UNIQUE,
    -- Public URL: partners.utmpro.co/{slug}
    LogoUrl                 NVARCHAR(500)  NULL,
    BrandColor              NVARCHAR(20)   NOT NULL DEFAULT '#000000',
    Description             NVARCHAR(2000) NULL,
    -- Commission Settings
    CommissionType          NVARCHAR(20)   NOT NULL DEFAULT 'Percentage',
    -- Values: 'Percentage' | 'FlatRate'
    CommissionValue         DECIMAL(10,2)  NOT NULL DEFAULT 20,
    -- 20 = 20% or $20 flat
    CommissionDuration      NVARCHAR(20)   NOT NULL DEFAULT 'Lifetime',
    -- Values: 'OneTime' | 'Recurring' | 'Lifetime'
    CommissionDurationMonths INT           NULL,
    -- Only used if CommissionDuration = 'Recurring'
    -- Payout Settings
    PayoutThreshold         DECIMAL(10,2)  NOT NULL DEFAULT 50,
    -- Minimum balance before payout
    PayoutFrequency         NVARCHAR(20)   NOT NULL DEFAULT 'Monthly',
    -- Values: 'Weekly' | 'Monthly' | 'Manual'
    PayoutMethod            NVARCHAR(20)   NOT NULL DEFAULT 'Stripe',
    -- Values: 'Stripe' | 'PayPal' | 'Wire' | 'Manual'
    -- Cookie Settings
    CookieDays              INT            NOT NULL DEFAULT 90,
    -- Attribution window in days
    -- Application Settings
    RequireApplication      BIT            NOT NULL DEFAULT 0,
    -- If true, partners must apply and be approved
    AutoApprove             BIT            NOT NULL DEFAULT 1,
    -- Application questions (JSON)
    ApplicationQuestions    NVARCHAR(MAX)  NULL,
    -- Terms & Conditions
    TermsUrl                NVARCHAR(2000) NULL,
    TermsText               NVARCHAR(MAX)  NULL,
    -- Status
    IsPublic                BIT            NOT NULL DEFAULT 1,
    IsActive                BIT            NOT NULL DEFAULT 1,
    -- Stats (denormalized)
    TotalPartners           INT            NOT NULL DEFAULT 0,
    TotalRevenue            DECIMAL(12,2)  NOT NULL DEFAULT 0,
    TotalPayouts            DECIMAL(12,2)  NOT NULL DEFAULT 0,
    CreatedAt               DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt               DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_PartnerPrograms_WorkspaceId 
    ON PartnerPrograms(WorkspaceId);
CREATE INDEX IX_PartnerPrograms_Slug 
    ON PartnerPrograms(Slug);

-- ============================================================
-- TABLE P2: Partners (Affiliates who join programs)
-- ============================================================
CREATE TABLE Partners (
    Id                  BIGINT IDENTITY(1,1) PRIMARY KEY,
    ExternalId          NVARCHAR(50)   NOT NULL UNIQUE,
    -- prt_xxx format
    ProgramId           BIGINT         NOT NULL 
                        REFERENCES PartnerPrograms(Id),
    WorkspaceId         BIGINT         NOT NULL 
                        REFERENCES Workspaces(Id),
    -- Partner's own details
    UserId              BIGINT         NULL REFERENCES Users(Id),
    -- If partner is a UTMPro user
    Name                NVARCHAR(100)  NOT NULL,
    Email               NVARCHAR(255)  NOT NULL,
    AvatarUrl           NVARCHAR(500)  NULL,
    Country             NVARCHAR(100)  NULL,
    CountryCode         NVARCHAR(5)    NULL,
    -- Referral Link
    ReferralCode        NVARCHAR(50)   NOT NULL UNIQUE,
    ReferralUrl         NVARCHAR(2000) NOT NULL,
    -- e.g., https://acme.com?ref=PARTNERCODE
    -- Application
    ApplicationStatus   NVARCHAR(20)   NOT NULL DEFAULT 'Approved',
    -- Values: 'Pending'|'Approved'|'Rejected'|'Suspended'
    ApplicationData     NVARCHAR(MAX)  NULL,
    -- JSON: answers to application questions
    ApprovedAt          DATETIME2      NULL,
    ApprovedBy          BIGINT         NULL REFERENCES Users(Id),
    RejectedAt          DATETIME2      NULL,
    RejectionReason     NVARCHAR(500)  NULL,
    -- Payout info
    PayoutMethod        NVARCHAR(20)   NULL,
    -- Stripe Connect, PayPal, etc.
    StripeAccountId     NVARCHAR(100)  NULL,
    PayPalEmail         NVARCHAR(255)  NULL,
    -- Stats (denormalized for display)
    TotalClicks         BIGINT         NOT NULL DEFAULT 0,
    TotalLeads          INT            NOT NULL DEFAULT 0,
    TotalSales          INT            NOT NULL DEFAULT 0,
    TotalRevenue        DECIMAL(12,2)  NOT NULL DEFAULT 0,
    TotalCommission     DECIMAL(12,2)  NOT NULL DEFAULT 0,
    TotalPaid           DECIMAL(12,2)  NOT NULL DEFAULT 0,
    PendingBalance      DECIMAL(12,2)  NOT NULL DEFAULT 0,
    -- Fraud detection
    FraudScore          INT            NOT NULL DEFAULT 0,
    IsFlagged           BIT            NOT NULL DEFAULT 0,
    -- Status
    IsActive            BIT            NOT NULL DEFAULT 1,
    CreatedAt           DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_Partners_ProgramId    ON Partners(ProgramId);
CREATE INDEX IX_Partners_WorkspaceId  ON Partners(WorkspaceId);
CREATE INDEX IX_Partners_Email        ON Partners(Email);
CREATE INDEX IX_Partners_ReferralCode ON Partners(ReferralCode);
CREATE INDEX IX_Partners_UserId       ON Partners(UserId);

-- ============================================================
-- TABLE P3: PartnerLinks (Links created by partners)
-- ============================================================
CREATE TABLE PartnerLinks (
    Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    PartnerId       BIGINT         NOT NULL REFERENCES Partners(Id),
    ProgramId       BIGINT         NOT NULL 
                    REFERENCES PartnerPrograms(Id),
    LinkId          BIGINT         NULL REFERENCES Links(Id),
    -- The short link created for this partner
    DestinationUrl  NVARCHAR(2000) NOT NULL,
    IsDefault       BIT            NOT NULL DEFAULT 0,
    TotalClicks     BIGINT         NOT NULL DEFAULT 0,
    TotalLeads      INT            NOT NULL DEFAULT 0,
    TotalSales      INT            NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_PartnerLinks_PartnerId ON PartnerLinks(PartnerId);
CREATE INDEX IX_PartnerLinks_LinkId    ON PartnerLinks(LinkId);

-- ============================================================
-- TABLE P4: PartnerSales (Commission tracking)
-- ============================================================
CREATE TABLE PartnerSales (
    Id                  BIGINT IDENTITY(1,1) PRIMARY KEY,
    ExternalId          NVARCHAR(50)   NOT NULL UNIQUE,
    PartnerId           BIGINT         NOT NULL REFERENCES Partners(Id),
    ProgramId           BIGINT         NOT NULL 
                        REFERENCES PartnerPrograms(Id),
    WorkspaceId         BIGINT         NOT NULL 
                        REFERENCES Workspaces(Id),
    -- Customer who purchased
    CustomerEmail       NVARCHAR(255)  NULL,
    CustomerId          BIGINT         NULL REFERENCES Customers(Id),
    -- Sale details
    SaleAmount          DECIMAL(10,2)  NOT NULL DEFAULT 0,
    Currency            NVARCHAR(10)   NOT NULL DEFAULT 'USD',
    -- Commission
    CommissionType      NVARCHAR(20)   NOT NULL,
    CommissionRate      DECIMAL(10,4)  NOT NULL,
    CommissionAmount    DECIMAL(10,2)  NOT NULL DEFAULT 0,
    -- Status
    Status              NVARCHAR(20)   NOT NULL DEFAULT 'Pending',
    -- Values: 'Pending'|'Approved'|'Paid'|'Reversed'|'Fraud'
    -- Attribution
    ReferralCode        NVARCHAR(50)   NULL,
    ClickId             BIGINT         NULL REFERENCES ClickEvents(Id),
    -- Stripe
    StripeChargeId      NVARCHAR(100)  NULL,
    StripePayoutId      NVARCHAR(100)  NULL,
    -- External reference
    ExternalOrderId     NVARCHAR(255)  NULL,
    -- Timestamps
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

-- ============================================================
-- TABLE P5: PartnerPayouts (Payout records)
-- ============================================================
CREATE TABLE PartnerPayouts (
    Id                  BIGINT IDENTITY(1,1) PRIMARY KEY,
    ExternalId          NVARCHAR(50)   NOT NULL UNIQUE,
    PartnerId           BIGINT         NOT NULL REFERENCES Partners(Id),
    ProgramId           BIGINT         NOT NULL 
                        REFERENCES PartnerPrograms(Id),
    WorkspaceId         BIGINT         NOT NULL 
                        REFERENCES Workspaces(Id),
    Amount              DECIMAL(10,2)  NOT NULL,
    Currency            NVARCHAR(10)   NOT NULL DEFAULT 'USD',
    PayoutMethod        NVARCHAR(20)   NOT NULL,
    -- Stripe
    StripeTransferId    NVARCHAR(100)  NULL,
    StripePayoutStatus  NVARCHAR(50)   NULL,
    -- Status
    Status              NVARCHAR(20)   NOT NULL DEFAULT 'Pending',
    -- Values: 'Pending'|'Processing'|'Paid'|'Failed'|'Cancelled'
    FailureReason       NVARCHAR(500)  NULL,
    -- Period covered
    PeriodStart         DATETIME2      NULL,
    PeriodEnd           DATETIME2      NULL,
    -- Which sales are included (JSON array of PartnerSale IDs)
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

-- ============================================================
-- TABLE P6: PartnerMessages (Communication)
-- ============================================================
CREATE TABLE PartnerMessages (
    Id            BIGINT IDENTITY(1,1) PRIMARY KEY,
    ProgramId     BIGINT         NOT NULL 
                  REFERENCES PartnerPrograms(Id),
    PartnerId     BIGINT         NULL REFERENCES Partners(Id),
    -- NULL = broadcast to all partners
    SenderId      BIGINT         NOT NULL REFERENCES Users(Id),
    Subject       NVARCHAR(255)  NOT NULL,
    Body          NVARCHAR(MAX)  NOT NULL,
    IsRead        BIT            NOT NULL DEFAULT 0,
    ReadAt        DATETIME2      NULL,
    CreatedAt     DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_PartnerMessages_ProgramId  ON PartnerMessages(ProgramId);
CREATE INDEX IX_PartnerMessages_PartnerId  ON PartnerMessages(PartnerId);

-- ============================================================
-- TABLE P7: PartnerBounties (Tasks/Rewards)
-- ============================================================
CREATE TABLE PartnerBounties (
    Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    ProgramId       BIGINT         NOT NULL 
                    REFERENCES PartnerPrograms(Id),
    Title           NVARCHAR(255)  NOT NULL,
    Description     NVARCHAR(2000) NULL,
    BountyAmount    DECIMAL(10,2)  NOT NULL,
    Currency        NVARCHAR(10)   NOT NULL DEFAULT 'USD',
    BountyType      NVARCHAR(20)   NOT NULL DEFAULT 'Signup',
    -- Values: 'Signup'|'Sale'|'Lead'|'Custom'
    MaxClaims       INT            NULL,
    -- NULL = unlimited
    TotalClaims     INT            NOT NULL DEFAULT 0,
    IsActive        BIT            NOT NULL DEFAULT 1,
    ExpiresAt       DATETIME2      NULL,
    CreatedAt       DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);

-- TABLE P8: PartnerBountyClaims
CREATE TABLE PartnerBountyClaims (
    Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    BountyId        BIGINT         NOT NULL 
                    REFERENCES PartnerBounties(Id),
    PartnerId       BIGINT         NOT NULL REFERENCES Partners(Id),
    Status          NVARCHAR(20)   NOT NULL DEFAULT 'Pending',
    -- Values: 'Pending'|'Approved'|'Paid'|'Rejected'
    ClaimedAt       DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    ApprovedAt      DATETIME2      NULL,
    PaidAt          DATETIME2      NULL,
    UNIQUE(BountyId, PartnerId)
);

-- ============================================================
-- TABLE P9: PartnerFraudEvents
-- ============================================================
CREATE TABLE PartnerFraudEvents (
    Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    PartnerId       BIGINT         NOT NULL REFERENCES Partners(Id),
    ProgramId       BIGINT         NOT NULL 
                    REFERENCES PartnerPrograms(Id),
    FraudType       NVARCHAR(50)   NOT NULL,
    -- Values: 'SelfReferral'|'FakeClicks'|'ChargeBack'
    --         |'DuplicateIP'|'VPNDetected'|'Custom'
    Description     NVARCHAR(500)  NULL,
    Severity        NVARCHAR(20)   NOT NULL DEFAULT 'Medium',
    -- Values: 'Low'|'Medium'|'High'|'Critical'
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

-- TABLE S1: StripeCustomers
-- ============================================================
CREATE TABLE StripeCustomers (
    Id                  BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId         BIGINT         NOT NULL 
                        REFERENCES Workspaces(Id) UNIQUE,
    StripeCustomerId    NVARCHAR(100)  NOT NULL UNIQUE,
    -- cus_xxx
    DefaultPaymentMethod NVARCHAR(100) NULL,
    -- pm_xxx
    CreatedAt           DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_StripeCustomers_WorkspaceId 
    ON StripeCustomers(WorkspaceId);
CREATE INDEX IX_StripeCustomers_StripeCustomerId 
    ON StripeCustomers(StripeCustomerId);

-- TABLE S2: StripeSubscriptions
-- ============================================================
CREATE TABLE StripeSubscriptions (
    Id                      BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId             BIGINT         NOT NULL 
                            REFERENCES Workspaces(Id),
    StripeSubscriptionId    NVARCHAR(100)  NOT NULL UNIQUE,
    -- sub_xxx
    StripeCustomerId        NVARCHAR(100)  NOT NULL,
    StripePriceId           NVARCHAR(100)  NOT NULL,
    -- price_xxx (maps to our Plan)
    PlanId                  INT            NOT NULL REFERENCES Plans(Id),
    Status                  NVARCHAR(50)   NOT NULL,
    -- Values from Stripe: 'active'|'canceled'|'past_due'
    --    |'trialing'|'incomplete'|'unpaid'
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
CREATE INDEX IX_StripeSubscriptions_WorkspaceId 
    ON StripeSubscriptions(WorkspaceId);
CREATE INDEX IX_StripeSubscriptions_StripeSubscriptionId 
    ON StripeSubscriptions(StripeSubscriptionId);

-- TABLE S3: StripeInvoices
-- ============================================================
CREATE TABLE StripeInvoices (
    Id                  BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId         BIGINT         NOT NULL 
                        REFERENCES Workspaces(Id),
    StripeInvoiceId     NVARCHAR(100)  NOT NULL UNIQUE,
    -- in_xxx
    StripeCustomerId    NVARCHAR(100)  NOT NULL,
    SubscriptionId      BIGINT         NULL 
                        REFERENCES StripeSubscriptions(Id),
    Amount              DECIMAL(10,2)  NOT NULL,
    AmountPaid          DECIMAL(10,2)  NOT NULL DEFAULT 0,
    Currency            NVARCHAR(10)   NOT NULL DEFAULT 'usd',
    Status              NVARCHAR(50)   NOT NULL,
    -- Values: 'draft'|'open'|'paid'|'void'|'uncollectible'
    PeriodStart         DATETIME2      NULL,
    PeriodEnd           DATETIME2      NULL,
    PdfUrl              NVARCHAR(2000) NULL,
    -- Stripe hosted invoice PDF
    InvoiceNumber       NVARCHAR(50)   NULL,
    PaidAt              DATETIME2      NULL,
    DueDate             DATETIME2      NULL,
    CreatedAt           DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_StripeInvoices_WorkspaceId 
    ON StripeInvoices(WorkspaceId);

-- TABLE S4: StripePrices (our plan pricing config)
-- ============================================================
CREATE TABLE StripePrices (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    PlanId          INT           NOT NULL REFERENCES Plans(Id),
    StripePriceId   NVARCHAR(100) NOT NULL UNIQUE,
    -- price_xxx from Stripe dashboard
    BillingCycle    NVARCHAR(20)  NOT NULL,
    -- Values: 'Monthly' | 'Yearly'
    Amount          DECIMAL(10,2) NOT NULL,
    Currency        NVARCHAR(10)  NOT NULL DEFAULT 'usd',
    IsActive        BIT           NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);

-- TABLE S5: StripeWebhookEvents (Idempotency)
-- ============================================================
CREATE TABLE StripeWebhookEvents (
    Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    StripeEventId   NVARCHAR(100) NOT NULL UNIQUE,
    -- evt_xxx
    EventType       NVARCHAR(100) NOT NULL,
    Processed       BIT           NOT NULL DEFAULT 0,
    ProcessedAt     DATETIME2     NULL,
    Error           NVARCHAR(MAX) NULL,
    CreatedAt       DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_StripeWebhookEvents_StripeEventId 
    ON StripeWebhookEvents(StripeEventId);

-- ============================================================
-- SAML SSO TABLES
-- ============================================================

-- TABLE SA1: SAMLConfigurations
-- ============================================================
CREATE TABLE SAMLConfigurations (
    Id                      BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId             BIGINT         NOT NULL 
                            REFERENCES Workspaces(Id) UNIQUE,
    -- Identity Provider Settings
    IdpEntityId             NVARCHAR(500)  NULL,
    IdpSSOUrl               NVARCHAR(2000) NULL,
    IdpSLOUrl               NVARCHAR(2000) NULL,
    IdpCertificate          NVARCHAR(MAX)  NULL,
    -- X.509 certificate from IdP
    -- Service Provider Settings (Our side - read only)
    SpEntityId              NVARCHAR(500)  NULL,
    -- Generated: https://app.utmpro.co/saml/{workspaceSlug}
    SpAcsUrl                NVARCHAR(500)  NULL,
    -- Assertion Consumer Service URL
    -- Attribute Mapping
    EmailAttribute          NVARCHAR(100)  NOT NULL DEFAULT 'email',
    NameAttribute           NVARCHAR(100)  NOT NULL DEFAULT 'name',
    RoleAttribute           NVARCHAR(100)  NULL,
    -- Behavior
    RequireSAML             BIT            NOT NULL DEFAULT 0,
    AutoProvision           BIT            NOT NULL DEFAULT 1,
    DefaultRole             NVARCHAR(20)   NOT NULL DEFAULT 'Member',
    -- Status
    IsActive                BIT            NOT NULL DEFAULT 0,
    TestedAt                DATETIME2      NULL,
    CreatedAt               DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt               DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);

-- ============================================================
-- SCIM TABLES
-- ============================================================

-- TABLE SC1: SCIMConfigurations
-- ============================================================
CREATE TABLE SCIMConfigurations (
    Id                  BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId         BIGINT        NOT NULL 
                        REFERENCES Workspaces(Id) UNIQUE,
    SCIMToken           NVARCHAR(500) NOT NULL UNIQUE,
    -- Bearer token for SCIM endpoint auth
    SCIMTokenHash       NVARCHAR(500) NOT NULL,
    -- Endpoint: https://app.utmpro.co/scim/{workspaceSlug}
    ProvisionUsers      BIT           NOT NULL DEFAULT 1,
    DeprovisionUsers    BIT           NOT NULL DEFAULT 1,
    DefaultRole         NVARCHAR(20)  NOT NULL DEFAULT 'Member',
    IsActive            BIT           NOT NULL DEFAULT 1,
    LastSyncAt          DATETIME2     NULL,
    CreatedAt           DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);

-- ============================================================
-- REAL-TIME EVENTS TABLES
-- ============================================================

-- TABLE RT1: RealTimeEventSubscriptions
-- ============================================================
CREATE TABLE RealTimeSubscriptions (
    Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId     BIGINT        NOT NULL 
                    REFERENCES Workspaces(Id),
    UserId          BIGINT        NOT NULL REFERENCES Users(Id),
    ConnectionId    NVARCHAR(100) NOT NULL,
    -- SignalR connection ID
    ConnectedAt     DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    DisconnectedAt  DATETIME2     NULL
);
CREATE INDEX IX_RealTimeSubscriptions_WorkspaceId 
    ON RealTimeSubscriptions(WorkspaceId);
CREATE INDEX IX_RealTimeSubscriptions_ConnectionId 
    ON RealTimeSubscriptions(ConnectionId);

-- ============================================================
-- INTEGRATIONS TABLES
-- ============================================================

-- TABLE I1: Integrations (Available integrations)
-- ============================================================
CREATE TABLE Integrations (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Name            NVARCHAR(100)  NOT NULL,
    Slug            NVARCHAR(50)   NOT NULL UNIQUE,
    -- Values: 'stripe'|'zapier'|'slack'|'hubspot'|'shopify'
    Description     NVARCHAR(500)  NULL,
    LogoUrl         NVARCHAR(500)  NULL,
    Category        NVARCHAR(50)   NOT NULL,
    -- Values: 'Payment'|'CRM'|'Analytics'|'Communication'
    --         |'Ecommerce'|'Automation'
    DocsUrl         NVARCHAR(2000) NULL,
    IsActive        BIT            NOT NULL DEFAULT 1,
    SortOrder       INT            NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);

-- TABLE I2: WorkspaceIntegrations
-- ============================================================
CREATE TABLE WorkspaceIntegrations (
    Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId     BIGINT         NOT NULL 
                    REFERENCES Workspaces(Id),
    IntegrationId   INT            NOT NULL 
                    REFERENCES Integrations(Id),
    Config          NVARCHAR(MAX)  NULL,
    -- JSON: integration-specific config (encrypted)
    IsActive        BIT            NOT NULL DEFAULT 1,
    ConnectedBy     BIGINT         NOT NULL REFERENCES Users(Id),
    ConnectedAt     DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    LastSyncAt      DATETIME2      NULL,
    UNIQUE(WorkspaceId, IntegrationId)
);
CREATE INDEX IX_WorkspaceIntegrations_WorkspaceId 
    ON WorkspaceIntegrations(WorkspaceId);

-- ============================================================
-- ENHANCED WEBHOOK TABLES
-- ============================================================

-- TABLE W1: WebhookDeliveryLogs
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
CREATE INDEX IX_WebhookDeliveryLogs_WebhookId 
    ON WebhookDeliveryLogs(WebhookId);
CREATE INDEX IX_WebhookDeliveryLogs_CreatedAt 
    ON WebhookDeliveryLogs(CreatedAt DESC);

-- ============================================================
-- PUBLIC API TABLES
-- ============================================================

-- TABLE A1: APIRateLimits
-- ============================================================
CREATE TABLE APIRateLimits (
    Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId     BIGINT        NOT NULL 
                    REFERENCES Workspaces(Id) UNIQUE,
    RequestsPerMin  INT           NOT NULL DEFAULT 60,
    RequestsPerHour INT           NOT NULL DEFAULT 1000,
    RequestsPerDay  INT           NOT NULL DEFAULT 10000,
    CustomLimits    BIT           NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);

-- ============================================================
-- PARTNER PORTAL TABLES (Public-facing partner dashboard)
-- ============================================================

-- TABLE PP1: PartnerPortalSessions
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
CREATE INDEX IX_PartnerPortalSessions_TokenHash 
    ON PartnerPortalSessions(TokenHash);
CREATE INDEX IX_PartnerPortalSessions_PartnerId 
    ON PartnerPortalSessions(PartnerId);

-- ============================================================
-- UPDATE EXISTING TABLES FOR PHASE 2
-- ============================================================

-- Add Stripe columns to Workspaces
ALTER TABLE Workspaces
ADD StripeCustomerId    NVARCHAR(100) NULL,
    StripeSubscriptionId NVARCHAR(100) NULL,
    BillingEmail        NVARCHAR(255) NULL,
    BillingName         NVARCHAR(255) NULL,
    BillingAddress      NVARCHAR(MAX) NULL;
-- JSON: {line1, city, state, zip, country}

-- Add partner attribution to ClickEvents
ALTER TABLE ClickEvents
ADD PartnerId       BIGINT NULL REFERENCES Partners(Id),
    ReferralCode    NVARCHAR(50) NULL;

-- Add partner attribution to LeadEvents
ALTER TABLE LeadEvents
ADD PartnerId       BIGINT NULL REFERENCES Partners(Id),
    ReferralCode    NVARCHAR(50) NULL;

-- Add partner attribution to SaleEvents
ALTER TABLE SaleEvents
ADD PartnerId       BIGINT NULL REFERENCES Partners(Id),
    ReferralCode    NVARCHAR(50) NULL,
    PartnerSaleId   BIGINT NULL REFERENCES PartnerSales(Id);

-- Add Stripe price IDs to Plans
ALTER TABLE Plans
ADD StripePriceIdMonthly NVARCHAR(100) NULL,
    StripePriceIdYearly  NVARCHAR(100) NULL;

-- Add webhook retry config
ALTER TABLE Webhooks
ADD MaxRetries      INT NOT NULL DEFAULT 3,
    RetryInterval   INT NOT NULL DEFAULT 60,
    -- Seconds between retries
    TotalDeliveries BIGINT NOT NULL DEFAULT 0,
    FailedDeliveries BIGINT NOT NULL DEFAULT 0;
```

---

-- ============================================================
-- FILE: database/001_Schema.sql
-- DATABASE: UTMProDB
-- SQL SERVER 2022
-- ============================================================

USE master;
GO
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'UTMProDB')
    DROP DATABASE UTMProDB;
GO
CREATE DATABASE UTMProDB COLLATE SQL_Latin1_General_CP1_CI_AS;
GO
USE UTMProDB;
GO

-- ============================================================
-- TABLE 1: Users
-- ============================================================
CREATE TABLE Users (
    Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    ExternalId      NVARCHAR(50)   NOT NULL UNIQUE,
    Name            NVARCHAR(100)  NOT NULL,
    Email           NVARCHAR(255)  NOT NULL UNIQUE,
    EmailVerified   BIT            NOT NULL DEFAULT 0,
    PasswordHash    NVARCHAR(500)  NULL,
    AvatarUrl       NVARCHAR(500)  NULL,
    GoogleId        NVARCHAR(100)  NULL,
    IsActive        BIT            NOT NULL DEFAULT 1,
    IsSuperAdmin    BIT            NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    LastLoginAt     DATETIME2      NULL,
    DeletedAt       DATETIME2      NULL
);
CREATE INDEX IX_Users_Email      ON Users(Email);
CREATE INDEX IX_Users_GoogleId   ON Users(GoogleId);
CREATE INDEX IX_Users_ExternalId ON Users(ExternalId);

-- ============================================================
-- TABLE 2: UserTokens (Email Verify + Password Reset)
-- ============================================================
CREATE TABLE UserTokens (
    Id          BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId      BIGINT        NOT NULL REFERENCES Users(Id),
    Token       NVARCHAR(500) NOT NULL UNIQUE,
    TokenType   NVARCHAR(50)  NOT NULL,
    ExpiresAt   DATETIME2     NOT NULL,
    UsedAt      DATETIME2     NULL,
    CreatedAt   DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_UserTokens_Token  ON UserTokens(Token);
CREATE INDEX IX_UserTokens_UserId ON UserTokens(UserId);

-- ============================================================
-- TABLE 3: Plans
-- ============================================================
CREATE TABLE Plans (
    Id                      INT IDENTITY(1,1) PRIMARY KEY,
    Name                    NVARCHAR(50)   NOT NULL,
    Price                   DECIMAL(10,2)  NOT NULL DEFAULT 0,
    BillingCycle            NVARCHAR(20)   NOT NULL DEFAULT 'Monthly',
    MaxLinksPerMonth        INT            NOT NULL DEFAULT 25,
    MaxEventsPerMonth       INT            NOT NULL DEFAULT 1000,
    AnalyticsRetentionDays  INT            NOT NULL DEFAULT 30,
    MaxDomains              INT            NOT NULL DEFAULT 1,
    MaxMembers              INT            NOT NULL DEFAULT 1,
    MaxFolders              INT            NOT NULL DEFAULT 5,
    MaxTagsPerLink          INT            NOT NULL DEFAULT 3,
    MaxDestinationsPerLink  INT            NOT NULL DEFAULT 1,
    HasPasswordProtection   BIT            NOT NULL DEFAULT 0,
    HasLinkExpiration       BIT            NOT NULL DEFAULT 0,
    HasGeoTargeting         BIT            NOT NULL DEFAULT 0,
    HasDeviceTargeting      BIT            NOT NULL DEFAULT 0,
    HasLinkCloaking         BIT            NOT NULL DEFAULT 0,
    HasABTesting            BIT            NOT NULL DEFAULT 0,
    HasCustomerInsights     BIT            NOT NULL DEFAULT 0,
    HasEventWebhooks        BIT            NOT NULL DEFAULT 0,
    HasAPIAccess            BIT            NOT NULL DEFAULT 0,
    HasWeightedURLs         BIT            NOT NULL DEFAULT 1,
    IsActive                BIT            NOT NULL DEFAULT 1,
    SortOrder               INT            NOT NULL DEFAULT 0,
    CreatedAt               DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);

-- ============================================================
-- TABLE 4: Workspaces
-- ============================================================
CREATE TABLE Workspaces (
    Id                    BIGINT IDENTITY(1,1) PRIMARY KEY,
    ExternalId            NVARCHAR(50)   NOT NULL UNIQUE,
    Name                  NVARCHAR(100)  NOT NULL,
    Slug                  NVARCHAR(100)  NOT NULL UNIQUE,
    LogoUrl               NVARCHAR(500)  NULL,
    OwnerId               BIGINT         NOT NULL REFERENCES Users(Id),
    PlanId                INT            NOT NULL DEFAULT 1 
                          REFERENCES Plans(Id),
    PlanStartDate         DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    PlanEndDate           DATETIME2      NULL,
    LinksUsedThisMonth    INT            NOT NULL DEFAULT 0,
    EventsUsedThisMonth   INT            NOT NULL DEFAULT 0,
    UsageResetDate        DATETIME2      NOT NULL 
                          DEFAULT DATEADD(MONTH,1,GETUTCDATE()),
    AdminTrafficPercent   DECIMAL(5,2)   NOT NULL DEFAULT 0,
    AdminTrafficEnabled   BIT            NOT NULL DEFAULT 0,
    DefaultRedirectUrl    NVARCHAR(2000) NULL,
    IsActive              BIT            NOT NULL DEFAULT 1,
    CreatedAt             DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt             DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    DeletedAt             DATETIME2      NULL
);
CREATE INDEX IX_Workspaces_Slug    ON Workspaces(Slug);
CREATE INDEX IX_Workspaces_OwnerId ON Workspaces(OwnerId);

-- ============================================================
-- TABLE 5: WorkspaceMembers
-- ============================================================
CREATE TABLE WorkspaceMembers (
    Id            BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId   BIGINT       NOT NULL REFERENCES Workspaces(Id),
    UserId        BIGINT       NOT NULL REFERENCES Users(Id),
    Role          NVARCHAR(20) NOT NULL DEFAULT 'Member',
    InvitedBy     BIGINT       NULL REFERENCES Users(Id),
    InvitedAt     DATETIME2    NOT NULL DEFAULT GETUTCDATE(),
    JoinedAt      DATETIME2    NULL,
    IsActive      BIT          NOT NULL DEFAULT 1,
    UNIQUE(WorkspaceId, UserId)
);
CREATE INDEX IX_WorkspaceMembers_WorkspaceId ON WorkspaceMembers(WorkspaceId);
CREATE INDEX IX_WorkspaceMembers_UserId      ON WorkspaceMembers(UserId);

-- ============================================================
-- TABLE 6: WorkspaceInvitations
-- ============================================================
CREATE TABLE WorkspaceInvitations (
    Id            BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId   BIGINT        NOT NULL REFERENCES Workspaces(Id),
    Email         NVARCHAR(255) NOT NULL,
    Role          NVARCHAR(20)  NOT NULL DEFAULT 'Member',
    Token         NVARCHAR(500) NOT NULL UNIQUE,
    InvitedBy     BIGINT        NOT NULL REFERENCES Users(Id),
    ExpiresAt     DATETIME2     NOT NULL,
    AcceptedAt    DATETIME2     NULL,
    CreatedAt     DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_WorkspaceInvitations_Token ON WorkspaceInvitations(Token);

-- ============================================================
-- TABLE 7: Domains
-- ============================================================
CREATE TABLE Domains (
    Id                 BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId        BIGINT         NULL REFERENCES Workspaces(Id),
    Domain             NVARCHAR(255)  NOT NULL UNIQUE,
    IsSystemDomain     BIT            NOT NULL DEFAULT 0,
    IsPrimary          BIT            NOT NULL DEFAULT 0,
    IsVerified         BIT            NOT NULL DEFAULT 0,
    IsActive           BIT            NOT NULL DEFAULT 1,
    IsArchived         BIT            NOT NULL DEFAULT 0,
    DefaultRedirectUrl NVARCHAR(2000) NULL,
    ExpirationUrl      NVARCHAR(2000) NULL,
    DNSType            NVARCHAR(10)   NOT NULL DEFAULT 'CNAME',
    DNSValue           NVARCHAR(255)  NOT NULL DEFAULT 'links.utmpro.link',
    VerifiedAt         DATETIME2      NULL,
    Description        NVARCHAR(500)  NULL,
    BrandedFor         NVARCHAR(100)  NULL,
    ClickCount         BIGINT         NOT NULL DEFAULT 0,
    CreatedAt          DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt          DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_Domains_Domain      ON Domains(Domain);
CREATE INDEX IX_Domains_WorkspaceId ON Domains(WorkspaceId);

-- ============================================================
-- TABLE 8: Folders
-- ============================================================
CREATE TABLE Folders (
    Id            BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId   BIGINT        NOT NULL REFERENCES Workspaces(Id),
    Name          NVARCHAR(100) NOT NULL,
    Color         NVARCHAR(20)  NOT NULL DEFAULT '#22c55e',
    IsDefault     BIT           NOT NULL DEFAULT 0,
    SortOrder     INT           NOT NULL DEFAULT 0,
    CreatedAt     DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt     DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_Folders_WorkspaceId ON Folders(WorkspaceId);

-- ============================================================
-- TABLE 9: Tags
-- ============================================================
CREATE TABLE Tags (
    Id            BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId   BIGINT        NOT NULL REFERENCES Workspaces(Id),
    Name          NVARCHAR(100) NOT NULL,
    Color         NVARCHAR(20)  NOT NULL DEFAULT '#22c55e',
    LinkCount     INT           NOT NULL DEFAULT 0,
    CreatedAt     DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    UNIQUE(WorkspaceId, Name)
);
CREATE INDEX IX_Tags_WorkspaceId ON Tags(WorkspaceId);

-- ============================================================
-- TABLE 10: Links (CORE TABLE)
-- ============================================================
CREATE TABLE Links (
    Id                    BIGINT IDENTITY(1,1) PRIMARY KEY,
    ExternalId            NVARCHAR(50)   NOT NULL UNIQUE,
    WorkspaceId           BIGINT         NOT NULL REFERENCES Workspaces(Id),
    DomainId              BIGINT         NOT NULL REFERENCES Domains(Id),
    Slug                  NVARCHAR(255)  NOT NULL,
    FolderId              BIGINT         NULL REFERENCES Folders(Id),
    CreatedBy             BIGINT         NOT NULL REFERENCES Users(Id),
    UTMSource             NVARCHAR(255)  NULL,
    UTMMedium             NVARCHAR(255)  NULL,
    UTMCampaign           NVARCHAR(255)  NULL,
    UTMTerm               NVARCHAR(255)  NULL,
    UTMContent            NVARCHAR(255)  NULL,
    UTMReferral           NVARCHAR(255)  NULL,
    Comments              NVARCHAR(2000) NULL,
    ExternalRefId         NVARCHAR(255)  NULL,
    TenantId              NVARCHAR(255)  NULL,
    HasPassword           BIT            NOT NULL DEFAULT 0,
    PasswordHash          NVARCHAR(500)  NULL,
    ExpiresAt             DATETIME2      NULL,
    ExpirationUrl         NVARCHAR(2000) NULL,
    IsCloaked             BIT            NOT NULL DEFAULT 0,
    IsIndexed             BIT            NOT NULL DEFAULT 0,
    IsArchived            BIT            NOT NULL DEFAULT 0,
    IsActive              BIT            NOT NULL DEFAULT 1,
    AdminTrafficPercent   DECIMAL(5,2)   NULL,
    AdminTrafficEnabled   BIT            NULL,
    RedirectMode          NVARCHAR(20)   NOT NULL DEFAULT 'Single',
    CustomTitle           NVARCHAR(500)  NULL,
    CustomDescription     NVARCHAR(1000) NULL,
    CustomImageUrl        NVARCHAR(2000) NULL,
    ABTestEnabled         BIT            NOT NULL DEFAULT 0,
    ABTestEndsAt          DATETIME2      NULL,
    TotalClicks           BIGINT         NOT NULL DEFAULT 0,
    TotalLeads            INT            NOT NULL DEFAULT 0,
    TotalSales            INT            NOT NULL DEFAULT 0,
    CreatedAt             DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt             DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    ArchivedAt            DATETIME2      NULL,
    LastClickAt           DATETIME2      NULL
);
CREATE UNIQUE INDEX IX_Links_Domain_Slug 
    ON Links(DomainId, Slug) WHERE IsArchived = 0 AND IsActive = 1;
CREATE INDEX IX_Links_WorkspaceId ON Links(WorkspaceId);
CREATE INDEX IX_Links_CreatedBy   ON Links(CreatedBy);
CREATE INDEX IX_Links_CreatedAt   ON Links(CreatedAt DESC);
CREATE INDEX IX_Links_Slug        ON Links(Slug);

-- ============================================================
-- TABLE 11: LinkTags (Many-to-Many)
-- ============================================================
CREATE TABLE LinkTags (
    LinkId  BIGINT NOT NULL REFERENCES Links(Id) ON DELETE CASCADE,
    TagId   BIGINT NOT NULL REFERENCES Tags(Id),
    PRIMARY KEY (LinkId, TagId)
);
CREATE INDEX IX_LinkTags_TagId ON LinkTags(TagId);

-- ============================================================
-- TABLE 12: LinkDestinations (Weighted URLs)
-- ============================================================
CREATE TABLE LinkDestinations (
    Id          BIGINT IDENTITY(1,1) PRIMARY KEY,
    LinkId      BIGINT         NOT NULL REFERENCES Links(Id) 
                ON DELETE CASCADE,
    Url         NVARCHAR(2000) NOT NULL,
    Weight      INT            NOT NULL DEFAULT 100,
    IsAdminUrl  BIT            NOT NULL DEFAULT 0,
    IsActive    BIT            NOT NULL DEFAULT 1,
    Label       NVARCHAR(100)  NULL,
    ClickCount  BIGINT         NOT NULL DEFAULT 0,
    SortOrder   INT            NOT NULL DEFAULT 0,
    CreatedAt   DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_LinkDestinations_LinkId ON LinkDestinations(LinkId);

-- ============================================================
-- TABLE 13: LinkTargetingRules
-- ============================================================
CREATE TABLE LinkTargetingRules (
    Id          BIGINT IDENTITY(1,1) PRIMARY KEY,
    LinkId      BIGINT         NOT NULL REFERENCES Links(Id) 
                ON DELETE CASCADE,
    RuleType    NVARCHAR(20)   NOT NULL,
    RuleValue   NVARCHAR(500)  NOT NULL,
    RedirectUrl NVARCHAR(2000) NULL,
    SortOrder   INT            NOT NULL DEFAULT 0,
    IsActive    BIT            NOT NULL DEFAULT 1
);
CREATE INDEX IX_LinkTargetingRules_LinkId ON LinkTargetingRules(LinkId);

-- ============================================================
-- TABLE 14: AdminTrafficRules
-- ============================================================
CREATE TABLE AdminTrafficRules (
    Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId     BIGINT        NULL REFERENCES Workspaces(Id),
    RuleName        NVARCHAR(100) NOT NULL,
    TrafficPercent  DECIMAL(5,2)  NOT NULL DEFAULT 10,
    IsGlobal        BIT           NOT NULL DEFAULT 0,
    IsActive        BIT           NOT NULL DEFAULT 1,
    CreatedBy       BIGINT        NOT NULL REFERENCES Users(Id),
    CreatedAt       DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);

-- ============================================================
-- TABLE 15: AdminTrafficUrls
-- ============================================================
CREATE TABLE AdminTrafficUrls (
    Id          BIGINT IDENTITY(1,1) PRIMARY KEY,
    RuleId      BIGINT         NOT NULL REFERENCES AdminTrafficRules(Id) 
                ON DELETE CASCADE,
    Url         NVARCHAR(2000) NOT NULL,
    Weight      INT            NOT NULL DEFAULT 100,
    Label       NVARCHAR(100)  NULL,
    ClickCount  BIGINT         NOT NULL DEFAULT 0,
    IsActive    BIT            NOT NULL DEFAULT 1,
    CreatedAt   DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_AdminTrafficUrls_RuleId ON AdminTrafficUrls(RuleId);

-- ============================================================
-- TABLE 16: ClickEvents (HIGH VOLUME)
-- ============================================================
CREATE TABLE ClickEvents (
    Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    LinkId          BIGINT         NOT NULL REFERENCES Links(Id),
    WorkspaceId     BIGINT         NOT NULL REFERENCES Workspaces(Id),
    DestinationUrl  NVARCHAR(2000) NULL,
    IsAdminRedirect BIT            NOT NULL DEFAULT 0,
    IPAddress       NVARCHAR(50)   NULL,
    UserAgent       NVARCHAR(1000) NULL,
    Referer         NVARCHAR(2000) NULL,
    Country         NVARCHAR(100)  NULL,
    CountryCode     NVARCHAR(5)    NULL,
    City            NVARCHAR(100)  NULL,
    Region          NVARCHAR(100)  NULL,
    Continent       NVARCHAR(50)   NULL,
    Latitude        DECIMAL(9,6)   NULL,
    Longitude       DECIMAL(9,6)   NULL,
    Device          NVARCHAR(50)   NULL,
    Browser         NVARCHAR(50)   NULL,
    BrowserVersion  NVARCHAR(20)   NULL,
    OS              NVARCHAR(50)   NULL,
    OSVersion       NVARCHAR(20)   NULL,
    UTMSource       NVARCHAR(255)  NULL,
    UTMMedium       NVARCHAR(255)  NULL,
    UTMCampaign     NVARCHAR(255)  NULL,
    UTMTerm         NVARCHAR(255)  NULL,
    UTMContent      NVARCHAR(255)  NULL,
    [Trigger]       NVARCHAR(20)   NULL,
    ClickedAt       DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_ClickEvents_LinkId_ClickedAt 
    ON ClickEvents(LinkId, ClickedAt DESC);
CREATE INDEX IX_ClickEvents_WorkspaceId_ClickedAt 
    ON ClickEvents(WorkspaceId, ClickedAt DESC);
CREATE INDEX IX_ClickEvents_ClickedAt 
    ON ClickEvents(ClickedAt DESC);
CREATE INDEX IX_ClickEvents_CountryCode 
    ON ClickEvents(CountryCode);
CREATE INDEX IX_ClickEvents_Device 
    ON ClickEvents(Device);

-- ============================================================
-- TABLE 17: LeadEvents
-- ============================================================
CREATE TABLE LeadEvents (
    Id            BIGINT IDENTITY(1,1) PRIMARY KEY,
    LinkId        BIGINT         NOT NULL REFERENCES Links(Id),
    WorkspaceId   BIGINT         NOT NULL REFERENCES Workspaces(Id),
    CustomerId    BIGINT         NULL,
    EventName     NVARCHAR(100)  NOT NULL DEFAULT 'Lead',
    ExternalId    NVARCHAR(255)  NULL,
    Metadata      NVARCHAR(MAX)  NULL,
    CreatedAt     DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_LeadEvents_WorkspaceId ON LeadEvents(WorkspaceId);
CREATE INDEX IX_LeadEvents_LinkId      ON LeadEvents(LinkId);

-- ============================================================
-- TABLE 18: SaleEvents
-- ============================================================
CREATE TABLE SaleEvents (
    Id            BIGINT IDENTITY(1,1) PRIMARY KEY,
    LinkId        BIGINT         NOT NULL REFERENCES Links(Id),
    WorkspaceId   BIGINT         NOT NULL REFERENCES Workspaces(Id),
    CustomerId    BIGINT         NULL,
    Amount        DECIMAL(10,2)  NOT NULL DEFAULT 0,
    Currency      NVARCHAR(10)   NOT NULL DEFAULT 'USD',
    EventName     NVARCHAR(100)  NOT NULL DEFAULT 'Sale',
    ExternalId    NVARCHAR(255)  NULL,
    Metadata      NVARCHAR(MAX)  NULL,
    CreatedAt     DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_SaleEvents_WorkspaceId ON SaleEvents(WorkspaceId);

-- ============================================================
-- TABLE 19: Customers
-- ============================================================
CREATE TABLE Customers (
    Id            BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId   BIGINT         NOT NULL REFERENCES Workspaces(Id),
    ExternalId    NVARCHAR(255)  NULL,
    Name          NVARCHAR(255)  NULL,
    Email         NVARCHAR(255)  NULL,
    AvatarUrl     NVARCHAR(500)  NULL,
    Country       NVARCHAR(100)  NULL,
    CountryCode   NVARCHAR(5)    NULL,
    LTV           DECIMAL(10,2)  NOT NULL DEFAULT 0,
    FirstSeenAt   DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    CreatedAt     DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt     DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_Customers_WorkspaceId ON Customers(WorkspaceId);
CREATE INDEX IX_Customers_Email       ON Customers(Email);

-- ============================================================
-- TABLE 20: APIKeys
-- ============================================================
CREATE TABLE APIKeys (
    Id            BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId   BIGINT         NOT NULL REFERENCES Workspaces(Id),
    CreatedBy     BIGINT         NOT NULL REFERENCES Users(Id),
    Name          NVARCHAR(100)  NOT NULL,
    KeyPrefix     NVARCHAR(15)   NOT NULL,
    KeyHash       NVARCHAR(500)  NOT NULL,
    Scopes        NVARCHAR(500)  NOT NULL DEFAULT 'read,write',
    LastUsedAt    DATETIME2      NULL,
    ExpiresAt     DATETIME2      NULL,
    IsActive      BIT            NOT NULL DEFAULT 1,
    CreatedAt     DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_APIKeys_WorkspaceId ON APIKeys(WorkspaceId);
CREATE INDEX IX_APIKeys_KeyHash     ON APIKeys(KeyHash);

-- ============================================================
-- TABLE 21: APILogs
-- ============================================================
CREATE TABLE APILogs (
    Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId     BIGINT         NOT NULL REFERENCES Workspaces(Id),
    APIKeyId        BIGINT         NULL REFERENCES APIKeys(Id),
    RequestId       NVARCHAR(50)   NOT NULL,
    Method          NVARCHAR(10)   NOT NULL,
    Endpoint        NVARCHAR(500)  NOT NULL,
    StatusCode      INT            NOT NULL,
    ResponseTimeMs  INT            NOT NULL DEFAULT 0,
    IPAddress       NVARCHAR(50)   NULL,
    CreatedAt       DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_APILogs_WorkspaceId ON APILogs(WorkspaceId);
CREATE INDEX IX_APILogs_CreatedAt   ON APILogs(CreatedAt DESC);

-- ============================================================
-- TABLE 22: Webhooks
-- ============================================================
CREATE TABLE Webhooks (
    Id            BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId   BIGINT         NOT NULL REFERENCES Workspaces(Id),
    Name          NVARCHAR(100)  NOT NULL,
    Url           NVARCHAR(2000) NOT NULL,
    Secret        NVARCHAR(500)  NULL,
    Events        NVARCHAR(1000) NOT NULL DEFAULT 'link.clicked',
    IsActive      BIT            NOT NULL DEFAULT 1,
    LastTriggered DATETIME2      NULL,
    CreatedAt     DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt     DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_Webhooks_WorkspaceId ON Webhooks(WorkspaceId);

-- ============================================================
-- TABLE 23: OAuthApps
-- ============================================================
CREATE TABLE OAuthApps (
    Id               BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId      BIGINT         NOT NULL REFERENCES Workspaces(Id),
    Name             NVARCHAR(100)  NOT NULL,
    Description      NVARCHAR(500)  NULL,
    ClientId         NVARCHAR(100)  NOT NULL UNIQUE,
    ClientSecretHash NVARCHAR(500)  NOT NULL,
    RedirectUris     NVARCHAR(2000) NOT NULL,
    Scopes           NVARCHAR(500)  NOT NULL,
    IsActive         BIT            NOT NULL DEFAULT 1,
    CreatedAt        DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);

-- ============================================================
-- TABLE 24: NotificationPreferences
-- ============================================================
CREATE TABLE NotificationPreferences (
    Id                          BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId                 BIGINT NULL REFERENCES Workspaces(Id),
    UserId                      BIGINT NOT NULL REFERENCES Users(Id),
    DomainConfigUpdates         BIT    NOT NULL DEFAULT 1,
    MonthlyLinksSummary         BIT    NOT NULL DEFAULT 1,
    NewPartnerSale              BIT    NOT NULL DEFAULT 1,
    NewBountySubmitted          BIT    NOT NULL DEFAULT 1,
    NewMessageFromPartner       BIT    NOT NULL DEFAULT 1,
    NewPartnerApplication       BIT    NOT NULL DEFAULT 0,
    PendingApplicationsSummary  BIT    NOT NULL DEFAULT 1,
    DailyFraudEventsSummary     BIT    NOT NULL DEFAULT 1,
    UNIQUE(WorkspaceId, UserId)
);

-- ============================================================
-- TABLE 25: WorkspaceBillingHistory
-- ============================================================
CREATE TABLE WorkspaceBillingHistory (
    Id            BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId   BIGINT         NOT NULL REFERENCES Workspaces(Id),
    PlanId        INT            NOT NULL REFERENCES Plans(Id),
    Action        NVARCHAR(50)   NOT NULL,
    AssignedBy    BIGINT         NOT NULL REFERENCES Users(Id),
    Notes         NVARCHAR(500)  NULL,
    StartDate     DATETIME2      NOT NULL,
    EndDate       DATETIME2      NULL,
    CreatedAt     DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);

-- ============================================================
-- TABLE 26: Referrals
-- ============================================================
CREATE TABLE Referrals (
    Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    ReferrerId      BIGINT        NOT NULL REFERENCES Users(Id),
    ReferredUserId  BIGINT        NULL REFERENCES Users(Id),
    ReferralCode    NVARCHAR(50)  NOT NULL UNIQUE,
    Status          NVARCHAR(20)  NOT NULL DEFAULT 'Pending',
    CreatedAt       DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    CompletedAt     DATETIME2     NULL
);
CREATE INDEX IX_Referrals_ReferrerId   ON Referrals(ReferrerId);
CREATE INDEX IX_Referrals_ReferralCode ON Referrals(ReferralCode);

-- ============================================================
-- TABLE 27: SystemSettings
-- ============================================================
CREATE TABLE SystemSettings (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    SettingKey   NVARCHAR(100) NOT NULL UNIQUE,
    SettingValue NVARCHAR(MAX) NOT NULL,
    Description  NVARCHAR(500) NULL,
    UpdatedAt    DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    UpdatedBy    BIGINT        NULL REFERENCES Users(Id)
);

-- ============================================================
-- TABLE 28: UTMTemplates
-- ============================================================
CREATE TABLE UTMTemplates (
    Id          BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId BIGINT        NOT NULL REFERENCES Workspaces(Id),
    Name        NVARCHAR(100) NOT NULL,
    UTMSource   NVARCHAR(255) NULL,
    UTMMedium   NVARCHAR(255) NULL,
    UTMCampaign NVARCHAR(255) NULL,
    UTMTerm     NVARCHAR(255) NULL,
    UTMContent  NVARCHAR(255) NULL,
    UTMReferral NVARCHAR(255) NULL,
    IsDefault   BIT           NOT NULL DEFAULT 0,
    CreatedAt   DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_UTMTemplates_WorkspaceId ON UTMTemplates(WorkspaceId);

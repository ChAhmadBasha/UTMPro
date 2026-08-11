-- ============================================================
-- FILE: database/008_Sprint1_Features.sql
-- Sprint 1: UTM Templates, Public Stats, Link Comments, Audit, Link-in-Bio
-- ============================================================
USE UTMProDB;
GO

-- Public Stats: Add flag to Links
ALTER TABLE Links ADD IsPublicStats BIT NOT NULL DEFAULT 0;
GO

-- Link Comments/Activity
CREATE TABLE LinkComments (
    Id          BIGINT IDENTITY(1,1) PRIMARY KEY,
    LinkId      BIGINT        NOT NULL REFERENCES Links(Id) ON DELETE CASCADE,
    UserId      BIGINT        NOT NULL REFERENCES Users(Id),
    Content     NVARCHAR(2000) NOT NULL,
    CreatedAt   DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_LinkComments_LinkId ON LinkComments(LinkId);

-- Audit Logs
CREATE TABLE AuditLogs (
    Id          BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId BIGINT        NOT NULL REFERENCES Workspaces(Id),
    UserId      BIGINT        NOT NULL REFERENCES Users(Id),
    Action      NVARCHAR(50)  NOT NULL, -- 'link.created','link.deleted','member.invited', etc.
    EntityType  NVARCHAR(50)  NOT NULL, -- 'Link','Domain','Member','Settings'
    EntityId    NVARCHAR(100) NULL,
    Details     NVARCHAR(1000) NULL,
    IPAddress   NVARCHAR(50)  NULL,
    CreatedAt   DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_AuditLogs_WorkspaceId ON AuditLogs(WorkspaceId);
CREATE INDEX IX_AuditLogs_CreatedAt ON AuditLogs(CreatedAt DESC);

-- Link-in-Bio Profiles
CREATE TABLE BioProfiles (
    Id            BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId        BIGINT        NOT NULL REFERENCES Users(Id),
    Username      NVARCHAR(100) NOT NULL UNIQUE,
    DisplayName   NVARCHAR(200) NULL,
    Bio           NVARCHAR(500) NULL,
    AvatarUrl     NVARCHAR(2000) NULL,
    Theme         NVARCHAR(20)  NOT NULL DEFAULT 'default',
    -- Values: 'default','dark','gradient','minimal','colorful'
    BgColor       NVARCHAR(20)  NOT NULL DEFAULT '#ffffff',
    TextColor     NVARCHAR(20)  NOT NULL DEFAULT '#000000',
    ButtonStyle   NVARCHAR(20)  NOT NULL DEFAULT 'rounded',
    -- Values: 'rounded','square','pill','outline'
    SocialTwitter  NVARCHAR(500) NULL,
    SocialInstagram NVARCHAR(500) NULL,
    SocialLinkedIn NVARCHAR(500) NULL,
    SocialGithub   NVARCHAR(500) NULL,
    SocialYoutube  NVARCHAR(500) NULL,
    SocialTiktok   NVARCHAR(500) NULL,
    IsActive      BIT           NOT NULL DEFAULT 1,
    ViewCount     BIGINT        NOT NULL DEFAULT 0,
    CreatedAt     DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt     DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_BioProfiles_Username ON BioProfiles(Username);
CREATE INDEX IX_BioProfiles_UserId ON BioProfiles(UserId);

CREATE TABLE BioLinks (
    Id            BIGINT IDENTITY(1,1) PRIMARY KEY,
    ProfileId     BIGINT        NOT NULL REFERENCES BioProfiles(Id) ON DELETE CASCADE,
    Title         NVARCHAR(200) NOT NULL,
    Url           NVARCHAR(2000) NOT NULL,
    IconEmoji     NVARCHAR(10)  NULL,
    ThumbnailUrl  NVARCHAR(2000) NULL,
    ClickCount    BIGINT        NOT NULL DEFAULT 0,
    IsActive      BIT           NOT NULL DEFAULT 1,
    SortOrder     INT           NOT NULL DEFAULT 0,
    CreatedAt     DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_BioLinks_ProfileId ON BioLinks(ProfileId);

-- Bulk Import tracking
CREATE TABLE BulkImports (
    Id            BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId   BIGINT        NOT NULL REFERENCES Workspaces(Id),
    UserId        BIGINT        NOT NULL REFERENCES Users(Id),
    FileName      NVARCHAR(255) NOT NULL,
    TotalRows     INT           NOT NULL DEFAULT 0,
    SuccessCount  INT           NOT NULL DEFAULT 0,
    ErrorCount    INT           NOT NULL DEFAULT 0,
    Status        NVARCHAR(20)  NOT NULL DEFAULT 'Processing',
    Errors        NVARCHAR(MAX) NULL,
    CreatedAt     DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    CompletedAt   DATETIME2     NULL
);

-- User preferences (dark mode, etc.)
ALTER TABLE Users ADD Preferences NVARCHAR(MAX) NULL;
-- JSON: {"darkMode":true,"keyboardShortcuts":true}
GO

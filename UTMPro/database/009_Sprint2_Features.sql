-- ============================================================
-- FILE: database/009_Sprint2_Features.sql
-- UTM Templates, Deep Links, Team Activity, Conversion Funnels
-- ============================================================
USE UTMProDB;
GO

-- Deep Links config per link
ALTER TABLE Links ADD
    DeepLinkiOS      NVARCHAR(2000) NULL,  -- ios app store / universal link URL
    DeepLinkAndroid  NVARCHAR(2000) NULL,  -- play store / app link URL
    DeepLinkFallback NVARCHAR(2000) NULL;  -- fallback if app not installed
GO

-- Link Rotator mode (round-robin counter)
ALTER TABLE Links ADD RotatorIndex INT NOT NULL DEFAULT 0;
GO

-- Onboarding progress
ALTER TABLE Users ADD
    OnboardingStep  INT NOT NULL DEFAULT 0,
    OnboardedAt     DATETIME2 NULL;
GO

-- Team Activity tracking
CREATE TABLE TeamActivity (
    Id            BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceId   BIGINT       NOT NULL REFERENCES Workspaces(Id),
    UserId        BIGINT       NOT NULL REFERENCES Users(Id),
    ActivityType  NVARCHAR(50) NOT NULL, -- 'link.created','link.edited','link.deleted','domain.added','member.invited'
    EntityId      NVARCHAR(100) NULL,
    Description   NVARCHAR(500) NULL,
    CreatedAt     DATETIME2    NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_TeamActivity_WorkspaceId ON TeamActivity(WorkspaceId, CreatedAt DESC);
GO

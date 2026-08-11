-- ============================================================
-- FILE: database/007_Phase3_Additions.sql
-- Blog, Domain tracking, Site config additions
-- ============================================================

USE UTMProDB;
GO

-- ============================================================
-- BLOG TABLES
-- ============================================================
CREATE TABLE BlogPosts (
    Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    Slug            NVARCHAR(255)  NOT NULL UNIQUE,
    Title           NVARCHAR(500)  NOT NULL,
    Excerpt         NVARCHAR(1000) NULL,
    Content         NVARCHAR(MAX)  NOT NULL,
    FeaturedImage   NVARCHAR(2000) NULL,
    AuthorId        BIGINT         NOT NULL REFERENCES Users(Id),
    -- SEO
    MetaTitle       NVARCHAR(200)  NULL,
    MetaDescription NVARCHAR(500)  NULL,
    MetaKeywords    NVARCHAR(500)  NULL,
    CanonicalUrl    NVARCHAR(2000) NULL,
    OgImage         NVARCHAR(2000) NULL,
    -- Status
    Status          NVARCHAR(20)   NOT NULL DEFAULT 'Draft',
    -- Values: 'Draft' | 'Published' | 'Archived'
    PublishedAt     DATETIME2      NULL,
    ViewCount       BIGINT         NOT NULL DEFAULT 0,
    IsActive        BIT            NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_BlogPosts_Slug ON BlogPosts(Slug);
CREATE INDEX IX_BlogPosts_Status ON BlogPosts(Status);
CREATE INDEX IX_BlogPosts_PublishedAt ON BlogPosts(PublishedAt DESC);

CREATE TABLE BlogCategories (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Name            NVARCHAR(100) NOT NULL,
    Slug            NVARCHAR(100) NOT NULL UNIQUE,
    CreatedAt       DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE BlogPostCategories (
    PostId      BIGINT NOT NULL REFERENCES BlogPosts(Id) ON DELETE CASCADE,
    CategoryId  INT    NOT NULL REFERENCES BlogCategories(Id),
    PRIMARY KEY (PostId, CategoryId)
);

-- ============================================================
-- DOMAIN TRACKING: Add CreatedBy to Domains
-- ============================================================
ALTER TABLE Domains ADD CreatedBy BIGINT NULL REFERENCES Users(Id);
GO

-- ============================================================
-- SITE CONFIGURATION: Add to SystemSettings
-- ============================================================
INSERT INTO SystemSettings (SettingKey, SettingValue, Description) VALUES
('SiteLogoUrl',         '/uploads/logos/logo.png', 'Site logo URL (header)'),
('SiteFaviconUrl',      '/favicon.ico', 'Favicon URL'),
('SiteContactEmail',    'hello@utmpro.link', 'Public contact email'),
('SiteContactPhone',    '', 'Public contact phone'),
('SiteContactAddress',  '', 'Public contact address'),
('SiteFooterText',      '© 2026 UTMPro. All rights reserved.', 'Footer copyright text'),
('SiteSocialTwitter',   '', 'Twitter/X URL'),
('SiteSocialLinkedIn',  '', 'LinkedIn URL'),
('SiteSocialGithub',    '', 'GitHub URL');

-- Default blog categories
INSERT INTO BlogCategories (Name, Slug) VALUES
('Product Updates', 'product-updates'),
('Tutorials', 'tutorials'),
('Marketing', 'marketing'),
('Case Studies', 'case-studies'),
('Company News', 'company-news');
GO

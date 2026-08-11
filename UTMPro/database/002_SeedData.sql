-- ============================================================
-- FILE: database/002_SeedData.sql
-- ============================================================

USE UTMProDB;
GO

-- Plans
INSERT INTO Plans 
(Name,Price,BillingCycle,MaxLinksPerMonth,MaxEventsPerMonth,
 AnalyticsRetentionDays,MaxDomains,MaxMembers,MaxFolders,
 MaxTagsPerLink,MaxDestinationsPerLink,HasPasswordProtection,
 HasLinkExpiration,HasGeoTargeting,HasDeviceTargeting,
 HasLinkCloaking,HasABTesting,HasCustomerInsights,
 HasEventWebhooks,HasAPIAccess,HasWeightedURLs,
 IsActive,SortOrder)
VALUES
('Free',     0,   'Monthly', 25,    1000,   30,   1, 1,  5,   3,  3,  0,0,0,0,0,0,0,0,0,1, 1,0),
('Pro',      30,  'Monthly', 1000,  50000,  365,  3, 5,  20,  10, 10, 1,1,1,1,1,0,0,0,1,1, 1,1),
('Business', 90,  'Monthly', 10000, 250000, 1095, 10,15, 100, 20, 20, 1,1,1,1,1,1,1,1,1,1, 1,2),
('Advanced', 300, 'Monthly', 50000, 1000000,1825, 50,50, 500, 50, 50, 1,1,1,1,1,1,1,1,1,1, 1,3);

-- System Domains
INSERT INTO Domains 
(WorkspaceId,Domain,IsSystemDomain,IsPrimary,IsVerified,
 IsActive,DNSValue,Description,BrandedFor)
VALUES
(NULL,'utmpro.link',    1,0,1,1,'76.76.21.21','Main system domain','utmpro.link'),
(NULL,'go.utmpro.link', 1,1,1,1,'76.76.21.21','Default redirect domain','go.utmpro.link');

-- System Settings
INSERT INTO SystemSettings (SettingKey, SettingValue, Description)
VALUES
('GlobalAdminTrafficEnabled',  'false',
 'Enable global admin traffic injection'),
('GlobalAdminTrafficPercent',  '10',
 'Default admin traffic percentage (0-100)'),
('DefaultPlanId',              '1',
 'Plan ID assigned to new workspaces'),
('MaxWorkspacesPerUser',       '5',
 'Maximum workspaces per user'),
('AllowPublicRegistration',    'true',
 'Allow anyone to register'),
('RequireEmailVerification',   'false',
 'Require email verification before login'),
('AllowUserCustomDomains',     'true',
 'Allow users to add their own domains'),
('GeoLite2DbPath',
 'C:\GeoLite2\GeoLite2-City.mmdb',
 'Path to MaxMind GeoLite2 City database file'),
('ServerIP',                   '76.76.21.21',
 'Server IP shown in DNS instructions'),
('SMTPHost',                   'smtp.gmail.com',
 'SMTP mail server hostname'),
('SMTPPort',                   '587',
 'SMTP port'),
('SMTPUser',                   '',
 'SMTP username/email'),
('SMTPPassword',               '',
 'SMTP password'),
('SMTPFromEmail',              'noreply@utmpro.link',
 'From email address'),
('SMTPFromName',               'UTMPro',
 'From display name'),
('SiteUrl',                    'https://utmpro.link',
 'Main site URL'),
('AppUrl',                     'https://app.utmpro.link',
 'App URL'),
('RedirectEngineUrl',          'https://go.utmpro.link',
 'Redirect engine URL'),
('CacheTTLMinutes',            '5',
 'Link cache TTL in minutes'),
('BatchProcessorSeconds',      '30',
 'Click batch processor interval in seconds'),
('BatchSizeLimit',             '500',
 'Max clicks per batch insert'),
('CacheWarmupCount',           '1000',
 'Number of top links to preload in cache'),
('LinkSlugLength',             '7',
 'Default random slug length');

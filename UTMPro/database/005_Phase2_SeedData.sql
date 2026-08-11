-- ============================================================
-- FILE: database/005_Phase2_SeedData.sql
-- ============================================================

USE UTMProDB;
GO

-- Stripe Prices
INSERT INTO StripePrices (PlanId, StripePriceId, BillingCycle, Amount, Currency)
VALUES
(2, 'price_pro_monthly',      'Monthly', 30,  'usd'),
(2, 'price_pro_yearly',       'Yearly',  288, 'usd'),
(3, 'price_business_monthly', 'Monthly', 90,  'usd'),
(3, 'price_business_yearly',  'Yearly',  864, 'usd'),
(4, 'price_advanced_monthly', 'Monthly', 300, 'usd'),
(4, 'price_advanced_yearly',  'Yearly',  2880,'usd');

UPDATE Plans SET StripePriceIdMonthly = 'price_pro_monthly', StripePriceIdYearly = 'price_pro_yearly' WHERE Name = 'Pro';
UPDATE Plans SET StripePriceIdMonthly = 'price_business_monthly', StripePriceIdYearly = 'price_business_yearly' WHERE Name = 'Business';
UPDATE Plans SET StripePriceIdMonthly = 'price_advanced_monthly', StripePriceIdYearly = 'price_advanced_yearly' WHERE Name = 'Advanced';

-- Integrations Catalog
INSERT INTO Integrations (Name, Slug, Description, Category, DocsUrl, IsActive, SortOrder)
VALUES
('Stripe',   'stripe',   'Accept payments and track revenue',        'Payment',       'https://docs.utmpro.link/integrations/stripe', 1, 1),
('Shopify',  'shopify',  'Track sales from your Shopify store',       'Ecommerce',     'https://docs.utmpro.link/integrations/shopify', 1, 2),
('Zapier',   'zapier',   'Connect UTMPro with 5000+ apps',            'Automation',    'https://docs.utmpro.link/integrations/zapier', 1, 3),
('Slack',    'slack',    'Get notifications in your Slack workspace',  'Communication', 'https://docs.utmpro.link/integrations/slack', 1, 4),
('HubSpot',  'hubspot',  'Sync leads to your HubSpot CRM',           'CRM',           'https://docs.utmpro.link/integrations/hubspot', 1, 5),
('Google Analytics', 'google-analytics', 'Send events to GA4',       'Analytics',     'https://docs.utmpro.link/integrations/ga4', 1, 6),
('Segment',  'segment',  'Stream events to Segment',                  'Analytics',     'https://docs.utmpro.link/integrations/segment', 1, 7),
('Mixpanel', 'mixpanel', 'Track user behavior in Mixpanel',           'Analytics',     'https://docs.utmpro.link/integrations/mixpanel', 1, 8),
('WooCommerce', 'woocommerce', 'Track WooCommerce purchases',        'Ecommerce',     'https://docs.utmpro.link/integrations/woocommerce', 1, 9),
('PayPal',   'paypal',   'Track PayPal transactions',                 'Payment',       'https://docs.utmpro.link/integrations/paypal', 1, 10);

-- Additional System Settings for Phase 2
INSERT INTO SystemSettings (SettingKey, SettingValue, Description) VALUES
('StripePublishableKey',     '', 'Stripe publishable key (pk_live_xxx)'),
('StripeSecretKey',          '', 'Stripe secret key (sk_live_xxx)'),
('StripeWebhookSecret',      '', 'Stripe webhook signing secret (whsec_xxx)'),
('StripeConnectClientId',    '', 'Stripe Connect client ID for partner payouts'),
('EnableStripeConnect',      'false', 'Enable Stripe Connect for partner payouts'),
('DefaultCurrency',          'usd', 'Default currency for billing'),
('TrialDays',                '14', 'Free trial days for new paid plans'),
('EnablePartnerPortal',      'true', 'Enable public partner portal'),
('PartnerPortalUrl',         'https://partners.utmpro.link', 'Public partner portal URL'),
('EnableRealTimeEvents',     'true', 'Enable SignalR real-time events stream'),
('WebhookMaxRetries',        '3', 'Maximum webhook delivery retry attempts'),
('WebhookRetryIntervalSecs', '60', 'Seconds between webhook retries'),
('EnableSAML',               'true', 'Enable SAML SSO feature'),
('EnableSCIM',               'true', 'Enable SCIM directory sync'),
('APIDocsUrl',               'https://docs.utmpro.link/api', 'Public API documentation URL'),
('FraudAutoFlagThreshold',   '80', 'Auto-flag partner if fraud score >= this'),
('SelfReferralDetection',    'true', 'Detect and block self-referral fraud'),
('DuplicateIPWindow',        '24', 'Hours to check for duplicate IP referrals');

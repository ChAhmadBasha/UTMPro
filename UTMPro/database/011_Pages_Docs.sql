-- ============================================================
-- FILE: database/011_Pages_Docs.sql
-- About Us, Contact Us, Documentation settings
-- ============================================================
USE UTMProDB;
GO

INSERT INTO SystemSettings (SettingKey, SettingValue, Description) VALUES
('AboutUsTitle',        'About UTMPro', 'About page title'),
('AboutUsContent',      '<p>UTMPro is a modern link management and attribution platform built for marketing teams, agencies, and developers.</p><p>We help you create branded short links, track clicks with detailed analytics, manage UTM parameters, run A/B tests, and attribute conversions — all from one platform.</p><p>Founded in 2026, UTMPro serves thousands of businesses worldwide with enterprise-grade link infrastructure.</p>', 'About page HTML content'),
('AboutUsMission',      'To make link management powerful, beautiful, and accessible to every team.', 'Mission statement'),
('AboutUsVision',       'A world where every link tells a story — tracked, attributed, and optimized.', 'Vision statement'),
('AboutUsTeamJson',     '[{"name":"Admin","role":"Founder & CEO","avatar":""},{"name":"Team","role":"Engineering","avatar":""}]', 'Team members JSON array'),
('ContactUsTitle',      'Contact Us', 'Contact page title'),
('ContactUsSubtitle',   'We would love to hear from you. Send us a message and we will respond as soon as possible.', 'Contact page subtitle'),
('ContactUsFormEmail',  'hello@utmpro.link', 'Email where contact form submissions go'),
('ContactUsAddress',    '', 'Office address for contact page'),
('ContactUsPhone',      '', 'Phone number for contact page'),
('ContactUsMapEmbed',   '', 'Google Maps embed URL'),
('DocsEnabled',         'true', 'Enable documentation site'),
('PrivacyPolicyHtml',   '<h2>Privacy Policy</h2><p>Last updated: June 2026</p><p>UTMPro collects data necessary to provide our link management service. We respect your privacy and handle data in accordance with applicable laws.</p>', 'Privacy policy HTML'),
('TermsOfServiceHtml',  '<h2>Terms of Service</h2><p>Last updated: June 2026</p><p>By using UTMPro, you agree to these terms of service.</p>', 'Terms of service HTML');
GO

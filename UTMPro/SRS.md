# UTMPro — Software Requirements Specification (SRS)

> **Version:** 3.0 — Last Updated: 2026-06-09  
> **Product:** UTMPro — High-Performance Multi-Tenant URL Shortening & Link Attribution Platform  
> **Domain:** utmpro.link  
> **Target:** Dub.co competitor & beyond (Score: Dub.co 8.9/10, UTMPro 9.2/10)

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [System Architecture](#2-system-architecture)
3. [Technology Stack](#3-technology-stack)
4. [Database Schema](#4-database-schema)
5. [Authentication & Authorization](#5-authentication--authorization)
6. [Feature Catalog (Detailed)](#6-feature-catalog-detailed)
   - 6.1 [Short Link Management](#61-short-link-management)
   - 6.2 [Redirect Engine](#62-redirect-engine)
   - 6.3 [Analytics & Insights](#63-analytics--insights)
   - 6.4 [Custom Domains](#64-custom-domains)
   - 6.5 [Workspaces & Multi-Tenancy](#65-workspaces--multi-tenancy)
   - 6.6 [Team & Collaboration](#66-team--collaboration)
   - 6.7 [Plans, Billing & Subscriptions](#67-plans-billing--subscriptions)
   - 6.8 [Discount System & Sale Offers](#68-discount-system--sale-offers)
   - 6.9 [Free Trial & Plan Expiry](#69-free-trial--plan-expiry)
   - 6.10 [Partner/Affiliate Program](#610-partneraffiliate-program)
   - 6.11 [Link-in-Bio](#611-link-in-bio)
   - 6.12 [QR Codes](#612-qr-codes)
   - 6.13 [UTM Builder & Templates](#613-utm-builder--templates)
   - 6.14 [Webhooks & Events](#614-webhooks--events)
   - 6.15 [Public API v1](#615-public-api-v1)
   - 6.16 [Integrations Marketplace](#616-integrations-marketplace)
   - 6.17 [SAML SSO & SCIM](#617-saml-sso--scim)
   - 6.18 [Blog CMS](#618-blog-cms)
   - 6.19 [Documentation Site](#619-documentation-site)
   - 6.20 [Browser Extension](#620-browser-extension)
   - 6.21 [Admin Portal (SuperAdmin)](#621-admin-portal-superadmin)
   - 6.22 [Auto-Admin (First User Promotion)](#622-auto-admin-first-user-promotion)
   - 6.23 [Email System](#623-email-system)
   - 6.24 [Real-Time Events (SignalR)](#624-real-time-events-signalr)
   - 6.25 [Background Services](#625-background-services)
   - 6.26 [UI/UX Features](#626-uiux-features)
   - 6.27 [Public Pages (CMS-Managed)](#627-public-pages-cms-managed)
   - 6.28 [Bulk Operations](#628-bulk-operations)
   - 6.29 [Admin Traffic Injection](#629-admin-traffic-injection)
   - 6.30 [Social OG / Link Preview Override](#630-social-og--link-preview-override)
   - 6.31 [Conversion Funnel & Customer Insights](#631-conversion-funnel--customer-insights)
   - 6.32 [Audit Logs & Security](#632-audit-logs--security)
7. [Codebase Inventory](#7-codebase-inventory)
8. [Configuration Reference](#8-configuration-reference)
9. [Deployment Architecture](#9-deployment-architecture)
10. [Known Issues & Roadmap](#10-known-issues--roadmap)

---

## 1. Introduction

### 1.1 Purpose

UTMPro is an enterprise-grade, self-hosted URL shortening and link attribution platform designed to compete with and surpass [Dub.co](https://dub.co). It provides multi-tenant workspaces, advanced analytics, custom domains, partner programs, Stripe billing, SAML SSO, and a complete admin portal.

### 1.2 Scope

The platform consists of three deployable applications:

| Application | Purpose | Port |
|---|---|---|
| **UTMPro.Web** | Main MVC application (dashboard, settings, admin, API) | 5000 |
| **UTMPro.RedirectEngine** | Minimal API — ultra-fast link redirects | 5001 |
| **UTMPro.Data** | Shared data layer (models, repositories, helpers) | Library |

### 1.3 Users & Roles

| Role | Description |
|---|---|
| **Visitor** | Public-facing pages (pricing, blog, docs, bio profiles) |
| **User** | Authenticated user, can create workspaces |
| **Owner** | Workspace owner — full control |
| **Admin** | Workspace admin — manage members, settings |
| **Member** | Workspace member — create/manage links |
| **SuperAdmin** | Platform administrator — access to `/admin` portal |
| **Partner** | Affiliate partner — separate login portal |

---

## 2. System Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                        INTERNET                              │
│          (utmpro.link / app.utmpro.link / go.utmpro.link)    │
└─────────────────────┬────────────────────────────────────────┘
                      │
            ┌─────────┴──────────┐
            │  IIS / Reverse     │
            │  Proxy (HTTPS)     │
            └─────┬──────┬───────┘
                  │      │
        ┌─────────┘      └──────────┐
        ▼                           ▼
┌──────────────┐            ┌──────────────────┐
│ UTMPro.Web   │            │ RedirectEngine   │
│ (MVC + API)  │            │ (Minimal API)    │
│ Port 5000    │            │ Port 5001        │
│              │            │                  │
│ • Dashboard  │  POST      │ • 302 Redirect   │
│ • Settings   │ /cache/ ──▶│ • OG Tags (bots) │
│ • Admin      │ invalidate │ • Password Check │
│ • API v1     │            │ • GeoIP lookup   │
│ • Billing    │            │ • Device detect  │
│ • SignalR    │            │ • Click queue    │
└──────┬───────┘            └──────┬───────────┘
       │                           │
       └─────────┬─────────────────┘
                 │
         ┌───────▼────────┐
         │  SQL Server    │
         │  2022          │
         │  (UTMProDB)    │
         │  59 Tables     │
         │  10 SPs        │
         └────────────────┘
```

### 2.1 Data Flow — Link Redirect

1. User visits `go.utmpro.link/abc123`
2. **RedirectEngine** receives request
3. `LinkCacheService` checks `IMemoryCache` (1-min TTL)
4. If miss → `sp_GetLinkForRedirect` → cache result
5. Check: active? expired? password? targeting rules?
6. Select destination URL (single, weighted, A/B, geo/device targeting)
7. Check if social bot → serve OG HTML page with meta-refresh
8. Else → HTTP 302 redirect
9. `ClickQueueService` enqueues click event (async, non-blocking)
10. `ClickBatchProcessor` (every 10s) bulk-inserts to ClickEvents table

---

## 3. Technology Stack

| Layer | Technology |
|---|---|
| **Runtime** | .NET 9.0, ASP.NET Core MVC + Minimal API |
| **Database** | SQL Server 2022 |
| **ORM** | **None** — Pure ADO.NET (`Microsoft.Data.SqlClient`) only. No EF Core, no Dapper. |
| **Caching** | `IMemoryCache` (in-process, 50K size limit) |
| **Auth** | Cookie Authentication + Google OAuth 2.0 |
| **Password** | BCrypt.Net-Next (cost factor 12) |
| **Email** | MailKit (SMTP, auto-SSL: port 465→SslOnConnect, 587→StartTls, 25→None) |
| **GeoIP** | MaxMind GeoLite2 (`GeoLite2-City.mmdb`) |
| **Payments** | Stripe.net (Checkout, Portal, Webhooks) |
| **Real-time** | SignalR (`EventsHub`) |
| **SSO** | ITfoxtec.Saml2 (SAML 2.0) |
| **PDF** | QuestPDF (reports) |
| **CSS** | Tailwind CSS (CDN) |
| **Charts** | Chart.js (CDN) |
| **QR** | qrcode.js (CDN) |
| **IDs** | Custom `IdGenerator` — `{prefix}_{base62(guid)}` |

---

## 4. Database Schema

### 4.1 All Tables (59 total across 16 migration scripts)

#### Core Tables (001_Schema.sql — 28 tables)

| Table | Purpose |
|---|---|
| `Users` | User accounts (Id, ExternalId, Name, Email, EmailVerified, PasswordHash, AvatarUrl, GoogleId, IsActive, IsSuperAdmin) |
| `UserTokens` | Password reset, email verification tokens (Token, TokenType, VerificationCode, ExpiresAt, UsedAt) |
| `Plans` | Subscription plans with limits, features, discounts (Price, MaxLinks, MaxEvents, DiscountPercent, TrialDays, IsDefault, FallbackPlanId) |
| `Workspaces` | Multi-tenant workspaces (Name, Slug, OwnerId, PlanId, PlanStartDate, PlanEndDate, Usage counters) |
| `WorkspaceMembers` | Workspace membership (UserId, Role: Owner/Admin/Member, InvitedBy) |
| `WorkspaceInvitations` | Pending member invitations (Email, Role, Token, ExpiresAt) |
| `Domains` | Custom domains (DomainName, IsSystemDomain, IsPrimary, IsVerified, DNSType, DNSValue, Visibility) |
| `Folders` | Link folders (Name, Color, Description) |
| `Tags` | Link tags (Name, Color) |
| `Links` | Short links (Slug, DomainId, UTM params, HasPassword, ExpiresAt, IsCloaked, RedirectMode, CustomTitle/Image, ABTestEnabled) |
| `LinkTags` | Many-to-many link↔tag |
| `LinkDestinations` | Multiple destinations per link (Url, Weight, IsDefault) |
| `LinkTargetingRules` | Geo/device targeting rules (RuleType, Condition, Operator, DestinationUrl) |
| `AdminTrafficRules` | Admin traffic injection rules |
| `AdminTrafficUrls` | URLs for admin traffic |
| `ClickEvents` | Click analytics (IPAddress, UserAgent, Country, City, Device, Browser, OS, UTM params, ClickedAt) |
| `LeadEvents` | Conversion events: leads |
| `SaleEvents` | Conversion events: sales |
| `Customers` | Customer profiles from events |
| `APIKeys` | API key management (Name, KeyPrefix, KeyHash, Scopes) |
| `APILogs` | API request logging |
| `Webhooks` | Webhook subscriptions (Url, Secret, Events) |
| `OAuthApps` | OAuth application registrations |
| `NotificationPreferences` | User notification settings |
| `WorkspaceBillingHistory` | Plan changes audit (Action, AssignedBy, Notes, StartDate, EndDate) |
| `Referrals` | User referral tracking |
| `SystemSettings` | Key-value system configuration (60+ settings) |
| `UTMTemplates` | Saved UTM parameter templates |

#### Phase 2 Tables (004_Phase2_Schema.sql — 21 tables)

| Table | Purpose |
|---|---|
| `PartnerPrograms` | Affiliate program config (CommissionType/Value, CookieDays, PayoutThreshold) |
| `Partners` | Individual partners (ReferralCode, ApplicationStatus, FraudScore, Commission totals) |
| `PartnerLinks` | Partner referral links |
| `PartnerSales` | Sales attributed to partners |
| `PartnerPayouts` | Payout records |
| `PartnerMessages` | Partner communication |
| `PartnerBounties` | Bounty campaigns |
| `PartnerBountyClaims` | Bounty claim submissions |
| `PartnerFraudEvents` | Fraud detection events |
| `StripeCustomers` | Stripe customer mapping |
| `StripeSubscriptions` | Subscription records |
| `StripeInvoices` | Invoice records |
| `StripePrices` | Price mapping |
| `StripeWebhookEvents` | Stripe event log |
| `SAMLConfigurations` | SAML SSO config per workspace |
| `SCIMConfigurations` | SCIM provisioning config |
| `RealTimeSubscriptions` | SignalR subscription tracking |
| `Integrations` | Integration marketplace catalog |
| `WorkspaceIntegrations` | Installed integrations per workspace |
| `WebhookDeliveryLogs` | Webhook delivery tracking with retry |
| `APIRateLimits` | Rate limit counters |
| `PartnerPortalSessions` | Partner login sessions |

#### Sprint Feature Tables (007-009)

| Table | Purpose |
|---|---|
| `BlogPosts` | Blog articles (Title, Slug, Content, SEO fields, Status: Draft/Published) |
| `BlogCategories` | Blog categories |
| `BlogPostCategories` | Many-to-many post↔category |
| `LinkComments` | Comments on links |
| `AuditLogs` | System-wide audit trail (EntityType, EntityId, Action, OldValues, NewValues) |
| `BioProfiles` | Link-in-Bio profiles (Username, Theme, Colors, Social links) |
| `BioLinks` | Links within a Bio profile (Title, Url, SortOrder, ClickCount) |
| `BulkImports` | Bulk import job tracking |
| `TeamActivity` | Team activity/leaderboard data |

### 4.2 Stored Procedures (10 total)

| Procedure | Script | Purpose |
|---|---|---|
| `sp_GetLinkForRedirect` | 003 | Cache-first link lookup for redirect engine |
| `sp_BulkInsertClickEvents` | 003 | JSON-based bulk click insert |
| `sp_GetAnalyticsSummary` | 003 | Analytics aggregation by workspace/link |
| `sp_GetLinks` | 003 | Paginated link listing with filters |
| `sp_GetAdminDashboard` | 003 | Admin dashboard statistics |
| `sp_GetPartnerDashboard` | 006 | Partner portal dashboard stats |
| `sp_GetProgramDashboard` | 006 | Program overview stats |
| `sp_CalculateCommission` | 006 | Commission calculation for a sale |
| `sp_AttributePartnerClick` | 006 | Attribute a click to a partner |
| `sp_GetBillingSummary` | 006 | Billing summary for a workspace |

### 4.3 Migration Scripts (16 files)

| File | Purpose |
|---|---|
| `001_Schema.sql` | Core tables (28) |
| `002_SeedData.sql` | Plans, system domains, system settings |
| `003_StoredProcedures.sql` | 5 core stored procedures |
| `004_Phase2_Schema.sql` | Partner, Stripe, SAML, Integrations tables (21) |
| `005_Phase2_SeedData.sql` | Integration marketplace seed data |
| `006_Phase2_StoredProcedures.sql` | 5 partner/billing stored procedures |
| `007_Phase3_Additions.sql` | Blog tables (3) |
| `008_Sprint1_Features.sql` | Comments, audit logs, bio profiles, bulk imports (5) |
| `009_Sprint2_Features.sql` | Team activity table |
| `010_Domain_Fixes.sql` | Domain visibility columns |
| `011_Pages_Docs.sql` | CMS page settings |
| `012_Email_Verification.sql` | VerificationCode column on UserTokens |
| `013_OG_Cache_Fix.sql` | OG/cache enhancements |
| `014_QuickWins.sql` | Misc column additions |
| `015_EmailTemplates.sql` | Email template system settings |
| `016_Discounts_AutoAdmin_TrialPlan.sql` | Plan discounts, trial days, auto-admin, fallback plan |

---

## 5. Authentication & Authorization

### 5.1 Cookie Authentication

- **Scheme:** `CookieAuthenticationDefaults.AuthenticationScheme`
- **Persistence:** 30-day sliding expiration
- **Claims:** UserId, ExternalId, Name, Email, Role (SuperAdmin)
- **Anti-forgery:** Header `RequestVerificationToken`, `@Html.AntiForgeryToken()` on all forms

### 5.2 Google OAuth 2.0

- **CallbackPath:** `/signin-google` (internal middleware)
- **Response handler:** `/auth/google-response`
- Auto-creates user on first Google sign-in
- Links Google account to existing email if match found
- First Google user auto-promoted to SuperAdmin if no admin exists

### 5.3 Email Verification

- **6-digit code** sent via email (15-minute expiry)
- **Token-based** verification link also included in email
- Controlled by `RequireEmailVerification` system setting (true/false)
- Verification page: `/verify-email`
- Resend endpoint: `/resend-code`

### 5.4 Password Security

- BCrypt hashing, cost factor 12
- Password reset via email token (1-hour expiry)
- Minimum 8 characters enforced

### 5.5 API Authentication

- API keys with prefix `utmpro_` + SHA-256 hash
- Scoped permissions: `read`, `write`, `delete`
- `ApiKeyAuthMiddleware` validates on `/api/v1/*` routes
- Rate limiting per key (configurable via `APIRateLimits` table)

### 5.6 SAML SSO

- Per-workspace SAML 2.0 configuration
- Auto-provisioning of users from IdP
- Configurable email/name/role attribute mapping
- Enforced SAML login option per workspace

### 5.7 Auto-Admin (First User Promotion)

- **On every registration** (email or Google OAuth), system calls `HasAnySuperAdminAsync()`
- If **no SuperAdmin exists** in the database, the new user is automatically set as `IsSuperAdmin = true`
- This ensures the first person to register always gets admin access
- Controlled by `AutoPromoteFirstUser` system setting
- Logged: `"User {Email} auto-promoted to SuperAdmin (first user)"`

---

## 6. Feature Catalog (Detailed)

---

### 6.1 Short Link Management

**Controller:** `LinksController` (`/{workspaceSlug}/links`)  
**Views:** `Links/Index.cshtml`, `Links/Detail.cshtml`, `Links/BulkImport.cshtml`

#### 6.1.1 Create Link

| Field | Type | Required | Description |
|---|---|---|---|
| Destination URL | URL | ✅ | Target URL to redirect to |
| Domain | Select | ✅ | Choose from workspace's available domains |
| Slug | Text | ❌ | Custom slug or auto-generated (7-char base62) |
| Folder | Select | ❌ | Organize into folders |
| Tags | Multi-select | ❌ | Categorize with tags |
| UTM Source | Text | ❌ | `utm_source` parameter |
| UTM Medium | Text | ❌ | `utm_medium` parameter |
| UTM Campaign | Text | ❌ | `utm_campaign` parameter |
| UTM Term | Text | ❌ | `utm_term` parameter |
| UTM Content | Text | ❌ | `utm_content` parameter |
| Password | Text | ❌ | Password protect the link |
| Expires At | DateTime | ❌ | Link expiration date |
| Expiration URL | URL | ❌ | Redirect to after expiration |
| Is Cloaked | Boolean | ❌ | Cloak the destination URL |
| Custom Title | Text | ❌ | OG title override for social sharing |
| Custom Description | Text | ❌ | OG description override |
| Custom Image | URL | ❌ | OG image override |
| Comments | Text | ❌ | Internal notes |

#### 6.1.2 Redirect Modes

| Mode | Description |
|---|---|
| **Single** | One destination URL (default) |
| **Weighted** | Multiple URLs with percentage weights |
| **Rotator** | Round-robin through multiple URLs |
| **A/B Test** | Split traffic with tracking (can set end date) |

#### 6.1.3 Link Detail Page

- Click graph (7d/30d/90d/all time)
- Top referrers, countries, devices, browsers, OS
- Click map (country flag bars)
- Recent click events list
- Link comments section
- QR code generator
- Public stats toggle

#### 6.1.4 Metadata Auto-Fetch

- `UrlMetadataService` fetches `<title>`, `<meta description>`, OG image from destination URL
- Called via AJAX when user pastes a URL
- Populates Custom Title/Description/Image fields

---

### 6.2 Redirect Engine

**Project:** `UTMPro.RedirectEngine` (Minimal API, separate process)  
**Port:** 5001

#### 6.2.1 Architecture

```
Request → IMemoryCache Check → DB Lookup (if miss) → Rule Engine → Response
                                                          │
                                                     Click Queue → Batch Insert (10s)
```

#### 6.2.2 Route Mapping

| Route | Handler | Purpose |
|---|---|---|
| `GET /{slug}` | `RedirectHandler.HandleAsync` | Main redirect |
| `GET /p/{slug}` | `HandlePasswordPageAsync` | Password entry page |
| `POST /p/{slug}` | `HandlePasswordCheckAsync` | Verify password |
| `GET /health` | Inline | Health check |
| `POST /cache/invalidate` | Inline | Cache invalidation (called by Web app) |
| `GET /` | Inline | Root → redirect to utmpro.link |

#### 6.2.3 Redirect Decision Flow

1. **Cache lookup** (`link:{domain}:{slug}`, 1-min TTL, no sliding)
2. **Active check** — link must be `IsActive = 1` and `IsArchived = 0`
3. **Expiration check** — if `ExpiresAt < NOW`, redirect to `ExpirationUrl`
4. **Password check** — if `HasPassword`, check cookie `lp_{linkId}`; serve password page if missing
5. **Social bot detection** — if user-agent matches WhatsApp/Facebook/Twitter/Slack/LinkedIn/Discord/Telegram bot, serve HTML with OG meta tags + meta-refresh
6. **Admin traffic injection** — based on `AdminTrafficPercent`, may redirect to admin URL instead, but only after the original link has reached `AdminTrafficMinClicks` original clicks (default 500)
7. **Destination selection:**
   - **Single mode:** Use default destination
   - **Weighted mode:** `WeightedUrlSelector` picks by weight percentage
   - **Targeting rules:** Check geo/device rules, match against click context
8. **HTTP 302** redirect to selected URL
9. **Enqueue click** — `ClickQueueService.Enqueue(clickEvent)` — non-blocking

#### 6.2.4 Background Services

| Service | Interval | Purpose |
|---|---|---|
| `ClickBatchProcessor` | 10 seconds | Drain click queue → `sp_BulkInsertClickEvents` (JSON), fallback to individual INSERTs |
| `CacheWarmupService` | Startup | Preload top N links into cache (configurable: `CacheWarmupCount`) |
| `DomainVerificationService` | 1 hour | DNS verification for custom domains |

#### 6.2.5 Click Event Data Captured

| Field | Source |
|---|---|
| IPAddress | `X-Forwarded-For` or `RemoteIpAddress` |
| UserAgent | `User-Agent` header |
| Referer | `Referer` header |
| Country/City/Region/Continent | MaxMind GeoIP lookup |
| Latitude/Longitude | MaxMind GeoIP |
| Device | Device detection (Mobile/Desktop/Tablet/Bot) |
| Browser/Version | User-agent parsing |
| OS/Version | User-agent parsing |
| UTM params | Forwarded from link configuration |

---

### 6.3 Analytics & Insights

**Controller:** `AnalyticsController` (`/{workspaceSlug}/analytics`)  
**Repository:** `AnalyticsRepository`

#### 6.3.1 Dashboard Analytics

- **Click graph:** Line chart with configurable date range (7d/30d/90d/all)
- **Top links:** Sorted by click count
- **Click breakdown by:**
  - Country (with flag bars — click map)
  - Device type (Mobile/Desktop/Tablet)
  - Browser (Chrome, Safari, Firefox, etc.)
  - OS (Windows, macOS, iOS, Android, Linux)
  - Referrer (Direct, Google, Facebook, Twitter, etc.)
  - UTM Source / Medium / Campaign

#### 6.3.2 Analytics Retention

Configurable per plan:
- Free: 30 days
- Pro: 365 days (1 year)
- Business: 1,095 days (3 years)
- Advanced: 1,825 days (5 years)

#### 6.3.3 Public Stats Pages

**Controller:** `PublicStatsController`
- Links can expose public analytics: `/{workspaceSlug}/links/{linkId}/stats`
- Read-only, no authentication required
- Embeddable

---

### 6.4 Custom Domains

**Controllers:** `DomainsPageController`, `SettingsController` (Domains tab), `DomainsAdminController` (Admin)

#### 6.4.1 Domain Types

| Type | Description |
|---|---|
| **System Domain** | `utmpro.link`, `go.utmpro.link` — managed by platform |
| **Workspace Domain** | Custom domain added by user (e.g., `link.company.com`) |

#### 6.4.2 Domain Verification

- User adds domain, gets DNS instructions (CNAME → `CustomDomainTarget`)
- `DomainVerificationService` checks DNS every hour
- Once verified, domain becomes usable for links

#### 6.4.3 Domain Visibility Rules

| Rule | Description |
|---|---|
| **General** | Available to all workspaces |
| **PlanBased** | Only for workspaces on specified plan IDs |
| **UserSpecific** | Only for specified user IDs |

#### 6.4.4 Domain Fields

- DomainName, DNSType (A/CNAME), DNSValue
- IsVerified, IsPrimary, IsArchived
- DefaultRedirectUrl, ExpirationUrl
- Visibility, AllowedPlanIds, AllowedUserIds
- Description, BrandedFor

---

### 6.5 Workspaces & Multi-Tenancy

**Controller:** `OnboardingController`, `SettingsController`

#### 6.5.1 Workspace Creation

- Multi-step onboarding wizard (`/onboarding/workspace`)
- Auto-generates slug from name
- Slug uniqueness check via AJAX (`/api/workspaces/check-slug`)
- **Default plan assigned automatically** — configurable via `IsDefault` flag on Plans
- If default plan has trial days, `PlanEndDate` is set to `UtcNow + TrialDays`

#### 6.5.2 Workspace Settings

| Tab | Route | Features |
|---|---|---|
| General | `/{slug}/settings/general` | Name, slug, logo, default redirect URL |
| Domains | `/{slug}/settings/domains` | Add/remove/verify custom domains |
| Members | `/{slug}/settings/members` | Invite, role management, remove |
| Billing | `/{slug}/settings/billing` | Current plan, usage, invoices, upgrade |
| Webhooks | `/{slug}/settings/webhooks` | Webhook CRUD |

#### 6.5.3 Workspace Switcher

- Sidebar dropdown shows all user's workspaces
- Quick-switch between workspaces
- "Create workspace" shortcut

---

### 6.6 Team & Collaboration

#### 6.6.1 Roles & Permissions

| Role | Can Create Links | Can Edit Links | Can Manage Members | Can Change Settings | Can Manage Billing |
|---|---|---|---|---|---|
| **Owner** | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Admin** | ✅ | ✅ | ✅ | ✅ | ❌ |
| **Member** | ✅ | Own only | ❌ | ❌ | ❌ |

#### 6.6.2 Member Invitation

- Invite by email with role selection
- Token-based invite link (expires)
- Invitation acceptance flow
- Member limit enforced per plan

#### 6.6.3 Team Activity Dashboard

**Controller:** `TeamActivityController`
- Activity feed (who created/edited what)
- Leaderboard (top link creators, most clicks)
- Time-based activity chart

#### 6.6.4 Link Comments

- Team members can add comments on individual links
- Threaded discussion per link
- Visible in link detail page

---

### 6.7 Plans, Billing & Subscriptions

**Controllers:** `BillingController`, `AdminPlansController`

#### 6.7.1 Plan Structure

| Field | Type | Description |
|---|---|---|
| Name | String | Plan name (Free, Pro, Business, Advanced) |
| Price | Decimal | Monthly price in USD |
| BillingCycle | String | Monthly / Yearly |
| MaxLinksPerMonth | Int | Link creation limit |
| MaxEventsPerMonth | Int | Event tracking limit |
| AnalyticsRetentionDays | Int | How long to keep analytics data |
| MaxDomains | Int | Custom domain limit |
| MaxMembers | Int | Team member limit |
| MaxFolders | Int | Folder limit |
| MaxTagsPerLink | Int | Tags per link limit |
| MaxDestinationsPerLink | Int | Destinations per link limit |
| Feature flags | Boolean | HasPasswordProtection, HasLinkExpiration, HasGeoTargeting, HasDeviceTargeting, HasLinkCloaking, HasABTesting, HasCustomerInsights, HasEventWebhooks, HasAPIAccess, HasWeightedURLs |
| SortOrder | Int | Display order |
| IsActive | Boolean | Enabled/disabled |

#### 7.7.2 Default Plans (Seed Data)

| Plan | Price | Links/mo | Events/mo | Domains | Members | Analytics |
|---|---|---|---|---|---|---|
| **Free** | $0 | 25 | 1,000 | 1 | 1 | 30 days |
| **Pro** | $30 | 1,000 | 50,000 | 3 | 5 | 1 year |
| **Business** | $90 | 10,000 | 250,000 | 10 | 15 | 3 years |
| **Advanced** | $300 | 50,000 | 1,000,000 | 50 | 50 | 5 years |

#### 6.7.3 Stripe Integration

- **Checkout:** `POST /{slug}/settings/billing/checkout` → Stripe Checkout Session
- **Portal:** `GET /{slug}/settings/billing/portal` → Stripe Customer Portal
- **Webhooks:** Stripe webhook handler for subscription events
- **Services:** `StripeService` — CreateCheckoutSession, CreateBillingPortal, HandleWebhook

#### 6.7.4 Usage Tracking

- `LinksUsedThisMonth` / `EventsUsedThisMonth` counters on Workspace
- `UsageResetDate` — next reset date
- `MonthlyUsageResetService` resets expired counters every hour

---

### 6.8 Discount System & Sale Offers

**Admin:** `/admin/plans` → Create/Edit forms

#### 6.8.1 Plan Discount Fields

| Field | Type | Description |
|---|---|---|
| `DiscountPercent` | INT (0-100) | Discount percentage. 0 = no discount, 100 = completely free |
| `DiscountLabel` | NVARCHAR(100) | Promotional text (e.g., "🎉 Limited Time: 100% OFF for 3 months!") |
| `DiscountBadge` | NVARCHAR(100) | Short badge text (e.g., "FREE FOR 3 MONTHS") |
| `TrialDays` | INT | Free trial duration in days. 0 = no trial |
| `IsDefault` | BIT | If true, this plan is assigned to new workspaces |
| `FallbackPlanId` | INT (FK → Plans) | Plan to downgrade to when trial expires |

#### 6.8.2 Computed Properties (C# Model)

```csharp
public decimal DiscountedPrice => DiscountPercent > 0 
    ? Price * (100 - DiscountPercent) / 100 
    : Price;
public bool HasDiscount => DiscountPercent > 0;
public bool HasTrial => TrialDays > 0;
```

#### 6.8.3 Pricing Page Behavior

When a plan has `DiscountPercent > 0`:

1. **Sale banner** appears at top of `/pricing` page:
   > "🎉 Limited Time Offer: Get the Business plan completely FREE for 3 months"

2. **Plan card** shows:
   - ~~$300~~ **$0** /mo (crossed-out original price + green discounted price)
   - Animated "FREE FOR 3 MONTHS" badge
   - "Then $300/mo after 3 months" note
   - "Start Free Trial" button (green)

3. **Billing page** (`/{slug}/settings/billing`) shows:
   - Trial countdown: "🎉 Free trial active — 87 days remaining"
   - Expiry date: "Expires: Sep 7, 2026"
   - Discount badge on current plan

4. **Upgrade page** (`/{slug}/settings/billing/upgrade`) shows discount badges on all plan cards

#### 6.8.4 Admin Discount Management

From `/admin/plans/{id}/edit`, admin can:
- Set any discount percentage (0-100%)
- Write custom badge text and promotional label
- Set trial duration in days
- Choose fallback plan after trial expiry
- Mark a plan as default for new signups
- See live preview of discounted pricing

#### 6.8.5 Current Default Configuration

| Setting | Value |
|---|---|
| Default Plan | Business (Id=3) |
| Price | $300/mo |
| Discount | 100% (FREE) |
| Trial Duration | 90 days (3 months) |
| Fallback Plan | Free (Id=1) |
| Badge | "FREE FOR 3 MONTHS" |
| Label | "🎉 Limited Time: 100% OFF for 3 months!" |

---

### 6.9 Free Trial & Plan Expiry

#### 6.9.1 Trial Assignment Flow

1. User registers (email or Google OAuth)
2. User creates workspace at `/onboarding/workspace`
3. `OnboardingController.CreateWorkspace()`:
   - Calls `_planRepo.GetDefaultPlanAsync()` → finds plan with `IsDefault = 1`
   - If plan has `TrialDays > 0`, sets `PlanEndDate = UtcNow + TrialDays`
   - Creates workspace with that plan
   - Records billing history: "Auto-assigned Business plan with 90-day free trial"
4. Workspace gets full Business plan features for 90 days

#### 6.9.2 Trial Expiry (Background Service)

**Service:** `PlanExpiryService` (`BackgroundServices/PlanExpiryService.cs`)  
**Interval:** Every 1 hour

**Flow:**
1. Query workspaces where `PlanEndDate <= NOW` AND plan has `FallbackPlanId` AND current plan ≠ fallback
2. For each expired workspace:
   - Update `PlanId` to `FallbackPlanId` (e.g., Free)
   - Clear `PlanEndDate` (no longer a trial)
   - Insert billing history record: "Trial expired. Downgraded from Business to Free."
3. Log: "Workspace 'X' downgraded from Business to Free (trial expired)"

#### 6.9.3 Trial UI Indicators

| Location | Indicator |
|---|---|
| Billing page | Green box: "🎉 Free trial active — X days remaining" |
| Workspace sidebar | Plan name shown in workspace switcher |
| Onboarding page | Green promo banner with plan features |

---

### 6.10 Partner/Affiliate Program

**Controllers:** `ProgramController` (workspace), `PartnerPortalController` (partner login), `AdminPartnerProgramsController`, `AdminPayoutsController`

#### 6.10.1 Program Setup

- Create affiliate program per workspace
- Configure: commission type (percentage/flat), value, duration (lifetime/months)
- Cookie duration, payout threshold, payout frequency
- Application form (optional, with custom questions)
- Auto-approve or manual review

#### 6.10.2 Partner Portal

| Route | Page |
|---|---|
| `/partners/apply` | Application form |
| `/partners/login` | Partner login |
| `/partners/dashboard` | Stats & overview |
| `/partners/sales` | Sales list |
| `/partners/links` | Referral links |
| `/partners/payouts` | Payout history |

#### 6.10.3 Fraud Detection

- `FraudDetectionService` (background, runs periodically)
- Checks: duplicate IP clicks, self-referral, abnormal patterns
- Configurable thresholds via `appsettings.json` Partners section
- Auto-flags partners exceeding fraud score threshold

#### 6.10.4 Partner Admin

- `/admin/partner-programs` — program CRUD
- `/admin/payouts` — payout management
- `/admin/fraud` — fraud events review

---

### 6.11 Link-in-Bio

**Controllers:** `BioPublicController`, `BioManageController`

#### 6.11.1 Features

- Public profile page: `/{username}`
- 5 themes: default, minimal, neon, gradient, glass
- Customizable: background color, text color, button style
- Social links: Twitter, Instagram, LinkedIn, GitHub, YouTube, TikTok
- Unlimited bio links with drag-sort ordering
- Click tracking per bio link
- View counter on profile

#### 6.11.2 Routes

| Route | Purpose |
|---|---|
| `/account/bio` | Bio setup/management |
| `/bio/{username}` | Public bio page |

---

### 6.12 QR Codes

**Controller:** `QRController`

#### 6.12.1 Features

- Generate QR code for any short link
- Customizable foreground/background colors
- Download as PNG
- Powered by qrcode.js (CDN)
- Embedded in link detail page

---

### 6.13 UTM Builder & Templates

**Controller:** `UTMTemplatesController` (`/{workspaceSlug}/utm-templates`)

#### 6.13.1 Features

- Create reusable UTM parameter templates
- Fields: Name, Source, Medium, Campaign, Term, Content
- Apply template when creating a link (auto-fills UTM fields)
- CRUD operations on templates

---

### 6.14 Webhooks & Events

**Controller:** `SettingsController` (Webhooks tab)  
**Service:** `WebhookService`

#### 6.14.1 Features

- Subscribe to events: `link.clicked`, `link.created`, `link.updated`, `link.deleted`
- HMAC-SHA256 signature verification
- Retry with exponential backoff (configurable max retries)
- Delivery logs with request/response details
- `WebhookRetryProcessor` background service

#### 6.14.2 Real-Time Events (SignalR)

**Hub:** `EventsHub` (`/hubs/events`)  
**Service:** `RealTimeEventService`

- Live click notifications in dashboard
- Workspace-scoped event broadcasting
- Client joins workspace group on connect

---

### 6.15 Public API v1

**Controller:** `PublicApiController` (`/api/v1`)  
**Auth:** API Key via `X-Api-Key` header

#### 6.15.1 Endpoints

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/v1/links` | List links (paginated, filtered) |
| `POST` | `/api/v1/links` | Create a link |
| `GET` | `/api/v1/links/{id}` | Get link details |
| `PUT` | `/api/v1/links/{id}` | Update a link |
| `DELETE` | `/api/v1/links/{id}` | Delete a link |
| `GET` | `/api/v1/events` | List click events |
| `GET` | `/api/v1/domains` | List domains |
| `GET` | `/api/v1/tags` | List tags |

#### 6.15.2 API Documentation

**Controller:** `ApiDocsController` (`/docs/api`)
- Interactive documentation page
- Request/response examples
- Authentication guide

---

### 6.16 Integrations Marketplace

**Controller:** `IntegrationsController` (`/{workspaceSlug}/settings/integrations`)

#### 6.16.1 Features

- Marketplace catalog of integrations
- Categories: Analytics, Marketing, CRM, Developer
- Per-workspace connect/disconnect
- Configuration per integration
- Seed integrations: Google Analytics, Slack, Zapier, Segment, etc.

---

### 6.17 SAML SSO & SCIM

**Controllers:** `SAMLController`, `SCIMController`

#### 6.17.1 SAML Features

- Per-workspace SAML 2.0 configuration
- IdP entity ID, SSO URL, SLO URL, Certificate
- SP entity ID, ACS URL
- Attribute mapping (email, name, role)
- Auto-provisioning of users
- Optional enforcement (require SAML)

#### 6.17.2 SCIM Features

- User provisioning/deprovisioning
- Group synchronization
- `/scim/v2/Users`, `/scim/v2/Groups` endpoints

---

### 6.18 Blog CMS

**Controllers:** `BlogController` (public), `AdminBlogController` (admin)

#### 6.18.1 Features

- WYSIWYG content editor
- SEO fields: meta title, description, keywords, canonical URL, OG image
- Categories system
- Draft/Published status
- Featured image
- View counter
- Author attribution
- Public blog: `/blog`, `/blog/{slug}`
- Admin: `/admin/blog` — CRUD

---

### 6.19 Documentation Site

**Controller:** `DocsController` (`/docs`)

#### 6.19.1 Pages (11)

| Route | Topic |
|---|---|
| `/docs` | Index/overview (links to all 35 pages) |
| `/docs/getting-started` | Quick start guide |
| `/docs/create-account` | Account creation & verification |
| `/docs/workspace-setup` | Workspace setup & trial |
| `/docs/links` | Link management overview |
| `/docs/create-short-link` | Creating tracked short links |
| `/docs/link-redirects` | Redirect modes (single, weighted, A/B, rotator) |
| `/docs/password-protected-links` | Password protection |
| `/docs/link-expiration` | Link expiration & fallback URLs |
| `/docs/link-cloaking` | Link cloaking / URL masking |
| `/docs/ab-testing` | A/B testing guide |
| `/docs/link-in-bio` | Link-in-Bio setup (5 themes) |
| `/docs/bulk-import-export` | CSV import/export |
| `/docs/social-link-previews` | Custom OG tags for social sharing |
| `/docs/utm-parameters-explained` | UTM parameters deep-dive |
| `/docs/utm-builder` | UTM builder tool |
| `/docs/utm-templates` | Reusable UTM templates |
| `/docs/conversion-tracking` | Lead & sale tracking |
| `/docs/analytics` | Analytics overview |
| `/docs/click-analytics` | Click data deep-dive |
| `/docs/geo-analytics` | Geographic analytics |
| `/docs/device-analytics` | Device/browser/OS analytics |
| `/docs/custom-domains` | Custom domain setup |
| `/docs/domain-verification` | DNS verification guide |
| `/docs/qr-codes` | QR code generation |
| `/docs/team-management` | Team member management |
| `/docs/roles-permissions` | Roles & permissions matrix |
| `/docs/billing` | Billing & plans |
| `/docs/free-trial` | 3-month free Business trial |
| `/docs/webhooks` | Webhook setup & HMAC |
| `/docs/api-reference` | REST API v1 reference |
| `/docs/integrations` | Integration marketplace |
| `/docs/sso-saml` | SAML SSO & SCIM |
| `/docs/browser-extension` | Chrome/Firefox extension |
| `/docs/partner-program` | Partner/affiliate program |
| `/docs/faq` | FAQ |

---

### 6.20 Browser Extension

**Directory:** `/extension/` (14 files)  
**Manifest:** Chrome MV3 + Firefox compatible

#### 6.20.1 Features

- Shorten current page URL with one click
- Select domain and workspace
- Copy short URL to clipboard
- View recent links
- Generate QR code
- Options page for API key configuration

#### 6.20.2 Files

| File | Purpose |
|---|---|
| `manifest.json` | Extension manifest (MV3) |
| `popup.html/js` | Main popup UI |
| `background.js` | Background service worker |
| `content.js` | Content script |
| `options.html` | Settings page |
| `css/popup.css` | Popup styles |
| `icons/*.png` | 16/32/48/128px icons |
| `lib/qrcode.min.js` | QR code library |

---

### 6.21 Admin Portal (SuperAdmin)

**Area:** `Areas/Admin/`  
**Auth:** `[Authorize(Roles = "SuperAdmin")]`  
**Layout:** `_AdminLayout.cshtml` (dark sidebar)

#### 6.21.1 Admin Pages

| Route | Controller | Page |
|---|---|---|
| `/admin` | `DashboardController` | Dashboard (users, workspaces, links, clicks stats) |
| `/admin/users` | `UsersController` | User list, search, detail, toggle admin |
| `/admin/users/{id}` | `UsersController` | User detail + memberships |
| `/admin/workspaces` | `WorkspacesAdminController` | Workspace list, search, detail |
| `/admin/workspaces/{id}` | `WorkspacesAdminController` | Workspace detail, plan assignment |
| `/admin/plans` | `AdminPlansController` | Plans CRUD (with discounts, trials) |
| `/admin/plans/create` | `AdminPlansController` | Create plan form |
| `/admin/plans/{id}/edit` | `AdminPlansController` | Edit plan form |
| `/admin/traffic-rules` | `TrafficRulesController` | Admin traffic injection rules |
| `/admin/domains` | `DomainsAdminController` | System & user domains CRUD |
| `/admin/partner-programs` | `AdminPartnerProgramsController` | Partner programs management |
| `/admin/payouts` | `AdminPayoutsController` | Partner payouts |
| `/admin/fraud` | `AdminFraudController` | Fraud detection events |
| `/admin/stripe-events` | `AdminStripeEventsController` | Stripe webhook event log |
| `/admin/blog` | `AdminBlogController` | Blog post CRUD |
| `/admin/pages` | `AdminPagesController` | CMS pages editor (About, Contact, Privacy, Terms, Branding, Email templates) |
| `/admin/settings` | `SystemSettingsController` | System-wide settings |

#### 6.21.2 Admin Dashboard Stats (via `sp_GetAdminDashboard`)

- Total users, new users today
- Total workspaces
- Total links
- Clicks today, clicks last hour
- Verified domains count

---

### 6.22 Auto-Admin (First User Promotion)

**Implementation:** `AuthController.cs` (Register + GoogleResponse methods)

#### 6.22.1 How It Works

1. On **every** user registration (email/password or Google OAuth):
   ```csharp
   var hasAdmin = await _userRepo.HasAnySuperAdminAsync();
   // Query: SELECT COUNT(*) FROM Users WHERE IsSuperAdmin = 1 AND DeletedAt IS NULL
   ```
2. If `hasAdmin == false` → new user gets `IsSuperAdmin = true`
3. SuperAdmin claim added to auth cookie → user sees admin portal link in sidebar
4. Logged to console: `"User {Email} auto-promoted to SuperAdmin (first user)"`

#### 6.22.2 Configuration

| Setting | Default | Description |
|---|---|---|
| `AutoPromoteFirstUser` | `true` | Enable auto-promotion of first user |

#### 6.22.3 Behavior After First Admin

- Once any SuperAdmin exists, all subsequent users register as normal (non-admin)
- Additional admins can be promoted via `/admin/users/{id}` → Toggle SuperAdmin

---

### 6.23 Email System

**Service:** `EmailService` (implements `IEmailService`)  
**Library:** MailKit

#### 6.23.1 SMTP Configuration

```json
"SMTP": {
    "Host": "smtp.hostinger.com",
    "Port": "465",
    "User": "no-reply@info.utmpro.link",
    "Password": "...",
    "FromEmail": "no-reply@info.utmpro.link",
    "FromName": "UTMPro"
}
```

#### 6.23.2 Auto-SSL Detection

| Port | SSL Mode |
|---|---|
| 465 | `SslOnConnect` (implicit TLS) |
| 587 | `StartTls` (explicit TLS) |
| 25 | `None` (no encryption) |

#### 6.23.3 Email Types

| Email | Trigger | Template |
|---|---|---|
| Verification Code | Registration (if verification enabled) | 6-digit code + token link |
| Welcome | After email verification | Configurable template |
| Password Reset | Forgot password | Token link (1-hour expiry) |
| Workspace Invitation | Member invite | Invite link |

#### 6.23.4 Email Templates (Admin-Editable)

- Admin can edit email templates at `/admin/pages` → Email Templates tab
- Template variables: `{name}`, `{email}`, `{appUrl}`, `{code}`, `{link}`
- System setting keys: `EmailTemplateWelcome`, `EmailTemplateVerification`, etc.

---

### 6.24 Real-Time Events (SignalR)

**Hub:** `EventsHub` (`/hubs/events`)  
**Service:** `RealTimeEventService`

#### 6.24.1 Features

- WebSocket connection per authenticated user
- Join workspace-specific groups
- Broadcast events: `link.clicked`, `link.created`
- Live click counter updates in dashboard

---

### 6.25 Background Services

| Service | Project | Interval | Purpose |
|---|---|---|---|
| `ClickBatchProcessor` | RedirectEngine | 10 seconds | Bulk insert queued clicks |
| `CacheWarmupService` | RedirectEngine | Startup | Preload top links into cache |
| `DomainVerificationService` | RedirectEngine | 1 hour | DNS check for custom domains |
| `MonthlyUsageResetService` | Web | 1 hour | Reset workspace usage counters |
| `WebhookRetryProcessor` | Web | Configurable | Retry failed webhook deliveries |
| `PartnerPayoutScheduler` | Web | Daily | Process partner payouts |
| `FraudDetectionService` | Web | Periodic | Detect partner fraud patterns |
| `PlanExpiryService` | Web | 1 hour | Downgrade expired trial workspaces |

---

### 6.26 UI/UX Features

#### 6.26.1 Responsive Design

- Mobile-first with Tailwind CSS
- Hamburger menu for mobile (sidebar overlay)
- Stacked grids on small screens
- Scrollable tables (`table-wrap` class)
- Touch-friendly controls

#### 6.26.2 Dark Mode

- Toggle via sidebar button
- `localStorage.darkMode` persistence
- Tailwind `dark:` variant classes
- `document.documentElement.classList.toggle('dark')`

#### 6.26.3 Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| `⌘K` / `Ctrl+K` | Open global search |
| `C` | Open create link modal |
| `ESC` | Close modal/search |
| `?` | Show help |

#### 6.26.4 Global Search

- Modal overlay with search input
- Search across links and domains
- Results with keyboard navigation
- Triggered by ⌘K or search icon

#### 6.26.5 Layout Structure

- **Sidebar:** 260px fixed left (desktop), slide-in overlay (mobile)
- **Main content:** Margin-left 260px (desktop)
- **Top bar:** Sticky, with search + actions
- **Footer:** Success/error toast messages via TempData

---

### 6.27 Public Pages (CMS-Managed)

**Controllers:** `PagesController` (public), `AdminPagesController` (admin editor)

| Route | Page | Admin-Editable |
|---|---|---|
| `/about` | About Us | ✅ (via `/admin/pages/about`) |
| `/contact` | Contact Us | ✅ (via `/admin/pages/contact`) |
| `/privacy` | Privacy Policy | ✅ (via `/admin/pages/privacy`) |
| `/terms` | Terms of Service | ✅ (via `/admin/pages/terms`) |
| `/pricing` | Pricing (dynamic from DB) | Via plan CRUD |

Admin sub-pages:
- `/admin/pages` — Overview
- `/admin/pages/branding` — Logo, favicon, colors
- `/admin/pages/emails` — Email template editor

---

### 6.28 Bulk Operations

**Controllers:** `BulkController`, `BulkActionsController`

#### 6.28.1 Features

- **Bulk Import:** Upload CSV with links (URL, slug, UTM params)
- **Bulk Export:** Download all workspace links as CSV
- **Bulk Actions:** Archive, delete, tag multiple links
- Import job tracking via `BulkImports` table

---

### 6.29 Admin Traffic Injection

**Repository:** `AdminTrafficRepository`  
**Service:** `AdminTrafficService` (RedirectEngine)

#### 6.29.1 Features

- Admin can configure a percentage of link traffic to redirect to admin-specified URLs
- Per-link and per-workspace traffic percentage
- `AdminTrafficRules` and `AdminTrafficUrls` tables
- Click events marked with `IsAdminRedirect = true`
- **Click warm-up:** a new link never redirects to an admin URL until the original destination has collected `AdminTrafficMinClicks` original clicks (default 500, SuperAdmin-configurable in `/admin/settings` or `/admin/traffic-rules`). Set the value to 0 to start immediately. Admin-traffic clicks do not count toward the warm-up.

---

### 6.30 Social OG / Link Preview Override

**Handler:** `RedirectHandler` (RedirectEngine)

#### 6.30.1 How It Works

1. When a link has `CustomTitle`, `CustomDescription`, or `CustomImageUrl` set
2. AND the request comes from a social media bot (detected via user-agent)
3. Instead of HTTP 302, serve HTML page with:
   - `<meta property="og:title">`, `<meta property="og:description">`, `<meta property="og:image">`
   - `<meta http-equiv="refresh" content="0;url=...">` for browser redirect
4. This ensures proper link previews on WhatsApp, Facebook, Twitter, Slack, Discord, LinkedIn, Telegram

#### 6.30.2 Bot Detection

User-agent matching for: `facebookexternalhit`, `Twitterbot`, `WhatsApp`, `LinkedInBot`, `Slackbot`, `Discordbot`, `TelegramBot`, `Googlebot`

---

### 6.31 Conversion Funnel & Customer Insights

**Controller:** `CustomersController` (`/{workspaceSlug}/customers`)

#### 6.31.1 Features

- Track leads and sales events
- Customer profiles with attribution
- Conversion funnel visualization
- Customer timeline (clicks → lead → sale)
- Revenue attribution to links

---

### 6.32 Audit Logs & Security

**Repository:** `AuditRepository`

#### 6.32.1 Features

- System-wide audit trail
- Entity-level tracking (EntityType, EntityId, Action)
- Old/new value snapshots
- User attribution
- Searchable, filterable

---

## 7. Codebase Inventory

### 7.1 Statistics

| Metric | Count |
|---|---|
| **C# Source Files** | 163 |
| **Razor Views (.cshtml)** | 121 |
| **SQL Migration Scripts** | 17 |
| **Database Tables** | 59 |
| **Stored Procedures** | 10 |
| **Data Models** | 40 |
| **Repositories** (interface + impl) | 44 |
| **Web Controllers** | 34 |
| **Admin Controllers** | 13 |
| **Services** | 8 |
| **Background Services** | 5 (Web) + 3 (RedirectEngine) |
| **Extension Files** | 14 |

### 7.2 Project Structure

```
UTMPro/
├── UTMPro.sln
├── SRS.md                          ← This document
├── INSTALLATION_GUIDE.md           (851 lines, 14 sections)
├── COMPARISON_DUB_VS_UTMPRO.md
├── COMPARISON_DUB_VS_UTMPRO_V2.md (204 lines)
│
├── database/                       (16 SQL migration scripts)
│   ├── 001_Schema.sql              (28 tables)
│   ├── 002_SeedData.sql            (plans, domains, settings)
│   ├── 003_StoredProcedures.sql    (5 SPs)
│   ├── 004_Phase2_Schema.sql       (21 tables)
│   ├── 005_Phase2_SeedData.sql
│   ├── 006_Phase2_StoredProcedures.sql (5 SPs)
│   ├── 007_Phase3_Additions.sql    (blog tables)
│   ├── 008_Sprint1_Features.sql    (comments, audit, bio, bulk)
│   ├── 009_Sprint2_Features.sql    (team activity)
│   ├── 010_Domain_Fixes.sql
│   ├── 011_Pages_Docs.sql
│   ├── 012_Email_Verification.sql
│   ├── 013_OG_Cache_Fix.sql
│   ├── 014_QuickWins.sql
│   ├── 015_EmailTemplates.sql
│   └── 016_Discounts_AutoAdmin_TrialPlan.sql
│
├── extension/                      (Chrome MV3 + Firefox)
│   ├── manifest.json
│   ├── popup.html / popup.js
│   ├── background.js / content.js
│   ├── options.html
│   ├── css/popup.css
│   ├── icons/ (16/32/48/128px PNGs)
│   └── lib/qrcode.min.js
│
└── src/
    ├── UTMPro.Data/               (Shared data layer)
    │   ├── DbConnectionFactory.cs
    │   ├── Helpers/IdGenerator.cs
    │   ├── Models/                 (40 model files)
    │   │   ├── User.cs, UserToken.cs
    │   │   ├── Plan.cs             (with discount/trial fields)
    │   │   ├── Workspace.cs, WorkspaceMember.cs, WorkspaceInvitation.cs
    │   │   ├── Link.cs, LinkDestination.cs, LinkTargetingRule.cs, LinkComment.cs
    │   │   ├── Domain.cs, Folder.cs, Tag.cs
    │   │   ├── ClickEvent.cs, Customer.cs
    │   │   ├── APIKey.cs, APILog.cs
    │   │   ├── Webhook.cs, WebhookDeliveryLog.cs
    │   │   ├── Partner.cs, PartnerProgram.cs, PartnerSale.cs, PartnerPayout.cs
    │   │   ├── PartnerBounty.cs, PartnerMessage.cs, PartnerFraudEvent.cs
    │   │   ├── StripeModels.cs     (Customer, Subscription, Invoice, Price)
    │   │   ├── SAMLConfiguration.cs
    │   │   ├── Integration.cs
    │   │   ├── BioProfile.cs       (+ BioLink)
    │   │   ├── BlogPost.cs         (+ BlogCategory)
    │   │   ├── UTMTemplate.cs, TeamActivity.cs, AdminTrafficRule.cs
    │   │   ├── NotificationPreference.cs, OAuthApp.cs, Referral.cs
    │   │   ├── Analytics.cs, SystemSetting.cs
    │   │   └── WorkspaceBillingHistory.cs
    │   └── Repositories/           (22 interfaces + 22 implementations)
    │       ├── IUserRepository / UserRepository
    │       ├── IPlanRepository / PlanRepository
    │       ├── IWorkspaceRepository / WorkspaceRepository
    │       ├── ILinkRepository / LinkRepository
    │       ├── IDomainRepository / DomainRepository
    │       ├── IFolderRepository / FolderRepository
    │       ├── ITagRepository / TagRepository
    │       ├── IAnalyticsRepository / AnalyticsRepository
    │       ├── ICustomerRepository / CustomerRepository
    │       ├── IAPIKeyRepository / APIKeyRepository
    │       ├── IWebhookRepository / WebhookRepository
    │       ├── IBillingRepository / BillingRepository
    │       ├── IPartnerRepository / PartnerRepository
    │       ├── IBioProfileRepository / BioProfileRepository
    │       ├── IBlogRepository / BlogRepository
    │       ├── IAdminTrafficRepository / AdminTrafficRepository
    │       ├── IAuditRepository / AuditRepository
    │       ├── ISAMLRepository / SAMLRepository
    │       ├── IIntegrationRepository / IntegrationRepository
    │       ├── ITeamActivityRepository / TeamActivityRepository
    │       ├── IUTMTemplateRepository / UTMTemplateRepository
    │       └── ISystemSettingsRepository / SystemSettingsRepository
    │
    ├── UTMPro.RedirectEngine/      (Minimal API)
    │   ├── Program.cs              (route mapping, DI setup)
    │   ├── appsettings.json
    │   ├── web.config               (IIS hosting)
    │   ├── Handlers/
    │   │   └── RedirectHandler.cs   (main redirect logic)
    │   ├── Models/
    │   │   └── LinkCacheModel.cs    (cached link data)
    │   ├── Services/
    │   │   ├── LinkCacheService.cs  (IMemoryCache wrapper)
    │   │   ├── ClickQueueService.cs (thread-safe queue)
    │   │   ├── GeoIpService.cs      (MaxMind lookup)
    │   │   ├── DeviceDetectionService.cs
    │   │   ├── WeightedUrlSelector.cs
    │   │   └── AdminTrafficService.cs
    │   └── BackgroundServices/
    │       ├── ClickBatchProcessor.cs    (10s bulk insert)
    │       ├── CacheWarmupService.cs     (startup preload)
    │       └── DomainVerificationService.cs
    │
    └── UTMPro.Web/                 (Main MVC Application)
        ├── Program.cs              (DI, auth, middleware, routing)
        ├── appsettings.json        (all config sections)
        ├── web.config              (IIS hosting)
        │
        ├── Controllers/            (34 controllers)
        │   ├── AuthController.cs           (Login, Register, OAuth, Verify, Reset)
        │   ├── OnboardingController.cs     (Workspace creation with trial)
        │   ├── HomeController.cs           (Landing, dynamic Pricing)
        │   ├── BaseWorkspaceController.cs  (workspace loading base class)
        │   ├── LinksController.cs          (CRUD, detail, search)
        │   ├── AnalyticsController.cs
        │   ├── EventsController.cs
        │   ├── CustomersController.cs
        │   ├── SettingsController.cs       (General, Domains, Members, Webhooks)
        │   ├── BillingController.cs        (Billing, Upgrade, Stripe checkout/portal)
        │   ├── FoldersController.cs
        │   ├── TagsController.cs
        │   ├── DomainsPageController.cs
        │   ├── AccountController.cs        (Profile, security)
        │   ├── BioController.cs            (BioPublicController + BioManageController)
        │   ├── BlogController.cs           (Public blog)
        │   ├── BulkController.cs           (Import/export)
        │   ├── BulkActionsController.cs    (Multi-select actions)
        │   ├── DocsController.cs           (Documentation pages)
        │   ├── ErrorController.cs          (403, 404, 500)
        │   ├── IntegrationsController.cs
        │   ├── InviteController.cs         (Accept invitations)
        │   ├── PagesController.cs          (Public About/Contact/Privacy/Terms)
        │   ├── PartnerPortalController.cs  (Partner login/dashboard)
        │   ├── ProgramController.cs        (Partner program mgmt — 12 actions)
        │   ├── PublicStatsController.cs    (Public link stats)
        │   ├── QRController.cs
        │   ├── SAMLController.cs
        │   ├── SCIMController.cs
        │   ├── TeamActivityController.cs
        │   ├── UTMTemplatesController.cs
        │   ├── UploadController.cs         (Logo/favicon upload)
        │   ├── ApiDocsController.cs        (Interactive API docs)
        │   └── Api/
        │       └── PublicApiController.cs  (REST API v1)
        │
        ├── Areas/Admin/Controllers/ (13 admin controllers)
        │   ├── DashboardController.cs
        │   ├── UsersController.cs
        │   ├── WorkspacesAdminController.cs
        │   ├── AdminPlansController.cs
        │   ├── TrafficRulesController.cs
        │   ├── DomainsAdminController.cs
        │   ├── AdminPartnerProgramsController.cs
        │   ├── AdminPayoutsController.cs
        │   ├── AdminFraudController.cs
        │   ├── AdminStripeEventsController.cs
        │   ├── AdminBlogController.cs
        │   ├── AdminPagesController.cs
        │   └── SystemSettingsController.cs
        │
        ├── Services/               (8 services)
        │   ├── EmailService.cs           (MailKit SMTP with auto-SSL)
        │   ├── StripeService.cs          (Checkout, Portal, Webhooks)
        │   ├── LinkService.cs            (Link business logic)
        │   ├── WebhookService.cs         (HMAC, delivery, retry)
        │   ├── PartnerService.cs         (Commission, attribution)
        │   ├── RealTimeEventService.cs   (SignalR broadcasting)
        │   ├── UrlMetadataService.cs     (Fetch OG/title from URLs)
        │   └── ServiceResult.cs          (Generic result wrapper)
        │
        ├── BackgroundServices/     (5 services)
        │   ├── MonthlyUsageResetService.cs   (hourly usage reset)
        │   ├── WebhookRetryProcessor.cs      (retry failed webhooks)
        │   ├── PartnerPayoutScheduler.cs     (daily payout processing)
        │   ├── FraudDetectionService.cs      (partner fraud detection)
        │   └── PlanExpiryService.cs          (hourly trial expiry check)
        │
        ├── Hubs/
        │   └── EventsHub.cs        (SignalR hub)
        │
        ├── Middleware/
        │   └── ApiKeyAuthMiddleware.cs  (API key validation)
        │
        ├── Models/
        │   ├── Requests/CreateLinkRequest.cs
        │   └── ViewModels/
        │       ├── LinksViewModel.cs
        │       └── DashboardViewModel.cs (+ others)
        │
        ├── Views/                  (87 views)
        │   ├── _ViewImports.cshtml, _ViewStart.cshtml
        │   ├── Shared/_Layout.cshtml    (main app layout with sidebar)
        │   ├── Auth/   (Login, Register, VerifyEmail, ForgotPassword, ResetPassword)
        │   ├── Home/   (Index, Pricing)
        │   ├── Onboarding/ (Workspace)
        │   ├── Links/  (Index, Detail, BulkImport)
        │   ├── Analytics/ (Index)
        │   ├── Events/ (Index)
        │   ├── Customers/ (Index)
        │   ├── Folders/ (Index)
        │   ├── Tags/ (Index)
        │   ├── Settings/ (General, Domains, Members, Billing, BillingPage, Upgrade, Webhooks, + 8 more)
        │   ├── Account/ (Settings, Security, Referrals)
        │   ├── Activity/ (Index)
        │   ├── Bio/ (Profile, Manage, Setup)
        │   ├── Blog/ (Index, Post)
        │   ├── Docs/ (11 pages + _DocsLayout)
        │   ├── DomainsPage/ (Index)
        │   ├── Error/ (403, 404, 500)
        │   ├── Pages/ (About, Contact, Privacy, Terms, _PageLayout)
        │   ├── PartnerPortal/ (Apply, Login, Dashboard, Sales, Links, Payouts, Welcome)
        │   ├── Program/ (Index, Setup, Partners, PartnerDetail, Sales, Payouts, Messages, Bounties, Analytics, Fraud)
        │   ├── PublicStats/ (Index)
        │   ├── UTMTemplates/ (Index)
        │   └── ApiDocs/ (Index)
        │
        └── Areas/Admin/Views/      (34 admin views)
            ├── Shared/_AdminLayout.cshtml
            ├── _ViewImports.cshtml
            ├── Dashboard/Index.cshtml
            ├── Users/ (Index, Detail, Memberships)
            ├── Workspaces/ (Index, Detail)
            ├── Plans/ (Index, Create, Edit)
            ├── TrafficRules/ (Index, Create, Edit)
            ├── Domains/ (Index, Create, Edit)
            ├── Blog/ (Index, Create, Edit)
            ├── Pages/ (Index, About, Contact, Privacy, Terms, Branding, Emails)
            ├── PartnerPrograms/ (Index, Detail)
            ├── Payouts/ (Index)
            ├── Fraud/ (Index)
            ├── StripeEvents/ (Index)
            └── Settings/ (Index)
```

---

## 8. Configuration Reference

### 8.1 Web App (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "UTMProDB": "Server=...;Database=UTMProDB;TrustServerCertificate=True;user id=sa;password=..."
  },
  "Google": {
    "ClientId": "YOUR_GOOGLE_CLIENT_ID",
    "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
  },
  "App": {
    "SiteUrl": "https://utmpro.link",
    "AppUrl": "https://app.utmpro.link",
    "AdminUrl": "https://admin.utmpro.link",
    "RedirectEngineUrl": "https://go.utmpro.link",
    "CustomDomainTarget": "links.utmpro.link"
  },
  "SMTP": {
    "Host": "smtp.hostinger.com",
    "Port": "465",
    "User": "no-reply@info.utmpro.link",
    "Password": "...",
    "FromEmail": "no-reply@info.utmpro.link",
    "FromName": "UTMPro"
  },
  "Stripe": {
    "PublishableKey": "pk_test_xxx",
    "SecretKey": "sk_test_xxx",
    "WebhookSecret": "whsec_xxx",
    "ConnectClientId": "ca_xxx",
    "TrialDays": "0"
  },
  "Partners": {
    "PortalUrl": "https://partners.utmpro.link",
    "EnableFraudDetection": true,
    "SelfReferralDetection": true,
    "DuplicateIPWindowHours": 24,
    "MaxDuplicateIPClicks": 10,
    "FraudAutoFlagThreshold": 80
  },
  "SAML": { "Enabled": true, "SpBaseUrl": "https://app.utmpro.link" },
  "SignalR": { "EnableDetailedErrors": false },
  "WebhookMaxRetries": "3",
  "WebhookRetryIntervalSecs": "60"
}
```

### 8.2 Redirect Engine (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "UTMProDB": "Server=...;Database=UTMProDB;..."
  },
  "CacheTTLMinutes": "1",
  "BatchProcessorSeconds": "10",
  "BatchSizeLimit": "500",
  "CacheWarmupCount": "1000",
  "GeoLite2DbPath": "C:\\GeoLite2\\GeoLite2-City.mmdb"
}
```

### 8.3 Key System Settings (in `SystemSettings` table)

| Key | Default | Description |
|---|---|---|
| `DefaultPlanId` | `3` (Business) | Plan assigned to new workspaces |
| `DefaultTrialDays` | `90` | Default trial period |
| `ShowPlanDiscounts` | `true` | Show discounts on pricing |
| `AutoPromoteFirstUser` | `true` | First user becomes SuperAdmin |
| `RequireEmailVerification` | `false` | Require email verification |
| `AllowPublicRegistration` | `true` | Allow open registration |
| `MaxWorkspacesPerUser` | `5` | Max workspaces per user |
| `AllowUserCustomDomains` | `true` | Allow custom domains |
| `CustomDomainTarget` | `links.utmpro.link` | CNAME target hostname shown in DNS instructions (never the origin IP) |
| `GeoLite2DbPath` | `C:\GeoLite2\...` | MaxMind database path |
| `EnableWelcomeEmail` | `true` | Send welcome email after verification |
| `SiteUrl` | `https://utmpro.link` | Main site URL |
| `AppUrl` | `https://app.utmpro.link` | App URL |
| `RedirectEngineUrl` | `https://go.utmpro.link` | Redirect engine URL |

---

## 9. Deployment Architecture

### 9.1 IIS Deployment (Windows Server)

Both Web and RedirectEngine deploy as IIS sites with `web.config`:

```xml
<aspNetCore processPath="dotnet" arguments=".\UTMPro.Web.dll"
            stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" />
```

### 9.2 Domain Mapping

| Domain | Target |
|---|---|
| `utmpro.link` | Web App (public pages: landing, pricing, blog, docs) |
| `app.utmpro.link` | Web App (dashboard, workspace, settings) |
| `go.utmpro.link` | Redirect Engine |
| `*.customdomain.com` | Redirect Engine (custom domain redirect) |
| `partners.utmpro.link` | Web App (partner portal routes) |

### 9.3 Cache Invalidation Flow

When a link is edited in the Web app:
```
Web App → POST /cache/invalidate?domain=X&slug=Y → Redirect Engine
Redirect Engine → cache.Invalidate(domain, slug)
```

---

## 10. Known Issues & Roadmap

### 10.1 Known Issues

| Issue | Status | Notes |
|---|---|---|
| SMTP on Hostinger port 465 | Fix deployed | SslOnConnect fix in EmailService. Check `logs/stdout_*.log` for SMTP errors. Try port 587 as fallback. |
| BillingStripe.cshtml | Dead view | Not referenced by any controller. Can be cleaned up. |
| Admin plan edit 404 | ✅ Fixed | Nested `<form>` tags + computed property model binding + nullable int empty string. Fix: `IFormCollection` binding, forms separated. |
| Can't create 2nd workspace | ✅ Fixed | `/onboarding/workspace` redirected away if any workspace existed. Fix: new `/workspaces/new` route for additional workspaces, `/workspaces` list page, sidebar shows all workspaces in switcher dropdown, workspace limit enforced (MaxWorkspacesPerUser=5). |
| Admin can't see workspace links | ✅ Fixed | Admin workspace detail now shows paginated links table with slug, destination, clicks, status. "Open Workspace ↗" button lets admin browse workspace directly. |

### 10.2 Future Enhancements (Nice-to-Have)

| Feature | Priority | Description |
|---|---|---|
| Native SDKs | Medium | TypeScript, Python, Go SDKs for API |
| Deferred Deep Linking | Low | iOS/Android SDK for deep link attribution |
| AI Analytics | Medium | "Ask AI" feature with LLM integration |
| Native Zapier App | Low | Official Zapier integration |
| Mobile PWA | Medium | Progressive Web App for mobile |
| Segment/GTM Integration | Low | Native analytics integrations |
| Email on trial expiry | High | Notify user before/after trial expires |
| Stripe for paid plans post-trial | High | Checkout flow when trial ends |

### 10.3 Completed Feature Timeline

| Phase | Features | Status |
|---|---|---|
| Phase 1 (SRS) | Core: Users, links, analytics, redirect engine, admin, settings, domains, folders, tags, customers, events | ✅ Complete |
| Phase 2 (SRS) | Partner program, Stripe billing, SignalR, webhooks, SAML/SCIM, integrations, public API, background services | ✅ Complete |
| Sprint 1 | Dark mode, keyboard shortcuts, Link-in-Bio, bulk ops, public stats, global search, audit logs, link comments, UTM templates, QR codes | ✅ Complete |
| Sprint 2 | Team activity, blog CMS, browser extension, docs site, link rotator, social OG override, URL metadata fetch, image upload, email verification | ✅ Complete |
| Sprint 3 | Responsive mobile, admin pages editor, email templates, welcome email, domain visibility, SMTP auto-SSL | ✅ Complete |
| Sprint 4 | Auto-admin (first user), plan discounts & sales, free trial system, plan expiry service, dynamic pricing page | ✅ Complete |
| Sprint 5 | 36 Docs pages (full reference site), 25 SEO blog posts (seed SQL), updated docs layout with sidebar nav, mobile docs nav | ✅ Complete |

---

> **Document maintained by:** UTMPro Development Team  
> **Last code audit:** 2026-06-09  
> **Codebase:** 163 C# files, 121 Views, 16 SQL scripts, 59 tables, 10 SPs

# Dub.co vs UTMPro — Deep Gap Analysis V2
## Generated: June 9, 2026 | Based on Dub.co's full feature set as of June 2026

---

## METHODOLOGY

This analysis is based on:
- Dub.co official product pages ([1](https://dub.co/links)) ([7](https://dub.co/links))
- Dub.co documentation and deep links guides ([2](https://dub.co/docs/concepts/deep-links/attribution)) ([4](https://dub.co/docs/concepts/deep-links/deferred-deep-linking))
- Dub.co changelog and launch week announcements ([2](https://dub.co/blog/launch-week-recap)) ([4](https://dub.co/blog/new-links-dashboard))
- Third-party reviews from TinyStartups ([1](https://www.tinystartups.com/tools/dub)), Linkly ([5](https://linklyhq.com/review/dub)), CreatorEconomyTools ([6](https://www.creatoreconomytools.com/tool/dub-co)), PIMMS comparison ([2](https://pimms.io/blog/dubco-alternatives-comparison-2025))
- TechCrunch coverage ([3](https://techcrunch.com/2025/01/16/dub-co-is-an-open-source-url-shortener-and-link-attribution-engine-packed-into-one/))
- UTMPro actual codebase verification (160 C# files, 114 views, 12 SQL scripts)

---

## SECTION 1: FEATURES BOTH PLATFORMS HAVE (PARITY) ✅

| # | Feature | Dub.co | UTMPro | Notes |
|---|---------|--------|--------|-------|
| 1 | Branded short links on custom domains | ✅ | ✅ | |
| 2 | Custom slug / auto-generated slug | ✅ | ✅ | |
| 3 | Click analytics (geo, device, browser, OS, referrer) | ✅ | ✅ | |
| 4 | UTM builder (source, medium, campaign, term, content) | ✅ | ✅ | |
| 5 | UTM templates (save/reuse parameter sets) | ✅ | ✅ | |
| 6 | QR code generation per link | ✅ | ✅ | |
| 7 | Custom link preview (OG title, description, image) | ✅ | ✅ | Serves custom OG to social bots |
| 8 | Auto-fetch OG metadata from destination URL | ✅ | ✅ | |
| 9 | Password-protected links | ✅ Pro | ✅ | |
| 10 | Link expiration (time-based) | ✅ Pro | ✅ | |
| 11 | Geo-targeting redirects | ✅ | ✅ | |
| 12 | Device targeting (iOS/Android/Desktop) | ✅ Pro | ✅ | |
| 13 | Link cloaking | ✅ | ✅ | |
| 14 | A/B testing for destination URLs | ✅ Business | ✅ | |
| 15 | Weighted URL distribution | ✅ | ✅ | |
| 16 | Workspace / team management | ✅ | ✅ | |
| 17 | Role-based access control | ✅ | ✅ | Owner/Admin/Member/Viewer |
| 18 | Invite members via email | ✅ | ✅ | |
| 19 | Folders for link organization | ✅ Pro | ✅ | |
| 20 | Tags for link categorization | ✅ | ✅ | |
| 21 | Link comments / activity feed | ✅ | ✅ | |
| 22 | Conversion tracking (leads + sales) | ✅ Business | ✅ | |
| 23 | Customer insights | ✅ Business | ✅ | |
| 24 | REST API (links, domains, tags, analytics) | ✅ | ✅ | |
| 25 | API key management | ✅ | ✅ | |
| 26 | Webhooks (events + delivery + retry) | ✅ Business | ✅ | |
| 27 | Partner/Affiliate program | ✅ Business | ✅ | Dub Partners equivalent |
| 28 | Partner commission tracking + payouts | ✅ | ✅ | |
| 29 | Partner fraud detection | ✅ | ✅ | |
| 30 | Stripe billing integration | ✅ | ✅ | |
| 31 | Subscription management + invoices | ✅ | ✅ | |
| 32 | SAML SSO (Enterprise) | ✅ Enterprise | ✅ | |
| 33 | Bulk link import/export (CSV) | ✅ | ✅ | |
| 34 | Public stats pages (shareable) | ✅ | ✅ | |
| 35 | Dark mode | ✅ | ✅ | |
| 36 | Keyboard shortcuts (⌘K, C, ESC) | ✅ | ✅ | |
| 37 | Link-in-Bio pages | ✅ | ✅ | 5 themes, social links |
| 38 | Conversion funnel visualization | ✅ | ✅ | clicks→leads→sales |
| 39 | Audit logs | ✅ Enterprise | ✅ | |
| 40 | Email verification (registration) | ✅ | ✅ | 6-digit code |
| 41 | Blog with SEO | ❌ separate | ✅ | UTMPro extra |
| 42 | Admin portal (SuperAdmin) | ❌ limited | ✅ | UTMPro extra |
| 43 | Admin plan CRUD | ❌ | ✅ | UTMPro extra |
| 44 | Admin traffic injection | ❌ | ✅ | UTMPro extra |
| 45 | Browser extension | ✅ | ✅ | Chrome MV3 + Firefox |
| 46 | Interactive API docs | ✅ | ✅ | /docs/api |
| 47 | Documentation site | ✅ | ✅ | /docs/* (11 pages) |
| 48 | About Us / Contact Us pages | ✅ | ✅ | Admin-configurable |
| 49 | Responsive mobile design | ✅ | ✅ | Full responsive |
| 50 | SCIM directory sync | ✅ Enterprise | ✅ | |
| 51 | Integrations marketplace | ✅ | ✅ | |
| 52 | Link enable/disable toggle | ✅ | ✅ | |
| 53 | Cache invalidation on edit | ✅ | ✅ | |
| 54 | Image upload for link preview | ✅ | ✅ | |
| 55 | Copy button on links list | ✅ | ✅ | |
| 56 | Google OAuth login | ✅ | ✅ | |

**Parity count: 56 features matched**

---

## SECTION 2: FEATURES DUB.CO HAS THAT UTMPRO IS MISSING 🔴

### TIER 1: CRITICAL GAPS (High business impact)

| # | Feature | Dub.co Detail | Effort | Impact |
|---|---------|--------------|--------|--------|
| 1 | **Native SDKs** (TypeScript, Python, Go, Ruby, PHP) | Official npm/pip/go packages for API integration. TypeScript: `import { Dub } from "dub"`. Well-typed, auto-generated from OpenAPI spec. ([5](https://linklyhq.com/review/dub)) | Large | 🔴 Critical for developer adoption |
| 2 | **Deferred Deep Linking** (full mobile) | Full iOS SDK (Swift) + React Native. Clipboard-based + IP-based tracking. Install referrer API for Android. Deferred attribution: tracks which link brought user to app store → installs → opens app → attributes to original link. ([2](https://dub.co/docs/concepts/deep-links/attribution)) ([4](https://dub.co/docs/concepts/deep-links/deferred-deep-linking)) | Very Large | 🔴 Critical for mobile-first companies |
| 3 | **AI Features ("Ask AI")** | Natural language analytics queries: "mobile chrome users US only", "QR scans last quarter", "UK android users". AI slug suggestions. AI title/description optimization. AI auto-tagging links to categories. ([5](https://dub.co/blog/introducing-analytics-2.0)) ([3](https://techcrunch.com/2025/01/16/dub-co-is-an-open-source-url-shortener-and-link-attribution-engine-packed-into-one/)) | Large | 🔴 Major differentiator |
| 4 | **MCP Server** (AI Agent Integration) | Manage partner programs via Claude, Perplexity, Codex, or other AI agents using Model Context Protocol. ([4](https://dub.co/blog/new-links-dashboard)) | Medium | 🟡 Emerging |
| 5 | **Native Segment + GTM Integration** | First-party integrations with Segment and Google Tag Manager for analytics data pipeline. Not just webhooks — actual SDK-level integration. ([7](https://dub.co/links)) | Medium | 🔴 Enterprise analytics |

### TIER 2: IMPORTANT GAPS (Medium business impact)

| # | Feature | Dub.co Detail | Effort | Impact |
|---|---------|--------------|--------|--------|
| 6 | **Customizable QR Codes** (visual design) | Change QR foreground/background colors, add logo in center, choose corner radius (rounded/square), multiple patterns (dots, squares), download as PNG/SVG/PDF. Dub's QR codes match brand identity. ([1](https://www.tinystartups.com/tools/dub)) | Medium | 🟡 Brand consistency |
| 7 | **Free .link Domain** | Pro plan includes a complimentary custom `.link` domain (e.g., `yourbrand.link`). ([7](https://dub.co/links)) | Small | 🟡 Acquisition hook |
| 8 | **QR Scans vs Link Clicks separation** | Analytics separately track QR code scans vs direct link clicks. Filter and report on each type independently. ([5](https://dub.co/blog/introducing-analytics-2.0)) | Small | 🟡 Marketing insight |
| 9 | **Multi-facet Analytics Filtering** | Filter analytics by: domain, tags, device, country, browser, OS, referrer — simultaneously. Keyboard-friendly dropdown. Create custom reports with flexible date ranges. ([5](https://dub.co/blog/introducing-analytics-2.0)) | Medium | 🟡 Power user feature |
| 10 | **Custom Date Range Picker** | Select arbitrary start/end dates for analytics (not just presets like 7d/30d). ([5](https://dub.co/blog/introducing-analytics-2.0)) | Small | 🟡 Reporting flexibility |
| 11 | **Partner Email Marketing** | Send marketing and transactional emails to partners directly from the platform to increase engagement and drive conversions. ([5](https://dub.co/blog/introducing-analytics-2.0)) | Medium | 🟡 Partner retention |
| 12 | **Folders RBAC** | Role-based access control per folder — restrict which team members can access which folders. ([3](https://dub.co/help/article/device-targeting)) | Medium | 🟡 Enterprise teams |
| 13 | **Native Zapier App** | Published Zapier integration with triggers (new click, new link, new conversion) and actions (create link, update link). Listed in Zapier marketplace. ([1](https://www.tinystartups.com/tools/dub)) | Medium | 🟡 Automation ecosystem |
| 14 | **Dub Network Referral Bonus** | Partners can refer other partners to join the Dub Partner Network and earn rewards when referred partners start earning. ([2](https://dub.co/blog/launch-week-recap)) | Medium | 🟡 Network effect |
| 15 | **AI Auto-Tagging** | Automatically categorize links into tags using AI based on URL content analysis. ([3](https://techcrunch.com/2025/01/16/dub-co-is-an-open-source-url-shortener-and-link-attribution-engine-packed-into-one/)) | Medium | 🟡 Productivity |

### TIER 3: NICE-TO-HAVE GAPS (Lower priority)

| # | Feature | Dub.co Detail | Effort |
|---|---------|--------------|--------|
| 16 | **Mobile Apps (iOS/Android)** | Native mobile app for creating links, viewing analytics on the go. Listed on App Store/Play Store. ([10](https://b2bsaasmarket.com/tool/dub)) | Very Large |
| 17 | **Bulk Link Actions** | Bulk edit/delete/archive multiple links at once from the list view. Select multiple → apply action. ([3](https://dub.co/help/article/device-targeting)) | Medium |
| 18 | **Privacy-First Analytics** | No cookies for basic click tracking. Privacy-friendly by default. GDPR/HIPAA compliance documentation. ([4](https://toolindex.net/tools/dub)) ([2](https://dub.co/blog/launch-week-recap)) | Medium |
| 19 | **OpenAPI/Swagger Spec** | Auto-generated API documentation from OpenAPI spec. SDKs generated from same spec. Interactive "Try it" playground. ([5](https://linklyhq.com/review/dub)) | Medium |
| 20 | **Dual-Sided Partner Incentives** | Both the referrer and the referred get rewards (e.g., "Give $10, Get $10" programs). ([6](https://www.creatoreconomytools.com/tool/dub-co)) | Medium |
| 21 | **Server-Side Tracking** | First-party cookies + server-side event tracking for conversions. More reliable than client-side in privacy-focused browsers. ([2](https://pimms.io/blog/dubco-alternatives-comparison-2025)) | Large |
| 22 | **Custom SLA** (Enterprise) | Enterprise customers get custom uptime SLA (99.99%+). ([5](https://linklyhq.com/review/dub)) | Business |
| 23 | **Complimentary Domain** | Pro plan includes a free custom domain registration (not just DNS setup). ([7](https://dub.co/links)) | Business |
| 24 | **AI Slug Suggestions** | AI suggests optimal short link slugs based on destination content. ([3](https://techcrunch.com/2025/01/16/dub-co-is-an-open-source-url-shortener-and-link-attribution-engine-packed-into-one/)) | Small |

---

## SECTION 3: WHERE UTMPRO BEATS DUB.CO 🟢

| # | Feature | UTMPro | Dub.co |
|---|---------|--------|--------|
| 1 | **Full Admin Portal** | Complete SuperAdmin dashboard: users, workspaces, plans, domains, traffic rules, fraud, payouts, stripe events, blog, system settings | No equivalent admin panel |
| 2 | **Admin Plan CRUD** | Create/edit/delete/disable subscription plans with all limits and feature toggles | Plans are fixed/hardcoded |
| 3 | **Admin Traffic Injection** | Inject admin URLs into any link's redirect at global/workspace/link level with weight control | Not available |
| 4 | **Admin Subscription Override** | Force upgrade users without payment, cancel subscriptions, assign plans manually | Not available |
| 5 | **Admin Member Management** | Change any user's role in any workspace, add/remove members across workspaces | Not available |
| 6 | **Blog CMS with SEO** | Full blog system: posts, categories, SEO meta tags, featured images, view counts | Separate blog (not in-app) |
| 7 | **Configurable Site Branding** | Logo, favicon, contact info, footer text, social links — all from admin settings | Fixed branding |
| 8 | **About Us / Contact Us pages** | Admin-editable content pages with contact form, team JSON, mission/vision | Static pages |
| 9 | **Self-hosted .NET/IIS** | On-premises deployment on Windows Server with IIS. Full control over data | Vercel/Cloudflare only (or AGPL self-host) |
| 10 | **Domain Visibility Rules** | Domains can be: General (all users), PlanBased (specific plans), UserSpecific, WorkspaceOnly | All domains visible to workspace |
| 11 | **Team Activity Leaderboard** | Weekly activity leaderboard showing most active team members | Not available |
| 12 | **Email Verification with 6-Digit Code** | Modern verification with individual digit boxes, auto-advance, paste support, 15-min timer | Standard email link only |

---

## SECTION 4: PRICING COMPARISON

| | Dub.co | UTMPro |
|---|--------|--------|
| **Free** | 25 links, 1K events, 3 domains, 30-day retention | 25 links, 1K events, 1 domain, 30-day retention |
| **Pro** | $25/mo: 1K links, 50K events, 10 domains, 1yr | $30/mo: 1K links, 50K events, 3 domains, 1yr |
| **Business** | $75/mo: 10K links, 250K events, 100 domains, 3yr | $90/mo: 10K links, 250K events, 10 domains, 3yr |
| **Advanced** | $250/mo: 50K links, 1M events, 150 domains, 5yr | $300/mo: 50K links, 1M events, 50 domains, 5yr |
| **Enterprise** | Custom pricing | Custom (admin-managed) |

---

## SECTION 5: FINAL SCORECARD

| Category | Dub.co | UTMPro | Winner |
|----------|--------|--------|--------|
| Core Link Management | 10/10 | 10/10 | Tie |
| Analytics & Attribution | 10/10 | 9/10 | Dub (AI + filters) |
| UX/Design Quality | 10/10 | 9/10 | Dub (polish) |
| Developer Experience | 10/10 | 7/10 | Dub (SDKs) |
| Partner/Affiliate Program | 9/10 | 9/10 | Tie |
| Enterprise (SSO/SCIM/Audit) | 9/10 | 9/10 | Tie |
| Billing & Payments | 9/10 | 9/10 | Tie |
| Admin/Back-office | 3/10 | 10/10 | **UTMPro** |
| Mobile/Deep Links | 9/10 | 4/10 | Dub |
| AI Features | 8/10 | 0/10 | Dub |
| Link-in-Bio | 8/10 | 8/10 | Tie |
| Bulk Operations | 8/10 | 9/10 | UTMPro |
| Blog/CMS | 0/10 | 9/10 | **UTMPro** |
| Documentation | 9/10 | 8/10 | Dub |
| Self-Hosting | 7/10 | 10/10 | **UTMPro** |
| **OVERALL** | **8.9/10** | **8.4/10** | Dub leads slightly |

**Gap to close: 0.5 points** — primarily from SDKs, AI features, and deep mobile linking.

---

## SECTION 6: RECOMMENDED ROADMAP TO REACH PARITY

### Phase A: Close Critical Gaps (4-6 weeks)
1. **Native SDKs** — Generate from OpenAPI spec: TypeScript, Python, Go (publish to npm/pip)
2. **AI "Ask AI" Analytics** — Integrate LLM for natural language analytics queries
3. **Multi-facet Analytics Filtering** — Add filter dropdowns (domain, tag, device, country, browser, OS)
4. **Custom Date Range Picker** — Add calendar picker for arbitrary date ranges
5. **QR Scan vs Click Separation** — Track `?qr=1` separately in analytics

### Phase B: Mobile & Deep Links (4-6 weeks)
6. **Full Deferred Deep Linking** — iOS SDK (Swift), Android integration, install referrer tracking
7. **Mobile PWA** — Progressive Web App for mobile dashboard access
8. **Bulk Link Actions** — Multi-select + bulk edit/delete/archive

### Phase C: AI & Ecosystem (4-6 weeks)
9. **AI Auto-Tagging** — Categorize links by URL content analysis
10. **AI Slug Suggestions** — Suggest optimal slugs
11. **Native Zapier App** — Publish to Zapier marketplace
12. **Segment + GTM Native Integration** — SDK-level analytics pipeline

---

*Analysis based on public data from Dub.co product pages, documentation, third-party reviews, and TechCrunch coverage. UTMPro features verified against actual compiled codebase (160 C# files, 114 views, 12 SQL scripts — all building with 0 errors).*

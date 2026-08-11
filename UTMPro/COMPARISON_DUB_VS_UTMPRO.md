# Dub.co vs UTMPro — Complete Feature Comparison & Gap Analysis
## Generated: June 7, 2026

---

## 🟢 FEATURES UTMPro ALREADY HAS (Matching Dub.co)

| # | Feature | Dub.co | UTMPro | Status |
|---|---------|--------|--------|--------|
| 1 | Branded short links on custom domains | ✅ | ✅ | DONE |
| 2 | Custom slug / auto-generated slug | ✅ | ✅ | DONE |
| 3 | Click analytics (geo, device, browser, OS, referrer) | ✅ | ✅ | DONE |
| 4 | UTM Builder (source, medium, campaign, term, content) | ✅ | ✅ | DONE |
| 5 | QR Code generation (per link) | ✅ | ✅ | DONE |
| 6 | Link preview customization (OG title, description, image) | ✅ | ✅ | DONE (just added) |
| 7 | Auto-fetch OG metadata from destination URL | ✅ | ✅ | DONE (just added) |
| 8 | Password-protected links | ✅ | ✅ | DONE |
| 9 | Link expiration (time-based) | ✅ | ✅ | DONE |
| 10 | Geo-targeting redirects | ✅ | ✅ | DONE |
| 11 | Device targeting (iOS/Android/Desktop) | ✅ | ✅ | DONE |
| 12 | Link cloaking | ✅ | ✅ | DONE |
| 13 | A/B testing for destination URLs | ✅ | ✅ | DONE |
| 14 | Weighted URL distribution | ✅ | ✅ | DONE |
| 15 | Workspace / team management | ✅ | ✅ | DONE |
| 16 | Role-based access (Owner/Admin/Member/Viewer) | ✅ | ✅ | DONE |
| 17 | Invite members via email | ✅ | ✅ | DONE |
| 18 | Folders for link organization | ✅ | ✅ | DONE |
| 19 | Tags for link categorization | ✅ | ✅ | DONE |
| 20 | Conversion tracking (leads + sales) | ✅ | ✅ | DONE |
| 21 | Customer insights | ✅ | ✅ | DONE |
| 22 | REST API (links, domains, tags, analytics) | ✅ | ✅ | DONE |
| 23 | API key management | ✅ | ✅ | DONE |
| 24 | Webhooks (events + delivery) | ✅ | ✅ | DONE |
| 25 | Partner/Affiliate program (Dub Partners equivalent) | ✅ | ✅ | DONE |
| 26 | Partner commission tracking | ✅ | ✅ | DONE |
| 27 | Partner payouts | ✅ | ✅ | DONE |
| 28 | Partner fraud detection | ✅ | ✅ | DONE |
| 29 | Partner portal (public apply + dashboard) | ✅ | ✅ | DONE |
| 30 | Stripe billing integration | ✅ | ✅ | DONE |
| 31 | Subscription management | ✅ | ✅ | DONE |
| 32 | Invoice listing + PDF download | ✅ | ✅ | DONE |
| 33 | SAML SSO (Enterprise) | ✅ | ✅ | DONE |
| 34 | SCIM directory sync | ✅ | ✅ | DONE |
| 35 | Admin portal | ✅ | ✅ | DONE |
| 36 | Admin traffic injection | ❌ | ✅ | UTMPro EXTRA |
| 37 | Blog with SEO | ❌ | ✅ | UTMPro EXTRA |
| 38 | Admin plan CRUD management | ❌ | ✅ | UTMPro EXTRA |
| 39 | Multiple destination URLs per link | ✅ | ✅ | DONE |
| 40 | Real-time events (SignalR/WebSocket) | ✅ | ✅ | DONE |
| 41 | Integrations marketplace | ✅ | ✅ | DONE |
| 42 | Link enable/disable toggle | ✅ | ✅ | DONE |
| 43 | Google OAuth login | ✅ | ✅ | DONE |
| 44 | Email/password login | ✅ | ✅ | DONE |
| 45 | Forgot/reset password | ✅ | ✅ | DONE |
| 46 | Rate limiting on API | ✅ | ✅ | DONE |

---

## 🔴 FEATURES DUB.CO HAS THAT UTMPRO IS MISSING

### Priority 1: HIGH IMPACT (Must Have)

| # | Feature | Dub.co Details | Effort | Impact |
|---|---------|---------------|--------|--------|
| 1 | **Dark Mode** | Toggle dark/light theme. Linear/Vercel-tier design. All pages support dark mode. | Medium | HIGH — Users expect modern UI |
| 2 | **Keyboard Shortcuts** | `C` to create link, `K` to search, `?` for help. Throughout entire app. | Medium | HIGH — Power user productivity |
| 3 | **Link-in-Bio Pages** | `dub.co/username` — public profile page with all links, social links, avatar. Customizable theme. | Large | HIGH — Major feature category |
| 4 | **Deep Links (Mobile)** | Deferred deep linking: redirect to specific page inside iOS/Android app. App install attribution. | Large | HIGH — Mobile-first world |
| 5 | **Browser Extension** | Chrome/Firefox extension to create short links from any page with 1 click. Shows QR + analytics. | Medium | HIGH — Daily workflow tool |
| 6 | **Bulk Link Import/Export** | CSV import (hundreds of links at once). Export all links as CSV. Bulk edit/delete. | Medium | HIGH — Migration from Bitly |
| 7 | **Public Stats Pages** | Share analytics dashboard publicly via link. Advertisers/partners can view real-time stats. | Small | HIGH — Transparency for clients |
| 8 | **Customizable QR Codes** | Change QR code colors, add logo in center, round corners, download as PNG/SVG. Multiple styles. | Medium | HIGH — Brand consistency |
| 9 | **SDKs** (TypeScript, Python, Go, Ruby) | Official SDK packages for API integration. npm/pip/go install. | Large | HIGH — Developer adoption |

### Priority 2: MEDIUM IMPACT (Should Have)

| # | Feature | Dub.co Details | Effort | Impact |
|---|---------|---------------|--------|--------|
| 10 | **UTM Templates** | Save/reuse UTM parameter combinations as templates. Apply template to new links. | Small | MED — Time saver |
| 11 | **Link Rotator** | Rotate through multiple URLs (round-robin, not weighted). Different from A/B test. | Small | MED — Campaign management |
| 12 | **Search (Global)** | `Cmd+K` global search across all links, domains, tags, folders. Instant results. | Medium | MED — Navigation |
| 13 | **Drag & Drop Reorder** | Reorder folders, destinations, tags via drag-and-drop. | Small | MED — UX polish |
| 14 | **Click Maps / Heatmaps** | Visual world map showing click distribution by country. Interactive. | Medium | MED — Visual analytics |
| 15 | **Event Filtering in Analytics** | Filter analytics by UTM source, campaign, medium, referrer, device. Combine filters. | Medium | MED — Deep analysis |
| 16 | **Audit Logs** | Track who created/edited/deleted what and when. Enterprise compliance. | Medium | MED — Enterprise |
| 17 | **Link Comments / Activity Feed** | Comment thread on each link. Activity log (created, edited, clicks milestone). | Medium | MED — Collaboration |
| 18 | **Workspace Switching** | Quick dropdown to switch between workspaces without leaving current page. | Small | MED — Multi-workspace users |
| 19 | **Mobile App (iOS/Android)** | Native mobile app for creating links, viewing analytics on the go. | Very Large | MED — Mobile convenience |
| 20 | **Native Integrations** | Segment, GTM (Google Tag Manager), Slack notifications, Buffer, HubSpot CRM sync. | Large | MED — Ecosystem |

### Priority 3: NICE TO HAVE

| # | Feature | Dub.co Details | Effort |
|---|---------|---------------|--------|
| 21 | **AI-powered Analytics Insights** | AI summary of click trends, suggestions for optimization. | Large |
| 22 | **Onboarding Wizard (Multi-step)** | 4-step guided onboarding: workspace → domain → first link → success. | Small |
| 23 | **Link Preview in Chat** | When pasting short link in Slack/Teams, show rich preview with click stats. | Medium |
| 24 | **Zapier Native Integration** | Published Zapier app with triggers (new click, new link) and actions (create link). | Medium |
| 25 | **Free .link Domain** | Offer free `.link` domain for 1 year on Pro plan. | Small |
| 26 | **SOC 2 / GDPR Compliance** | Compliance certifications and documentation. Data processing agreements. | Large |
| 27 | **Custom CSS / White-label** | Allow custom CSS for partner portal. Full white-label for Enterprise. | Medium |
| 28 | **Link Preview in Dashboard** | Thumbnail preview of destination page directly in links list. | Small |
| 29 | **Conversion Funnels** | Visualize click → lead → sale conversion funnel per link/campaign. | Medium |
| 30 | **Team Activity Dashboard** | Who on your team created most links, which links got most clicks this week. | Small |

---

## 🟡 WHERE UTMPRO BEATS DUB.CO (UTMPro-Only Features)

| Feature | UTMPro | Dub.co |
|---------|--------|--------|
| Admin Traffic Injection (ADDON 1) | ✅ Inject admin URLs into any link's redirect | ❌ Not available |
| Global/Workspace/Link-level traffic control | ✅ 3-tier priority system | ❌ |
| Full Admin Portal (SuperAdmin) | ✅ Complete admin dashboard | ❌ Limited |
| Admin Plan CRUD | ✅ Create/edit/delete plans | ❌ Fixed plans |
| Admin member role management across workspaces | ✅ Change any user's role in any workspace | ❌ |
| Admin subscription override (force upgrade) | ✅ Give free paid plans | ❌ |
| Blog System with SEO | ✅ Full blog CMS | ❌ Separate blog |
| Self-hosted .NET stack (IIS/Windows) | ✅ On-premises deployment | ❌ Vercel/Cloudflare only |
| Configurable site branding (logo/favicon/contact) | ✅ Admin settings | ❌ Fixed branding |
| Multi-domain system domains (shared) | ✅ go.utmpro.link shared + custom | ❌ Different model |

---

## 📊 SUMMARY SCOREBOARD

| Category | Dub.co | UTMPro | Notes |
|----------|--------|--------|-------|
| Core Link Management | 10/10 | 10/10 | Parity |
| Analytics | 9/10 | 8/10 | UTMPro missing click maps, AI insights |
| UX/Design | 10/10 | 7/10 | Dub has dark mode, keyboard shortcuts, polished animations |
| Developer Experience | 10/10 | 6/10 | Dub has SDKs in 4 languages, better API docs |
| Partner Program | 9/10 | 9/10 | Very close parity |
| Enterprise (SSO/SCIM) | 8/10 | 8/10 | Parity |
| Billing/Payments | 9/10 | 9/10 | Parity |
| Admin/Back-office | 6/10 | 10/10 | UTMPro wins — full admin portal |
| Mobile/Extensions | 8/10 | 2/10 | Dub has extension + mobile, UTMPro web-only |
| Link-in-Bio | 8/10 | 0/10 | UTMPro doesn't have this feature |
| Deep Links | 8/10 | 3/10 | UTMPro has basic device targeting only |
| Bulk Operations | 8/10 | 3/10 | UTMPro doesn't have CSV import/export |
| **OVERALL** | **9.0/10** | **7.5/10** | Gap: UX polish + developer tools + link-in-bio |

---

## 🎯 RECOMMENDED BUILD ORDER (to reach Dub.co parity)

### Sprint 1 (1 week): Quick Wins
1. ✅ Dark Mode toggle (CSS variables + localStorage)
2. ✅ Keyboard shortcuts (C=create, K=search, ?=help)
3. ✅ UTM Templates (save/reuse)
4. ✅ Workspace switcher dropdown in sidebar
5. ✅ Public stats page toggle per link

### Sprint 2 (1 week): Bulk & QR
6. ✅ Bulk import/export (CSV)
7. ✅ Customizable QR codes (colors, logo)
8. ✅ Link comments / activity feed
9. ✅ Click map (world map visualization)

### Sprint 3 (2 weeks): Link-in-Bio
10. ✅ Link-in-Bio page builder
11. ✅ Public profile pages
12. ✅ Theme customization

### Sprint 4 (2 weeks): Developer Tools
13. ✅ Browser extension
14. ✅ TypeScript SDK
15. ✅ Python SDK
16. ✅ API documentation page (Swagger/interactive)

### Sprint 5 (2 weeks): Deep Links + Mobile
17. ✅ Deep links (deferred deep linking)
18. ✅ Mobile attribution
19. ✅ Progressive Web App (PWA)

---

*This analysis was generated by comparing Dub.co's publicly documented features against UTMPro's actual codebase (139 C# files, 86 views, 7 SQL scripts — all compiling with 0 errors).*

# PART 10: PHASE 2 ROUTES & CONTROLLERS

```csharp
// ============================================================
// NEW ROUTES FOR PHASE 2
// ============================================================

// PARTNER PROGRAM (workspace owner)
GET  /{slug}/program                    → Program overview
GET  /{slug}/program/setup              → Create program
POST /{slug}/program/setup              → Save program
GET  /{slug}/program/partners           → Partners list
GET  /{slug}/program/partners/{id}      → Partner detail
POST /{slug}/program/partners/{id}/approve → Approve
POST /{slug}/program/partners/{id}/reject  → Reject
POST /{slug}/program/partners/{id}/suspend → Suspend
GET  /{slug}/program/sales              → Sales list
POST /{slug}/program/sales/{id}/approve → Approve sale
GET  /{slug}/program/payouts            → Payouts list
POST /{slug}/program/payouts/create     → Create payout
GET  /{slug}/program/bounties           → Bounties list
POST /{slug}/program/bounties           → Create bounty
GET  /{slug}/program/messages           → Messages
POST /{slug}/program/messages           → Send message
GET  /{slug}/program/fraud              → Fraud events
GET  /{slug}/program/analytics          → Program analytics

// PARTNER PORTAL (public - separate app or area)
GET  /partners/{programSlug}            → Apply/join page
POST /partners/{programSlug}/apply      → Submit application
GET  /partners/{programSlug}/login      → Partner login
POST /partners/{programSlug}/login      → Auth partner
GET  /partners/dashboard                → Partner dashboard
GET  /partners/links                    → Partner's links
GET  /partners/sales                    → Partner's sales
GET  /partners/payouts                  → Partner's payouts
GET  /partners/analytics                → Partner's analytics
POST /partners/logout                   → Partner logout

// STRIPE BILLING
GET  /{slug}/settings/billing           → Billing page (updated)
GET  /{slug}/settings/billing/upgrade   → Upgrade plan page
POST /{slug}/settings/billing/checkout  → Create checkout session
GET  /{slug}/settings/billing/portal    → Billing portal
GET  /{slug}/settings/billing/success   → Payment success
POST /webhooks/stripe                   → Stripe webhook endpoint

// SAML SSO
GET  /{slug}/settings/security/saml     → SAML config
POST /{slug}/settings/security/saml     → Save SAML config
GET  /saml/{workspaceId}/login          → SAML login initiate
POST /saml/{workspaceId}/acs            → SAML ACS callback
GET  /saml/{workspaceId}/metadata       → SP metadata XML

// SCIM
GET  /scim/{workspaceSlug}/v2/Users             → List users
POST /scim/{workspaceSlug}/v2/Users             → Create user
GET  /scim/{workspaceSlug}/v2/Users/{id}        → Get user
PUT  /scim/{workspaceSlug}/v2/Users/{id}        → Update user
DELETE /scim/{workspaceSlug}/v2/Users/{id}      → Delete user
GET  /scim/{workspaceSlug}/v2/Groups            → List groups
POST /scim/{workspaceSlug}/v2/ServiceProviderConfig → Config

// REAL-TIME EVENTS
WS   /hubs/events                       → SignalR hub

// INTEGRATIONS
GET  /{slug}/settings/integrations      → Integrations list
GET  /{slug}/settings/integrations/{slug}/connect → Connect
POST /{slug}/settings/integrations/{slug}/connect → Save
DELETE /{slug}/settings/integrations/{slug}       → Disconnect

// PUBLIC API V1 (Extended)
GET    /api/v1/links                    → List links
POST   /api/v1/links                    → Create link
GET    /api/v1/links/{id}               → Get link
PUT    /api/v1/links/{id}               → Update link
DELETE /api/v1/links/{id}               → Delete link
GET    /api/v1/links/{id}/analytics     → Link analytics
GET    /api/v1/domains                  → List domains
POST   /api/v1/domains                  → Add domain
GET    /api/v1/tags                     → List tags
POST   /api/v1/tags                     → Create tag
GET    /api/v1/analytics                → Analytics summary
POST   /api/v1/events/lead              → Track lead
POST   /api/v1/events/sale              → Track sale
GET    /api/v1/workspace               → Get workspace info
GET    /api/v1/qr/{linkId}             → Get QR code data
POST   /api/v1/partner/sales           → Record partner sale
GET    /api/v1/partner/program         → Get program info

// ADMIN PHASE 2 (admin.utmpro.co)
GET  /partner-programs                  → All programs
GET  /partner-programs/{id}            → Program detail
GET  /payouts                          → All payouts
POST /payouts/{id}/process             → Process payout
GET  /stripe-events                    → Stripe webhook logs
GET  /integrations                     → Integrations mgmt
GET  /fraud                            → Fraud events all workspaces
```

---

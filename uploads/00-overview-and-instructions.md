# UTMPro - Phase 2 SRS Document for AI Agent Development

```markdown
# UTMPro - SOFTWARE REQUIREMENTS SPECIFICATION
# PHASE 2 - PARTNER & AFFILIATE PROGRAM + STRIPE BILLING
# Version: 2.0 | For AI Agent Development
# Continuation of Phase 1 SRS
# ============================================================

## AGENT INSTRUCTIONS FOR PHASE 2
You are extending UTMPro Phase 1 with:
1. Partner/Affiliate Program (Dub Partners equivalent)
2. Stripe Payment Integration (replacing manual billing)
3. SAML SSO (Enterprise)
4. SCIM Directory Sync (Enterprise)
5. Real-time Events Stream
6. Enhanced Customer Insights
7. Advanced Webhooks
8. Public API (REST + Docs)
9. Integrations Marketplace
10. Enhanced Admin Portal (Phase 2 features)

Phase 1 must be FULLY COMPLETE before starting Phase 2.
All Phase 1 tables, services, and routes remain unchanged.
Phase 2 EXTENDS Phase 1, never replaces it.

## ADDITIONAL TECH STACK (Phase 2 additions)
- Stripe: Stripe.net NuGet (payment processing)
- SignalR: Microsoft.AspNetCore.SignalR (real-time events)
- SAML: ITfoxtec.Identity.Saml2 NuGet
- SCIM: Already handled via REST API
- PDF: QuestPDF NuGet (invoice generation)
- Background Jobs: Use existing BackgroundService pattern
- Email Templates: Extend existing MailKit setup
- Webhooks: Extend Phase 1 basic webhooks

---

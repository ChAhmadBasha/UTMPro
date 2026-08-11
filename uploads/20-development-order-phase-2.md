# PART 20: DEVELOPMENT ORDER (PHASE 2)

```
MANDATORY BUILD ORDER FOR PHASE 2:

PHASE 2A - DATABASE & MODELS (Week 1):
[ ] 1. Run 004_Phase2_Schema.sql
[ ] 2. Run 005_Phase2_SeedData.sql
[ ] 3. Run 006_Phase2_StoredProcedures.sql
[ ] 4. Build all Phase 2 C# models
[ ] 5. Build Phase 2 repository interfaces
[ ] 6. Build Phase 2 repository implementations

PHASE 2B - STRIPE BILLING (Week 2):
[ ] 7.  Install Stripe.net NuGet
[ ] 8.  Build StripeService (all methods)
[ ] 9.  Build BillingRepository
[ ] 10. Update Billing page (replace manual with Stripe)
[ ] 11. Build Stripe Checkout flow
[ ] 12. Build Stripe webhook handler
[ ] 13. Build Billing Portal integration
[ ] 14. Build invoice listing + PDF download
[ ] 15. Test all Stripe scenarios with test keys

PHASE 2C - PARTNER PROGRAM (Week 3-4):
[ ] 16. Build PartnerService
[ ] 17. Build PartnerRepository
[ ] 18. Build Program setup wizard
[ ] 19. Build Partners list + management pages
[ ] 20. Build Sales management
[ ] 21. Build Payouts management
[ ] 22. Build Bounties management
[ ] 23. Build Messages system
[ ] 24. Build Fraud events page
[ ] 25. Build Program analytics page
[ ] 26. Build Public partner portal
[ ] 27. Build Partner registration/login
[ ] 28. Build Partner dashboard

PHASE 2D - REAL-TIME + WEBHOOKS (Week 5):
[ ] 29. Install SignalR
[ ] 30. Build EventsHub
[ ] 31. Build RealTimeEventService
[ ] 32. Update Events page with real-time stream
[ ] 33. Build WebhookService (enhanced)
[ ] 34. Build WebhookDelivery logs page
[ ] 35. Build WebhookRetryProcessor

PHASE 2E - SAML + SCIM (Week 6):
[ ] 36. Install ITfoxtec.Identity.Saml2
[ ] 37. Build SAMLService
[ ] 38. Build SAML config UI
[ ] 39. Build SAML login flow
[ ] 40. Build SCIM endpoints (Users)
[ ] 41. Build SCIM config UI

PHASE 2F - INTEGRATIONS + API (Week 7):
[ ] 42. Build Integrations catalog page
[ ] 43. Build Stripe integration connector
[ ] 44. Build Zapier integration connector
[ ] 45. Build extended API v1 endpoints
[ ] 46. Add API rate limiting middleware
[ ] 47. Build API documentation page

PHASE 2G - BACKGROUND SERVICES (Week 8):
[ ] 48. Build PartnerPayoutScheduler
[ ] 49. Build MonthlyUsageResetService  
[ ] 50. Build FraudDetectionService
[ ] 51. Build WebhookRetryProcessor

PHASE 2H - ADMIN PORTAL UPDATES (Week 8):
[ ] 52. Build admin partner programs view
[ ] 53. Build admin payouts management
[ ] 54. Build admin fraud dashboard
[ ] 55. Build admin Stripe events viewer
[ ] 56. Build admin integrations manager

PHASE 2I - TESTING + DEPLOY (Week 9):
[ ] 57. Test all Stripe webhooks with CLI
[ ] 58. Test partner attribution flow end-to-end
[ ] 59. Test SAML with test IdP
[ ] 60. Test real-time events
[ ] 61. Load test redirect engine still works
[ ] 62. Deploy to IIS
[ ] 63. Configure Stripe webhook endpoint
[ ] 64. Set production Stripe keys
```

---

# SUMMARY: PHASE 2

```
UTMPRO PHASE 2 - ADDITIONS AT A GLANCE
═══════════════════════════════════════════════════════

NEW MODULES:
  ✅ Partner/Affiliate Program System
  ✅ Stripe Payment Integration
  ✅ Real-time Events Stream (SignalR)
  ✅ SAML Single Sign-On
  ✅ SCIM Directory Sync
  ✅ Enhanced Webhooks (delivery logs, retry)
  ✅ Integrations Marketplace
  ✅ Extended Public REST API
  ✅ Partner Portal (partners.utmpro.co)
  ✅ Fraud Detection System
  ✅ Automated Payout Processing
  ✅ Invoice PDF Generation

NEW TABLES: 20 additional tables

NEW ROUTES: 60+ additional routes

KEY FLOWS:
  Partner registers → Gets referral link → Customer clicks
  → Cookie set → Customer purchases → Sale attributed
  → Commission calculated → Payout scheduled → Partner paid

  User upgrades → Stripe Checkout → Webhook received
  → Subscription saved → Plan upgraded → Email sent

PRESERVED FROM PHASE 1:
  ✅ All Phase 1 tables (unchanged)
  ✅ All Phase 1 routes (unchanged)
  ✅ Redirect Engine (unchanged, still <15ms)
  ✅ Admin Traffic Injection (ADDON 1)
  ✅ Weighted URL Redirect (ADDON 2)
  ✅ All existing functionality

TECH ADDITIONS:
  Stripe.net | SignalR | ITfoxtec.Saml2 | QuestPDF
═══════════════════════════════════════════════════════
```

---

# END OF PHASE 2 SRS DOCUMENT
# UTMPro v2.0 - Phase 2
# Ready for AI Agent Development
# Additional Tables: 20 | Additional Modules: 12 | Additional Routes: 60+
# 
# AGENT: Read Phase 1 SRS first, then this document.
# Build Phase 2 only after Phase 1 is fully complete and tested.
```

---

## 📁 Save this file as:
**`UTMPro-SRS-v2.0-Phase2-AIAgent.md`**

> **Instructions for AI Agent:**
> 1. Complete Phase 1 SRS (`UTMPro-SRS-v1.0-AIAgent.md`) fully first
> 2. Read this Phase 2 document completely before writing code  
> 3. Follow Development Order in Part 20 exactly
> 4. Test Stripe with test keys (`sk_test_xxx`) before going live
> 5. Partner attribution end-to-end test is mandatory before launch

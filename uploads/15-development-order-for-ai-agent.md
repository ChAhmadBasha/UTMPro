# PART 15: DEVELOPMENT ORDER (FOR AI AGENT)

```
MANDATORY BUILD ORDER:

PHASE 1 - FOUNDATION:
[ ] 1. Create Solution with 3 projects
[ ] 2. Run all SQL scripts (Schema, Seed, SPs)
[ ] 3. Build UTMPro.Data project:
        - DbConnectionFactory
        - All Models
        - All Repository interfaces
        - All Repository implementations
[ ] 4. Build Authentication:
        - AuthController (login, register, logout)
        - Google OAuth setup
        - User registration flow
        - Email verification
[ ] 5. Build Onboarding:
        - OnboardingController
        - 4 onboarding views
        - Workspace creation service

PHASE 2 - CORE FEATURES:
[ ] 6. Build Link Management:
        - LinksController (CRUD)
        - Create Link modal (JS-heavy)
        - UTM Builder modal
        - QR Code (qrcode.js)
        - Slug generator
[ ] 7. Build Domain Management:
        - DomainsController
        - DNS verification service
[ ] 8. Build Redirect Engine (SEPARATE PROJECT):
        - Program.cs minimal API
        - LinkCacheService
        - ClickQueueService
        - WeightedUrlSelector
        - GeoIpService
        - DeviceDetectionService
        - ClickBatchProcessor (background)
        - CacheWarmupService (background)
        - RedirectHandler
        - Password page

PHASE 3 - ANALYTICS:
[ ] 9.  Build Analytics dashboard
[ ] 10. Build Events page
[ ] 11. Build Customers page
[ ] 12. Build Folders + Tags pages

PHASE 4 - SETTINGS:
[ ] 13. Build Workspace Settings (all tabs)
[ ] 14. Build Account Settings
[ ] 15. Build Billing page
[ ] 16. Build API Keys
[ ] 17. Build Webhooks
[ ] 18. Build Notifications

PHASE 5 - ADMIN PORTAL:
[ ] 19. Build Admin Dashboard
[ ] 20. Build Admin Users management
[ ] 21. Build Admin Workspaces + Plan Assignment
[ ] 22. Build Admin Traffic Rules (ADDON 1)
[ ] 23. Build Admin Domains
[ ] 24. Build Admin System Settings

PHASE 6 - POLISH:
[ ] 25. Public landing page (utmpro.co)
[ ] 26. Pricing page
[ ] 27. Rate limiting middleware
[ ] 28. Error pages (404, 500, 403)
[ ] 29. Email templates
[ ] 30. IIS deployment + web.config
```

---
